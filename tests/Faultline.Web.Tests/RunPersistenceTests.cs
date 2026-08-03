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
        Assert.Contains("unit: 0 Vanguard 3 Ready\n", text);
        Assert.Contains("unit: 3 Wardbearer 0 Voided\n", text);
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
