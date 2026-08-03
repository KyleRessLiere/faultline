using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// The Raider (docs/archive/CURATED_SET.md §5A). One priority list, two branches, and one thing that makes
/// it unlike every other enemy in the game: its list has no clause about player units in it at all.
/// One test per branch, plus the four the archetype's oddness demands — an ignored player, a fallen
/// structure, no structure at all, and ordinary physics.
/// </summary>
public class RaiderTests
{
    private static readonly Coord Shrine = new Coord(4, 0);

    // ---- branch 1: adjacent to the structure → claw it -----------------------------------------

    [Fact]
    public void Raider_AdjacentToTheStructure_ClawsItWithoutMoving()
    {
        var state = Lane(raider: 3, vanguard: 0);
        var intent = Ai.Declare(state, state.Find(UnitKind.Raider));

        Assert.Equal(IntentAction.Attack, intent.Action);
        Assert.Null(intent.MoveTo);
        Assert.Equal(Shrine, intent.TargetPosition);
        Assert.Null(intent.TargetId);
        Assert.Equal(UnitTemplate.For(UnitKind.Raider).Damage, intent.Damage);
    }

    /// <summary>
    /// The claw itself is the siege rule (D-034): the Raider spends no action on it, it simply ends
    /// its activation next to the shrine. What the archetype adds is that it *chose* to be there.
    /// </summary>
    [Fact]
    public void Raider_EndingItsActivationBesideTheStructure_TakesAPointOffIt()
    {
        var state = Lane(raider: 3, vanguard: 0, active: Team.Enemy);
        var raider = state.Find(UnitKind.Raider);

        var result = state.Step(Ai.Plan(state, raider));
        var attacked = result.Single<StructureAttacked>();

        Assert.Equal(raider.Id, attacked.AttackerId);
        Assert.Equal(Shrine, attacked.At);
        Assert.Equal(1, attacked.Damage);
        Assert.Equal(Objective.DefaultProtectHp - 1, result.NewState.StructureAt(Shrine)!.Hp);
    }

    // ---- branch 2: else path to it, Husk rules --------------------------------------------------

    [Fact]
    public void Raider_AwayFromTheStructure_WalksAtItAndClawsOnArrival()
    {
        var state = Lane(raider: 0, vanguard: 6);
        var intent = Ai.Declare(state, state.Find(UnitKind.Raider));

        // Move 3 from (0,0) lands on (3,0), which is the ring — so the walk ends in a claw (D-022).
        Assert.Equal(IntentAction.Attack, intent.Action);
        Assert.Equal(new Coord(3, 0), intent.MoveTo);
        Assert.Equal(Shrine, intent.TargetPosition);
    }

    [Fact]
    public void Raider_TooFarToArriveThisRound_JustAdvances()
    {
        var state = Shrined(new Coord(8, 0), "..........")
            .Enemy(UnitKind.Raider, 0, 0)
            .PlayerA(UnitKind.Vanguard, 9, 0)
            .Build();

        var intent = Ai.Declare(state, state.Find(UnitKind.Raider));

        Assert.Equal(IntentAction.Advance, intent.Action);
        Assert.Equal(new Coord(3, 0), intent.MoveTo);
        Assert.Null(intent.TargetId);
    }

    /// <summary>
    /// The structure is walked to by the same breadth-first field every other archetype uses (D-029),
    /// so a wall between a Raider and the shrine is a detour and never a dead end.
    /// </summary>
    [Fact]
    public void Raider_BehindAWall_WalksTheLongWayRoundRatherThanPressingAgainstIt()
    {
        var state = Shrined(new Coord(4, 0), "..#..", "..#..", ".....")
            .Enemy(UnitKind.Raider, 0, 0)
            .PlayerA(UnitKind.Vanguard, 4, 2)
            .Build();

        // Straight-line greed would take (1,0), which is a cul-de-sac. The path field sends it south
        // to the bottom row, which is the only route to the shrine.
        Assert.Equal(new Coord(1, 2), Ai.Declare(state, state.Find(UnitKind.Raider)).MoveTo);
    }

    [Fact]
    public void Raider_WithTwoShrines_WalksAtTheNearer()
    {
        var state = BoardBuilder.Rows(".......")
            .Objective(ObjectiveKind.Protect, 0, 6, new Coord(0, 0), new Coord(6, 0))
            .PlayerA(UnitKind.Vanguard, 4, 0)
            .Enemy(UnitKind.Raider, 3, 0)
            .Build();

        var intent = Ai.Declare(state, state.Find(UnitKind.Raider));

        Assert.Equal(new Coord(1, 0), intent.MoveTo);
        Assert.Equal(new Coord(0, 0), intent.TargetPosition);
    }

    // ---- the whole point: it never targets a player unit ----------------------------------------

    [Fact]
    public void Raider_WithAPlayerUnitAdjacent_StillWalksAtTheStructure()
    {
        var state = Lane(raider: 1, vanguard: 0, active: Team.Enemy);
        var raider = state.Find(UnitKind.Raider);
        var intent = Ai.Declare(state, raider);

        Assert.Equal(new Coord(3, 0), intent.MoveTo);
        Assert.Equal(Shrine, intent.TargetPosition);
        Assert.Null(intent.TargetId);
        Assert.IsType<MoveCommand>(Ai.Plan(state, raider));
    }

    [Fact]
    public void Raider_BesideBothAPlayerUnitAndTheStructure_ClawsTheStructure()
    {
        var state = Lane(raider: 3, vanguard: 2, active: Team.Enemy);
        var raider = state.Find(UnitKind.Raider);

        Assert.Equal(new EndActivationCommand(raider.Id), Ai.Plan(state, raider));

        var result = state.Step(Ai.Plan(state, raider));

        Assert.False(result.Has<UnitAttacked>());
        Assert.True(result.Has<StructureAttacked>());
        Assert.Equal(
            UnitTemplate.For(UnitKind.Vanguard).MaxHp,
            result.NewState.Find(UnitKind.Vanguard).Hp);
    }

    /// <summary>
    /// The free finish on a clinging unit is a clause about player units, so an archetype with no
    /// such clause does not get one either (D-041). A Husk in the same tile does take it.
    /// </summary>
    [Fact]
    public void Raider_NextToAClingingPlayerUnit_WalksPastItWhereAHuskWouldFinishIt()
    {
        var raiderState = ClingingLane(UnitKind.Raider);
        var huskState = ClingingLane(UnitKind.Husk);

        Assert.IsNotType<FinishClingingCommand>(
            Ai.Plan(raiderState, raiderState.Find(UnitKind.Raider)));
        Assert.IsType<FinishClingingCommand>(
            Ai.Plan(huskState, huskState.Find(UnitKind.Husk)));
    }

    // ---- degradation: no structure to walk at ---------------------------------------------------

    [Fact]
    public void Raider_WhenTheStructureFalls_HoldsInsteadOfThrowingOrFreezing()
    {
        var standing = Lane(raider: 0, vanguard: 6, active: Team.Enemy);
        var state = standing.WithStructure(standing.StructureAt(Shrine)! with { Hp = 0 });

        var raider = state.Find(UnitKind.Raider);
        var intent = Ai.Declare(state, raider);

        Assert.Equal(IntentAction.Hold, intent.Action);
        Assert.Null(intent.MoveTo);
        Assert.Equal(new EndActivationCommand(raider.Id), Ai.Plan(state, raider));

        // "Holds" and not "freezes": the list is re-run from scratch every activation, so a standing
        // structure puts it straight back in motion rather than leaving it stuck for the fight.
        var restored = state.WithStructure(state.Structures[0] with { Hp = Objective.DefaultProtectHp });
        Assert.Equal(IntentAction.Attack, Ai.Declare(restored, restored.Find(UnitKind.Raider)).Action);
    }

    [Fact]
    public void Raider_OnABoardWithNoProtectStructureAtAll_HoldsAndTouchesNobody()
    {
        var state = BoardBuilder.Rows(".......")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Raider, 1, 0)
            .Active(Team.Enemy)
            .Build();

        var raider = state.Find(UnitKind.Raider);

        Assert.Equal(IntentAction.Hold, Ai.Declare(state, raider).Action);

        var result = state.Step(Ai.Plan(state, raider));

        Assert.False(result.Has<UnitAttacked>());
        Assert.False(result.Has<UnitDamaged>());
    }

    /// <summary>A Destroy structure is not a Raider's business — nothing can attack one at all.</summary>
    [Fact]
    public void Raider_WithOnlyADestroyStructureOnTheBoard_Holds()
    {
        var state = BoardBuilder.Rows(".......")
            .Objective(ObjectiveKind.Destroy, 0, 8, new Coord(4, 0))
            .PlayerA(UnitKind.Vanguard, 6, 0)
            .Enemy(UnitKind.Raider, 0, 0)
            .Active(Team.Enemy)
            .Build();

        Assert.Equal(IntentAction.Hold, Ai.Declare(state, state.Find(UnitKind.Raider)).Action);
    }

    // ---- it is not special: displacement reads it as a Husk -------------------------------------

    [Fact]
    public void Raider_IsPushedAndHurtExactlyAsAHuskIs()
    {
        var state = Lane(raider: 1, vanguard: 0);
        var vanguard = state.Find(UnitKind.Vanguard);
        var raiderId = state.Find(UnitKind.Raider).Id;

        var after = state.Then(new AttackCommand(vanguard.Id, raiderId)).UnitById(raiderId);

        Assert.Equal(new Coord(2, 0), after.Position);
        Assert.Equal(UnitTemplate.For(UnitKind.Raider).MaxHp - 1, after.Hp);
    }

    [Fact]
    public void Raider_ShovedIntoAPit_ClingsLikeAnythingElse()
    {
        // The second Raider is not decoration: with nothing else of its side standing, a clinging
        // enemy is swept where it hangs (D-081), and this test is about the cling rather than that.
        var state = Shrined(new Coord(0, 0), ".....O.")
            .PlayerA(UnitKind.Vanguard, 3, 0)
            .Enemy(UnitKind.Raider, 4, 0)
            .Enemy(UnitKind.Raider, 0, 1)
            .Build();

        var result = state.Step(new AttackCommand(
            state.Find(UnitKind.Vanguard).Id, state.Find(UnitKind.Raider).Id));

        Assert.True(result.Has<Clinging>());
        Assert.True(result.NewState.UnitById(result.Single<Clinging>().UnitId).Clinging);
    }

    // ---- helpers --------------------------------------------------------------------------------

    private static BoardBuilder Shrined(Coord at, params string[] rows) =>
        BoardBuilder.Rows(rows).Objective(ObjectiveKind.Protect, 0, Objective.DefaultProtectHp, at);

    private static GameState Lane(int raider, int vanguard, Team active = Team.PlayerA) =>
        Shrined(Shrine, ".......")
            .PlayerA(UnitKind.Vanguard, vanguard, 0)
            .Enemy(UnitKind.Raider, raider, 0)
            .Active(active)
            .Build();

    private static GameState ClingingLane(UnitKind kind)
    {
        var state = Shrined(new Coord(3, 0), "O......")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(kind, 1, 0)
            .Active(Team.Enemy)
            .Build();

        return state.WithUnit(state.Find(UnitKind.Vanguard) with
        {
            Clinging = true,
            ClingingSinceRound = state.Round,
        });
    }
}
