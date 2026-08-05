using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// The ten enemy variants from <c>docs/ENEMY_ROSTER.md</c>: one test per branch of every new
/// priority list, plus the single thing each variant exists to prove. The balance variants get the
/// proof that they share their archetype's list rather than a copy of it, because a copy is the
/// failure mode the roster doc names explicitly.
/// </summary>
public class EnemyVariantTests
{
    // ================================================================================
    // Warden — HP 6, Move 0, melee 2, two negating Footing tokens. The door you break down.
    // ================================================================================

    // Priority 1: hit whatever is adjacent.
    [Fact]
    public void Warden_AdjacentPlayerUnit_AttacksForTwoWithoutMoving()
    {
        var state = BoardBuilder.Open(5, 1)
            .PlayerA(UnitKind.Vanguard, 1, 0)
            .Enemy(UnitKind.Warden, 2, 0)
            .Active(Team.Enemy)
            .Build();

        var warden = state.Find(UnitKind.Warden);
        var vanguard = state.Find(UnitKind.Vanguard);

        var intent = Ai.Declare(state, warden);
        Assert.Equal(IntentAction.Attack, intent.Action);
        Assert.Equal(vanguard.Id, intent.TargetId);
        Assert.Null(intent.MoveTo);
        Assert.Equal(4, intent.Damage);

        Assert.Equal(new AttackCommand(warden.Id, vanguard.Id), Ai.Plan(state, warden));

        var result = state.Step(Ai.Plan(state, warden));
        Assert.Equal(10, result.NewState.Get(vanguard.Id).Hp);
        Assert.Equal(new Coord(2, 0), result.NewState.Get(warden.Id).Position);
    }

    // Priority 2: with nothing in reach it holds — there is no closing branch at all.
    [Fact]
    public void Warden_TargetAcrossTheBoard_HoldsAndDoesNotMove()
    {
        var state = BoardBuilder.Open(7, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Warden, 6, 0)
            .Active(Team.Enemy)
            .Build();

        var warden = state.Find(UnitKind.Warden);

        var intent = Ai.Declare(state, warden);
        Assert.Equal(IntentAction.Hold, intent.Action);
        Assert.Null(intent.MoveTo);

        Assert.Equal(new EndActivationCommand(warden.Id), Ai.Plan(state, warden));

        var result = state.Step(Ai.Plan(state, warden));
        Assert.Equal(new Coord(6, 0), result.NewState.Get(warden.Id).Position);
    }

    // Move 0 is the risk the roster doc flags: the planner must not offer a move it cannot make.
    [Fact]
    public void Warden_HasNoReachableTilesAndNoLegalMoveCommand()
    {
        var state = BoardBuilder.Open(5, 3)
            .Enemy(UnitKind.Warden, 2, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Active(Team.Enemy)
            .Build();

        var warden = state.Find(UnitKind.Warden);

        Assert.Empty(Movement.Reachable(state, warden));
        Assert.DoesNotContain(Game.LegalCommands(state), c => c is MoveCommand);
        Assert.Contains(new EndActivationCommand(warden.Id), Game.LegalCommands(state));
    }

    // ...and the activation loop must let go of it, or the round never ends.
    [Fact]
    public void Warden_WithNothingToDo_DoesNotStallTheRound()
    {
        var state = BoardBuilder.Open(7, 3)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .PlayerB(UnitKind.Archer, 0, 2)
            .Enemy(UnitKind.Warden, 6, 0)
            .Active(Team.Enemy)
            .Build();

        var warden = state.Find(UnitKind.Warden);

        int commands = 0;
        while (Game.IsEnemyTurn(state) && commands < 20)
        {
            var command = Game.NextEnemyCommand(state);
            Assert.NotNull(command);
            state = state.Then(command!);
            commands++;
        }

        Assert.Equal(1, commands);
        Assert.False(Game.IsEnemyTurn(state));
        Assert.True(state.Get(warden.Id).HasActivated);
        Assert.Equal(new Coord(6, 0), state.Get(warden.Id).Position);
    }

    [Fact]
    // D-102: no longer a permanent one-tile shrug. While its two tokens stand, nothing moves it at
    // all — and once they are gone it is an ordinary Move 0 body you shove out of the lane.
    public void Warden_HoldsTwoRefusals_WhichAreNotArithmetic()
    {
        var state = BoardBuilder.Open(7, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Warden, 3, 0)
            .Build();

        var warden = state.Find(UnitKind.Warden);

        Assert.Equal(2, warden.Footing);
        Assert.Equal(0, warden.Template.PushResistance);

        // Footing left the arithmetic (D-143): the distance is the distance, and the tokens buy two
        // whole refusals instead of shortening anything.
        foreach (int distance in new[] { 1, 2, 3 })
        {
            Assert.Equal(
                distance,
                Displacement.EffectiveDistance(state, warden, DisplacementKind.Push, distance, out _));
            Assert.Equal(
                distance,
                Displacement.EffectiveDistance(state, warden, DisplacementKind.Pull, distance, out _));
        }

        var events = new System.Collections.Generic.List<GameEvent>();
        var after = Displacement.Resolve(
            state, warden.Id, new Coord(0, 0), DisplacementKind.Push, 3, refused: true, events: events);

        Assert.Equal(warden.Position, after.Get(warden.Id).Position);
        Assert.Equal(1, after.Get(warden.Id).Footing);
    }

    // ================================================================================
    // Perch — HP 3, Move 2, range 3, 1 dmg. Takes the high ground and fires at +1.
    // ================================================================================

    // Priority 1: shoot from where it stands, +1 when that is a ledge.
    [Fact]
    public void Perch_OnHighGround_ShootsForOneMoreThanOnTheFlat()
    {
        var state = BoardBuilder.Rows("H...")
            .PlayerA(UnitKind.Vanguard, 3, 0)
            .Enemy(UnitKind.Perch, 0, 0)
            .Active(Team.Enemy)
            .Build();

        var perch = state.Find(UnitKind.Perch);
        var vanguard = state.Find(UnitKind.Vanguard);

        var intent = Ai.Declare(state, perch);
        Assert.Equal(IntentAction.Attack, intent.Action);
        Assert.Null(intent.MoveTo);
        Assert.Equal(4, intent.Damage);
        Assert.Equal(2, UnitTemplate.For(UnitKind.Perch).Damage);

        var result = state.Step(Ai.Plan(state, perch));
        Assert.Equal(vanguard.Hp - intent.Damage, result.NewState.Get(vanguard.Id).Hp);
    }

    // Priority 2: contact breaks the whole plan, exactly as it does for a Lobber.
    [Fact]
    public void Perch_PlayerAdjacent_BreaksContactBeforeShooting()
    {
        var state = BoardBuilder.Open(7, 1)
            .PlayerA(UnitKind.Vanguard, 2, 0)
            .Enemy(UnitKind.Perch, 3, 0)
            .Active(Team.Enemy)
            .Build();

        var perch = state.Find(UnitKind.Perch);

        var intent = Ai.Declare(state, perch);
        Assert.Equal(new Coord(5, 0), intent.MoveTo);
        Assert.NotEqual(IntentAction.Hold, intent.Action);
        Assert.False(intent.MoveTo!.Value.IsAdjacentTo(new Coord(2, 0)));
    }

    // Priority 3: climb to the nearest ledge it can route to.
    [Fact]
    public void Perch_NothingInRange_WalksTowardTheNearestHighGround()
    {
        var state = BoardBuilder.Rows("H......")
            .PlayerA(UnitKind.Vanguard, 6, 0)
            .Enemy(UnitKind.Perch, 2, 0)
            .Active(Team.Enemy)
            .Build();

        var perch = state.Find(UnitKind.Perch);

        var intent = Ai.Declare(state, perch);
        Assert.Equal(IntentAction.Advance, intent.Action);
        Assert.Equal(new Coord(1, 0), intent.MoveTo);

        state = state.Then(Ai.Plan(state, perch));
        Assert.Equal(new Coord(1, 0), state.Get(perch.Id).Position);
    }

    [Fact]
    public void Perch_OneTileFromTheLedge_SpendsItsWholeMoveClimbing()
    {
        var state = BoardBuilder.Rows("H......")
            .PlayerA(UnitKind.Vanguard, 6, 0)
            .Enemy(UnitKind.Perch, 1, 0)
            .Active(Team.Enemy)
            .Build();

        var perch = state.Find(UnitKind.Perch);

        Assert.Equal(new Coord(0, 0), Ai.Declare(state, perch).MoveTo);

        state = state.Then(Ai.Plan(state, perch));
        Assert.Equal(new Coord(0, 0), state.Get(perch.Id).Position);
        Assert.Equal(TileType.HighGround, state.Board.At(state.Get(perch.Id).Position));
    }

    // Priority 4a: on a ledge with nothing in range it holds. It does not come down.
    [Fact]
    public void Perch_OnHighGroundWithNothingInRange_HoldsTheLedge()
    {
        var state = BoardBuilder.Rows("H......")
            .PlayerA(UnitKind.Vanguard, 6, 0)
            .Enemy(UnitKind.Perch, 0, 0)
            .Active(Team.Enemy)
            .Build();

        var perch = state.Find(UnitKind.Perch);

        Assert.Equal(IntentAction.Hold, Ai.Declare(state, perch).Action);
        Assert.Equal(new EndActivationCommand(perch.Id), Ai.Plan(state, perch));
    }

    // Priority 4b: with no ledge anywhere it is a Lobber, and plans the identical move.
    [Fact]
    public void Perch_NoHighGroundOnTheBoard_PlansExactlyAsALobberDoes()
    {
        var perchState = BoardBuilder.Open(9, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Perch, 8, 0)
            .Active(Team.Enemy)
            .Build();

        var lobberState = BoardBuilder.Open(9, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Lobber, 8, 0)
            .Active(Team.Enemy)
            .Build();

        var perch = Ai.Declare(perchState, perchState.Find(UnitKind.Perch));
        var lobber = Ai.Declare(lobberState, lobberState.Find(UnitKind.Lobber));

        Assert.Equal(IntentAction.Advance, perch.Action);
        Assert.Equal(lobber.Action, perch.Action);
        Assert.Equal(lobber.MoveTo, perch.MoveTo);
    }

    // ================================================================================
    // Bulwark — HP 5, Move 2, melee 1, hold aura. The enemy Wardbearer.
    // ================================================================================

    // Priority 1: the Husk's list. Adjacent → attack.
    [Fact]
    public void Bulwark_AdjacentPlayerUnit_AttacksForOne()
    {
        var state = BoardBuilder.Open(5, 1)
            .PlayerA(UnitKind.Vanguard, 1, 0)
            .Enemy(UnitKind.Bulwark, 2, 0)
            .Active(Team.Enemy)
            .Build();

        var bulwark = state.Find(UnitKind.Bulwark);
        var vanguard = state.Find(UnitKind.Vanguard);

        var intent = Ai.Declare(state, bulwark);
        Assert.Equal(IntentAction.Attack, intent.Action);
        Assert.Equal(2, intent.Damage);

        var result = state.Step(Ai.Plan(state, bulwark));
        Assert.Equal(12, result.NewState.Get(vanguard.Id).Hp);
    }

    // Priority 2: else close, two tiles at a time.
    [Fact]
    public void Bulwark_NobodyAdjacent_ClosesTwoTiles()
    {
        var state = BoardBuilder.Open(9, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Bulwark, 8, 0)
            .Active(Team.Enemy)
            .Build();

        var bulwark = state.Find(UnitKind.Bulwark);

        var intent = Ai.Declare(state, bulwark);
        Assert.Equal(IntentAction.Advance, intent.Action);
        Assert.Equal(new Coord(6, 0), intent.MoveTo);
    }

    // The thing it exists to prove.
    [Fact]
    public void Bulwark_AnAdjacentAllysDisplacement_IsCappedAtOneTile()
    {
        var state = HuskBesideABulwark();
        var husk = state.Find(UnitKind.Husk);
        var vanguard = state.Find(UnitKind.Vanguard);

        Assert.True(Displacement.HasHold(state, husk));
        Assert.Equal(1, Displacement.EffectiveDistance(state, husk, DisplacementKind.Push, 2, out _));

        var result = state.Step(new AbilityCommand(vanguard.Id, Ability.BullRush, null, Direction.Right));

        // Push 2 becomes 1: it stops one tile short and never reaches the collision.
        Assert.Equal(new Coord(3, 0), result.NewState.Get(husk.Id).Position);
        Assert.False(result.Has<Collision>());
    }

    [Fact]
    public void Bulwark_Downed_RestoresTheFullShove()
    {
        var state = HuskBesideABulwark();
        var bulwark = state.Find(UnitKind.Bulwark);
        var husk = state.Find(UnitKind.Husk);
        var vanguard = state.Find(UnitKind.Vanguard);

        state = state.WithUnit(state.Get(bulwark.Id) with { Hp = 0, IsDeployed = false });

        Assert.False(Displacement.HasHold(state, state.Get(husk.Id)));
        Assert.Equal(
            2, Displacement.EffectiveDistance(state, state.Get(husk.Id), DisplacementKind.Push, 2, out _));

        var result = state.Step(new AbilityCommand(vanguard.Id, Ability.BullRush, null, Direction.Right));

        Assert.Equal(new Coord(4, 0), result.NewState.Get(husk.Id).Position);
    }

    [Fact]
    public void Bulwark_DoesNotProtectItself()
    {
        var state = HuskBesideABulwark();
        var bulwark = state.Find(UnitKind.Bulwark);

        Assert.False(Displacement.HasHold(state, state.Get(bulwark.Id)));
        Assert.Equal(
            2, Displacement.EffectiveDistance(state, state.Get(bulwark.Id), DisplacementKind.Push, 2, out _));
    }

    [Fact]
    public void Bulwark_DoesNotProtectThePlayerUnitsItStandsNextTo()
    {
        var state = BoardBuilder.Open(8, 2)
            .PlayerA(UnitKind.Vanguard, 2, 0)
            .Enemy(UnitKind.Bulwark, 2, 1)
            .Build();

        Assert.False(Displacement.HasHold(state, state.Find(UnitKind.Vanguard)));
    }

    // Hold caps distance, never damage: a shove of exactly 1 into a body still collides for 2.
    [Fact]
    public void Bulwark_DoesNotStopACollisionThatOnlyNeededOneTile()
    {
        var state = BoardBuilder.Open(8, 2)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 12)
            .Enemy(UnitKind.Bulwark, 1, 1)
            .Enemy(UnitKind.Anchor, 2, 0)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        var husk = state.Find(UnitKind.Husk);

        var result = state.Step(new AbilityCommand(vanguard.Id, Ability.BullRush, null, Direction.Right));

        Assert.True(result.Has<Collision>());
        Assert.Equal(8, result.NewState.Get(husk.Id).Hp);
    }

    private static GameState HuskBesideABulwark() =>
        BoardBuilder.Open(8, 2)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 2, 0, hp: 12)
            .Enemy(UnitKind.Bulwark, 2, 1)
            .Build();

    // ================================================================================
    // Harrier — HP 4, Move 4, push 1 range 1. Displacement without hazards.
    // ================================================================================

    // Priority 1: flank and shove a target away from its own side.
    [Fact]
    public void Harrier_ShovesATargetAwayFromItsNearestAlly()
    {
        var state = BoardBuilder.Open(7, 2)
            .PlayerA(UnitKind.Archer, 0, 0)
            .PlayerA(UnitKind.Vanguard, 2, 0)
            .Enemy(UnitKind.Harrier, 1, 1)
            .Active(Team.Enemy)
            .Build();

        var harrier = state.Find(UnitKind.Harrier);
        var vanguard = state.Find(UnitKind.Vanguard);

        var intent = Ai.Declare(state, harrier);

        Assert.Equal(IntentAction.Push, intent.Action);
        Assert.Equal(vanguard.Id, intent.TargetId);
        Assert.Equal(new Coord(1, 0), intent.MoveTo);
        Assert.Equal(Direction.Right, intent.DisplacementDirection);
        Assert.Equal(new Coord(3, 0), intent.DisplacementTo);

        // Two tiles from its Archer before, three after.
        state = state.Then(Ai.Plan(state, harrier));
        state = state.Then(Ai.Plan(state, state.Get(harrier.Id)));
        Assert.Equal(new Coord(3, 0), state.Get(vanguard.Id).Position);
    }

    // Priority 2: a shove that does not move the target is worth nothing and is never taken. This
    // is the whole difference from the Stalker, so it is asserted against one on the same board.
    [Fact]
    public void Harrier_WillNotShoveIntoTheBoardEdge_WhereAStalkerWould()
    {
        var harrierState = BoardBuilder.Open(5, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Harrier, 2, 0)
            .Active(Team.Enemy)
            .Build();

        var stalkerState = BoardBuilder.Open(5, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Stalker, 2, 0)
            .Active(Team.Enemy)
            .Build();

        Assert.Equal(
            IntentAction.Push,
            Ai.Declare(stalkerState, stalkerState.Find(UnitKind.Stalker)).Action);

        Assert.Equal(
            IntentAction.Advance,
            Ai.Declare(harrierState, harrierState.Find(UnitKind.Harrier)).Action);
    }

    // Priority 3: nothing to separate, so it closes.
    [Fact]
    public void Harrier_NoSeparatingShoveAvailable_ClosesOnTheNearest()
    {
        var state = BoardBuilder.Open(9, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .PlayerA(UnitKind.Archer, 1, 0)
            .Enemy(UnitKind.Harrier, 8, 0)
            .Active(Team.Enemy)
            .Build();

        var intent = Ai.Declare(state, state.Find(UnitKind.Harrier));

        Assert.Equal(IntentAction.Advance, intent.Action);
        Assert.Equal(new Coord(4, 0), intent.MoveTo);
    }

    [Fact]
    public void Harrier_ATargetWithNoAlliesLeft_IsStillShoved()
    {
        var state = BoardBuilder.Open(5, 3)
            .PlayerA(UnitKind.Vanguard, 2, 1)
            .Enemy(UnitKind.Harrier, 0, 1)
            .Active(Team.Enemy)
            .Build();

        var intent = Ai.Declare(state, state.Find(UnitKind.Harrier));

        Assert.Equal(IntentAction.Push, intent.Action);
        Assert.Equal(new Coord(2, 0), intent.DisplacementTo);
    }

    // ================================================================================
    // Runt — HP 1, Move 4, melee 1. Swarm.
    // ================================================================================

    [Fact]
    public void Runt_AdjacentPlayerUnit_AttacksForOne()
    {
        var state = BoardBuilder.Open(5, 1)
            .PlayerA(UnitKind.Vanguard, 1, 0)
            .Enemy(UnitKind.Runt, 2, 0)
            .Active(Team.Enemy)
            .Build();

        var runt = state.Find(UnitKind.Runt);
        var vanguard = state.Find(UnitKind.Vanguard);

        Assert.Equal(IntentAction.Attack, Ai.Declare(state, runt).Action);

        var result = state.Step(Ai.Plan(state, runt));
        Assert.Equal(12, result.NewState.Get(vanguard.Id).Hp);
    }

    [Fact]
    public void Runt_NobodyAdjacent_ClosesFourTiles()
    {
        var state = BoardBuilder.Open(9, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Runt, 8, 0)
            .Active(Team.Enemy)
            .Build();

        var intent = Ai.Declare(state, state.Find(UnitKind.Runt));

        Assert.Equal(IntentAction.Advance, intent.Action);
        Assert.Equal(new Coord(4, 0), intent.MoveTo);
    }

    // The thing it exists to prove.
    [Fact]
    public void Runt_DiesToASingleCollision()
    {
        var state = BoardBuilder.Open(5, 1)
            .PlayerA(UnitKind.Vanguard, 2, 0)
            .Enemy(UnitKind.Runt, 1, 0)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        var runt = state.Find(UnitKind.Runt);

        Assert.Equal(2, state.Get(runt.Id).Hp);

        var result = state.Step(new AbilityCommand(vanguard.Id, Ability.BullRush, null, Direction.Left));

        Assert.True(result.Has<Collision>());
        Assert.False(result.NewState.Get(runt.Id).IsAlive);
    }

    [Fact]
    public void Runt_ShovedIntoAnotherRunt_KillsBothForOneAction()
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Runt, 2, 0)
            .Enemy(UnitKind.Runt, 3, 0)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        var runts = state.Units.Where(u => u.Kind == UnitKind.Runt).Select(u => u.Id).ToList();

        var result = state.Step(new AbilityCommand(vanguard.Id, Ability.BullRush, null, Direction.Right));

        foreach (var id in runts)
        {
            Assert.False(result.NewState.Get(id).IsAlive);
        }

        Assert.Equal(2, result.All<UnitDowned>().Count);
    }

    // ================================================================================
    // Colossus — HP 10, Move 1, melee 3, push resistance 2. Genuinely immovable.
    // ================================================================================

    [Fact]
    public void Colossus_AdjacentPlayerUnit_AttacksForThree()
    {
        var state = BoardBuilder.Open(5, 1)
            .PlayerA(UnitKind.Vanguard, 1, 0)
            .Enemy(UnitKind.Colossus, 2, 0)
            .Active(Team.Enemy)
            .Build();

        var colossus = state.Find(UnitKind.Colossus);
        var vanguard = state.Find(UnitKind.Vanguard);

        var intent = Ai.Declare(state, colossus);
        Assert.Equal(IntentAction.Attack, intent.Action);
        Assert.Equal(6, intent.Damage);

        var result = state.Step(Ai.Plan(state, colossus));
        Assert.Equal(8, result.NewState.Get(vanguard.Id).Hp);
    }

    [Fact]
    public void Colossus_NobodyAdjacent_TakesItsOneStep()
    {
        var state = BoardBuilder.Open(7, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Colossus, 6, 0)
            .Active(Team.Enemy)
            .Build();

        var intent = Ai.Declare(state, state.Find(UnitKind.Colossus));

        Assert.Equal(IntentAction.Advance, intent.Action);
        Assert.Equal(new Coord(5, 0), intent.MoveTo);
    }

    // The design, stated as three assertions: Push 1 fails, Push 2 fails, Pull works.
    [Fact]
    public void Colossus_PushOneAndPushTwo_BothMoveItNowhere()
    {
        var state = ColossusInTheOpen();
        var colossus = state.Find(UnitKind.Colossus);

        Assert.Equal(0, Displacement.EffectiveDistance(state, colossus, DisplacementKind.Push, 1, out _));
        Assert.Equal(0, Displacement.EffectiveDistance(state, colossus, DisplacementKind.Push, 2, out _));
    }

    [Fact]
    public void Colossus_VanguardsBasicShove_DealsDamageAndMovesItNothing()
    {
        var state = BoardBuilder.Open(7, 1)
            .PlayerA(UnitKind.Vanguard, 2, 0)
            .Enemy(UnitKind.Colossus, 3, 0)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        var colossus = state.Find(UnitKind.Colossus);

        var result = state.Step(new AttackCommand(vanguard.Id, colossus.Id));

        Assert.Equal(18, result.NewState.Get(colossus.Id).Hp);
        Assert.Equal(new Coord(3, 0), result.NewState.Get(colossus.Id).Position);
    }

    [Fact]
    public void Colossus_BullRush_MovesItNothing()
    {
        var state = ColossusInTheOpen();
        var vanguard = state.Find(UnitKind.Vanguard);
        var colossus = state.Find(UnitKind.Colossus);

        var result = state.Step(new AbilityCommand(vanguard.Id, Ability.BullRush, null, Direction.Right));

        Assert.Equal(new Coord(3, 0), result.NewState.Get(colossus.Id).Position);
    }

    [Fact]
    public void Colossus_BullRushOnAStaggeredColossus_MovesItExactlyOneTile()
    {
        var state = ColossusInTheOpen();
        var vanguard = state.Find(UnitKind.Vanguard);
        var colossus = state.Find(UnitKind.Colossus);

        state = state.WithUnit(state.Get(colossus.Id) with { Staggered = true });

        var result = state.Step(new AbilityCommand(vanguard.Id, Ability.BullRush, null, Direction.Right));

        Assert.Equal(new Coord(4, 0), result.NewState.Get(colossus.Id).Position);
    }

    [Fact]
    public void Colossus_StaggeredPushOne_StillMovesItNowhere()
    {
        var state = ColossusInTheOpen();
        var colossus = state.Find(UnitKind.Colossus) with { Staggered = true };

        Assert.Equal(0, Displacement.EffectiveDistance(state, colossus, DisplacementKind.Push, 1, out _));
    }

    // Was Colossus_Pull_IsCompletelyUnaffectedByPushResistance. D-139 put Pull under the same
    // arithmetic as Push, so an ordinary drag is shortened by the Colossus's 2 like everything else
    // — and Reel is the one carve-out left, because its printed text says "all the way to adjacent"
    // and a shortened Reel would stop doing what the card says. Reported to the designer as the
    // open question; today's behaviour is pinned here rather than left to chance.
    [Fact]
    public void Colossus_OrdinaryPull_IsShortened_ButReelStillDragsItAllTheWayIn()
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerB(UnitKind.Threadcaster, 0, 0)
            .Enemy(UnitKind.Colossus, 3, 0)
            .Build();

        var caster = state.Find(UnitKind.Threadcaster);
        var colossus = state.Find(UnitKind.Colossus);

        Assert.Equal(0, Displacement.EffectiveDistance(state, colossus, DisplacementKind.Pull, 2, out _));

        var result = state.Step(new AbilityCommand(caster.Id, Ability.Reel, colossus.Id));

        Assert.Equal(new Coord(1, 0), result.NewState.Get(colossus.Id).Position);
    }

    private static GameState ColossusInTheOpen() =>
        BoardBuilder.Open(8, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Colossus, 3, 0)
            .Build();

    // ================================================================================
    // Balance variants — same priority list, different numbers.
    // ================================================================================

    [Theory]
    [InlineData(UnitKind.LesserGrappler, UnitKind.Grappler)]
    [InlineData(UnitKind.BluntedStalker, UnitKind.Stalker)]
    [InlineData(UnitKind.HeavyHusk, UnitKind.Husk)]
    [InlineData(UnitKind.MobileAnchor, UnitKind.Anchor)]
    [InlineData(UnitKind.Bulwark, UnitKind.Husk)]
    [InlineData(UnitKind.Runt, UnitKind.Husk)]
    [InlineData(UnitKind.Colossus, UnitKind.Anchor)]
    public void AVariant_ReusesItsArchetypesPriorityList_RatherThanCopyingIt(UnitKind variant, UnitKind original)
    {
        Assert.Equal(UnitTemplate.For(original).Plan, UnitTemplate.For(variant).Plan);
    }

    [Fact]
    public void EveryEnemyKind_NamesAPriorityListAndEveryPlayerClassDoesNot()
    {
        foreach (var kind in EnemyBehaviour.EnemyKinds)
        {
            Assert.NotEqual(EnemyPlan.None, UnitTemplate.For(kind).Plan);
        }

        foreach (var descriptor in AbilityDescriptor.All())
        {
            Assert.Equal(EnemyPlan.None, UnitTemplate.For(descriptor.Kind).Plan);
        }
    }

    // ---- Lesser Grappler: pull range 2 -------------------------------------------------------

    [Fact]
    public void LesserGrappler_TargetTwoTilesAway_Pulls()
    {
        var state = BoardBuilder.Open(7, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.LesserGrappler, 2, 0)
            .Active(Team.Enemy)
            .Build();

        var intent = Ai.Declare(state, state.Find(UnitKind.LesserGrappler));

        Assert.Equal(IntentAction.Pull, intent.Action);
        Assert.Equal(DisplacementKind.Pull, intent.Displacement);
        Assert.Equal(new Coord(1, 0), intent.DisplacementTo);
    }

    // Priority 2: out of reach, so it closes to its band first. A Grappler at the same distance is
    // already in range and yanks without taking a step — that one tile of reach is the whole variant.
    [Fact]
    public void LesserGrappler_TargetThreeTilesAway_MustCloseFirstWhereAGrapplerDoesNot()
    {
        var lesser = BoardBuilder.Open(9, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.LesserGrappler, 3, 0)
            .Active(Team.Enemy)
            .Build();

        var full = BoardBuilder.Open(9, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Grappler, 3, 0)
            .Active(Team.Enemy)
            .Build();

        var fullIntent = Ai.Declare(full, full.Find(UnitKind.Grappler));
        var lesserIntent = Ai.Declare(lesser, lesser.Find(UnitKind.LesserGrappler));

        Assert.Equal(IntentAction.Pull, fullIntent.Action);
        Assert.Null(fullIntent.MoveTo);

        Assert.Equal(IntentAction.Pull, lesserIntent.Action);
        Assert.Equal(new Coord(2, 0), lesserIntent.MoveTo);
    }

    // ---- Blunted Stalker: pit > spikes, and nothing else ---------------------------------------

    [Fact]
    public void BluntedStalker_PlayerBesideAPit_StillShovesThemIn()
    {
        var state = BoardBuilder.Rows("...O..")
            .PlayerA(UnitKind.Vanguard, 2, 0)
            .Enemy(UnitKind.BluntedStalker, 0, 0)
            .Active(Team.Enemy)
            .Build();

        var intent = Ai.Declare(state, state.Find(UnitKind.BluntedStalker));

        Assert.Equal(IntentAction.Push, intent.Action);
        Assert.Equal(new Coord(1, 0), intent.MoveTo);
        Assert.Equal(new Coord(3, 0), intent.DisplacementTo);
    }

    [Fact]
    public void BluntedStalker_PlayerBesideSpikes_StillShovesThemOn()
    {
        var state = BoardBuilder.Rows("...^..")
            .PlayerA(UnitKind.Vanguard, 2, 0)
            .Enemy(UnitKind.BluntedStalker, 0, 0)
            .Active(Team.Enemy)
            .Build();

        var intent = Ai.Declare(state, state.Find(UnitKind.BluntedStalker));

        Assert.Equal(IntentAction.Push, intent.Action);
        Assert.Equal(new Coord(3, 0), intent.DisplacementTo);
    }

    // The one it exists for. The board edge is available everywhere, which made the ordinary
    // Stalker's shove the most reliable damage in the enemy roster.
    [Fact]
    public void BluntedStalker_WillNotShoveIntoTheBoardEdge_WhereAStalkerWould()
    {
        var blunted = BoardBuilder.Open(5, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.BluntedStalker, 2, 0)
            .Active(Team.Enemy)
            .Build();

        var ordinary = BoardBuilder.Open(5, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Stalker, 2, 0)
            .Active(Team.Enemy)
            .Build();

        Assert.Equal(IntentAction.Push, Ai.Declare(ordinary, ordinary.Find(UnitKind.Stalker)).Action);
        Assert.Equal(IntentAction.Hold, Ai.Declare(blunted, blunted.Find(UnitKind.BluntedStalker)).Action);
    }

    [Fact]
    public void BluntedStalker_WillNotShoveIntoAWall_WhereAStalkerWould()
    {
        var blunted = BoardBuilder.Rows("...#..")
            .PlayerA(UnitKind.Vanguard, 2, 0)
            .Enemy(UnitKind.BluntedStalker, 0, 0)
            .Active(Team.Enemy)
            .Build();

        var ordinary = BoardBuilder.Rows("...#..")
            .PlayerA(UnitKind.Vanguard, 2, 0)
            .Enemy(UnitKind.Stalker, 0, 0)
            .Active(Team.Enemy)
            .Build();

        Assert.Equal(IntentAction.Push, Ai.Declare(ordinary, ordinary.Find(UnitKind.Stalker)).Action);
        Assert.Equal(IntentAction.Hold, Ai.Declare(blunted, blunted.Find(UnitKind.BluntedStalker)).Action);
    }

    // Its step-2 search drops walls and the board edge too, or it would loiter for nothing.
    [Fact]
    public void BluntedStalker_NoPitAndNoSpikesAnywhere_Holds()
    {
        var state = BoardBuilder.Open(9, 3)
            .PlayerA(UnitKind.Vanguard, 8, 1)
            .Enemy(UnitKind.BluntedStalker, 0, 1)
            .Active(Team.Enemy)
            .Build();

        var intent = Ai.Declare(state, state.Find(UnitKind.BluntedStalker));

        Assert.Equal(IntentAction.Hold, intent.Action);
        Assert.Null(intent.MoveTo);
    }

    [Fact]
    public void BluntedStalker_KeepsThePitOverSpikesRanking()
    {
        var state = BoardBuilder.Rows(
                "..^...",
                "...O..",
                "......")
            .PlayerA(UnitKind.Vanguard, 2, 1)
            .Enemy(UnitKind.BluntedStalker, 0, 0)
            .Active(Team.Enemy)
            .Build();

        var intent = Ai.Declare(state, state.Find(UnitKind.BluntedStalker));

        Assert.Equal(IntentAction.Push, intent.Action);
        Assert.Equal(new Coord(3, 1), intent.DisplacementTo);
    }

    // ---- Heavy Husk: HP 3 ----------------------------------------------------------------------

    [Fact]
    public void HeavyHusk_SurvivesTheCollisionThatKillsAHusk()
    {
        var heavy = ChaffAgainstTheWall(UnitKind.HeavyHusk);
        var ordinary = ChaffAgainstTheWall(UnitKind.Husk);

        var heavyResult = heavy.Step(new AbilityCommand(
            heavy.Find(UnitKind.Vanguard).Id, Ability.BullRush, null, Direction.Left));
        var ordinaryResult = ordinary.Step(new AbilityCommand(
            ordinary.Find(UnitKind.Vanguard).Id, Ability.BullRush, null, Direction.Left));

        Assert.True(heavyResult.NewState.Get(heavy.Find(UnitKind.HeavyHusk).Id).IsAlive);
        Assert.Equal(2, heavyResult.NewState.Get(heavy.Find(UnitKind.HeavyHusk).Id).Hp);
        Assert.False(ordinaryResult.NewState.Get(ordinary.Find(UnitKind.Husk).Id).IsAlive);
    }

    [Fact]
    public void HeavyHusk_PlansExactlyAsAHuskDoes()
    {
        var heavy = BoardBuilder.Open(9, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.HeavyHusk, 8, 0)
            .Active(Team.Enemy)
            .Build();

        var ordinary = BoardBuilder.Open(9, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 8, 0)
            .Active(Team.Enemy)
            .Build();

        var a = Ai.Declare(heavy, heavy.Find(UnitKind.HeavyHusk));
        var b = Ai.Declare(ordinary, ordinary.Find(UnitKind.Husk));

        Assert.Equal(b.Action, a.Action);
        Assert.Equal(b.MoveTo, a.MoveTo);
        Assert.Equal(b.Damage, a.Damage);
    }

    private static GameState ChaffAgainstTheWall(UnitKind kind) =>
        BoardBuilder.Open(5, 1)
            .PlayerA(UnitKind.Vanguard, 2, 0)
            .Enemy(kind, 1, 0)
            .Build();

    // ---- Mobile Anchor: Move 2 -----------------------------------------------------------------

    [Fact]
    public void MobileAnchor_ClosesTwoTilesWhereAnAnchorClosesOne()
    {
        var mobile = BoardBuilder.Open(9, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.MobileAnchor, 8, 0)
            .Active(Team.Enemy)
            .Build();

        var original = BoardBuilder.Open(9, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Anchor, 8, 0)
            .Active(Team.Enemy)
            .Build();

        Assert.Equal(new Coord(6, 0), Ai.Declare(mobile, mobile.Find(UnitKind.MobileAnchor)).MoveTo);
        Assert.Equal(new Coord(7, 0), Ai.Declare(original, original.Find(UnitKind.Anchor)).MoveTo);
    }

    [Fact]
    public void MobileAnchor_StillShrugsOffOneTileOfEveryPush()
    {
        var state = BoardBuilder.Open(7, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.MobileAnchor, 3, 0)
            .Build();

        var anchor = state.Find(UnitKind.MobileAnchor);

        Assert.Equal(0, Displacement.EffectiveDistance(state, anchor, DisplacementKind.Push, 1, out _));
        Assert.Equal(1, Displacement.EffectiveDistance(state, anchor, DisplacementKind.Push, 2, out _));

        // D-139: the same tile comes off a Pull. It used to come off Pushes only.
        Assert.Equal(1, Displacement.EffectiveDistance(state, anchor, DisplacementKind.Pull, 2, out _));
    }

    [Fact]
    public void MobileAnchor_AdjacentPlayerUnit_StillAttacksForTwo()
    {
        var state = BoardBuilder.Open(5, 1)
            .PlayerA(UnitKind.Vanguard, 1, 0)
            .Enemy(UnitKind.MobileAnchor, 2, 0)
            .Active(Team.Enemy)
            .Build();

        Assert.Equal(4, Ai.Declare(state, state.Find(UnitKind.MobileAnchor)).Damage);
    }

    // ================================================================================
    // The generalisations: data on the template, not a second branch in the rules.
    // ================================================================================

    [Theory]
    [InlineData(UnitKind.Anchor, 1)]
    [InlineData(UnitKind.MobileAnchor, 1)]
    [InlineData(UnitKind.Warden, 0)]
    [InlineData(UnitKind.Colossus, 2)]
    [InlineData(UnitKind.Husk, 0)]
    public void PushResistance_IsANumberOnTheStatBlock(UnitKind kind, int expected)
    {
        Assert.Equal(expected, UnitTemplate.For(kind).PushResistance);
    }

    // Changed by D-058. It used to assert the Wardbearer and the Bulwark shared the flag; the
    // Wardbearer's aura is deleted and the Bulwark's is not, so the flag now belongs to exactly one
    // archetype in the whole game. The mechanism is still a number on the stat block (D-031) — that
    // is what is being pinned, not the aura's popularity.
    [Fact]
    public void HoldAura_IsAFlagOnTheStatBlock_AndOnlyTheBulwarkCarriesItNow()
    {
        Assert.False(UnitTemplate.For(UnitKind.Wardbearer).HoldAura);
        Assert.True(UnitTemplate.For(UnitKind.Bulwark).HoldAura);

        foreach (UnitKind kind in Enum.GetValues(typeof(UnitKind)))
        {
            if (kind == UnitKind.Bulwark)
            {
                continue;
            }

            Assert.False(UnitTemplate.For(kind).HoldAura, kind + " should not carry a hold aura.");
        }
    }

    [Theory]
    [InlineData(UnitKind.Stalker, 3)]
    [InlineData(UnitKind.BluntedStalker, 2)]
    [InlineData(UnitKind.Harrier, 0)]
    [InlineData(UnitKind.Husk, 0)]
    public void HazardRanks_IsANumberOnTheStatBlock(UnitKind kind, int expected)
    {
        Assert.Equal(expected, UnitTemplate.For(kind).HazardRanks);
    }

    // A push resistance of 2 must behave as 2, not as "the Anchor rule applied twice by hand".
    [Fact]
    public void PushResistance_SubtractsItsOwnValueAndNeverGoesNegative()
    {
        var state = BoardBuilder.Open(9, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Colossus, 4, 0)
            .Build();

        var colossus = state.Find(UnitKind.Colossus);

        for (int requested = 0; requested <= 5; requested++)
        {
            int expected = requested <= 0 ? 0 : System.Math.Max(0, requested - 2);
            Assert.Equal(
                expected,
                Displacement.EffectiveDistance(state, colossus, DisplacementKind.Push, requested, out _));
        }
    }
}
