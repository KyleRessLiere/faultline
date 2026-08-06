using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// "Protect an objective" is not a tactical problem until a player can compute urgency, so every
/// structure on the board has to be nameable, countable and predictable from the same code path the
/// win check reads. These tests pin the three ways that could quietly stop being true: a status that
/// sums two structures into one number, a status that counts scenery as the objective, and a
/// telegraph that promises damage the resolution does not take off.
/// </summary>
public class StructureVisibilityTests
{
    // ---- naming -----------------------------------------------------------------------------

    [Fact]
    public void Structure_TakesItsDisplayNameFromItsRole()
    {
        var protect = new Structure { At = new Coord(0, 0), Hp = 12, MaxHp = 12, Role = ObjectiveKind.Protect };
        var destroy = new Structure { At = new Coord(1, 0), Hp = 24, MaxHp = 24, Role = ObjectiveKind.Destroy };
        var debris = new Structure
        {
            At = new Coord(2, 0), Hp = 6, MaxHp = 6, Role = ObjectiveKind.Destroy, IsBlocker = true,
        };

        Assert.Equal("Shrine", Naming.Of(protect));
        Assert.Equal("Gate", Naming.Of(destroy));
        Assert.Equal("Debris", Naming.Of(debris));
    }

    [Fact]
    public void StructureStatus_LabelsItselfWithItsNameAndBothHitPointNumbers()
    {
        var state = ProtectBoard();

        var status = StructureStatus.For(state, new Coord(2, 0));

        Assert.NotNull(status);
        Assert.Equal("Shrine", status!.Name);
        Assert.Equal("Shrine 12/12", status.Label);
        Assert.Equal(new Coord(2, 0), status.At);
    }

    [Fact]
    public void StructureStatus_OnAnEmptyTile_IsNullRatherThanAZeroHitPointStructure()
    {
        // A "0/0" card for a tile with nothing on it is a silent no-op wearing a number.
        Assert.Null(StructureStatus.For(ProtectBoard(), new Coord(5, 0)));
    }

    // ---- the objective stops summing --------------------------------------------------------

    [Fact]
    public void ObjectiveStatus_WithTwoStructures_ReportsEachOneSeparately()
    {
        var state = TwoStructureBoard();

        var status = ObjectiveStatus.For(state);

        Assert.Equal(
            new[] { "Shrine 12/12", "Shrine 6/12" },
            status.Structures.Select(s => s.Label).ToArray());
    }

    [Fact]
    public void ObjectiveStatus_WithABlockerOnTheBoard_LeavesItOutOfTheObjectivesOwnAccount()
    {
        // A wall somebody knocked through to get across the map is neither a win nor a loss
        // condition (D-114), so folding its hit points into "Shrine 18/18" would report a bar that
        // the win check does not believe in.
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 5, 0)
            .Objective(ObjectiveKind.Protect, hp: 12, tiles: new Coord(2, 0))
            .Blockers(6, new Coord(4, 0))
            .Build();

        var status = ObjectiveStatus.For(state);

        Assert.Equal(2, state.Structures.Count);
        var only = Assert.Single(status.Structures);
        Assert.Equal("Shrine 12/12", only.Label);
        Assert.Equal(12, status.Progress);
        Assert.Equal(12, status.Target);
        Assert.Equal("Shrine 12/12", status.Label);
    }

    [Fact]
    public void ObjectiveStatus_ForASingleStructure_NamesItInTheBarLabel()
    {
        Assert.Equal("Shrine 12/12", ObjectiveStatus.For(ProtectBoard()).Label);
    }

    [Fact]
    public void ObjectiveStatus_ForADestroyObjective_QuotesTheChipTheRulesActuallyTake()
    {
        // The goal line used to say "attacks chip it for 1" while Objectives.AttackDamageToStructure
        // took 2. A player computing how many swings the gate is worth was reading a lie off the one
        // panel that exists to stop that happening.
        var status = ObjectiveStatus.For(DestroyBoard());

        Assert.Contains(Objectives.AttackDamageToStructure.ToString(), status.Goal);
        Assert.DoesNotContain("chip it for 1", status.Goal);
    }

    // ---- the telegraph agrees with the resolution -------------------------------------------

    [Fact]
    public void RaiderClaw_PredictsTheHitPointsTheStructureActuallyEndsWith()
    {
        // The shape that caught four bugs in Stage A: read the telegraph, resolve it, and compare.
        // Nothing here types an expected number — the point is that the two agree, not what they are.
        var state = RaiderBoard();
        var raider = state.Find(UnitKind.Raider);
        var tile = new Coord(2, 0);

        var intent = Ai.Declare(state, raider);
        Assert.Equal(IntentAction.Attack, intent.Action);
        Assert.Equal(tile, intent.TargetPosition);

        var before = StructureStatus.For(state, tile)!;
        int predicted = before.HpAfter(intent.Damage);

        var result = state.Step(new EndActivationCommand(raider.Id));

        Assert.Equal(predicted, result.NewState.StructureAt(tile)!.Hp);
    }

    [Fact]
    public void RaiderClaw_TelegraphsTheFlatChipRatherThanItsWeaponDamage()
    {
        // Objectives.Damage overrides every attack to the flat chip (D-060), so a claw that
        // published Template.Damage would be promising a number the resolution never uses. The two
        // happen to coincide for the shipped Raider; this pins the source, not the coincidence.
        var state = RaiderBoard();

        var intent = Ai.Declare(state, state.Find(UnitKind.Raider));

        Assert.Equal(Objectives.AttackDamageToStructure, intent.Damage);
    }

    [Fact]
    public void RaiderMarch_NamesTheStructureItIsWalkingAt()
    {
        // An approaching Raider that names nothing is a body crossing the board for no stated
        // reason. Core carries the tile so the telegraph can say which structure it is coming for.
        var state = BoardBuilder.Open(9, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Raider, 8, 0)
            .Objective(ObjectiveKind.Protect, hp: 12, tiles: new Coord(2, 0))
            .Active(Team.Enemy)
            .Build();

        var intent = Ai.Declare(state, state.Find(UnitKind.Raider));

        Assert.Equal(IntentAction.Advance, intent.Action);
        Assert.Null(intent.TargetId);
        Assert.Equal(new Coord(2, 0), intent.TargetPosition);
        Assert.NotNull(StructureStatus.For(state, intent.TargetPosition!.Value));
    }

    // ---- fixtures ---------------------------------------------------------------------------

    private static GameState ProtectBoard() =>
        BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 5, 0)
            .Objective(ObjectiveKind.Protect, hp: 12, tiles: new Coord(2, 0))
            .Build();

    private static GameState DestroyBoard() =>
        BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 5, 0)
            .Objective(ObjectiveKind.Destroy, hp: 24, tiles: new Coord(2, 0))
            .Build();

    /// <summary>A Protect objective on two tiles, so the status has two structures to keep apart.</summary>
    private static GameState TwoStructureBoard()
    {
        var state = BoardBuilder.Open(8, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 7, 0)
            .Objective(ObjectiveKind.Protect, 0, 12, new Coord(2, 0), new Coord(4, 0))
            .Build();

        // Half the second one down, so a status that summed would read 18/24 and hide which of the
        // two is the one in trouble.
        var events = new System.Collections.Generic.List<GameEvent>();
        return Objectives.Damage(state, new Coord(4, 0), 6, DamageSource.Collision, events);
    }

    private static GameState RaiderBoard() =>
        BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Raider, 3, 0)
            .Objective(ObjectiveKind.Protect, hp: 12, tiles: new Coord(2, 0))
            .Active(Team.Enemy)
            .Build();
}
