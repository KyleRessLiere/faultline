using System;
using System.Collections.Generic;
using Faultline.Core;
using Faultline.Web.Shell;

namespace Faultline.Web.Tests;

/// <summary>
/// Progressing through the spine: wins advance, losses end the run, and campaign fights that have
/// not been authored yet are stepped over visibly rather than crashing or vanishing.
/// </summary>
public sealed class CampaignProgressTests
{
    /// <summary>A stand-in library, so "this fight does not exist yet" is testable either way.</summary>
    private static Dictionary<string, FightDefinition> Library(params string[] ids)
    {
        var byId = new Dictionary<string, FightDefinition>(StringComparer.Ordinal);
        foreach (var id in ids)
        {
            byId[id] = new FightDefinition
            {
                Id = id,
                Name = id,
                RosterA = new[] { UnitKind.Vanguard, UnitKind.Archer },
                RosterB = new[] { UnitKind.Threadcaster, UnitKind.Wardbearer },
            };
        }

        return byId;
    }

    private static GameState Won(GameState state) => state with { Outcome = FightOutcome.Won };

    private static GameState Blank()
    {
        var fight = new FightDefinition
        {
            Id = "blank",
            RosterA = new[] { UnitKind.Vanguard },
            RosterB = new[] { UnitKind.Archer },
        };

        return Game.Start(fight, 1).NewState;
    }

    [Fact]
    public void ANewRun_StartsOnTheFirstCampaignFight()
    {
        var run = CampaignRun.Begin("r", 3).Settle(CampaignPlan.Active());

        Assert.Equal(0, run.Index);
        Assert.Equal("first-contact", run.CurrentId);
        Assert.Equal(CampaignStatus.Playing, run.Status);
    }

    [Fact]
    public void AWin_AdvancesToTheNextFightAndRecordsTheClear()
    {
        var library = Library(CampaignPlan.Order[0], CampaignPlan.Order[1]);
        var run = CampaignRun.Begin("r", 3).Settle(library).Advance(Won(Blank()), library);

        Assert.Equal(1, run.Index);
        Assert.Equal(CampaignPlan.Order[1], run.CurrentId);
        Assert.Equal(new[] { CampaignPlan.Order[0] }, run.Cleared);
    }

    [Fact]
    public void ALoss_EndsTheRun()
    {
        var library = Library(CampaignPlan.Order[0], CampaignPlan.Order[1]);
        var run = CampaignRun.Begin("r", 3).Settle(library).Fail(Blank());

        Assert.Equal(CampaignStatus.Lost, run.Status);
        Assert.False(run.InProgress);
        Assert.Empty(run.Cleared);
    }

    [Fact]
    public void AnUnauthoredFight_IsSkippedAndNamedRatherThanSilentlyDropped()
    {
        // Slot 1 exists, slot 2 does not, slot 3 does.
        var library = Library(CampaignPlan.Order[0], CampaignPlan.Order[2]);
        var run = CampaignRun.Begin("r", 3).Settle(library).Advance(Won(Blank()), library);

        Assert.Equal(2, run.Index);
        Assert.Equal(CampaignPlan.Order[2], run.CurrentId);
        Assert.Contains(CampaignPlan.Order[1], run.Skipped);
        Assert.DoesNotContain(CampaignPlan.Order[1], run.Cleared);
    }

    [Fact]
    public void ARunOfNothingButUnauthoredFights_CompletesInsteadOfHanging()
    {
        var run = CampaignRun.Begin("r", 3).Settle(Library());

        Assert.Equal(CampaignStatus.Won, run.Status);
        Assert.Equal(CampaignPlan.Length, run.Skipped.Count);
        Assert.Null(run.CurrentId);
    }

    [Fact]
    public void ClearingTheLastFight_CompletesTheRun()
    {
        var library = Library(CampaignPlan.Order[CampaignPlan.Length - 1]);
        var run = (CampaignRun.Begin("r", 3) with { Index = CampaignPlan.Length - 1 })
            .Settle(library)
            .Advance(Won(Blank()), library);

        Assert.Equal(CampaignStatus.Won, run.Status);
        Assert.False(run.InProgress);
    }

    [Fact]
    public void AFightThatLandsLater_JoinsTheSpineOnItsOwn()
    {
        // A run parked on slot 0 while slot 1 is unauthored: it has not passed the gap yet, so the
        // file landing is enough. No migration, no code change.
        var before = CampaignRun.Begin("r", 3).Settle(Library(CampaignPlan.Order[0]));
        Assert.Equal(0, before.Index);

        var after = before.Settle(Library(CampaignPlan.Order[0], CampaignPlan.Order[1]));
        var advanced = after.Advance(Won(Blank()), Library(CampaignPlan.Order[0], CampaignPlan.Order[1]));

        Assert.Equal(CampaignPlan.Order[1], advanced.CurrentId);
        Assert.Empty(advanced.Skipped);
    }

    [Fact]
    public void ASideWithNoUnitsLeft_EndsTheRunInsteadOfStartingAnUnplayableFight()
    {
        // Player A's whole roster voided. Core's deployment phase would offer no legal command at
        // all, so the run has to stop before it gets there.
        var library = Library(CampaignPlan.Order[0], CampaignPlan.Order[1]);
        var run = (CampaignRun.Begin("r", 3) with
        {
            Lost = new[] { UnitKind.Vanguard, UnitKind.Archer },
        }).Settle(library);

        Assert.Equal(CampaignStatus.Wiped, run.Status);
        Assert.False(run.InProgress);
    }

    [Fact]
    public void ARun_SurvivesBeingWrittenToStorageAndReadBack()
    {
        var run = CampaignRun.Begin("0000000000000000042", 9) with
        {
            Index = 3,
            Cleared = new[] { "first-contact", "the-teeth" },
            Skipped = new[] { "the-shrine" },
            Lost = new[] { UnitKind.Archer, UnitKind.Wardbearer },
            Status = CampaignStatus.Playing,
        };

        var read = CampaignRun.Parse(run.Render());

        Assert.NotNull(read);
        Assert.Equal(run.Id, read!.Id);
        Assert.Equal(run.Seed, read.Seed);
        Assert.Equal(run.Index, read.Index);
        Assert.Equal(run.Status, read.Status);
        Assert.Equal(run.Cleared, read.Cleared);
        Assert.Equal(run.Skipped, read.Skipped);
        Assert.Equal(run.Lost, read.Lost);
    }

    [Fact]
    public void AnUnreadableRecord_ReadsBackAsNoRunRatherThanAnEmptyOne()
    {
        Assert.Null(CampaignRun.Parse(null));
        Assert.Null(CampaignRun.Parse(string.Empty));
        Assert.Null(CampaignRun.Parse("seed: 4\nindex: 2\n"));
    }
}
