using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// Shoulder (D-100): the Husk barrels through a body in its way rather than stopping or going round.
/// The blocker is knocked one tile sideways and takes a point of contact damage; the Husk pays a
/// movement point and keeps walking.
/// </summary>
public class TrampleTests
{
    // ---- the basic shove ------------------------------------------------------------------

    [Fact]
    public void Trample_KnocksTheBlockerAside_AndDealsContactDamage()
    {
        var state = Open(out var husk, out var blocker);

        var result = state.Step(new MoveCommand(husk, new Coord(2, 1)));

        var trample = result.Single<UnitTrampled>();
        Assert.Equal(husk, trample.UnitId);
        Assert.Equal(blocker, trample.VictimId);
        Assert.Equal(new Coord(1, 1), trample.At);
        Assert.Equal(Direction.Right, trample.Heading);
        Assert.Equal(Trample.ContactDamage, trample.Damage);

        Assert.Equal(new Coord(2, 1), result.NewState.Get(husk).Position);
        Assert.Equal(new Coord(1, 0), result.NewState.Get(blocker).Position);
        Assert.Equal(12, result.NewState.Get(blocker).Hp);
    }

    // Both sides open, so N/E/S/W decides and north wins. Asserted a hundred times over, because a
    // tie-break that is only usually the same is a replay that is only usually the same.
    [Fact]
    public void Trample_WithBothSidesOpen_KnocksItNorth_EveryTime()
    {
        for (int i = 0; i < 100; i++)
        {
            var state = Open(out var husk, out _);
            var result = state.Step(new MoveCommand(husk, new Coord(2, 1)));

            Assert.Equal(Direction.Up, result.Single<UnitTrampled>().Aside);
        }
    }

    [Fact]
    public void Trample_TakesItsOwnAllyAsReadilyAsAPlayerUnit()
    {
        var state = BoardBuilder.Rows("....", "....", "....")
            .Enemy(UnitKind.Husk, 0, 1)
            .Enemy(UnitKind.Lobber, 1, 1)
            .PlayerA(UnitKind.Vanguard, 3, 2)
            .Build();

        var husk = state.Find(UnitKind.Husk).Id;
        var ally = state.Find(UnitKind.Lobber).Id;

        var result = EnemyTurn(state).Step(new MoveCommand(husk, new Coord(2, 1)));

        Assert.Equal(ally, result.Single<UnitTrampled>().VictimId);
        Assert.Equal(new Coord(1, 0), result.NewState.Get(ally).Position);
    }

    // ---- what the board does to the victim -------------------------------------------------

    // 1 for the contact, then the shove's own consequences on top: a Staggered blocker travels two,
    // finds the edge, and takes the collision for it.
    [Fact]
    public void Trample_ShoulderIntoAWall_Chains_OneThenTwoAndAStagger()
    {
        var state = BoardBuilder.Rows("....", "....")
            .Enemy(UnitKind.Husk, 0, 1)
            .PlayerA(UnitKind.Vanguard, 1, 1)
            .PlayerA(UnitKind.Archer, 3, 0)
            .Build();

        var husk = state.Find(UnitKind.Husk).Id;
        var blocker = state.Find(UnitKind.Vanguard).Id;

        state = state.WithUnit(state.Get(blocker) with { Staggered = true });
        var before = state.Get(blocker).Hp;

        var result = EnemyTurn(state).Step(new MoveCommand(husk, new Coord(2, 1)));

        Assert.Equal(Direction.Up, result.Single<UnitTrampled>().Aside);
        Assert.Equal(new Coord(1, 0), result.NewState.Get(blocker).Position);
        Assert.Equal(Displacement.CollisionDamage, result.Single<Collision>().Damage);
        Assert.Equal(before - Trample.ContactDamage - Displacement.CollisionDamage,
            result.NewState.Get(blocker).Hp);
        Assert.True(result.NewState.Get(blocker).Staggered);
    }

    // Being knocked somewhere terrible is the point of the mechanic, not an exception to it.
    [Fact]
    public void Trample_TowardADrain_LeavesTheVictimClinging()
    {
        var state = Drain(out var husk, out var blocker);

        var result = state.Step(new MoveCommand(husk, new Coord(2, 1)));

        Assert.True(result.Has<Clinging>());
        Assert.True(result.NewState.Get(blocker).Clinging);
        Assert.Equal(new Coord(1, 0), result.NewState.Get(blocker).Position);
        Assert.Equal(new Coord(2, 1), result.NewState.Get(husk).Position);
    }

    [Fact]
    public void Trample_AVictimLeftClinging_CanStillBeHauledOut()
    {
        var state = Drain(out var husk, out var blocker);
        var after = state.Then(new MoveCommand(husk, new Coord(2, 1)));

        var rescuer = after.Find(UnitKind.Archer).Id;
        Assert.True(after.Get(blocker).Clinging);

        var players = PlayerTurn(after);
        var command = players.Rescue(rescuer, blocker);
        TestPlay.AssertLegal(players, command);

        var rescued = players.Step(command);
        Assert.True(rescued.Has<Rescued>());
        Assert.False(rescued.NewState.Get(blocker).Clinging);
    }

    [Fact]
    public void Trample_AVictimLeftClinging_CanStillBeKickedIn()
    {
        var state = Drain(out var husk, out var blocker);
        var after = state.Then(new MoveCommand(husk, new Coord(2, 1)));

        // Whoever is standing beside the ledge finishes it, and finishing is free.
        var kicker = after.Find(UnitKind.Lobber).Id;
        Assert.True(Pits.CanFinish(after, after.Get(kicker), after.Get(blocker)));

        // The Husk has had its slot; the kick belongs to the next enemy to take one.
        var next = after.Then(new EndActivationCommand(husk));
        var result = next.Step(new FinishClingingCommand(kicker, blocker));

        Assert.True(result.Has<Voided>());
        Assert.True(result.NewState.Get(blocker).Voided);
    }

    // ---- when the blocker is a wall ---------------------------------------------------------

    // Push resistance 2 eats the side-shove whole, so the Wardbearer never vacates and there is no
    // trample at all — no damage, and the Husk stops short. This is what makes him a door.
    [Fact]
    public void Trample_AgainstPushResistanceTwo_HaltsWithoutTouchingAnybody()
    {
        var state = BoardBuilder.Rows("....", "....", "....")
            .Enemy(UnitKind.Husk, 0, 1)
            .PlayerB(UnitKind.Wardbearer, 1, 1)
            .PlayerA(UnitKind.Archer, 3, 2)
            .Build();

        var husk = state.Find(UnitKind.Husk).Id;
        var ward = state.Find(UnitKind.Wardbearer).Id;
        var turn = EnemyTurn(state);

        Assert.Null(Trample.Side(turn, turn.Get(husk), new Coord(1, 1), Direction.Right));
        Assert.False(Movement.Reachable(turn, turn.Get(husk)).ContainsKey(new Coord(2, 1)));

        Assert.Equal(14, turn.Get(ward).Hp);
    }

    // Both perpendicular tiles walled: the blocker is masonry and the route simply does not exist.
    [Fact]
    public void Trample_WithBothSidesBlocked_TheBlockerIsAWall()
    {
        var state = BoardBuilder.Rows(".#..", "....", ".#..")
            .Enemy(UnitKind.Husk, 0, 1)
            .PlayerA(UnitKind.Vanguard, 1, 1)
            .PlayerA(UnitKind.Archer, 3, 2)
            .Build();

        var husk = state.Find(UnitKind.Husk).Id;
        var turn = EnemyTurn(state);

        Assert.Null(Trample.Side(turn, turn.Get(husk), new Coord(1, 1), Direction.Right));
        Assert.False(Movement.Reachable(turn, turn.Get(husk)).ContainsKey(new Coord(2, 1)));
    }

    // A Footing token cancels the side-shove, which is the same halt by a different route. Both
    // sides have to be shut or the Husk would simply pick the other one.
    [Fact]
    public void Trample_WhenFootingCancelsTheSideShove_HaltsAndSpendsNothing()
    {
        var state = BoardBuilder.Rows(".O..", "....", ".#..")
            .Enemy(UnitKind.Husk, 0, 1)
            .Enemy(UnitKind.Anchor, 1, 1, footing: 1)
            .PlayerA(UnitKind.Vanguard, 3, 2)
            .Build();

        var husk = state.Find(UnitKind.Husk).Id;
        var blocker = state.Find(UnitKind.Anchor).Id;
        var turn = EnemyTurn(state);

        // North is the drain and the token buys it out of that; south is a wall.
        Assert.Null(Trample.Side(turn, turn.Get(husk), new Coord(1, 1), Direction.Right));
        Assert.False(Movement.Reachable(turn, turn.Get(husk)).ContainsKey(new Coord(2, 1)));

        // Nothing resolved, so nothing was spent: a token is only used by a shove that happens.
        Assert.Equal(1, turn.Get(blocker).Footing);
        Assert.Equal(new Coord(1, 1), turn.Get(blocker).Position);
    }

    // ---- what it costs ----------------------------------------------------------------------

    [Fact]
    public void Trample_CostsAMovementPointOnTopOfTheTerrain()
    {
        var state = Open(out var husk, out _);
        var turn = EnemyTurn(state);

        Assert.True(Movement.TryGetMove(turn, turn.Get(husk), new Coord(2, 1), out var through));
        Assert.Equal(3, through!.Cost);

        // The same two tiles with nobody in the way cost two.
        var clear = BoardBuilder.Rows("....", "....", "....")
            .Enemy(UnitKind.Husk, 0, 1)
            .PlayerA(UnitKind.Archer, 3, 2)
            .Build();

        var alone = clear.Find(UnitKind.Husk);
        Assert.True(Movement.TryGetMove(clear, alone, new Coord(2, 1), out var open));
        Assert.Equal(2, open!.Cost);
    }

    // The trample is priced into the comparison rather than bolted on after it. On flat ground a
    // single body is always cheaper to shoulder than to walk round — a detour costs two extra tiles
    // and the shoulder costs one — and that is the point of the mechanic. Around wins exactly when
    // the body cannot be shifted, which is when the shoulder is not on offer at any price.
    [Fact]
    public void Trample_RoutesStraightThroughAShiftableBody()
    {
        var through = Blocked(UnitKind.Vanguard, out var husk);

        Assert.True(Movement.TryGetMove(through, through.Get(husk), new Coord(2, 1), out var shoulder));
        Assert.Contains(new Coord(1, 1), shoulder!.Path);
        Assert.Equal(3, shoulder.Cost);

        // A body it cannot shift is a wall, and the way round costs four against a budget of three
        // — which is the whole difference the shoulder makes.
        var around = Blocked(UnitKind.Wardbearer, out var stopped);
        Assert.False(Movement.Reachable(around, around.Get(stopped)).ContainsKey(new Coord(2, 1)));
    }

    // The routing metric prices the two the same way the walk does: one for a body that moves, the
    // ordinary toll for one that does not, so the planner goes round exactly what would stop it.
    [Fact]
    public void Trample_TheRoutingMetric_PricesAShiftableBodyLower()
    {
        var shiftable = Blocked(UnitKind.Vanguard, out var huskA);
        var immovable = Blocked(UnitKind.Wardbearer, out var huskB);

        var toShiftable = PathField.To(shiftable, shiftable.Get(huskA), new Coord(3, 1));
        var toImmovable = PathField.To(immovable, immovable.Get(huskB), new Coord(3, 1));

        Assert.Equal(PathField.TramplePenalty + 1, toShiftable.At(new Coord(1, 1)) - toShiftable.At(new Coord(2, 1)));
        Assert.Equal(PathField.OccupiedPenalty + 1, toImmovable.At(new Coord(1, 1)) - toImmovable.At(new Coord(2, 1)));
    }

    // A body is transit, never a destination: nothing may plan to finish its move standing in
    // somebody else's square, which is what an enemy asked to close on a target would otherwise do.
    [Fact]
    public void Trample_ATileSomebodyIsStandingOn_IsNeverOfferedAsADestination()
    {
        var state = Open(out var husk, out _);
        var turn = EnemyTurn(state);

        Assert.False(Movement.Reachable(turn, turn.Get(husk)).ContainsKey(new Coord(1, 1)));
    }

    // ---- the Husk is still the Husk ---------------------------------------------------------

    [Fact]
    public void Husk_KeepsTwoHitPointsAndStillDiesToOneBareCollision()
    {
        Assert.Equal(4, UnitTemplate.For(UnitKind.Husk).MaxHp);
        Assert.Equal(2, UnitTemplate.For(UnitKind.Husk).Damage);
        Assert.True(UnitTemplate.For(UnitKind.Husk).Tramples);

        var state = BoardBuilder.Rows("...#")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 2, 0)
            .Build();

        var target = state.Find(UnitKind.Husk).Id;
        var events = new System.Collections.Generic.List<GameEvent>();
        var after = Displacement.Resolve(
            state, target, new Coord(1, 0), DisplacementKind.Push, 1, false, events);

        Assert.Equal(Displacement.CollisionDamage, events.OfType<Collision>().Single().Damage);
        Assert.Contains(events, e => e is UnitDowned);
        Assert.False(after.Get(target).IsOnBoard);
    }

    [Fact]
    public void OnlyTheHusk_Tramples()
    {
        foreach (UnitKind kind in System.Enum.GetValues(typeof(UnitKind)))
        {
            if (kind == UnitKind.Husk)
            {
                continue;
            }

            Assert.False(UnitTemplate.For(kind).Tramples, kind.ToString());
        }
    }

    // ---- telegraphs ---------------------------------------------------------------------------

    // A hit that arrives with no warning is the thing intents exist to prevent, and a shoulder is a
    // hit. The plan carries the victim, the tile and the vector.
    [Fact]
    public void Trample_IsTelegraphedOnTheIntent()
    {
        // The body in the doorway is its own ally, so the Husk has a reason to walk past rather
        // than simply hit it: the player it is closing on is two tiles beyond.
        var state = BoardBuilder.Rows("....", "....", "....")
            .Enemy(UnitKind.Husk, 0, 1)
            .Enemy(UnitKind.Lobber, 1, 1)
            .PlayerA(UnitKind.Archer, 3, 1)
            .Build();

        var husk = state.Find(UnitKind.Husk);
        var blocker = state.Find(UnitKind.Lobber).Id;

        var intent = Ai.Declare(EnemyTurn(state), husk);

        Assert.Equal(blocker, intent.TrampleVictim);
        Assert.Equal(new Coord(1, 1), intent.TrampleAt);
        Assert.Equal(Direction.Up, intent.TrampleAside);
    }

    [Fact]
    public void APlanThatWalksRoundEverybody_TelegraphsNoTrample()
    {
        var state = BoardBuilder.Rows("....", "....", "....")
            .Enemy(UnitKind.Husk, 0, 1)
            .PlayerA(UnitKind.Archer, 3, 1)
            .Build();

        var intent = Ai.Declare(EnemyTurn(state), state.Find(UnitKind.Husk));

        Assert.Null(intent.TrampleVictim);
        Assert.Null(intent.TrampleAt);
    }

    // The overlay and the round-one damage guarantee both have to count the lanes, or the agency law
    // reads a Husk's reach as its attack alone and calls a deployment safe that is not (D-080).
    [Fact]
    public void TrampleLanes_AreThreatenedTiles()
    {
        var state = BoardBuilder.Rows("....", "....", "....")
            .Enemy(UnitKind.Husk, 0, 1)
            .PlayerA(UnitKind.Vanguard, 1, 1)
            .PlayerA(UnitKind.Archer, 3, 2)
            .Build();

        var threat = Threat.ForUnit(EnemyTurn(state), state.Find(UnitKind.Husk));

        Assert.Contains(new Coord(1, 1), threat);
    }

    [Fact]
    public void TrampleLanes_DoNotAppearForAnArchetypeThatDoesNotTrample()
    {
        var state = BoardBuilder.Rows("....", "....", "....")
            .Enemy(UnitKind.Anchor, 0, 1)
            .PlayerA(UnitKind.Vanguard, 1, 1)
            .PlayerA(UnitKind.Archer, 3, 2)
            .Build();

        var anchor = state.Find(UnitKind.Anchor);
        Assert.Empty(Trample.Lanes(state, anchor, new[] { anchor.Position }));
    }

    // ---- determinism -------------------------------------------------------------------------

    [Fact]
    public void Trample_ReplaysIdentically()
    {
        var state = Open(out var husk, out _);
        var log = new[] { (Command)new MoveCommand(husk, new Coord(2, 1)) };

        var first = TestPlay.Replay(state, log);
        var second = TestPlay.Replay(state, log);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    // ---- fixtures ----------------------------------------------------------------------------

    // Husk at (0,1) facing a Vanguard at (1,1) with both sides of the blocker open.
    private static GameState Open(out UnitId husk, out UnitId blocker)
    {
        var state = BoardBuilder.Rows("....", "....", "....")
            .Enemy(UnitKind.Husk, 0, 1)
            .PlayerA(UnitKind.Vanguard, 1, 1)
            .PlayerA(UnitKind.Archer, 3, 2)
            .Build();

        husk = state.Find(UnitKind.Husk).Id;
        blocker = state.Find(UnitKind.Vanguard).Id;
        return EnemyTurn(state);
    }

    // The same, with a drain on the side the blocker will be knocked toward.
    private static GameState Drain(out UnitId husk, out UnitId blocker)
    {
        var state = BoardBuilder.Rows(".O..", "....", "....")
            .Enemy(UnitKind.Husk, 0, 1)
            .Enemy(UnitKind.Lobber, 0, 0)
            .PlayerA(UnitKind.Vanguard, 1, 1)
            .PlayerA(UnitKind.Archer, 2, 0)
            .Build();

        husk = state.Find(UnitKind.Husk).Id;
        blocker = state.Find(UnitKind.Vanguard).Id;
        return EnemyTurn(state);
    }

    // A Husk at (0,1) with the named blocker in the doorway at (1,1), both sides of it open.
    private static GameState Blocked(UnitKind blocker, out UnitId husk)
    {
        var state = BoardBuilder.Rows("....", "....", "....")
            .Enemy(UnitKind.Husk, 0, 1)
            .PlayerB(blocker, 1, 1)
            .PlayerA(UnitKind.Archer, 3, 2)
            .Build();

        husk = state.Find(UnitKind.Husk).Id;
        return EnemyTurn(state);
    }

    // Hands the slot back to the players, for the half of a test that happens after the Husk walks.
    private static GameState PlayerTurn(GameState state)
    {
        foreach (var unit in state.Units.ToList())
        {
            state = state.WithUnit(state.Get(unit.Id) with { HasActivated = unit.Team == Team.Enemy });
        }

        return state with { ActiveTeam = Team.PlayerA, NextPlayerTeam = Team.PlayerA, ActiveUnitId = null };
    }

    // Hands the activation slot to the enemy side without playing a player turn first.
    private static GameState EnemyTurn(GameState state)
    {
        foreach (var unit in state.Units.ToList())
        {
            if (unit.Team != Team.Enemy)
            {
                state = state.WithUnit(state.Get(unit.Id) with { HasActivated = true });
            }
        }

        return state with { ActiveTeam = Team.Enemy, NextPlayerTeam = Team.PlayerA, ActiveUnitId = null };
    }
}
