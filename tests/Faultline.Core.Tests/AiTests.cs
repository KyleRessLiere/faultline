using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// M3. One test per branch of every priority list in AGENT_BRIEF.md §2 "Enemies", plus the
/// properties the planner has to hold globally: determinism, a fixed tie-break, no deadlock, and
/// intents that lock until their target dies.
/// </summary>
public class AiTests
{
    // ---- Husk: 1. adjacent → attack. 2. else move toward nearest. -----------------------------

    [Fact]
    public void Husk_AdjacentPlayerUnit_Attacks()
    {
        var state = BoardBuilder.Open(5, 1)
            .PlayerA(UnitKind.Vanguard, 1, 0)
            .Enemy(UnitKind.Husk, 2, 0)
            .Active(Team.Enemy)
            .Build();

        var husk = state.Find(UnitKind.Husk);
        var vanguard = state.Find(UnitKind.Vanguard);

        var intent = Ai.Declare(state, husk);
        Assert.Equal(IntentAction.Attack, intent.Action);
        Assert.Equal(vanguard.Id, intent.TargetId);
        Assert.Null(intent.MoveTo);
        Assert.Equal(1, intent.Damage);

        var result = state.Step(Ai.Plan(state, husk));
        Assert.Equal(new AttackCommand(husk.Id, vanguard.Id), Ai.Plan(state, husk));
        Assert.Equal(6, result.NewState.Get(vanguard.Id).Hp);
    }

    [Fact]
    public void Husk_NobodyAdjacent_MovesTowardTheNearestPlayerUnit()
    {
        var state = BoardBuilder.Open(7, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 6, 0)
            .Active(Team.Enemy)
            .Build();

        var husk = state.Find(UnitKind.Husk);
        var vanguard = state.Find(UnitKind.Vanguard);

        var intent = Ai.Declare(state, husk);
        Assert.Equal(IntentAction.Advance, intent.Action);
        Assert.Equal(vanguard.Id, intent.TargetId);
        Assert.Equal(new Coord(3, 0), intent.MoveTo);

        // Move 3, so it spends all three closing from 6 tiles away to 3.
        var result = state.Step(Ai.Plan(state, husk));
        Assert.Equal(new Coord(3, 0), result.NewState.Get(husk.Id).Position);
    }

    [Fact]
    public void Husk_WalkThatEndsInReach_AlsoSpendsTheActionOnTheAttack()
    {
        var state = BoardBuilder.Open(5, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 4, 0)
            .Active(Team.Enemy)
            .Build();

        var husk = state.Find(UnitKind.Husk);
        var vanguard = state.Find(UnitKind.Vanguard);

        var intent = Ai.Declare(state, husk);
        Assert.Equal(IntentAction.Attack, intent.Action);
        Assert.Equal(new Coord(1, 0), intent.MoveTo);

        state = state.Then(Ai.Plan(state, husk));
        state = state.Then(Ai.Plan(state, state.Get(husk.Id)));

        Assert.Equal(new Coord(1, 0), state.Get(husk.Id).Position);
        Assert.Equal(6, state.Get(vanguard.Id).Hp);
    }

    // ---- Lobber: 1. in range and nothing adjacent → shoot. 2. adjacent → move away. 3. advance. --

    [Fact]
    public void Lobber_TargetInRangeAndNothingAdjacent_ShootsWithoutMoving()
    {
        var state = BoardBuilder.Open(5, 1)
            .PlayerA(UnitKind.Archer, 3, 0)
            .Enemy(UnitKind.Lobber, 0, 0)
            .Active(Team.Enemy)
            .Build();

        var lobber = state.Find(UnitKind.Lobber);
        var archer = state.Find(UnitKind.Archer);

        var intent = Ai.Declare(state, lobber);
        Assert.Equal(IntentAction.Attack, intent.Action);
        Assert.Null(intent.MoveTo);

        var result = state.Step(Ai.Plan(state, lobber));
        Assert.Equal(3, result.NewState.Get(archer.Id).Hp);
    }

    [Fact]
    public void Lobber_PlayerAdjacent_MovesAwayMaximisingDistance()
    {
        var state = BoardBuilder.Open(7, 1)
            .PlayerA(UnitKind.Vanguard, 2, 0)
            .Enemy(UnitKind.Lobber, 3, 0)
            .Active(Team.Enemy)
            .Build();

        var lobber = state.Find(UnitKind.Lobber);

        // Move 2: (5,0) is the furthest it can get, and no other reachable tile matches it.
        var intent = Ai.Declare(state, lobber);
        Assert.Equal(new Coord(5, 0), intent.MoveTo);

        var result = state.Step(Ai.Plan(state, lobber));
        Assert.Equal(new Coord(5, 0), result.NewState.Get(lobber.Id).Position);
    }

    [Fact]
    public void Lobber_WithNobodyAdjacent_NeverRetreats()
    {
        var state = BoardBuilder.Open(7, 1)
            .PlayerA(UnitKind.Vanguard, 3, 0)
            .Enemy(UnitKind.Lobber, 6, 0)
            .Active(Team.Enemy)
            .Build();

        var intent = Ai.Declare(state, state.Find(UnitKind.Lobber));

        Assert.Equal(IntentAction.Attack, intent.Action);
        Assert.Null(intent.MoveTo);
    }

    [Fact]
    public void Lobber_OutOfRange_AdvancesTowardRangeWithoutEnteringMelee()
    {
        var state = BoardBuilder.Open(9, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Lobber, 8, 0)
            .Active(Team.Enemy)
            .Build();

        var lobber = state.Find(UnitKind.Lobber);
        var intent = Ai.Declare(state, lobber);

        Assert.Equal(IntentAction.Advance, intent.Action);
        Assert.Equal(new Coord(6, 0), intent.MoveTo);

        var result = state.Step(Ai.Plan(state, lobber));
        Assert.Equal(new Coord(6, 0), result.NewState.Get(lobber.Id).Position);
    }

    // ---- Anchor: 1. adjacent → attack for 2. 2. else advance (Move 1). -------------------------

    [Fact]
    public void Anchor_AdjacentPlayerUnit_AttacksForTwo()
    {
        var state = BoardBuilder.Open(5, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Anchor, 1, 0)
            .Active(Team.Enemy)
            .Build();

        var anchor = state.Find(UnitKind.Anchor);
        var vanguard = state.Find(UnitKind.Vanguard);

        var intent = Ai.Declare(state, anchor);
        Assert.Equal(IntentAction.Attack, intent.Action);
        Assert.Equal(2, intent.Damage);
        Assert.Null(intent.MoveTo);

        var result = state.Step(Ai.Plan(state, anchor));
        Assert.Equal(5, result.NewState.Get(vanguard.Id).Hp);
    }

    [Fact]
    public void Anchor_NobodyAdjacent_AdvancesItsSingleTile()
    {
        var state = BoardBuilder.Open(5, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Anchor, 4, 0)
            .Active(Team.Enemy)
            .Build();

        var anchor = state.Find(UnitKind.Anchor);

        var intent = Ai.Declare(state, anchor);
        Assert.Equal(IntentAction.Advance, intent.Action);
        Assert.Equal(new Coord(3, 0), intent.MoveTo);

        var result = state.Step(Ai.Plan(state, anchor));
        Assert.Equal(new Coord(3, 0), result.NewState.Get(anchor.Id).Position);
    }

    // ---- Grappler: 1. pull 2, preferring HighGround then the Archer. 2. else advance. ----------

    [Fact]
    public void Grappler_PlayerWithinRangeThree_PullsItTwoTilesIn()
    {
        var state = BoardBuilder.Open(5, 1)
            .PlayerA(UnitKind.Archer, 3, 0)
            .Enemy(UnitKind.Grappler, 0, 0)
            .Active(Team.Enemy)
            .Build();

        var grappler = state.Find(UnitKind.Grappler);
        var archer = state.Find(UnitKind.Archer);

        var intent = Ai.Declare(state, grappler);
        Assert.Equal(IntentAction.Pull, intent.Action);
        Assert.Equal(archer.Id, intent.TargetId);
        Assert.Equal(DisplacementKind.Pull, intent.Displacement);
        Assert.Equal(2, intent.DisplacementDistance);
        Assert.Equal(new Coord(1, 0), intent.DisplacementTo);

        var command = Ai.Plan(state, grappler);
        Assert.Equal(new AttackCommand(grappler.Id, archer.Id, AttackMode.Pull), command);

        var result = state.Step(command);
        Assert.Equal(new Coord(1, 0), result.NewState.Get(archer.Id).Position);
    }

    [Fact]
    public void Grappler_PrefersAUnitOnHighGroundOverALowerId()
    {
        var state = BoardBuilder.Rows("...", "...", "H..")
            .PlayerA(UnitKind.Archer, 2, 0)
            .PlayerA(UnitKind.Vanguard, 0, 2)
            .Enemy(UnitKind.Grappler, 0, 0)
            .Active(Team.Enemy)
            .Build();

        var grappler = state.Find(UnitKind.Grappler);
        var vanguard = state.Find(UnitKind.Vanguard);

        // Both are exactly 2 away; the Archer has the lower id, but height outranks it.
        var intent = Ai.Declare(state, grappler);
        Assert.Equal(IntentAction.Pull, intent.Action);
        Assert.Equal(vanguard.Id, intent.TargetId);
    }

    [Fact]
    public void Grappler_WithNobodyElevated_PrefersTheArcher()
    {
        var state = BoardBuilder.Open(3, 3)
            .PlayerA(UnitKind.Vanguard, 2, 0)
            .PlayerA(UnitKind.Archer, 0, 2)
            .Enemy(UnitKind.Grappler, 0, 0)
            .Active(Team.Enemy)
            .Build();

        var grappler = state.Find(UnitKind.Grappler);
        var archer = state.Find(UnitKind.Archer);

        var intent = Ai.Declare(state, grappler);
        Assert.Equal(IntentAction.Pull, intent.Action);
        Assert.Equal(archer.Id, intent.TargetId);
    }

    [Fact]
    public void Grappler_NothingInRange_AdvancesOnTheArcher()
    {
        var state = BoardBuilder.Open(9, 3)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .PlayerA(UnitKind.Archer, 0, 2)
            .Enemy(UnitKind.Grappler, 8, 0)
            .Active(Team.Enemy)
            .Build();

        var grappler = state.Find(UnitKind.Grappler);
        var archer = state.Find(UnitKind.Archer);

        var intent = Ai.Declare(state, grappler);

        Assert.Equal(IntentAction.Advance, intent.Action);
        Assert.Equal(archer.Id, intent.TargetId);
        Assert.NotNull(intent.MoveTo);
    }

    [Fact]
    public void Grappler_HasNoBasicAttack_SoItsPlanIsALegalPullCommand()
    {
        var state = BoardBuilder.Open(5, 1)
            .PlayerA(UnitKind.Archer, 3, 0)
            .Enemy(UnitKind.Grappler, 0, 0)
            .Active(Team.Enemy)
            .Build();

        var grappler = state.Find(UnitKind.Grappler);

        Assert.Equal(AttackKind.None, grappler.Template.Attack);
        TestPlay.AssertLegal(state, Ai.Plan(state, grappler));
    }

    // ---- Stalker: 1. flank a hazard and shove. 2. else close on someone near one. 3. else hold. --

    [Fact]
    public void Stalker_PlayerBesideAPit_MovesToFlankThenShovesItIn()
    {
        var state = BoardBuilder.Rows("O....")
            .PlayerA(UnitKind.Vanguard, 1, 0)
            .Enemy(UnitKind.Stalker, 4, 0)
            .Active(Team.Enemy)
            .Build();

        var stalker = state.Find(UnitKind.Stalker);
        var vanguard = state.Find(UnitKind.Vanguard);

        var intent = Ai.Declare(state, stalker);
        Assert.Equal(IntentAction.Push, intent.Action);
        Assert.Equal(vanguard.Id, intent.TargetId);
        Assert.Equal(new Coord(2, 0), intent.MoveTo);
        Assert.Equal(new Coord(0, 0), intent.DisplacementTo);

        state = state.Then(Ai.Plan(state, stalker));
        Assert.Equal(new Coord(2, 0), state.Get(stalker.Id).Position);

        var result = state.Step(Ai.Plan(state, state.Get(stalker.Id)));

        // Clinging fires at hazard entry, which is the shove landing. The Vanguard is the only
        // player unit on this board, so nobody could ever have hauled it out and D-081 sweeps it in
        // the same breath — both halves are asserted rather than only the one that used to happen.
        Assert.True(result.Has<Clinging>());
        Assert.Equal(vanguard.Id, result.Single<Clinging>().UnitId);
        Assert.True(result.Has<Voided>());
        Assert.True(result.NewState.Get(vanguard.Id).Voided);
    }

    [Fact]
    public void Stalker_PlayerBesideSpikes_ShovesThemOntoIt()
    {
        var state = BoardBuilder.Rows("^....")
            .PlayerA(UnitKind.Vanguard, 1, 0)
            .Enemy(UnitKind.Stalker, 2, 0)
            .Active(Team.Enemy)
            .Build();

        var stalker = state.Find(UnitKind.Stalker);
        var vanguard = state.Find(UnitKind.Vanguard);

        var intent = Ai.Declare(state, stalker);
        Assert.Equal(IntentAction.Push, intent.Action);
        Assert.Null(intent.MoveTo);
        Assert.Equal(new Coord(0, 0), intent.DisplacementTo);

        var result = state.Step(Ai.Plan(state, stalker));

        Assert.Equal(3, result.Single<SpikeHit>().Damage);
        Assert.Equal(4, result.NewState.Get(vanguard.Id).Hp);
    }

    [Fact]
    public void Stalker_NoReachableFlank_ClosesOnThePlayerNearestAHazard()
    {
        var state = BoardBuilder.Rows(
                ".......",
                "...O...",
                ".......",
                ".......",
                ".......",
                ".......",
                ".......")
            .PlayerA(UnitKind.Vanguard, 3, 3)
            .Enemy(UnitKind.Stalker, 6, 6)
            .Active(Team.Enemy)
            .Build();

        var stalker = state.Find(UnitKind.Stalker);
        var vanguard = state.Find(UnitKind.Vanguard);

        var intent = Ai.Declare(state, stalker);

        Assert.Equal(IntentAction.Advance, intent.Action);
        Assert.Equal(vanguard.Id, intent.TargetId);

        var result = state.Step(Ai.Plan(state, stalker));
        var moved = result.NewState.Get(stalker.Id);

        Assert.True(moved.Position.DistanceTo(vanguard.Position) < 6);
    }

    [Fact]
    public void Stalker_NobodyNearAHazard_HoldsPosition()
    {
        var state = BoardBuilder.Open(7, 7)
            .PlayerA(UnitKind.Vanguard, 3, 3)
            .Enemy(UnitKind.Stalker, 5, 5)
            .Active(Team.Enemy)
            .Build();

        var stalker = state.Find(UnitKind.Stalker);

        Assert.Equal(IntentAction.Hold, Ai.Declare(state, stalker).Action);
        Assert.Equal(new EndActivationCommand(stalker.Id), Ai.Plan(state, stalker));

        var result = state.Step(Ai.Plan(state, stalker));
        Assert.Equal(new Coord(5, 5), result.NewState.Get(stalker.Id).Position);
    }

    [Fact]
    public void Stalker_HasNoBasicAttack_SoItsPlanIsALegalPushCommand()
    {
        var state = BoardBuilder.Rows("^....")
            .PlayerA(UnitKind.Vanguard, 1, 0)
            .Enemy(UnitKind.Stalker, 2, 0)
            .Active(Team.Enemy)
            .Build();

        var stalker = state.Find(UnitKind.Stalker);

        Assert.Equal(AttackKind.None, stalker.Template.Attack);
        TestPlay.AssertLegal(state, Ai.Plan(state, stalker));
    }

    // ---- global properties --------------------------------------------------------------------

    [Fact]
    public void Ties_BreakByLowestUnitId()
    {
        var left = BoardBuilder.Open(3, 3)
            .PlayerA(UnitKind.Vanguard, 0, 1)
            .PlayerB(UnitKind.Vanguard, 2, 1)
            .Enemy(UnitKind.Husk, 1, 1)
            .Active(Team.Enemy)
            .Build();

        var husk = left.Find(UnitKind.Husk);
        Assert.Equal(new UnitId(0), Ai.Declare(left, husk).TargetId);

        // Same board, ids swapped: the choice follows the id, not the geometry.
        var right = BoardBuilder.Open(3, 3)
            .PlayerB(UnitKind.Vanguard, 2, 1)
            .PlayerA(UnitKind.Vanguard, 0, 1)
            .Enemy(UnitKind.Husk, 1, 1)
            .Active(Team.Enemy)
            .Build();

        Assert.Equal(new UnitId(0), Ai.Declare(right, right.Find(UnitKind.Husk)).TargetId);
        Assert.Equal(new Coord(2, 1), right.Get(new UnitId(0)).Position);
    }

    [Fact]
    public void Plan_OverTheSameState_ReturnsTheSameCommandTwice()
    {
        var state = BoardBuilder.Rows(
                "..O....",
                ".......",
                "^......",
                ".......",
                "....H..")
            .PlayerA(UnitKind.Vanguard, 1, 1)
            .PlayerA(UnitKind.Archer, 4, 4)
            .PlayerB(UnitKind.Threadcaster, 0, 3)
            .PlayerB(UnitKind.Wardbearer, 2, 3)
            .Enemy(UnitKind.Husk, 6, 0)
            .Enemy(UnitKind.Lobber, 6, 2)
            .Enemy(UnitKind.Anchor, 6, 4)
            .Enemy(UnitKind.Grappler, 5, 1)
            .Enemy(UnitKind.Stalker, 5, 3)
            .Active(Team.Enemy)
            .Build();

        foreach (var enemy in state.Units.Where(u => u.Team == Team.Enemy))
        {
            Assert.Equal(Ai.Plan(state, enemy), Ai.Plan(state, enemy));
            Assert.Equal(Ai.Declare(state, enemy), Ai.Declare(state, enemy));
        }
    }

    [Fact]
    public void Fight_PlayedTwiceFromTheSameSeed_EndsOnAnIdenticalStateAndHash()
    {
        var first = TestPlay.PlayWithAi(Game.Start(FightLibrary.Fight1(), seed: 7).NewState, 4000);
        var second = TestPlay.PlayWithAi(Game.Start(FightLibrary.Fight1(), seed: 7).NewState, 4000);

        Assert.Equal(first.Log, second.Log);
        Assert.Equal(first.State, second.State);
        Assert.Equal(first.State.GetHashCode(), second.State.GetHashCode());
    }

    [Fact]
    public void EnemyCommands_LandInTheCommandLog_AndReplayIdentically()
    {
        var start = Game.Start(FightLibrary.Fight1(), seed: 31).NewState;
        var (played, log, _) = TestPlay.PlayWithAi(start, 4000);

        var replayed = TestPlay.Replay(Game.Start(FightLibrary.Fight1(), seed: 31).NewState, log);

        Assert.Contains(log, c => c is AttackCommand);
        Assert.Equal(played, replayed);
        Assert.Equal(played.GetHashCode(), replayed.GetHashCode());
    }

    [Fact]
    public void Planner_ConsultsNoRandomness()
    {
        var (played, _, _) = TestPlay.PlayWithAi(Game.Start(FightLibrary.Fight1(), seed: 5).NewState, 4000);

        // Nothing in M3 draws from the generator, so a fight full of AI decisions leaves it untouched.
        Assert.Equal(played.Seed, played.RngState);
    }

    [Fact]
    public void FullFight_RunsToAConclusionWithoutDeadlock()
    {
        const int Bound = 4000;
        var (played, _, steps) = TestPlay.PlayWithAi(Game.Start(FightLibrary.Fight1(), seed: 12345).NewState, Bound);

        Assert.True(steps < Bound, "The fight did not terminate inside the step bound.");
        Assert.NotEqual(FightOutcome.InProgress, played.Outcome);
    }

    [Fact]
    public void Enemy_WithNoLegalAction_EndsItsActivationInsteadOfThrowing()
    {
        var state = BoardBuilder.Rows("###", "..#", "###")
            .PlayerA(UnitKind.Vanguard, 0, 1)
            .Enemy(UnitKind.Grappler, 1, 1)
            .Active(Team.Enemy)
            .Build();

        var grappler = state.Find(UnitKind.Grappler);
        var command = Ai.Plan(state, grappler);

        Assert.Equal(new EndActivationCommand(grappler.Id), command);

        var result = state.Step(command);
        Assert.True(result.NewState.Get(grappler.Id).HasActivated);
    }

    // ---- intents ------------------------------------------------------------------------------

    [Fact]
    public void Intents_AreDeclaredForEveryEnemyAtRoundStart()
    {
        var state = BoardBuilder.Open(7, 3)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .PlayerB(UnitKind.Wardbearer, 0, 2)
            .Enemy(UnitKind.Husk, 6, 0)
            .Enemy(UnitKind.Lobber, 6, 2)
            .Build();

        // Pass every activation slot; the round rolls over and declares round 2's intents.
        for (int i = 0; i < 3; i++)
        {
            state = state.PassCurrent().NewState;
        }

        var rollover = state.PassCurrent();
        var declared = rollover.All<IntentDeclared>();

        Assert.Equal(2, rollover.Single<RoundStarted>().Round);
        Assert.Equal(2, declared.Count);
        Assert.All(declared, e => Assert.False(e.Replanned));
        Assert.Equal(2, rollover.NewState.Intents.Count);

        // Declaration lands with the round opening, before anybody in it activates.
        var events = rollover.Events.ToList();
        int roundIndex = events.FindIndex(e => e is RoundStarted);
        int firstIntent = events.FindIndex(e => e is IntentDeclared);

        Assert.True(roundIndex < firstIntent);
        Assert.DoesNotContain(events.Skip(roundIndex), e => e is ActivationStarted);
        Assert.All(rollover.NewState.Units, u => Assert.False(u.HasActivated));
    }

    [Fact]
    public void Intent_CarriesEnoughToTelegraphWithoutQueryingState()
    {
        var state = BoardBuilder.Rows("O....")
            .PlayerA(UnitKind.Vanguard, 1, 0)
            .Enemy(UnitKind.Stalker, 4, 0)
            .Active(Team.Enemy)
            .Build()
            .WithIntents();

        var intent = state.Intents.Single();

        Assert.Equal(UnitKind.Stalker, intent.Kind);
        Assert.Equal(new Coord(4, 0), intent.From);
        Assert.Equal(new Coord(2, 0), intent.MoveTo);
        Assert.Equal(new Coord(1, 0), intent.TargetPosition);
        Assert.Equal(DisplacementKind.Push, intent.Displacement);
        Assert.Equal(Direction.Left, intent.DisplacementDirection);
        Assert.Equal(1, intent.DisplacementDistance);
        Assert.Equal(new Coord(0, 0), intent.DisplacementTo);
    }

    [Fact]
    public void Intent_IsRedeclaredWhenItsTargetDies()
    {
        var state = BoardBuilder.Rows("^......", ".......", ".......")
            .PlayerA(UnitKind.Vanguard, 1, 0, hp: 1)
            .PlayerB(UnitKind.Wardbearer, 0, 2)
            .Enemy(UnitKind.Husk, 6, 0)
            .Build()
            .WithIntents();

        var vanguard = state.Find(UnitKind.Vanguard);
        var wardbearer = state.Find(UnitKind.Wardbearer);

        Assert.Equal(vanguard.Id, state.Intents.Single().TargetId);

        // The Vanguard walks onto the spikes on 1 HP and dies, invalidating the Husk's plan.
        var result = state.Step(new MoveCommand(vanguard.Id, new Coord(0, 0)));

        var replanned = result.All<IntentDeclared>().Single();
        Assert.True(replanned.Replanned);
        Assert.Equal(wardbearer.Id, replanned.Intent.TargetId);
        Assert.Equal(wardbearer.Id, result.NewState.Intents.Single().TargetId);
    }

    [Fact]
    public void Intent_IsNotRedeclaredWhenItsTargetMerelyMoves()
    {
        var state = BoardBuilder.Open(7, 3)
            .PlayerA(UnitKind.Vanguard, 1, 0)
            .PlayerB(UnitKind.Wardbearer, 0, 2)
            .Enemy(UnitKind.Husk, 6, 0)
            .Build()
            .WithIntents();

        var vanguard = state.Find(UnitKind.Vanguard);
        var before = state.Intents.Single();

        var result = state.Step(new MoveCommand(vanguard.Id, new Coord(1, 2)));

        Assert.Empty(result.All<IntentDeclared>());
        Assert.Equal(before, result.NewState.Intents.Single());
    }

    [Fact]
    public void Intent_LocksTheTargetButResolvesGeometryLive()
    {
        var state = BoardBuilder.Open(7, 3)
            .PlayerA(UnitKind.Vanguard, 1, 0)
            .PlayerB(UnitKind.Wardbearer, 5, 2)
            .Enemy(UnitKind.Husk, 6, 0)
            .Build()
            .WithIntents();

        var husk = state.Find(UnitKind.Husk);
        var vanguard = state.Find(UnitKind.Vanguard);
        var wardbearer = state.Find(UnitKind.Wardbearer);

        // Declared against the nearer Wardbearer.
        Assert.Equal(wardbearer.Id, state.Intents.Single().TargetId);

        // The Vanguard walks right up to the Husk. The intent still names the Wardbearer, so the
        // Husk walks past the free hit rather than switching targets mid-round.
        state = state.Then(new MoveCommand(vanguard.Id, new Coord(4, 0)));
        state = state.Then(new EndActivationCommand(vanguard.Id));

        Assert.Equal(Team.Enemy, state.ActiveTeam);
        var command = Game.NextEnemyCommand(state);

        Assert.IsType<MoveCommand>(command);
        Assert.Equal(wardbearer.Id, Ai.IntentFor(state, husk.Id)!.TargetId);
    }

    [Fact]
    public void Intents_AreDroppedForEnemiesThatLeaveTheBoard()
    {
        var state = BoardBuilder.Open(5, 1)
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 2, 0)
            .Enemy(UnitKind.Husk, 3, 0)
            .Build()
            .WithIntents();

        Assert.Equal(2, state.Intents.Count);

        var archer = state.Find(UnitKind.Archer);
        var result = state.Step(new AttackCommand(archer.Id, new UnitId(1)));

        Assert.True(result.Has<UnitDowned>());
        Assert.Equal(new UnitId(2), result.NewState.Intents.Single().UnitId);
    }

    [Fact]
    public void Enemy_AdjacentToAClingingPlayer_FinishesItAsAFreeAction()
    {
        var state = BoardBuilder.Rows("O....")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0)
            .Active(Team.Enemy)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        var husk = state.Find(UnitKind.Husk);
        state = state.WithUnit(vanguard with { Clinging = true, ClingingSinceRound = 1 });

        var command = Ai.Plan(state, husk);
        Assert.Equal(new FinishClingingCommand(husk.Id, vanguard.Id), command);

        var result = state.Step(command);
        Assert.True(result.NewState.Get(vanguard.Id).Voided);
    }

    [Fact]
    public void Enemies_ResolveTheirWholeActivationThroughApply()
    {
        var state = BoardBuilder.Open(5, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 4, 0)
            .Active(Team.Enemy)
            .Build();

        var log = new List<Command>();
        while (Game.IsEnemyTurn(state))
        {
            var command = Game.NextEnemyCommand(state)!;
            log.Add(command);
            state = state.Then(command);
        }

        // Move, then attack — the whole activation, every step of it a logged command.
        Assert.Collection(
            log,
            c => Assert.IsType<MoveCommand>(c),
            c => Assert.IsType<AttackCommand>(c));
    }
}
