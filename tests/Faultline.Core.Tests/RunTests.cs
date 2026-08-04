using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// The run layer: a squad walking an ordered list of nodes, carrying its damage between fights.
/// These are the rules that make a run a run rather than ten fights in a row.
/// </summary>
public class RunTests
{
    // --- Attrition: the rule the whole layer exists for ------------------------------------------

    [Fact]
    public void Run_ASurvivorCarriesItsExactHpIntoTheNextFight()
    {
        // No healing between fights. Not "mostly", not "rounded up" — the same number.
        var run = RunFixture.StartedInFirstFight(out var vanguard);
        run = RunFixture.Deploy(run);
        run = RunFixture.HurtTo(run, vanguard, 3);

        run = RunFixture.WinTheFight(run);
        Assert.Equal(3, run.FindUnit(vanguard)!.Hp);

        run = RunFixture.Enter(run);
        Assert.Equal(3, RunFixture.OnBoard(run, vanguard).Hp);
    }

    [Fact]
    public void Run_NothingHealsBetweenTwoOrdinaryFights()
    {
        var run = RunFixture.StartedInFirstFight(out var vanguard);
        run = RunFixture.Deploy(run);
        run = RunFixture.HurtTo(run, vanguard, 2);

        run = RunFixture.WinTheFight(run);
        Assert.Equal(2, run.FindUnit(vanguard)!.Hp);

        // Standing on the next node, and standing on it after entering: still 2, never 3.
        run = RunFixture.Enter(run);
        Assert.Equal(2, RunFixture.OnBoard(run, vanguard).Hp);
        Assert.Equal(RunUnitStatus.Ready, run.FindUnit(vanguard)!.Status);
    }

    [Fact]
    public void Run_ADownedUnitReturnsBedraggledAtAQuarterOfItsMaximum()
    {
        var run = RunFixture.StartedInFirstFight(out var vanguard);
        int max = run.FindUnit(vanguard)!.MaxHp;

        run = RunFixture.Deploy(run);
        run = RunFixture.HurtTo(run, vanguard, 0);
        run = RunFixture.WinTheFight(run);

        // Between fights it reads as what it is: down, on nothing.
        Assert.Equal(RunUnitStatus.Downed, run.FindUnit(vanguard)!.Status);
        Assert.Equal(0, run.FindUnit(vanguard)!.Hp);
        Assert.True(run.FindUnit(vanguard)!.ReturnsBedraggled);

        run = RunFixture.Enter(run);

        // Was MaxHp / 2 until the Bedraggled ruling (MASTER_DESIGN §3, locked 2026-08-02(d)).
        Assert.Equal(Bedraggled.ReturningHp(max), RunFixture.OnBoard(run, vanguard).Hp);
        Assert.Equal(RunUnitStatus.Ready, run.FindUnit(vanguard)!.Status);
    }

    [Theory]
    [InlineData(UnitKind.Vanguard)]
    [InlineData(UnitKind.Archer)]
    [InlineData(UnitKind.Threadcaster)]
    [InlineData(UnitKind.Wardbearer)]
    public void Run_AQuarterOfTheMaximumRoundsUpForEveryClass(UnitKind kind)
    {
        int max = UnitTemplate.For(kind).MaxHp;
        var unit = RunUnit.Fresh(new RunUnitId(0), kind) with { Hp = 0, Status = RunUnitStatus.Downed };

        Assert.Equal(Bedraggled.ReturningHp(max), unit.FieldingHp);
        Assert.True(unit.FieldingHp >= 1, "the return floor is 1, not " + unit.FieldingHp);
        Assert.True(unit.FieldingHp * 4 >= max, "a quarter of " + max + " rounded down to " + unit.FieldingHp);
    }

    [Fact]
    public void Run_AVoidedUnitStaysDeadAndIsNeverFieldedAgain()
    {
        var run = RunFixture.StartedInFirstFight(UnitKind.Archer, out var archer);
        run = RunFixture.Deploy(run);
        run = RunFixture.Void(run, archer);
        run = RunFixture.WinTheFight(run);

        Assert.Equal(RunUnitStatus.Voided, run.FindUnit(archer)!.Status);
        Assert.DoesNotContain(run.Available(), u => u.Id.Equals(archer));

        // And it stays gone: every later fight is fought a body down.
        for (int i = 0; i < 3 && run.Phase != RunPhase.Complete; i++)
        {
            run = RunFixture.Enter(run);
            if (run.Fight is not null)
            {
                Assert.DoesNotContain(
                    run.Fight.Units,
                    u => u.Team.IsPlayer() && u.Kind == UnitKind.Archer);
                run = RunFixture.WinTheFight(run);
            }
        }
    }

    [Fact]
    public void Run_AVoidedUnitLeavesItsSideShortRatherThanBeingReplaced()
    {
        var run = RunFixture.StartedInFirstFight(UnitKind.Archer, out var archer);
        var kind = run.FindUnit(archer)!.Kind;
        int before = run.Fight!.Units.Count(u => u.Team.IsPlayer());
        run = RunFixture.Deploy(run);

        run = RunFixture.Void(run, archer);
        run = RunFixture.WinTheFight(run);
        run = RunFixture.Enter(run);

        Assert.Equal(before - 1, run.Fight!.Units.Count(u => u.Team.IsPlayer()));
        Assert.DoesNotContain(run.Fight.Units, u => u.Team.IsPlayer() && u.Kind == kind);
    }

    // --- Rest -------------------------------------------------------------------------------------

    [Fact]
    public void Rest_FullyHealsEveryLivingUnit()
    {
        var run = RunFixture.AtTheFirstRest(out var hurt);

        Assert.NotEmpty(hurt);
        run = RunFixture.Enter(run);

        foreach (var unit in run.Squad.Where(u => u.IsAvailable))
        {
            Assert.Equal(unit.MaxHp, unit.Hp);
            Assert.Equal(RunUnitStatus.Ready, unit.Status);
        }
    }

    [Fact]
    public void Rest_ClearsADownedMarkToo()
    {
        // "Fully heals all living units" — and the only thing that is not living is voided.
        var run = RunFixture.StartedInFirstFight(out var vanguard);
        run = RunFixture.Deploy(run);
        run = RunFixture.HurtTo(run, vanguard, 0);
        run = RunFixture.WinTheFight(run);
        Assert.Equal(RunUnitStatus.Downed, run.FindUnit(vanguard)!.Status);

        run = RunFixture.PlayForwardToRest(run);
        run = RunFixture.Enter(run);

        Assert.Equal(RunUnitStatus.Ready, run.FindUnit(vanguard)!.Status);
        Assert.Equal(run.FindUnit(vanguard)!.MaxHp, run.FindUnit(vanguard)!.Hp);
    }

    [Fact]
    public void Rest_DoesNotBringBackAVoidedUnit()
    {
        var run = RunFixture.StartedInFirstFight(UnitKind.Archer, out var archer);
        run = RunFixture.Deploy(run);
        run = RunFixture.Void(run, archer);
        run = RunFixture.WinTheFight(run);

        run = RunFixture.PlayForwardToRest(run);
        run = RunFixture.Enter(run);

        Assert.Equal(RunUnitStatus.Voided, run.FindUnit(archer)!.Status);
        Assert.Equal(0, run.FindUnit(archer)!.Hp);
    }

    [Fact]
    public void Rest_ResolvesOnEntryAndAdvancesWithoutHoldingControl()
    {
        var run = RunFixture.AtTheFirstRest(out _);
        int index = run.NodeIndex;

        var step = Campaign.ApplyRun(run, new EnterNodeCommand());

        Assert.Equal(index + 1, step.NewState.NodeIndex);
        Assert.Equal(RunPhase.AtNode, step.NewState.Phase);
        Assert.Null(step.NewState.Fight);
        Assert.Empty(step.FightEvents);
    }

    [Fact]
    public void Rest_ReportsWhatItRestoredWithFullPayloads()
    {
        var run = RunFixture.AtTheFirstRest(out var hurt);

        var step = Campaign.ApplyRun(run, new EnterNodeCommand());
        var rested = step.All<UnitRested>();

        Assert.Equal(hurt.Count, rested.Count);
        foreach (var e in rested)
        {
            Assert.True(e.To > e.From, "a rest that restored nothing was still reported");
            Assert.Equal(UnitTemplate.For(e.Kind).MaxHp, e.To);
        }
    }

    // --- The campaign shape -----------------------------------------------------------------------

    [Fact]
    public void Campaign_IsTenFightsWithACheckpointAfterTheFourthAndTheEighth()
    {
        var nodes = CampaignLibrary.Faultline.Nodes;

        Assert.Equal(12, nodes.Count);
        Assert.IsType<RestNode>(nodes[4]);
        Assert.IsType<RestNode>(nodes[9]);
        Assert.Equal(10, nodes.Count(n => n is FightNode));

        // Four fights, rest, four fights, rest, two fights.
        Assert.All(nodes.Take(4), n => Assert.IsType<FightNode>(n));
        Assert.All(nodes.Skip(5).Take(4), n => Assert.IsType<FightNode>(n));
        Assert.All(nodes.Skip(10), n => Assert.IsType<FightNode>(n));
    }

    [Fact]
    public void Campaign_PlaysTheCuratedSetSpineInOrder()
    {
        Assert.Equal(
            new[]
            {
                "first-contact",
                "cb-06-bait-and-break",
                "the-teeth",
                "broken-bridge",
                "the-shrine",
                "break-the-gate",
                "high-road",
                "hz-09-the-trench",
                "hold-the-gate",
                "quarry-king",
            },
            CampaignLibrary.Faultline.FightIds());
    }

    [Fact]
    public void Campaign_EveryFightItNamesExistsAndRostersOnlyTheSquad()
    {
        var squad = CampaignLibrary.Faultline.Squad;

        foreach (var id in CampaignLibrary.Faultline.FightIds())
        {
            var fight = FightLibrary.ById(id);

            foreach (var kind in fight.RosterA.Concat(fight.RosterB))
            {
                Assert.Contains(kind, squad);
            }
        }
    }

    [Fact]
    public void Campaign_TheSpineOrderIsNotTheLibraryOrder()
    {
        // The reason nodes hold ids and not indexes: cb-06 is authoring number 506 and campaign slot
        // 2, so anything walking FightLibrary.All() in order would play a different game.
        var spine = CampaignLibrary.Faultline.FightIds();
        var byNumber = FightLibrary.All()
            .Where(f => spine.Contains(f.Id))
            .Select(f => f.Id)
            .ToList();

        Assert.NotEqual(spine, byNumber);
    }

    // --- Progression ------------------------------------------------------------------------------

    [Fact]
    public void Run_AWonFightAdvancesToTheNextNode()
    {
        var run = RunFixture.StartedInFirstFight(out _);
        int index = run.NodeIndex;

        run = RunFixture.WinTheFight(run);

        Assert.Equal(index + 1, run.NodeIndex);
        Assert.Equal(RunPhase.AtNode, run.Phase);
        Assert.Equal(RunOutcome.InProgress, run.Outcome);
        Assert.Equal(1, run.FightsWon);
    }

    [Fact]
    public void Run_ALostFightEndsTheRun()
    {
        // Driven for real rather than rigged. A board emptied of players between commands would be a
        // state no command can leave — the outcome is only checked when something is applied — and a
        // loss the engine reaches on its own is the only one worth asserting on. The squad opens on
        // one hit point each so the loss is certain without being arranged; it used to rely on the
        // first-legal driver dying at a particular seed, which is a fact about board tuning rather
        // than about the run layer, and it stopped being true when fight 1 was made survivable.
        var (run, _) = RunFixture.PlayForward(RunFixture.CrippledInFirstFight());

        Assert.Equal(RunOutcome.Lost, run.Outcome);
        Assert.Equal(RunPhase.Complete, run.Phase);
        Assert.Empty(Campaign.LegalRunCommands(run));
    }

    [Fact]
    public void Run_ALostFightStillCarriesItsCasualtiesOut()
    {
        // The squad is read off the finished board before the run is declared over, so a loss records
        // who was downed and who was lost rather than throwing the fight away.
        var (run, _) = RunFixture.PlayForward(RunFixture.CrippledInFirstFight());

        Assert.Equal(RunOutcome.Lost, run.Outcome);
        Assert.All(run.Squad, u => Assert.NotEqual(RunUnitStatus.Ready, u.Status));
    }

    [Fact]
    public void Run_AFightWonBeforeTheLossStillCounts()
    {
        var (run, _) = RunFixture.PlayWholeRun(seed: 4242);

        Assert.True(run.FightsWon > 0, "the run ended without clearing a single node");

        // Not FightsWon == NodeIndex: a rest advances the index without winning anything, and this
        // used to hold only because the run never got as far as the first one.
        int rests = CampaignLibrary.Faultline.Nodes
            .Take(run.NodeIndex)
            .Count(n => n is RestNode);

        Assert.Equal(run.FightsWon, run.NodeIndex - rests);
    }

    [Fact]
    public void Run_ClearingEveryNodeWinsIt()
    {
        var run = RunFixture.Start();

        while (run.Phase != RunPhase.Complete)
        {
            run = RunFixture.Enter(run);
            if (run.Phase == RunPhase.InFight)
            {
                run = RunFixture.WinTheFight(run);
            }
        }

        Assert.Equal(RunOutcome.Won, run.Outcome);
        Assert.Equal(10, run.FightsWon);
    }

    [Fact]
    public void Run_AtANodeTheOnlyLegalCommandIsToEnterIt()
    {
        var run = RunFixture.Start();

        var legal = Campaign.LegalRunCommands(run);

        Assert.Single(legal);
        Assert.IsType<EnterNodeCommand>(legal[0]);
    }

    [Fact]
    public void Run_InAFightTheLegalCommandsAreTheFightsOwn()
    {
        var run = RunFixture.StartedInFirstFight(out _);

        var legal = Campaign.LegalRunCommands(run);

        Assert.NotEmpty(legal);
        Assert.All(legal, c => Assert.IsType<PlayCommand>(c));
        Assert.Equal(
            Game.LegalCommands(run.Fight!),
            legal.Cast<PlayCommand>().Select(p => p.Command).ToList());
    }

    [Fact]
    public void Run_ACombatCommandBeforeTheFightHasBegunIsRefused()
    {
        var run = RunFixture.Start();

        Assert.Throws<System.InvalidOperationException>(() =>
            Campaign.ApplyRun(run, new PlayCommand(new EndActivationCommand(new UnitId(0)))));
    }

    [Fact]
    public void Run_EnteringTheSameNodeTwiceIsRefused()
    {
        var run = RunFixture.StartedInFirstFight(out _);

        Assert.Throws<System.InvalidOperationException>(() =>
            Campaign.ApplyRun(run, new EnterNodeCommand()));
    }

    // --- Determinism ------------------------------------------------------------------------------

    [Fact]
    public void Run_ReplaysFromSeedAndCommandLogToAnIdenticalStateAndHash()
    {
        var (played, log) = RunFixture.PlayWholeRun(seed: 4242);

        var replayed = Campaign.Replay(CampaignLibrary.Faultline, 4242, log);

        Assert.Equal(played, replayed);
        Assert.Equal(played.GetHashCode(), replayed.GetHashCode());
    }

    [Fact]
    public void Run_TheLogIsOneStreamCarryingBothLevels()
    {
        // A combat command reaches the fight wrapped, so there is exactly one thing to record.
        var (_, log) = RunFixture.PlayWholeRun(seed: 4242);

        Assert.Contains(log, c => c is EnterNodeCommand);
        Assert.Contains(log, c => c is PlayCommand);
        Assert.All(log, c => Assert.True(c is EnterNodeCommand or PlayCommand));
    }

    [Fact]
    public void Run_TheSameSeedPlayedTheSameWayIsTheSameRun()
    {
        var (first, _) = RunFixture.PlayWholeRun(seed: 77);
        var (second, _) = RunFixture.PlayWholeRun(seed: 77);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Run_HashingSeesTheFightInProgress()
    {
        // Not "two seeds hash differently" — the seed is hashed directly, so that would hold even if
        // the hash saw nothing else. The load-bearing claim is that two runs standing at the same
        // node with different boards under them are different runs.
        var start = RunFixture.StartedInFirstFight(out var vanguard);
        var deployed = RunFixture.Deploy(start);
        var hurt = RunFixture.HurtTo(deployed, vanguard, 1);

        Assert.NotEqual(deployed, hurt);
        Assert.NotEqual(deployed.GetHashCode(), hurt.GetHashCode());
    }

    [Fact]
    public void Run_HashingSeesCarriedDamage()
    {
        // The run hash has to notice the thing the run is for. A squad on full health and the same
        // squad three fights of attrition later must not hash alike.
        var fresh = RunFixture.Start();
        var hurt = fresh.WithUnit(fresh.Squad[0] with { Hp = 1 });

        Assert.NotEqual(fresh.GetHashCode(), hurt.GetHashCode());
        Assert.NotEqual(fresh, hurt);
    }

    // --- The seam ---------------------------------------------------------------------------------

    [Fact]
    public void Seam_EveryNodeTypeInTheShippedCampaignHasAHandler()
    {
        foreach (var node in CampaignLibrary.Faultline.Nodes)
        {
            Assert.True(
                CampaignNodeHandlers.IsRegistered(node.GetType()),
                node.GetType().Name + " has no handler.");
        }
    }

    [Fact]
    public void Seam_ThereAreExactlyTwoNodeTypes()
    {
        // Pinned deliberately. A third node type is a change worth noticing in a diff, not something
        // that appears because a handler was added in passing.
        Assert.Equal(2, CampaignNodeHandlers.Count);
        Assert.True(CampaignNodeHandlers.IsRegistered(typeof(FightNode)));
        Assert.True(CampaignNodeHandlers.IsRegistered(typeof(RestNode)));
    }

    [Fact]
    public void Seam_AnUnregisteredNodeTypeIsRefusedByName()
    {
        Assert.Throws<System.NotSupportedException>(() =>
            CampaignNodeHandlers.For(new UnknownNode()));
    }

    [Fact]
    public void Seam_ANodeThatResolvesOnEntryTakesNoCommands()
    {
        var handler = CampaignNodeHandlers.For(new RestNode());

        Assert.False(handler.HoldsControl(RunFixture.Start()));
        Assert.Empty(handler.LegalSteps(RunFixture.Start(), new RestNode()));
        Assert.Throws<System.InvalidOperationException>(() =>
            handler.Step(RunFixture.Start(), new RestNode(), new EnterNodeCommand(), new RunContext()));
    }


    // --- What a step reports, and what a save holds ------------------------------------------------

    [Fact]
    public void Step_ReportsTheBoardTheWinningBlowLandedOn()
    {
        // The winning command is also the command that leaves the fight, so RunState.Fight is already
        // cleared by the time the caller sees it (D-055). Without FinalBoard a renderer could never
        // draw the blow that ended the fight.
        var run = RunFixture.StartedInFirstFight(out _);
        var step = RunFixture.EndFightInAWin(run);

        Assert.Null(step.NewState.Fight);
        Assert.NotNull(step.FinalBoard);
        Assert.Equal(FightOutcome.Won, step.FinalBoard!.Outcome);
        Assert.NotEmpty(step.FightEvents);
    }

    [Fact]
    public void Step_ReportsTheBoardForAnOrdinaryCommandToo()
    {
        var run = RunFixture.StartedInFirstFight(out _);

        var step = Campaign.ApplyRun(run, Campaign.LegalRunCommands(run)[0]);

        Assert.NotNull(step.FinalBoard);
        Assert.Equal(step.NewState.Fight, step.FinalBoard);
    }

    [Fact]
    public void Step_EnteringARestReportsNoBoardAtAll()
    {
        var run = RunFixture.AtTheFirstRest(out _);

        var step = Campaign.ApplyRun(run, new EnterNodeCommand());

        Assert.Null(step.FinalBoard);
    }

    [Fact]
    public void Restore_BringsBackTheRunButNotTheBoard()
    {
        var run = RunFixture.StartedInFirstFight(out var vanguard);
        run = RunFixture.Deploy(run);
        run = RunFixture.HurtTo(run, vanguard, 2);
        run = RunFixture.WinTheFight(run);
        run = RunFixture.Enter(run);

        var restored = Campaign.Restore(
            run.Campaign, run.Seed, run.NodeIndex, run.Squad, run.FightsWon, run.Outcome);

        Assert.Equal(run.NodeIndex, restored.NodeIndex);
        Assert.Equal(2, restored.FindUnit(vanguard)!.Hp);
        Assert.Equal(RunPhase.AtNode, restored.Phase);
        Assert.Null(restored.Fight);
        Assert.Empty(restored.Bindings);

        // And it can be played on: the node it stands on is entered again from deployment.
        var again = RunFixture.Enter(restored);
        Assert.Equal(RunPhase.InFight, again.Phase);
        Assert.Equal(Phase.Deployment, again.Fight!.Phase);
        Assert.Equal(2, RunFixture.OnBoard(again, vanguard).Hp);
    }

    [Fact]
    public void Restore_RefusesASquadThatIsNotThisCampaigns()
    {
        var run = RunFixture.Start();
        var wrong = new List<RunUnit>(run.Squad) { RunUnit.Fresh(new RunUnitId(9), UnitKind.Husk) };

        Assert.Throws<System.ArgumentException>(() =>
            Campaign.Restore(run.Campaign, run.Seed, 0, wrong, 0, RunOutcome.InProgress));

        var swapped = run.Squad.Reverse().ToList();
        Assert.Throws<System.ArgumentException>(() =>
            Campaign.Restore(run.Campaign, run.Seed, 0, swapped, 0, RunOutcome.InProgress));
    }

    [Fact]
    public void Restore_ARunPastItsLastNodeIsAWonRunNotAPlayableOne()
    {
        var run = RunFixture.Start();

        var restored = Campaign.Restore(
            run.Campaign, run.Seed, run.Campaign.Length, run.Squad, 10, RunOutcome.InProgress);

        Assert.Equal(RunOutcome.Won, restored.Outcome);
        Assert.Equal(RunPhase.Complete, restored.Phase);
        Assert.Empty(Campaign.LegalRunCommands(restored));
    }

    [Fact]
    public void Restore_ARestoredRunPlaysOnIdenticallyToTheOneItCameFrom()
    {
        var run = RunFixture.StartedInFirstFight(out _);
        run = RunFixture.WinTheFight(run);

        var restored = Campaign.Restore(
            run.Campaign, run.Seed, run.NodeIndex, run.Squad, run.FightsWon, run.Outcome);

        Assert.Equal(run.GetHashCode(), restored.GetHashCode());
        Assert.Equal(run, restored);
    }


    [Fact]
    public void Carrying_ReportsWhatTheUnitWillFieldAtSoNoRendererHasToWorkItOut()
    {
        // A renderer computing the quarter itself to draw this event is holding a copy of the
        // Bedraggled return rule. The event carries the number instead.
        var run = RunFixture.StartedInFirstFight(out var vanguard);
        run = RunFixture.Deploy(run);
        run = RunFixture.HurtTo(run, vanguard, 0);

        var step = RunFixture.EndFightInAWin(run);
        var carried = step.All<UnitCarried>().Single(c => c.RunUnitId.Equals(vanguard));

        Assert.Equal(RunUnitStatus.Downed, carried.Status);
        Assert.Equal(0, carried.Hp);
        Assert.Equal(Bedraggled.ReturningHp(carried.MaxHp), carried.FieldingHp);

        // And it is the truth: that is what actually walks onto the next board.
        var next = RunFixture.Enter(step.NewState);
        Assert.Equal(carried.FieldingHp, RunFixture.OnBoard(next, vanguard).Hp);
    }

    [Fact]
    public void Carrying_AVoidedUnitFieldsAtNothingBecauseItDoesNotField()
    {
        var run = RunFixture.StartedInFirstFight(UnitKind.Archer, out var archer);
        run = RunFixture.Deploy(run);
        run = RunFixture.Void(run, archer);

        var step = RunFixture.EndFightInAWin(run);
        var carried = step.All<UnitCarried>().Single(c => c.RunUnitId.Equals(archer));

        Assert.Equal(RunUnitStatus.Voided, carried.Status);
        Assert.Equal(0, carried.FieldingHp);
    }

    [Fact]
    public void Carrying_ASurvivorFieldsAtExactlyWhatItCarried()
    {
        var run = RunFixture.StartedInFirstFight(out var vanguard);
        run = RunFixture.Deploy(run);
        run = RunFixture.HurtTo(run, vanguard, 3);

        var step = RunFixture.EndFightInAWin(run);
        var carried = step.All<UnitCarried>().Single(c => c.RunUnitId.Equals(vanguard));

        Assert.Equal(3, carried.Hp);
        Assert.Equal(3, carried.FieldingHp);
    }


    [Fact]
    public void EveryCampaignBoardAuthorsTheDefaultSplit()
    {
        // D-092 resolves the split at run start, which made the files free to disagree with it —
        // and they did, so a campaign board played standalone from the picker fielded the old teams
        // while the same board inside a run fielded the new ones. The runtime resolution stays as
        // the guard; this is what stops the files drifting under it again.
        foreach (var node in CampaignLibrary.Faultline.Nodes.OfType<FightNode>())
        {
            var fight = FightLibrary.ById(node.FightId);

            DefaultTeams.Split(
                fight.RosterA.Concat(fight.RosterB), out var expectedA, out var expectedB);

            Assert.Equal(expectedA, fight.RosterA);
            Assert.Equal(expectedB, fight.RosterB);
        }
    }

    [Fact]
    public void ACampaignBoardFieldsTheSameTeamsInARunAsOnItsOwn()
    {
        // The symptom that sent me looking: the two paths have to agree.
        var fight = FightLibrary.Fight1();
        var standalone = Game.Start(fight, seed: 1).NewState;

        var run = Campaign.ApplyRun(
            Campaign.Start(CampaignLibrary.Faultline, seed: 1).NewState,
            new EnterNodeCommand()).NewState;

        Assert.Equal(
            Sides(standalone),
            Sides(run.Fight!));

        static IEnumerable<string> Sides(GameState state) =>
            state.Units
                .Where(u => u.Team.IsPlayer())
                .Select(u => u.Team + ":" + u.Kind)
                .OrderBy(s => s, System.StringComparer.Ordinal)
                .ToList();
    }

    [Fact]
    public void PlayerAOpensWithTheVanguardAndTheFisher()
    {
        var run = Campaign.ApplyRun(
            Campaign.Start(CampaignLibrary.Faultline, seed: 1).NewState,
            new EnterNodeCommand()).NewState;

        var a = run.Fight!.Units.Where(u => u.Team == Team.PlayerA).Select(u => u.Kind).ToList();
        var b = run.Fight!.Units.Where(u => u.Team == Team.PlayerB).Select(u => u.Kind).ToList();

        Assert.Equal(new[] { UnitKind.Vanguard, UnitKind.Threadcaster }, a);
        Assert.Equal(new[] { UnitKind.Wardbearer, UnitKind.Archer }, b);
    }

    [Fact]
    public void Run_ASideWithNothingLeftToFieldEndsTheRunRatherThanFreezingIt()
    {
        // Found by tools/Faultline.Playtest on its first sweep: a run that had lost both of one
        // player's classes walked into a board and opened deployment on a side with nothing to
        // place — no legal command, never reaching the objective check, so the fight could not
        // start, end, or be left. Frozen is worse than lost.
        //
        // Whose two classes those are is read from the default split rather than named here: under
        // D-092 the Vanguard and the Archer are on opposite sides, and hard-coding them made this
        // test silently stop testing anything the day that changed.
        var run = RunFixture.Start();

        var gutted = run.Squad
            .Select(u => DefaultTeams.SideFor(u.Kind) == Team.PlayerA
                ? u with { Hp = 0, Status = RunUnitStatus.Voided }
                : u)
            .ToList();

        var state = run with { Squad = gutted };

        Assert.NotEmpty(state.Available());

        var step = Campaign.ApplyRun(state, new EnterNodeCommand());

        Assert.Equal(RunOutcome.Lost, step.NewState.Outcome);
        Assert.Equal(RunPhase.Complete, step.NewState.Phase);
        Assert.Contains("no units left to field", step.Single<RunLost>().Reason);
        Assert.Empty(Campaign.LegalRunCommands(step.NewState));
    }

    [Fact]
    public void Run_ASideLosingOnlyOneOfItsTwoClassesStillPlays()
    {
        // The guard has to be about an empty roster, not about casualties. One down is a fight you
        // fight a body short, which is the whole point of carrying losses forward.
        var run = RunFixture.Start();

        var gutted = run.Squad
            .Select(u => u.Kind == UnitKind.Archer ? u with { Hp = 0, Status = RunUnitStatus.Voided } : u)
            .ToList();

        var step = Campaign.ApplyRun(run with { Squad = gutted }, new EnterNodeCommand());

        Assert.Equal(RunOutcome.InProgress, step.NewState.Outcome);
        Assert.Equal(RunPhase.InFight, step.NewState.Phase);
    }

    private sealed record UnknownNode : CampaignNode
    {
        public override string Describe() => "unknown";
    }
}
