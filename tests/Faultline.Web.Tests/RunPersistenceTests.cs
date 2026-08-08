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
                .With(Unlock.LongBoot)
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
    /// <b>A rearranged kit is run state, so the save carries it.</b> Three saves have now shipped
    /// that dropped a field Core had grown (D-125, the camp, D-222's destination); a duck that came
    /// back from a reload with the class kit it had traded away would be the fourth, and the most
    /// expensive, because nothing about it would look wrong.
    /// </summary>
    [Fact]
    public void ADucksAbilitySlots_RideInTheSaveAndComeBackInOrder()
    {
        var run = Campaign.Start(CampaignLibrary.Faultline, Seed).NewState;
        var ward = run.Squad.First(u => u.Kind == UnitKind.Wardbearer);
        var kit = Kits.SlotsOf(ward.Kind, ward.Loadout);

        // He trades Guard Stance away and keeps the spear — legal, and the point of the ruling.
        int stance = kit.ToList().IndexOf(KitEntry.GuardStance);
        var traded = run.WithUnit(ward with
        {
            Loadout = ward.Loadout.With(Mod.Thorough).Replacing(stance, KitEntry.StaggerShot, kit),
        });

        var text = RunSave.Of("0000000000000000005", traded).Render();

        // Still one space-free token, so the positional unit line still parses.
        var line = text.Split('\n').First(l => l.StartsWith("unit: " + ward.Id.Value + " ", StringComparison.Ordinal));
        Assert.Equal(8, line.Split(' ').Length);
        Assert.Contains("|s", line, StringComparison.Ordinal);

        var read = RunSave.Parse(text)!.Restore();
        var back = read.FindUnit(ward.Id)!;

        Assert.Equal(traded.FindUnit(ward.Id)!.Loadout, back.Loadout);
        Assert.Equal(KitEntry.StaggerShot, Kits.SlotsOf(back.Kind, back.Loadout)[stance]);
        Assert.DoesNotContain(KitEntry.GuardStance, Kits.SlotsOf(back.Kind, back.Loadout));

        // A duck whose kit is untouched writes no slot list at all, and still fields its whole kit.
        var fresh = read.Squad.First(u => u.Kind == UnitKind.Vanguard);
        Assert.Empty(fresh.Loadout.Slots);
        Assert.Equal(Kits.StartingKit(UnitKind.Vanguard), Kits.SlotsOf(fresh.Kind, fresh.Loadout));
    }

    /// <summary>
    /// <b>The Pluck slot, the disabled abilities and the granted slot counts all ride in the save.</b>
    /// Four saves have now shipped that dropped a field Core had grown — D-125, the camp, D-222's
    /// destination and D-229's epithet — so this loadout carries one of everything the slot system
    /// added and round-trips the lot at once rather than field by field (D-231, D-232).
    /// </summary>
    /// <remarks>
    /// <b>The epithet is deliberately not in this fixture.</b> D-229 is open and its fix is not this
    /// session's; a loadout carrying one would fail here for a reason that has nothing to do with
    /// slots, and papering over it with a fix in the wrong session is how the last three shipped.
    /// </remarks>
    /// <summary>
    /// A duck's epithet — the permanent legendary a gilt destination pays — survives a reload.
    /// </summary>
    /// <remarks>
    /// <b>It did not, and that made the gilt destination pay nothing.</b> `LoadoutText` wrote eight
    /// sections and the epithet was not one of them, so a run that took Follow Through at High Road
    /// came back without it. Worse, <see cref="DuckLoadout.IsEmpty"/> counts the epithet, so a duck
    /// carrying <em>only</em> one wrote as a bare dash — the save agreed the duck had nothing.
    ///
    /// The fifth instance of the same defect: Core grows a field and the record does not learn it
    /// (D-125's fork, D-127's camp, D-222's destination phase, the slot counts caught in time, and
    /// this). D-229 filed it; this closes it.
    /// </remarks>
    [Fact]
    public void ADucksEpithet_RidesInTheSave_AndADuckCarryingOnlyOneIsNotWrittenOffAsEmpty()
    {
        var run = Campaign.Start(CampaignLibrary.Faultline, Seed).NewState;
        var vanguard = run.Squad.First(u => u.Kind == UnitKind.Vanguard);

        // Nothing but the epithet, which is the case that used to write as "-".
        var crowned = vanguard.Loadout with { Epithet = Legendary.FollowThrough };
        Assert.False(crowned.IsEmpty);

        var text = RunSave.Of("0000000000000000008", run.WithUnit(vanguard with { Loadout = crowned })).Render();

        var line = text.Split('\n').First(l => l.StartsWith("unit: " + vanguard.Id.Value + " ", StringComparison.Ordinal));
        Assert.Equal(8, line.Split(' ').Length);
        Assert.Contains("|e", line, StringComparison.Ordinal);
        Assert.DoesNotContain(" - ", line, StringComparison.Ordinal);

        var back = RunSave.Parse(text)!.Restore().FindUnit(vanguard.Id)!;

        Assert.Equal(Legendary.FollowThrough, back.Loadout.Epithet);
        Assert.Equal(crowned, back.Loadout);
    }

    [Fact]
    public void APlucksSlotADisabledAbilityAndAGrantedSlot_AllRideInTheSave()
    {
        var run = Campaign.Start(CampaignLibrary.Faultline, Seed).NewState;
        var ward = run.Squad.First(u => u.Kind == UnitKind.Wardbearer);

        // Preen traded for Cast on the Pluck axis, Guard Stance traded off the ability axis, and a
        // slot granted on each — every field the slot system added, on one duck.
        var spenders = Kits.SpenderSlotsOf(ward.Kind, ward.Loadout);
        var abilities = Kits.SlotsOf(ward.Kind, ward.Loadout);
        int stance = abilities.ToList().IndexOf(KitEntry.GuardStance);

        var surgery = ward.Loadout
            .With(Mod.Thorough)
            .ReplacingSpender(spenders.ToList().IndexOf(KitEntry.Preen), KitEntry.Cast, spenders)
            .Replacing(stance, KitEntry.StaggerShot, abilities)
            with
        { ExtraAbilitySlots = 1, ExtraPluckSlots = 1 };

        var traded = run.WithUnit(ward with { Loadout = surgery });
        var text = RunSave.Of("0000000000000000007", traded).Render();

        // Still one space-free token, so the positional unit line still parses.
        var line = text.Split('\n').First(l => l.StartsWith("unit: " + ward.Id.Value + " ", StringComparison.Ordinal));
        Assert.Equal(8, line.Split(' ').Length);
        Assert.Contains("|k", line, StringComparison.Ordinal);
        Assert.Contains("|d", line, StringComparison.Ordinal);
        Assert.Contains("|x", line, StringComparison.Ordinal);

        var back = RunSave.Parse(text)!.Restore().FindUnit(ward.Id)!;

        Assert.Equal(surgery, back.Loadout);

        // Said again as behaviour, not as field equality: the same questions answer the same way.
        Assert.Equal(new[] { KitEntry.Cast }, Kits.SpenderSlotsOf(back.Kind, back.Loadout));
        Assert.False(Kits.Holds(back.Kind, back.Loadout, KitEntry.Preen));
        Assert.True(Kits.Knows(back.Kind, back.Loadout, KitEntry.Preen));
        Assert.True(Kits.Knows(back.Kind, back.Loadout, KitEntry.GuardStance));
        Assert.Equal(Kits.WardbearerSlots + 1, Kits.AbilitySlotsFor(back.Kind, back.Loadout));
        Assert.Equal(Kits.PluckSlotsPerDuck + 1, Kits.PluckSlotsFor(back.Kind, back.Loadout));

        // And a duck nothing touched still writes a bare dash rather than an empty tangle.
        var fresh = RunSave.Parse(text)!.Restore().Squad.First(u => u.Kind == UnitKind.Vanguard);
        Assert.True(fresh.Loadout.IsEmpty);
        Assert.Empty(fresh.Loadout.SpenderSlots);
        Assert.Empty(fresh.Loadout.Disabled);
        Assert.Equal(0, fresh.Loadout.ExtraPluckSlots);
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

        // No picks yet, so no line: a camp nobody has picked at writes nothing rather than an empty
        // key that a parser would have to decide what to do with.
        Assert.DoesNotContain("camp-picks:", text);
    }

    /// <summary>
    /// <b>A camp with one table spent is a state, and the save carries it.</b> Every player picks at
    /// every camp (D-247), so the run really does sit between the two picks — and D-125, D-127,
    /// D-222, D-231 and D-234 are five shipped bugs where Core grew a state and this record dropped
    /// it. The round trip is played into, not constructed: the fight is won and the first pick is
    /// actually taken.
    /// </summary>
    [Fact]
    public async Task AHalfPickedCampIsWrittenDown_SoAReloadStillOwesTheOtherPlayerAPick()
    {
        var storage = new FakeJsRuntime();
        var session = new RunSession(new RunStore(new FightFiles(storage)), new GameSession());
        await session.StartAsync(Seed, CampaignLibrary.Act1Id);

        Assert.Equal(FightOutcome.Won, CampPlayer.PlayCurrentFight(session));
        Assert.Equal(RunPhase.AtCamp, session.State!.Phase);

        var table = session.Camp!;
        session.PickCamp(Team.PlayerA, 1);
        Assert.Equal(RunPhase.AtCamp, session.State!.Phase);

        var text = RunSave.Of("0000000000000000005", session.State).Render();
        Assert.Contains("camp-picks: PlayerA:1\n", text);

        var reloaded = RunSave.Parse(text)!.Restore();

        Assert.Equal(RunPhase.AtCamp, reloaded.Phase);
        Assert.Equal(table, Camp.Draw(reloaded));
        Assert.True(Camp.HasPicked(reloaded, Team.PlayerA));
        Assert.False(Camp.HasPicked(reloaded, Team.PlayerB));

        // Only Player B is still owed a pick, and finishing from the reload hands out BOTH cards —
        // a save that dropped the line would have given Player A's away for nothing.
        var legal = Campaign.LegalRunCommands(reloaded);
        Assert.NotEmpty(legal);
        Assert.All(legal, c => Assert.Equal(Team.PlayerB, Assert.IsType<CampPickCommand>(c).Player));

        var done = Campaign.ApplyRun(reloaded, legal[0]).NewState;

        Assert.NotEqual(RunPhase.AtCamp, done.Phase);
        Assert.False(done.FindUnit(table.For(Team.PlayerA)[1].Duck)!.Loadout.IsEmpty);
        Assert.False(done.FindUnit(table.For(Team.PlayerB)[0].Duck)!.Loadout.IsEmpty);
    }

    /// <summary>
    /// A save written before camps took a pick each still loads. Its <c>last-pick:</c> and
    /// <c>previous-pick:</c> keys fed §8.6's ownership-fairness row, which dissolved (D-249); the
    /// parser reads by key, so their presence costs nothing and their absence is not an error.
    /// </summary>
    [Fact]
    public void AnOlderSaveCarryingTheRetiredFairnessKeys_StillLoads()
    {
        var run = Campaign.Start(CampaignLibrary.Act1, Seed).NewState;
        var text = RunSave.Of("0000000000000000006", run).Render()
            + "last-pick: PlayerA\nprevious-pick: PlayerA\n";

        var parsed = RunSave.Parse(text);

        Assert.NotNull(parsed);
        Assert.Empty(parsed!.CampPicks);
        Assert.Equal(run, parsed.Restore());
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
