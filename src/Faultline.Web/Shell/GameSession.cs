using System;
using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Web.Shell;

/// <summary>
/// Holds the current <see cref="GameState"/> and the selection the player is building up.
/// </summary>
/// <remarks>
/// This type answers no rules questions of its own. Every "can I?" is read straight out of the
/// <see cref="StepResult.LegalNext"/> list Core handed back, which is what keeps CLAUDE.md's
/// "rules change only in Core" true even as the UI grows.
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

    /// <summary>Restarts the run on a fresh fight.</summary>
    /// <param name="seed">Run seed.</param>
    public void NewRun(int seed)
    {
        Seed = seed;
        _log.Clear();
        Selected = null;
        Adopt(Game.Start(FightLibrary.Fight1(), seed));
    }

    /// <summary>Applies a command and folds the result into the session.</summary>
    /// <param name="command">Command drawn from <see cref="Legal"/>.</param>
    public void Submit(Command command) => Adopt(Game.Apply(State, command));

    /// <summary>Selects a unit to act with, when Core allows it.</summary>
    /// <param name="id">Unit to select.</param>
    public void Select(UnitId id)
    {
        if (Selectable.Contains(id))
        {
            Selected = id;
        }
    }

    /// <summary>Clears the current selection unless Core has already locked one in.</summary>
    public void ClearSelection()
    {
        if (!State.ActiveUnitId.HasValue)
        {
            Selected = null;
        }
    }

    /// <summary>Units that have at least one legal command right now.</summary>
    public IReadOnlyCollection<UnitId> Selectable =>
        Legal.Select(UnitOf).Where(id => id != UnitId.None).Distinct().ToList();

    /// <summary>Where the selected unit may walk, keyed by destination.</summary>
    public IReadOnlyDictionary<Coord, MoveCommand> MoveTargets =>
        Selected is null
            ? new Dictionary<Coord, MoveCommand>()
            : Legal.OfType<MoveCommand>()
                .Where(m => m.UnitId == Selected.Value)
                .GroupBy(m => m.To)
                .ToDictionary(g => g.Key, g => g.First());

    /// <summary>Enemies the selected unit may attack, keyed by target id.</summary>
    public IReadOnlyDictionary<UnitId, AttackCommand> AttackTargets =>
        Selected is null
            ? new Dictionary<UnitId, AttackCommand>()
            : Legal.OfType<AttackCommand>()
                .Where(a => a.UnitId == Selected.Value)
                .GroupBy(a => a.TargetId)
                .ToDictionary(g => g.Key, g => g.First());

    /// <summary>Tiles the current player may deploy onto.</summary>
    public IReadOnlyDictionary<Coord, DeployCommand> DeployTargets =>
        Legal.OfType<DeployCommand>()
            .Where(d => PendingDeployUnit is null || d.UnitId == PendingDeployUnit.Value)
            .GroupBy(d => d.At)
            .ToDictionary(g => g.Key, g => g.First());

    /// <summary>The unit that will be placed by the next deployment click.</summary>
    public UnitId? PendingDeployUnit =>
        Legal.OfType<DeployCommand>().Select(d => (UnitId?)d.UnitId).FirstOrDefault();

    /// <summary>The command that ends the selected unit's activation, when it exists.</summary>
    public EndActivationCommand? EndCommand =>
        Selected is null
            ? null
            : Legal.OfType<EndActivationCommand>().FirstOrDefault(e => e.UnitId == Selected.Value);

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
        EndActivationCommand e => e.UnitId,
        DeployCommand d => d.UnitId,
        _ => UnitId.None,
    };
}
