using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Faultline.Core;
using Faultline.Web.Shell;

namespace Faultline.Web.Tests;

/// <summary>
/// What the shell is actually responsible for now that the run lives in Core: writing a run into
/// browser storage and getting the same squad back after a reload.
/// </summary>
/// <remarks>
/// Nothing here re-tests a rule. Carrying, the half-strength return, resting and replay determinism
/// are Core's, and Core's suite covers them. These tests only ask whether the four things
/// DECISIONS.md D-050 promises — seed, node, squad, and <em>not</em> the board — make the trip.
/// </remarks>
public sealed class RunPersistenceTests
{
    private const int Seed = 4242;

    [Fact]
    public async Task ASquadOnMixedHealth_SurvivesAReload()
    {
        var storage = new FakeJsRuntime();
        var run = Wounded();

        await new RunStore(new FightFiles(storage)).WriteAsync(run);

        // A second store over the same storage is exactly what a reload produces.
        var reloaded = await new RunStore(new FightFiles(storage)).ReadAsync();
        var restored = reloaded!.Restore();

        Assert.Equal(Seed, restored.Seed);
        Assert.Equal(6, restored.NodeIndex);
        Assert.Equal(4, restored.FightsWon);
        Assert.Equal(
            run.Squad.Select(u => (u.Kind, u.Hp, u.Status)),
            restored.Squad.Select(u => (u.Kind, u.Hp, u.Status)));
    }

    [Fact]
    public async Task ADownedAndAVoidedMember_ComeBackAsDownedAndVoided()
    {
        var restored = await RoundTrip(Wounded());

        var archer = restored.Squad.Single(u => u.Kind == UnitKind.Archer);
        var wardbearer = restored.Squad.Single(u => u.Kind == UnitKind.Wardbearer);

        Assert.Equal(RunUnitStatus.Downed, archer.Status);
        Assert.Equal(RunUnitStatus.Voided, wardbearer.Status);
        Assert.False(wardbearer.IsAvailable);
        Assert.Equal(3, restored.Available().Count);
    }

    [Fact]
    public async Task CarriedHitPoints_AreExactAfterAReload()
    {
        var restored = await RoundTrip(Wounded());

        var vanguard = restored.Squad.Single(u => u.Kind == UnitKind.Vanguard);

        Assert.Equal(3, vanguard.Hp);
        Assert.Equal(14, vanguard.MaxHp);
    }

    [Fact]
    public async Task AReload_RestoresTheRunAndNotTheBoard()
    {
        // D-050: the half-played fight does not survive, and the run comes back standing on its node
        // with the fight not yet entered, so it restarts from deployment.
        var run = Wounded() with
        {
            Phase = RunPhase.InFight,
            Fight = Game.Start(FightLibrary.Fight1(), Seed).NewState,
        };

        var storage = new FakeJsRuntime();
        await new RunStore(new FightFiles(storage)).WriteAsync(run);
        var save = await new RunStore(new FightFiles(storage)).ReadAsync();

        Assert.True(save!.WasInFight);

        var restored = save.Restore();
        Assert.Equal(RunPhase.AtNode, restored.Phase);
        Assert.Null(restored.Fight);
        Assert.Empty(restored.Bindings);
    }

    [Fact]
    public async Task AFinishedRun_ComesBackFinished()
    {
        var lost = Wounded() with { Phase = RunPhase.Complete, Outcome = RunOutcome.Lost };

        var restored = await RoundTrip(lost);

        Assert.Equal(RunOutcome.Lost, restored.Outcome);
        Assert.Equal(RunPhase.Complete, restored.Phase);
    }

    [Fact]
    public async Task AbandoningARun_LeavesNothingToReadBack()
    {
        var storage = new FakeJsRuntime();
        var store = new RunStore(new FightFiles(storage));

        await store.WriteAsync(Wounded());
        await store.ClearAsync();

        Assert.Null(await new RunStore(new FightFiles(storage)).ReadAsync());
    }

    [Fact]
    public void AnUnreadableRecord_ReadsBackAsNoRunRatherThanAnEmptyOne()
    {
        Assert.Null(RunSave.Parse(null));
        Assert.Null(RunSave.Parse(string.Empty));
        Assert.Null(RunSave.Parse("seed: 3\nnode: 2\n"));
    }

    [Fact]
    public void ARecordNamingAnUnknownCampaign_IsUnreadable()
    {
        // Erring towards "no run" beats restoring a run against a campaign this build cannot walk.
        Assert.Null(RunSave.Parse("id: 1\ncampaign: not-a-campaign\nseed: 1\nnode: 0\n"));
    }

    [Fact]
    public async Task ACorruptedSave_IsReportedRatherThanThrownAtTheScreen()
    {
        // Core validates the squad against the campaign. A record whose third slot holds a Vanguard
        // where the campaign fields a Threadcaster must reach the player as a sentence, not as an
        // unhandled exception on a blank page.
        var storage = new FakeJsRuntime();
        await new FightFiles(storage).SetAsync("faultline.run.1", string.Join("\n", new[]
        {
            "id: 1", "campaign: faultline", "seed: 1", "node: 3", "fights-won: 3",
            "outcome: InProgress", "in-fight: no",
            "unit: 0 Vanguard 7 Ready", "unit: 1 Archer 4 Ready",
            "unit: 2 Vanguard 4 Ready", "unit: 3 Wardbearer 6 Ready", string.Empty,
        }));
        await new FightFiles(storage).SetAsync("faultline.runs", "1");

        var session = new RunSession(new RunStore(new FightFiles(storage)), new GameSession());
        await session.LoadAsync();

        Assert.True(session.Loaded);
        Assert.Null(session.State);
        Assert.False(session.InProgress);
        Assert.NotNull(session.Problem);
        Assert.Contains("could not be read", session.Problem!);
    }

    [Fact]
    public async Task ASaveWithNoSquadAtAll_IsAlsoReportedRatherThanQuietlyFilledIn()
    {
        var storage = new FakeJsRuntime();
        await new FightFiles(storage).SetAsync(
            "faultline.run.1", "id: 1\ncampaign: faultline\nseed: 1\nnode: 2\n");
        await new FightFiles(storage).SetAsync("faultline.runs", "1");

        var session = new RunSession(new RunStore(new FightFiles(storage)), new GameSession());
        await session.LoadAsync();

        Assert.Null(session.State);
        Assert.NotNull(session.Problem);
    }

    [Fact]
    public void AnUnreadableSquadLine_IsDroppedRatherThanGuessedAt()
    {
        var save = RunSave.Parse(
            "id: 1\ncampaign: faultline\nseed: 1\nnode: 0\nunit: 0 Vanguard 3 Ready\nunit: 1 Wombat 9 Ready\n");

        Assert.Single(save!.Squad);
        Assert.Equal(UnitKind.Vanguard, save.Squad[0].Kind);
    }

    [Fact]
    public void TheStoredRecord_IsHandWrittenKeyValueLines()
    {
        // The format is readable in one place and depends on no serialiser — the same promise
        // CustomFightStore and PlaytestNotes make.
        var text = RunSave.Of("0000000000000000001", Wounded()).Render();

        Assert.Contains("seed: 4242\n", text);
        Assert.Contains("node: 6\n", text);

        // Seven positional values per member now: the four the linear ten always wrote, then the
        // meter and the ceiling a map run can change, then what the camps have hung on the duck —
        // a bare '-' while that is nothing. Appended rather than reshaped, so a record written
        // before any of the three still reads — see the four-field Parse test above.
        Assert.Contains("unit: 0 Vanguard 3 Ready 0 0 -\n", text);
        Assert.Contains("unit: 3 Wardbearer 0 Voided 0 0 -\n", text);

        // The run RNG's cursor rides along, so a restored run does not re-flip a coin it has spent.
        Assert.Contains("rng: ", text);

        // Both phases the node under them has already been cleared, and both written for every
        // shape: a linear run camps after its fights too, and a camp the save drops is a run
        // restored onto the fight it just won (D-125).
        Assert.Contains("at-vote: no\n", text);
        Assert.Contains("at-camp: no\n", text);

        // A linear campaign has no graph, so it writes no route at all rather than an empty one.
        Assert.DoesNotContain("route:", text);
    }

    /// <summary>
    /// What the camps gave a duck rides in the save, as one space-free token per member, and comes
    /// back the same. A loadout that vanished across a reload would be a run quietly rolled back.
    /// </summary>
    [Fact]
    public void ADucksLoadout_IsWrittenAsOneTokenAndReadBackWhole()
    {
        var run = Campaign.Start(CampaignLibrary.Faultline, Seed).NewState;
        var vanguard = run.Squad.First(u => u.Kind == UnitKind.Vanguard);

        var loaded = run.WithUnit(vanguard with
        {
            Loadout = DuckLoadout.Empty
                .With(Mod.Heavier)
                .With(Mod.Echo)
                .With(SecondWind.StaggerAnEnemy)
                .With(Unlock.Climber)
                .WithPocket(Consumable.OldRope),
        });

        var text = RunSave.Of("0000000000000000003", loaded).Render();

        // One token, no spaces in it, so the positional unit line still parses by splitting on space.
        var line = text.Split('\n').First(l => l.StartsWith("unit: 0 ", StringComparison.Ordinal));
        Assert.Equal(8, line.Split(' ').Length);

        var read = RunSave.Parse(text)!.Restore();

        Assert.Equal(
            loaded.FindUnit(vanguard.Id)!.Loadout,
            read.FindUnit(vanguard.Id)!.Loadout);

        // And a duck carrying nothing writes a bare dash rather than an empty tangle of separators.
        Assert.Contains("unit: 1 ", text);
        Assert.True(read.Squad.Where(u => u.Id != vanguard.Id).All(u => u.Loadout.IsEmpty));
    }

    /// <summary>
    /// A camp is a phase the save has to carry, for the reason a fork is: the node under it has
    /// already been cleared (D-125).
    /// </summary>
    [Fact]
    public void ACampIsWrittenDown_SoAReloadDoesNotWalkBackOntoTheClearedNode()
    {
        var run = Campaign.Start(CampaignLibrary.Act1, Seed).NewState;
        var camped = Campaign.Restore(
            CampaignLibrary.Act1, run.Seed, run.NodeIndex, run.Squad, run.FightsWon, run.Outcome,
            run.MapState, run.RngState, atVote: false, atCamp: true);

        var text = RunSave.Of("0000000000000000004", camped).Render();

        Assert.Contains("at-camp: yes\n", text);
        Assert.Equal(RunPhase.AtCamp, RunSave.Parse(text)!.Restore().Phase);
    }

    /// <summary>A map run's position is its route, and the route makes the trip whole and in order.</summary>
    [Fact]
    public void AMapRunsRoute_IsWrittenInOrderAndReadBack()
    {
        var run = Campaign.Start(CampaignLibrary.Act1, Seed).NewState;
        var walked = run with
        {
            MapState = run.MapState!.MoveTo("c2-the-teeth").MoveTo("c3-molting-pool"),
            RngState = 99,
        };

        var text = RunSave.Of("0000000000000000002", walked).Render();

        Assert.Contains("route: c1-first-contact>c2-the-teeth>c3-molting-pool\n", text);
        Assert.Contains("act-cleared: no\n", text);
        Assert.Contains("rng: 99\n", text);

        var read = RunSave.Parse(text)!;

        Assert.Equal(walked.MapState.Route, read.Route);
        Assert.Equal(99, read.RngState);
        Assert.Equal(walked.MapState.RouteHash(), read.Restore().MapState!.RouteHash());
    }

    private static async Task<RunState> RoundTrip(RunState run)
    {
        var storage = new FakeJsRuntime();
        await new RunStore(new FightFiles(storage)).WriteAsync(run);
        var save = await new RunStore(new FightFiles(storage)).ReadAsync();
        return save!.Restore();
    }

    /// <summary>A run four fights in, on mixed health, with one member down and one lost.</summary>
    private static RunState Wounded()
    {
        var run = Campaign.Start(CampaignLibrary.Faultline, Seed).NewState;

        var squad = new List<RunUnit>();
        foreach (var unit in run.Squad)
        {
            squad.Add(unit.Kind switch
            {
                UnitKind.Vanguard => unit with { Hp = 3 },
                UnitKind.Archer => unit with { Hp = 0, Status = RunUnitStatus.Downed },
                UnitKind.Wardbearer => unit with { Hp = 0, Status = RunUnitStatus.Voided },
                _ => unit,
            });
        }

        return run with { NodeIndex = 6, FightsWon = 4, Squad = squad };
    }
}
