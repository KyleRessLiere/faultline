using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// The displacement half of Brief §4, one named test per bullet, plus the edge cases CLAUDE.md
/// calls first-class.
/// </summary>
public class DisplacementTests
{
    // --- Collisions -------------------------------------------------------------------

    /// <summary>
    /// CLAUDE.md makes the push preview rules-critical UI sourced from Core, and the shell prints
    /// "it does not budge" whenever the preview calls itself a no-op. A unit with its back to a wall
    /// enters no tile, so reading an empty path as "nothing happened" described a collision for 2 —
    /// the most basic board play in the game — as an option not worth taking.
    /// </summary>
    [Fact]
    public void Preview_ShoveIntoAnAdjacentWall_IsNotANoOp()
    {
        var state = BoardBuilder.Rows("..#")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 12)
            .Build();

        var husk = state.Find(UnitKind.Husk);
        var preview = Displacement.Preview(state, husk.Id, new Coord(0, 0), DisplacementKind.Push, 1);

        Assert.Empty(preview.Path);
        Assert.Equal(DisplacementStop.Collision, preview.Stop);
        Assert.Equal(4, preview.DamageToUnit);
        Assert.True(preview.WouldStagger);
        Assert.False(preview.IsNoOp);
    }

    [Fact]
    public void Preview_ShoveIntoAUnitStandingDirectlyBehind_IsNotANoOp()
    {
        // The double kill this actually produces in first-contact: two Husks back to back, the
        // second with nowhere to go, both taking 2.
        var state = BoardBuilder.Open(3, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 4)
            .Enemy(UnitKind.Husk, 2, 0, hp: 4)
            .Build();

        var near = state.UnitAt(new Coord(1, 0))!;
        var preview = Displacement.Preview(state, near.Id, new Coord(0, 0), DisplacementKind.Push, 2);

        Assert.Empty(preview.Path);
        Assert.Equal(DisplacementStop.Collision, preview.Stop);
        Assert.Equal(4, preview.DamageToUnit);
        Assert.Equal(4, preview.DamageToObstacle);
        Assert.True(preview.WouldDown);
        Assert.False(preview.IsNoOp);
    }

    /// <summary>Resistance that eats the shove whole really is nothing, and still reports so.</summary>
    [Fact]
    public void Preview_ShoveNegatedOutright_IsStillANoOp()
    {
        var state = BoardBuilder.Open(4, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Anchor, 1, 0)
            .Build();

        var anchor = state.Find(UnitKind.Anchor);
        var preview = Displacement.Preview(state, anchor.Id, new Coord(0, 0), DisplacementKind.Push, 1);

        Assert.True(preview.IsNoOp);
    }

    [Fact]
    public void Push_IntoWall_MovesOne_DealsCollision_AndStaggers()
    {
        // Vanguard at 0, Husk at 1, wall at 3. Push 2 moves the Husk one tile, then it hits the wall.
        var state = BoardBuilder.Rows("...#")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 12)
            .Build();

        var husk = state.Find(UnitKind.Husk);
        var events = new System.Collections.Generic.List<GameEvent>();
        var after = Displacement.Resolve(state, husk.Id, new Coord(0, 0), DisplacementKind.Push, 2, false, events);

        var pushed = events.OfType<UnitPushed>().Single();
        Assert.Equal(new Coord(2, 0), pushed.To);
        Assert.Single(pushed.Path);

        var collision = events.OfType<Collision>().Single();
        Assert.Null(collision.ObstacleId);
        Assert.Equal(4, collision.Damage);

        var moved = after.Get(husk.Id);
        Assert.Equal(8, moved.Hp);
        Assert.True(moved.Staggered);
        Assert.Contains(events, e => e is Staggered);
    }

    [Fact]
    public void Push_IntoAnotherUnit_DamagesAndStaggersBoth()
    {
        var state = BoardBuilder.Rows("....")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 12)
            .Enemy(UnitKind.Anchor, 2, 0)
            .Build();

        var husk = state.Find(UnitKind.Husk);
        var anchor = state.Find(UnitKind.Anchor);
        var events = new System.Collections.Generic.List<GameEvent>();
        var after = Displacement.Resolve(state, husk.Id, new Coord(0, 0), DisplacementKind.Push, 1, false, events);

        var collision = events.OfType<Collision>().Single();
        Assert.Equal(anchor.Id, collision.ObstacleId);

        Assert.Equal(8, after.Get(husk.Id).Hp);
        Assert.Equal(8, after.Get(anchor.Id).Hp);
        Assert.True(after.Get(husk.Id).Staggered);
        Assert.True(after.Get(anchor.Id).Staggered);
        Assert.Equal(new Coord(1, 0), after.Get(husk.Id).Position);
    }

    [Fact]
    public void Push_IntoBoardEdge_CollidesRatherThanFalling()
    {
        var state = BoardBuilder.Rows("...")
            .PlayerA(UnitKind.Archer, 2, 0)
            .Enemy(UnitKind.Husk, 0, 0, hp: 12)
            .Build();

        var husk = state.Find(UnitKind.Husk);
        var events = new System.Collections.Generic.List<GameEvent>();
        var after = Displacement.Resolve(state, husk.Id, new Coord(2, 0), DisplacementKind.Push, 2, false, events);

        Assert.Single(events.OfType<Collision>());
        Assert.Equal(new Coord(0, 0), after.Get(husk.Id).Position);
        Assert.Equal(8, after.Get(husk.Id).Hp);
    }

    // --- Stagger ----------------------------------------------------------------------

    [Fact]
    public void Push_AgainstStaggeredTarget_GainsOneDistanceAndConsumesTheStagger()
    {
        var state = BoardBuilder.Rows("......")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 12)
            .Build();

        var husk = state.Find(UnitKind.Husk);
        state = state.WithUnit(state.Get(husk.Id) with { Staggered = true });

        var events = new System.Collections.Generic.List<GameEvent>();
        var after = Displacement.Resolve(state, husk.Id, new Coord(0, 0), DisplacementKind.Push, 1, false, events);

        Assert.Equal(new Coord(3, 0), after.Get(husk.Id).Position);
        Assert.Equal(2, events.OfType<UnitPushed>().Single().Distance);
        Assert.False(after.Get(husk.Id).Staggered);
    }

    [Fact]
    public void Stagger_ClearsAtEndOfRound()
    {
        var state = BoardBuilder.Open(6, 2)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 5, 0, hp: 12)
            .Build();

        var husk = state.Find(UnitKind.Husk);
        state = state.WithUnit(state.Get(husk.Id) with { Staggered = true });

        state = state.PassCurrent().NewState;
        state = state.PassCurrent().NewState;

        Assert.Equal(2, state.Round);
        Assert.False(state.Get(husk.Id).Staggered);
    }

    // --- Spikes -----------------------------------------------------------------------

    [Fact]
    public void Push_OntoSpikes_DealsThree_Stops_AndStaggers()
    {
        var state = BoardBuilder.Rows(".. ^.".Replace(" ", string.Empty))
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 12)
            .Build();

        var husk = state.Find(UnitKind.Husk);
        var events = new System.Collections.Generic.List<GameEvent>();
        var after = Displacement.Resolve(state, husk.Id, new Coord(0, 0), DisplacementKind.Push, 3, false, events);

        var spike = events.OfType<SpikeHit>().Single();
        Assert.Equal(6, spike.Damage);
        Assert.False(spike.Voluntary);

        Assert.Equal(new Coord(2, 0), after.Get(husk.Id).Position);
        Assert.Equal(6, after.Get(husk.Id).Hp);
        Assert.True(after.Get(husk.Id).Staggered);
    }

    [Fact]
    public void Walk_OntoSpikes_DealsOne_AndDoesNotStagger()
    {
        var state = BoardBuilder.Rows(".^.")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 2, 0)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        var result = state.Step(new MoveCommand(vanguard.Id, new Coord(1, 0)));

        var spike = result.Single<SpikeHit>();
        Assert.Equal(2, spike.Damage);
        Assert.True(spike.Voluntary);
        Assert.False(result.NewState.Get(vanguard.Id).Staggered);
    }

    // --- Pits, Clinging, rescue, Voided ------------------------------------------------

    [Fact]
    public void Push_IntoPit_LeavesTheUnitClinging()
    {
        var state = BoardBuilder.Rows("..O.")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0)
            .Build();

        var husk = state.Find(UnitKind.Husk);
        var events = new System.Collections.Generic.List<GameEvent>();
        var after = Displacement.Resolve(state, husk.Id, new Coord(0, 0), DisplacementKind.Push, 2, false, events);

        Assert.Single(events.OfType<Clinging>());
        var clinging = after.Get(husk.Id);
        Assert.True(clinging.Clinging);
        Assert.Equal(new Coord(2, 0), clinging.Position);
        Assert.True(clinging.IsAlive);
    }

    [Fact]
    public void Clinging_UnrescuedAfterOneRound_IsVoided()
    {
        var state = BoardBuilder.Rows(
                "..O..",
                ".....")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0)
            .Enemy(UnitKind.Anchor, 4, 1)
            .Build();

        var husk = state.Find(UnitKind.Husk);
        var events = new System.Collections.Generic.List<GameEvent>();
        state = Displacement.Resolve(state, husk.Id, new Coord(0, 0), DisplacementKind.Push, 2, false, events);
        Assert.True(state.Get(husk.Id).Clinging);

        // Play out rounds; the clinging Husk holds a slot but can never spend it.
        for (int i = 0; i < 12 && state.Get(husk.Id).IsAlive; i++)
        {
            state = state.PassCurrent().NewState;
        }

        Assert.True(state.Get(husk.Id).Voided);
        Assert.False(state.Get(husk.Id).IsAlive);
    }

    [Fact]
    public void Clinging_TakingAnyDamage_IsVoidedOutright()
    {
        var state = BoardBuilder.Rows("..O.")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0)
            .Enemy(UnitKind.Anchor, 3, 0)
            .Build();

        var husk = state.Find(UnitKind.Husk);
        var events = new System.Collections.Generic.List<GameEvent>();
        state = Displacement.Resolve(state, husk.Id, new Coord(0, 0), DisplacementKind.Push, 2, false, events);

        var damageEvents = new System.Collections.Generic.List<GameEvent>();
        state = Combat.ApplyDamage(state, husk.Id, 1, DamageSource.Attack, damageEvents);

        Assert.Single(damageEvents.OfType<Voided>());
        Assert.True(state.Get(husk.Id).Voided);
    }

    [Fact]
    public void Clinging_AdjacentAllySpendingItsWholeActivation_Rescues()
    {
        var state = BoardBuilder.Rows(
                ".O..",
                "....")
            .PlayerA(UnitKind.Vanguard, 1, 1)
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 3, 1)
            .Build();

        var archer = state.Find(UnitKind.Archer);
        var vanguard = state.Find(UnitKind.Vanguard);

        // Drop the Archer into the pit at (1,0), directly above the Vanguard.
        var events = new System.Collections.Generic.List<GameEvent>();
        state = Displacement.Resolve(state, archer.Id, new Coord(-1, 0), DisplacementKind.Push, 1, false, events);
        Assert.True(state.Get(archer.Id).Clinging);

        TestPlay.AssertLegal(state, state.Rescue(vanguard.Id, archer.Id));
        var result = state.Step(state.Rescue(vanguard.Id, archer.Id));

        var rescued = result.Single<Rescued>();
        Assert.Equal(archer.Id, rescued.UnitId);
        Assert.False(result.NewState.Get(archer.Id).Clinging);
        Assert.True(result.NewState.Get(archer.Id).Position.IsAdjacentTo(vanguard.Position));

        // D-082 made a rescue the action half rather than the whole activation, and D-097 then
        // made every action close the move half — so a rescuer who had not walked yet does not get to.
        Assert.True(result.NewState.Get(vanguard.Id).HasActivated);
    }

    [Fact]
    public void Clinging_UnitCannotBeActivated()
    {
        var state = BoardBuilder.Rows(
                ".O..",
                "....")
            .PlayerA(UnitKind.Archer, 0, 0)
            .PlayerA(UnitKind.Vanguard, 0, 1)
            .Enemy(UnitKind.Husk, 3, 1)
            .Build();

        var archer = state.Find(UnitKind.Archer);
        var events = new System.Collections.Generic.List<GameEvent>();
        state = Displacement.Resolve(state, archer.Id, new Coord(-1, 0), DisplacementKind.Push, 1, false, events);

        TestPlay.AssertIllegal(state, new EndActivationCommand(archer.Id));
        Assert.DoesNotContain(Game.LegalCommands(state), c => c is EndActivationCommand e && e.UnitId == archer.Id);
    }

    [Fact]
    public void Clinging_AdjacentEnemyFinishesItAsAFreeAction()
    {
        var state = BoardBuilder.Rows(
                ".O..",
                "....")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 1, 1)
            .Enemy(UnitKind.Anchor, 3, 1)
            .Build();

        var husk = state.Find(UnitKind.Husk);
        var vanguard = state.Find(UnitKind.Vanguard);

        var events = new System.Collections.Generic.List<GameEvent>();
        state = Displacement.Resolve(state, husk.Id, new Coord(1, 2), DisplacementKind.Push, 1, false, events);
        Assert.True(state.Get(husk.Id).Clinging);

        var result = state.Step(new FinishClingingCommand(vanguard.Id, husk.Id));

        Assert.Single(result.All<Voided>());
        Assert.True(result.NewState.Get(husk.Id).Voided);

        // Free action: the Vanguard still has both halves of its activation.
        Assert.False(result.NewState.Get(vanguard.Id).HasActivated);
    }

    // --- Anchor -----------------------------------------------------------------------

    [Fact]
    public void Anchor_IgnoresPushOne()
    {
        var state = AnchorBoard();
        var anchor = state.Find(UnitKind.Anchor);

        var events = new System.Collections.Generic.List<GameEvent>();
        var after = Displacement.Resolve(state, anchor.Id, new Coord(0, 0), DisplacementKind.Push, 1, false, events);

        Assert.Equal(anchor.Position, after.Get(anchor.Id).Position);

        // Push 1 minus the Anchor's resistance of 1 is nothing. The event still fires and its
        // distance is 0, which is what lets a renderer shudder it and a log say it did not budge.
        var reported = Assert.Single(events.OfType<UnitPushed>());
        Assert.Empty(reported.Path);
        Assert.Equal(0, reported.Distance);
    }

    [Fact]
    public void Anchor_IsMovedByPushTwo()
    {
        var state = AnchorBoard();
        var anchor = state.Find(UnitKind.Anchor);

        var events = new System.Collections.Generic.List<GameEvent>();
        var after = Displacement.Resolve(state, anchor.Id, new Coord(0, 0), DisplacementKind.Push, 2, false, events);

        Assert.Equal(new Coord(3, 0), after.Get(anchor.Id).Position);
    }

    [Fact]
    public void Anchor_StaggeredAndPushedOne_MovesOne()
    {
        var state = AnchorBoard();
        var anchor = state.Find(UnitKind.Anchor);
        state = state.WithUnit(state.Get(anchor.Id) with { Staggered = true });

        var events = new System.Collections.Generic.List<GameEvent>();
        var after = Displacement.Resolve(state, anchor.Id, new Coord(0, 0), DisplacementKind.Push, 1, false, events);

        Assert.Equal(new Coord(3, 0), after.Get(anchor.Id).Position);
        Assert.False(after.Get(anchor.Id).Staggered);
    }

    // Was Anchor_IsPulledNormally, which pinned D-018's "Pull is untouched". MASTER_DESIGN §3 runs
    // Push and Pull through one arithmetic, so the Anchor's tile comes off a drag too (D-139).
    [Fact]
    public void Anchor_ShrugsOffTheSameTileOfAPullAsOfAPush()
    {
        var state = AnchorBoard();
        var anchor = state.Find(UnitKind.Anchor);

        var events = new System.Collections.Generic.List<GameEvent>();
        var after = Displacement.Resolve(state, anchor.Id, new Coord(0, 0), DisplacementKind.Pull, 1, false, events);

        Assert.Equal(anchor.Position, after.Get(anchor.Id).Position);
        Assert.Equal(0, Assert.Single(events.OfType<UnitPushed>()).Distance);

        var pulledTwo = Displacement.Resolve(
            state, anchor.Id, new Coord(0, 0), DisplacementKind.Pull, 2, false,
            new System.Collections.Generic.List<GameEvent>());

        Assert.Equal(new Coord(1, 0), pulledTwo.Get(anchor.Id).Position);
    }

    // --- Hold auras and Footing ---------------------------------------------------------
    //
    // These three used to be arranged around a Wardbearer. D-058 deleted its aura, so the fixtures
    // are now built around the Bulwark, which keeps it. The rules being asserted are unchanged: the
    // cap is still 1, it still stacks with Footing down to 0, and it still only covers allies.

    [Fact]
    public void Hold_CapsAdjacentAllyDisplacementAtOne()
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0)
            .Enemy(UnitKind.Bulwark, 2, 0)
            .Build();

        var husk = state.Find(UnitKind.Husk);
        Assert.True(Displacement.HasHold(state, state.Get(husk.Id)));

        int distance = Displacement.EffectiveDistance(
            state, state.Get(husk.Id), DisplacementKind.Push, 3, false, out _);

        Assert.Equal(1, distance);
    }

    [Fact]
    public void Hold_AndFooting_StackDownToZero()
    {
        // The Husk's token is granted by this fixture: Footing is scenario-granted, not automatic.
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, footing: 1)
            .Enemy(UnitKind.Bulwark, 2, 0)
            .Build();

        var husk = state.Get(state.Find(UnitKind.Husk).Id);

        int distance = Displacement.EffectiveDistance(
            state, husk, DisplacementKind.Push, 3, true, out _);

        Assert.Equal(0, distance);
    }

    [Fact]
    public void Hold_DoesNotProtectTheOtherSide()
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Archer, 2, 0)
            .Enemy(UnitKind.Bulwark, 3, 0)
            .Build();

        Assert.False(Displacement.HasHold(state, state.Find(UnitKind.Archer)));
    }

    // D-058: the Wardbearer used to carry the identical aura and no longer does. Standing next to
    // one is now worth nothing at all to a shove — the protection it offers has to be declared.
    [Fact]
    public void Hold_TheWardbearerNoLongerConfersIt()
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Archer, 1, 0)
            .PlayerB(UnitKind.Wardbearer, 2, 0)
            .Enemy(UnitKind.Husk, 5, 0)
            .Build();

        var archer = state.Get(state.Find(UnitKind.Archer).Id);

        Assert.False(UnitTemplate.For(UnitKind.Wardbearer).HoldAura);
        Assert.False(Displacement.HasHold(state, archer));
        Assert.Equal(
            3, Displacement.EffectiveDistance(state, archer, DisplacementKind.Push, 3, false, out _));
    }

    [Fact]
    public void EnemyFooting_IsSpentOnlyWhenTheDisplacementWouldEndInAPit()
    {
        // Pit two tiles along, so giving up one tile of travel is what keeps the Husk out of it. The
        // token is granted by the fixture — no archetype starts a fight holding one.
        var pitBoard = BoardBuilder.Rows("...O.")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, footing: 1)
            .Build();
        var husk = pitBoard.Find(UnitKind.Husk);

        Assert.True(Displacement.EnemyWouldSpendFooting(
            pitBoard, husk.Id, new Coord(0, 0), DisplacementKind.Push, 2));

        var openBoard = BoardBuilder.Rows(".....")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, footing: 1)
            .Build();

        Assert.False(Displacement.EnemyWouldSpendFooting(
            openBoard, openBoard.Find(UnitKind.Husk).Id, new Coord(0, 0), DisplacementKind.Push, 2));
    }

    [Fact]
    public void EnemyFooting_IsNotSpentWhenItCannotAvoidThePitAnyway()
    {
        // Pit immediately adjacent: shortening the shove by one changes nothing, so the token this
        // fixture granted is kept.
        var state = BoardBuilder.Rows("..O.")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, footing: 1)
            .Build();

        Assert.False(Displacement.EnemyWouldSpendFooting(
            state, state.Find(UnitKind.Husk).Id, new Coord(0, 0), DisplacementKind.Push, 2));
    }

    [Fact]
    public void EnemyFooting_SpendingIt_KeepsTheUnitOutOfThePit()
    {
        var state = BoardBuilder.Rows("...O.")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, footing: 1)
            .Build();

        var husk = state.Find(UnitKind.Husk);
        var events = new System.Collections.Generic.List<GameEvent>();
        var after = Displacement.ResolveAuto(
            state, husk.Id, new Coord(0, 0), DisplacementKind.Push, 2, events);

        Assert.False(after.Get(husk.Id).Clinging);
        Assert.Equal(new Coord(2, 0), after.Get(husk.Id).Position);
        Assert.Equal(0, after.Get(husk.Id).Footing);
        Assert.Single(events.OfType<FootingSpent>());
    }

    [Fact]
    public void EnemyFooting_OnceSpent_TheNextShoveGoesInThePit()
    {
        var state = BoardBuilder.Rows("...O.")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, footing: 0)
            .Build();

        var husk = state.Find(UnitKind.Husk);
        var events = new System.Collections.Generic.List<GameEvent>();
        var after = Displacement.ResolveAuto(
            state, husk.Id, new Coord(0, 0), DisplacementKind.Push, 2, events);

        Assert.True(after.Get(husk.Id).Clinging);
        Assert.Empty(events.OfType<FootingSpent>());
    }

    // --- HighGround --------------------------------------------------------------------

    [Fact]
    public void HighGround_CannotBePushedUpOnto_TheLedgeCollides()
    {
        var state = BoardBuilder.Rows("..H.")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 12)
            .Build();

        var husk = state.Find(UnitKind.Husk);
        var events = new System.Collections.Generic.List<GameEvent>();
        var after = Displacement.Resolve(state, husk.Id, new Coord(0, 0), DisplacementKind.Push, 2, false, events);

        Assert.Single(events.OfType<Collision>());
        Assert.Equal(new Coord(1, 0), after.Get(husk.Id).Position);
        Assert.Equal(8, after.Get(husk.Id).Hp);
    }

    [Fact]
    public void HighGround_PushedDownOff_TakesOneAndKeepsGoing()
    {
        var state = BoardBuilder.Rows("H....")
            .PlayerA(UnitKind.Archer, 4, 0)
            .Enemy(UnitKind.Husk, 0, 0, hp: 12)
            .Build();

        var husk = state.Find(UnitKind.Husk);
        var events = new System.Collections.Generic.List<GameEvent>();

        // Pulled from (0,0) toward the Archer: leaves the ledge, takes 1, and keeps travelling.
        var after = Displacement.Resolve(state, husk.Id, new Coord(4, 0), DisplacementKind.Pull, 2, false, events);

        Assert.Equal(new Coord(2, 0), after.Get(husk.Id).Position);
        Assert.Equal(10, after.Get(husk.Id).Hp);
        Assert.Contains(events, e => e is UnitDamaged d && d.Source == DamageSource.Fall);
        Assert.False(after.Get(husk.Id).Staggered);
    }

    // --- Edge cases CLAUDE.md asks for -------------------------------------------------

    [Fact]
    public void Displacement_OfZeroDistance_DoesNothing()
    {
        var state = BoardBuilder.Open(4, 1)
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0)
            .Build();

        var husk = state.Find(UnitKind.Husk);
        var events = new System.Collections.Generic.List<GameEvent>();
        var after = Displacement.Resolve(state, husk.Id, new Coord(0, 0), DisplacementKind.Push, 0, false, events);

        Assert.Equal(husk.Position, after.Get(husk.Id).Position);

        // The shove is reported even though it moved nothing, with the effective distance saying
        // why (D-057). Nothing moved is the rule; silence was only ever how it was implemented.
        var reported = Assert.Single(events.OfType<UnitPushed>());
        Assert.Empty(reported.Path);
        Assert.Equal(0, reported.Distance);
        Assert.Equal(husk.Position, reported.To);
    }

    [Fact]
    public void Displacement_ThatDownsTheTarget_StopsAndRemovesIt()
    {
        var state = BoardBuilder.Rows("...#")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 4)
            .Enemy(UnitKind.Anchor, 0, 0)
            .Active(Team.PlayerA)
            .Build();

        var husk = state.Find(UnitKind.Husk);
        var events = new System.Collections.Generic.List<GameEvent>();
        var after = Displacement.Resolve(state, husk.Id, new Coord(0, 0), DisplacementKind.Push, 2, false, events);

        Assert.Single(events.OfType<UnitDowned>());
        Assert.False(after.Get(husk.Id).IsOnBoard);
    }

    [Fact]
    public void Displacement_FromTheSameTile_HasNoDefinedDirectionAndDoesNothing()
    {
        var state = BoardBuilder.Open(3, 1)
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0)
            .Build();

        var husk = state.Find(UnitKind.Husk);
        var events = new System.Collections.Generic.List<GameEvent>();
        var after = Displacement.Resolve(state, husk.Id, husk.Position, DisplacementKind.Push, 2, events: events, spendFooting: false);

        Assert.Equal(husk.Position, after.Get(husk.Id).Position);
        Assert.Empty(events);
    }

    private static GameState AnchorBoard() =>
        BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Anchor, 2, 0)
            .Build();

    [Fact]
    public void Displacement_ThatMovesNothing_IsStillReportedWithTheReasonInItsDistance()
    {
        // A shove turned aside is a result, not a non-event. It is what a renderer shudders on and
        // what a combat log needs to say "it did not budge" — and it was invisible until D-057,
        // which is why first-contact's marquee shove read as nothing happening at all.
        var state = BoardBuilder.Open(4, 1)
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Anchor, 1, 0)
            .Build();

        var anchor = state.Find(UnitKind.Anchor);
        var events = new System.Collections.Generic.List<GameEvent>();

        Displacement.Resolve(state, anchor.Id, new Coord(0, 0), DisplacementKind.Push, 1, false, events);

        var shove = Assert.Single(events.OfType<UnitPushed>());
        Assert.Equal(anchor.Position, shove.From);
        Assert.Equal(anchor.Position, shove.To);
        Assert.Empty(shove.Path);
        Assert.Equal(0, shove.Distance);
        Assert.Equal(DisplacementKind.Push, shove.Kind);
    }

    [Fact]
    public void Displacement_ThatMoves_StillReportsItsPathAndDistance()
    {
        // The other half: reporting the nothing case must not have blurred the ordinary one.
        var state = BoardBuilder.Open(4, 1)
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0)
            .Build();

        var husk = state.Find(UnitKind.Husk);
        var events = new System.Collections.Generic.List<GameEvent>();

        Displacement.Resolve(state, husk.Id, new Coord(0, 0), DisplacementKind.Push, 2, false, events);

        var shove = Assert.Single(events.OfType<UnitPushed>());
        Assert.Equal(new Coord(1, 0), shove.From);
        Assert.Equal(new Coord(3, 0), shove.To);
        Assert.Equal(2, shove.Path.Count);
        Assert.Equal(2, shove.Distance);
    }

}
