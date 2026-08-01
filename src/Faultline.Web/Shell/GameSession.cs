using System;
using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Web.Shell;

/// <summary>
/// Holds the current <see cref="GameState"/> and the action the player is aiming.
/// </summary>
/// <remarks>
/// This type answers no rules questions of its own. Legality comes from the
/// <see cref="StepResult.LegalNext"/> list Core hands back, and every preview comes from a Core
/// query — <see cref="Displacement.Preview"/>, <see cref="Abilities.PreviewCharge"/> and friends.
/// CLAUDE.md: duplicated rule logic in the shell is a bug.
/// </remarks>
public sealed class GameSession
{
    private readonly List<string> _log = new();

    /// <summary>Creates a session already sitting on a fresh fight.</summary>
    public GameSession() => NewRun(DefaultSeed);

    /// <summary>Seed used when the shell starts with no explicit choice.</summary>
    public const int DefaultSeed = 1;

    /// <summary>Current state.</summary>
    public GameState State { get; private set; } = null!;

    /// <summary>Commands Core says are legal right now.</summary>
    public IReadOnlyList<Command> Legal { get; private set; } = Array.Empty<Command>();

    /// <summary>Seed the current fight was started from.</summary>
    public int Seed { get; private set; }

    /// <summary>Human-readable event history, newest last.</summary>
    public IReadOnlyList<string> Log => _log;

    /// <summary>Unit the player is currently looking at, if any.</summary>
    public UnitId? Selected { get; private set; }

    /// <summary>Which action the player is aiming.</summary>
    public ActionMode Mode { get; private set; } = ActionMode.Move;

    /// <summary>Tile the pointer is over, if any.</summary>
    public Coord? Hovered { get; private set; }

    /// <summary>The selected unit, resolved.</summary>
    public Unit? SelectedUnit => Selected is null ? null : State.FindUnit(Selected.Value);

    /// <summary>The selected unit's class ability, if it has one.</summary>
    public AbilityDescriptor? SelectedAbility =>
        SelectedUnit is null ? null : AbilityDescriptor.ForKind(SelectedUnit.Kind);

    /// <summary>Restarts the run on a fresh fight.</summary>
    /// <param name="seed">Run seed.</param>
    public void NewRun(int seed)
    {
        Seed = seed;
        _log.Clear();
        Selected = null;
        Hovered = null;
        Mode = ActionMode.Move;
        Adopt(Game.Start(FightLibrary.Fight1(), seed));
    }

    /// <summary>Applies a command and folds the result into the session.</summary>
    /// <param name="command">Command drawn from <see cref="Legal"/>.</param>
    public void Submit(Command command)
    {
        Adopt(Game.Apply(State, command));
        Hovered = null;
        Mode = DefaultModeFor(SelectedUnit);
    }

    /// <summary>Selects a unit to act with, when Core allows it.</summary>
    /// <param name="id">Unit to select.</param>
    public void Select(UnitId id)
    {
        if (!Selectable.Contains(id))
        {
            return;
        }

        Selected = id;
        Hovered = null;
        Mode = DefaultModeFor(SelectedUnit);
    }

    /// <summary>Switches which action the player is aiming.</summary>
    /// <param name="mode">Mode to aim.</param>
    public void SetMode(ActionMode mode)
    {
        if (IsAvailable(mode))
        {
            Mode = mode;
            Hovered = null;
        }
    }

    /// <summary>Records the tile under the pointer, for previewing.</summary>
    /// <param name="coord">Tile hovered, or <c>null</c> when the pointer leaves the board.</param>
    public void Hover(Coord? coord) => Hovered = coord;

    /// <summary>Units that have at least one legal command right now.</summary>
    public IReadOnlyCollection<UnitId> Selectable =>
        Legal.Select(UnitOf).Where(id => id != UnitId.None).Distinct().ToList();

    /// <summary>Whether an action mode has any legal target for the selected unit.</summary>
    /// <param name="mode">Mode to test.</param>
    /// <returns>Whether it can be aimed.</returns>
    public bool IsAvailable(ActionMode mode) => TargetsFor(mode).Count > 0;

    /// <summary>Clickable tiles for the current mode, each mapped to the command it submits.</summary>
    public IReadOnlyDictionary<Coord, Command> Targets => TargetsFor(Mode);

    /// <summary>Tiles to tint as "within reach" for the current mode.</summary>
    public IReadOnlyCollection<Coord> RangeTiles
    {
        get
        {
            var unit = SelectedUnit;
            if (unit is null || !unit.IsOnBoard)
            {
                return Array.Empty<Coord>();
            }

            switch (Mode)
            {
                case ActionMode.Attack:
                case ActionMode.Pull:
                    return Combat.RangeTiles(State, unit).ToList();
                case ActionMode.Ability:
                    return Abilities.RangeTiles(State, unit).ToList();
                default:
                    return Array.Empty<Coord>();
            }
        }
    }

    /// <summary>Tiles the hovered action would travel through, for drawing the projected route.</summary>
    public IReadOnlyCollection<Coord> ProjectedPath
    {
        get
        {
            var unit = SelectedUnit;
            if (unit is null || Hovered is null || !Targets.ContainsKey(Hovered.Value))
            {
                return Array.Empty<Coord>();
            }

            var tiles = new List<Coord>();

            if (Mode == ActionMode.Move && Movement.TryGetMove(State, unit, Hovered.Value, out var option))
            {
                tiles.AddRange(option.Path);
                return tiles;
            }

            var charge = HoveredCharge();
            if (charge is not null)
            {
                tiles.AddRange(charge.Path);
                if (charge.Contact is not null)
                {
                    tiles.AddRange(charge.Contact.Path);
                }

                return tiles;
            }

            var displacement = HoveredDisplacement();
            if (displacement is not null)
            {
                tiles.AddRange(displacement.Path);
            }

            return tiles;
        }
    }

    /// <summary>
    /// Plain-language description of what the hovered action would do, built entirely from Core
    /// previews so it can never promise something the rules will not deliver.
    /// </summary>
    public string? PreviewText
    {
        get
        {
            var unit = SelectedUnit;
            if (unit is null || Hovered is null || !Targets.TryGetValue(Hovered.Value, out var command))
            {
                return null;
            }

            switch (command)
            {
                case MoveCommand:
                    return DescribeMove(unit, Hovered.Value);
                case AttackCommand attack when attack.Mode == AttackMode.Damage:
                    return DescribeAttack(unit, attack);
                case AttackCommand:
                    return Describe("Pull 1", HoveredDisplacement());
                case AbilityCommand ability:
                    return DescribeAbility(unit, ability);
                case RescueCommand:
                    return "Haul the clinging ally out. Costs your whole activation.";
                case FinishClingingCommand:
                    return "Kick it off the ledge — gone for the run. Free action.";
                default:
                    return null;
            }
        }
    }

    /// <summary>The command that ends the selected unit's activation, when it exists.</summary>
    public EndActivationCommand? EndCommand =>
        Selected is null
            ? null
            : Legal.OfType<EndActivationCommand>().FirstOrDefault(e => e.UnitId == Selected.Value);

    /// <summary>Tiles the current player may deploy onto.</summary>
    public IReadOnlyDictionary<Coord, DeployCommand> DeployTargets =>
        Legal.OfType<DeployCommand>()
            .Where(d => PendingDeployUnit is null || d.UnitId == PendingDeployUnit.Value)
            .GroupBy(d => d.At)
            .ToDictionary(g => g.Key, g => g.First());

    /// <summary>The unit that will be placed by the next deployment click.</summary>
    public UnitId? PendingDeployUnit =>
        Legal.OfType<DeployCommand>().Select(d => (UnitId?)d.UnitId).FirstOrDefault();

    /// <summary>True when the enemy side owes an activation. M3 replaces the pass with real AI.</summary>
    public bool AwaitingEnemy => Game.IsEnemyTurn(State) && Legal.Count > 0;

    /// <summary>Resolves one pending enemy activation.</summary>
    public void ResolveEnemyActivation()
    {
        var pass = Legal.OfType<EndActivationCommand>().FirstOrDefault();
        if (pass is not null)
        {
            Submit(pass);
        }
    }

    private IReadOnlyDictionary<Coord, Command> TargetsFor(ActionMode mode)
    {
        var map = new Dictionary<Coord, Command>();
        if (Selected is null)
        {
            return map;
        }

        var id = Selected.Value;

        foreach (var command in Legal)
        {
            if (UnitOf(command) != id)
            {
                continue;
            }

            switch (command)
            {
                case MoveCommand move when mode == ActionMode.Move:
                    map[move.To] = move;
                    break;

                case AttackCommand attack when mode == ActionMode.Attack && attack.Mode == AttackMode.Damage:
                    Add(map, attack.TargetId, attack);
                    break;

                case AttackCommand attack when mode == ActionMode.Pull && attack.Mode == AttackMode.Pull:
                    Add(map, attack.TargetId, attack);
                    break;

                case AbilityCommand ability when mode == ActionMode.Ability:
                    AddAbility(map, ability);
                    break;

                case RescueCommand rescue when mode == ActionMode.Rescue:
                    Add(map, rescue.ClingingId, rescue);
                    break;

                case FinishClingingCommand finish when mode == ActionMode.Finish:
                    Add(map, finish.ClingingId, finish);
                    break;
            }
        }

        return map;
    }

    private void AddAbility(Dictionary<Coord, Command> map, AbilityCommand ability)
    {
        if (ability.TargetId.HasValue)
        {
            Add(map, ability.TargetId.Value, ability);
            return;
        }

        // A charge is aimed by clicking where it lands: the enemy it would hit, or the tile it
        // would end on when the lane is empty.
        var unit = SelectedUnit;
        if (unit is null || !ability.Direction.HasValue)
        {
            return;
        }

        var charge = Abilities.PreviewCharge(State, unit, ability.Direction.Value);
        var tile = charge.Contact is not null
            ? State.UnitById(charge.Contact.UnitId).Position
            : charge.Destination;

        map[tile] = ability;
    }

    private void Add(Dictionary<Coord, Command> map, UnitId unitId, Command command)
    {
        var unit = State.FindUnit(unitId);
        if (unit is not null && unit.IsOnBoard)
        {
            map[unit.Position] = command;
        }
    }

    private ChargePreview? HoveredCharge()
    {
        var unit = SelectedUnit;
        if (unit is null || Hovered is null || !Targets.TryGetValue(Hovered.Value, out var command))
        {
            return null;
        }

        return command is AbilityCommand { Direction: { } direction }
            ? Abilities.PreviewCharge(State, unit, direction)
            : null;
    }

    private DisplacementPreview? HoveredDisplacement()
    {
        var unit = SelectedUnit;
        if (unit is null || Hovered is null || !Targets.TryGetValue(Hovered.Value, out var command))
        {
            return null;
        }

        switch (command)
        {
            case AbilityCommand { TargetId: { } targetId }:
                return Abilities.PreviewTarget(State, unit, targetId);

            case AttackCommand { Mode: AttackMode.Pull } pull:
                return Displacement.PreviewAuto(State, pull.TargetId, unit.Position, DisplacementKind.Pull, 1);

            case AttackCommand attack when unit.Template.AttackPush > 0:
                return Displacement.PreviewAuto(
                    State, attack.TargetId, unit.Position, DisplacementKind.Push, unit.Template.AttackPush);

            default:
                return null;
        }
    }

    private string DescribeMove(Unit unit, Coord destination)
    {
        if (!Movement.TryGetMove(State, unit, destination, out var option))
        {
            return string.Empty;
        }

        string text = $"Move {option.Cost} MP";
        if (option.SpikeTiles > 0)
        {
            text += $" · {option.SpikeTiles} damage from spikes";
        }

        return text;
    }

    private string DescribeAttack(Unit unit, AttackCommand attack)
    {
        var target = State.UnitById(attack.TargetId);
        Combat.CanAttack(State, unit, target, out int damage);

        string text = $"{damage} damage to {target.Name}";
        if (Combat.IsElevatedShot(State, unit))
        {
            text += " (high ground +1)";
        }

        if (damage >= target.Hp)
        {
            return text + " — kills it";
        }

        var push = HoveredDisplacement();
        return push is null ? text : text + ", then " + Describe("push " + unit.Template.AttackPush, push);
    }

    private string DescribeAbility(Unit unit, AbilityCommand command)
    {
        var descriptor = AbilityDescriptor.For(command.Ability);

        if (command.Direction.HasValue)
        {
            var charge = Abilities.PreviewCharge(State, unit, command.Direction.Value);
            string text = charge.Path.Count > 0 ? $"Charge {charge.Path.Count} to {charge.Destination}" : "Shove from here";

            if (charge.SelfDamage > 0)
            {
                text += $" · you take {charge.SelfDamage} from spikes";
            }

            return charge.Contact is null ? text : text + ", then " + Describe("push 2", charge.Contact);
        }

        var displacement = HoveredDisplacement();
        string head = descriptor.Damage > 0 ? $"{descriptor.Damage} damage" : descriptor.Name;
        return displacement is null ? head : head + ", " + Describe(descriptor.PullsToAdjacent ? "reel in" : "push", displacement);
    }

    private string Describe(string verb, DisplacementPreview? preview)
    {
        if (preview is null)
        {
            return verb;
        }

        if (preview.IsNoOp)
        {
            return $"{verb} — it does not budge";
        }

        var target = State.FindUnit(preview.UnitId);
        string name = target?.Name ?? "target";

        switch (preview.Stop)
        {
            case DisplacementStop.Collision:
                string into = preview.ObstacleId.HasValue
                    ? State.UnitById(preview.ObstacleId.Value).Name
                    : "the wall";
                string both = preview.ObstacleId.HasValue ? " to both" : string.Empty;
                return $"{verb} {preview.EffectiveDistance} into {into} — {preview.DamageToUnit} damage{both}, staggered"
                    + (preview.WouldDown ? ", kills it" : string.Empty);

            case DisplacementStop.Spikes:
                return $"{verb} onto spikes — {preview.DamageToUnit} damage, staggered"
                    + (preview.WouldDown ? ", kills it" : string.Empty);

            case DisplacementStop.Pit:
                return $"{verb} into the pit — {name} is left clinging";

            default:
                string tail = preview.DamageToUnit > 0 ? $" · {preview.DamageToUnit} falling damage" : string.Empty;
                return $"{verb} {preview.EffectiveDistance} to {preview.Destination}{tail}";
        }
    }

    private static ActionMode DefaultModeFor(Unit? unit)
    {
        if (unit is null)
        {
            return ActionMode.Move;
        }

        return unit.HasMoved ? ActionMode.Attack : ActionMode.Move;
    }

    private void Adopt(StepResult result)
    {
        State = result.NewState;
        Legal = result.LegalNext;

        foreach (var evt in result.Events)
        {
            _log.Add(EventText.Describe(evt, State));
        }

        // Core commits a unit to its activation on that unit's first command; follow it so the
        // highlighted unit and the one Core will accept commands for never disagree.
        if (State.ActiveUnitId.HasValue)
        {
            Selected = State.ActiveUnitId;
        }
        else if (Selected.HasValue && !Selectable.Contains(Selected.Value))
        {
            Selected = null;
        }
    }

    private static UnitId UnitOf(Command command) => command switch
    {
        MoveCommand m => m.UnitId,
        AttackCommand a => a.UnitId,
        AbilityCommand a => a.UnitId,
        RescueCommand r => r.UnitId,
        FinishClingingCommand f => f.UnitId,
        EndActivationCommand e => e.UnitId,
        DeployCommand d => d.UnitId,
        _ => UnitId.None,
    };
}
