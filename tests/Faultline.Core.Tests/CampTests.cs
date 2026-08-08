using System;
using System.Collections.Generic;
using System.Linq;
using Faultline.Core;
using Xunit;

namespace Faultline.Core.Tests;

/// <summary>
/// The Camp: after every won Fight or Elite, <b>two tables of two and one pick each</b>
/// (MASTER_DESIGN §8.6's offer director, as D-247 restates it; the one-table shape it reverses is
/// D-154, and D-127 is the two-table camp before either).
/// </summary>
/// <remarks>
/// These are about the <em>offer</em> — that it is dealt from the run seed, that it is the same four
/// cards on a replay, that every player gets a table, and that nothing outside the built pools can
/// ever be on one. What each card <em>does</em> is pinned in ModTests, SecondWindTests, UnlockTests
/// and ConsumableTests.
/// </remarks>
public class CampTests
{
    private static readonly Team[] Sides = { Team.PlayerA, Team.PlayerB };

    // ---- the seam -----------------------------------------------------------------------------------

    [Fact]
    public void AWonFight_StopsTheRunAtACampBeforeTheNextNode()
    {
        var run = RunFixture.StartedInFirstFight(out _);
        var step = RunFixture.EndFightInAWin(run);

        Assert.Equal(RunPhase.AtCamp, step.NewState.Phase);

        var offered = step.Single<CampOffered>();
        Assert.Equal("first-contact", offered.FightId);
        Assert.False(offered.Table.IsEmpty);
    }

    [Fact]
    public void ACamp_OffersEveryPlayerTwoCards_AndNothingElseIsLegal()
    {
        var run = AtACamp(out var table);

        foreach (var player in Sides)
        {
            Assert.Equal(Camp.OffersPerTable, table.For(player).Count);
        }

        // One command per card of every unspent table, and nothing else. There is deliberately no
        // decline on the list: camps are the reward, and turning one down is not a decision
        // (MASTER_DESIGN §8.5).
        var legal = Campaign.LegalRunCommands(run);
        Assert.Equal(Camp.OffersPerTable * Sides.Length, legal.Count);
        Assert.All(legal, c => Assert.IsType<CampPickCommand>(c));

        // And every player is on the list, which is the whole of "each player picks" (D-247).
        Assert.Equal(
            Sides,
            legal.Cast<CampPickCommand>().Select(c => c.Player).Distinct().OrderBy(t => t).ToArray());
    }

    // ---- the camp does not resolve until both tables are spent (I2 / D-251) -----------------------

    [Fact]
    public void OnePick_LeavesTheRunAtTheCamp_WithOnlyTheOtherTableStillOnOffer()
    {
        var run = AtACamp(out var table);

        var step = Campaign.ApplyRun(run, new CampPickCommand(table, Team.PlayerA, 0));
        var after = step.NewState;

        Assert.Equal(RunPhase.AtCamp, after.Phase);
        Assert.True(Camp.HasPicked(after, Team.PlayerA));
        Assert.False(Camp.HasPicked(after, Team.PlayerB));

        // Everything still legal is Player B's, so the camp has no completion path that does not run
        // through them. That is the construction, not a guard: the exit condition IS this list.
        var legal = Campaign.LegalRunCommands(after);
        Assert.Equal(Camp.OffersPerTable, legal.Count);
        Assert.All(legal, c => Assert.Equal(Team.PlayerB, Assert.IsType<CampPickCommand>(c).Player));

        // And Player A cannot take a second card off a spent table — refused by name, not ignored.
        var refusal = Assert.Throws<InvalidOperationException>(
            () => Campaign.ApplyRun(after, new CampPickCommand(table, Team.PlayerA, 1)));
        Assert.Contains("already picked", refusal.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ACampCannotResolveWithAnUnspentTable_AtEverySeed()
    {
        // Asserted on the node's completion path, not on any UI state: after one pick the run's phase
        // is still AtCamp and its node index has not moved, whichever player picked first.
        for (int seed = 1; seed <= 12; seed++)
        {
            foreach (var first in Sides)
            {
                var run = AtACamp(out var table, seed);
                int node = run.NodeIndex;

                var half = Campaign.ApplyRun(run, new CampPickCommand(table, first, 0)).NewState;

                Assert.Equal(RunPhase.AtCamp, half.Phase);
                Assert.Equal(node, half.NodeIndex);
                Assert.NotEmpty(Camp.Unspent(half, table));

                var done = Campaign.ApplyRun(
                    half, new CampPickCommand(table, first.OtherPlayer(), 0)).NewState;

                Assert.NotEqual(RunPhase.AtCamp, done.Phase);
                Assert.Empty(done.CampPicks);
            }
        }
    }

    [Fact]
    public void EitherPlayerMayPickFirst_AndTheCampEndsInTheSamePlaceEitherWay()
    {
        var runA = AtACamp(out var table, 4242);
        var runB = AtACamp(out _, 4242);

        var aFirst = Campaign.ApplyRun(runA, new CampPickCommand(table, Team.PlayerA, 0)).NewState;
        aFirst = Campaign.ApplyRun(aFirst, new CampPickCommand(table, Team.PlayerB, 1)).NewState;

        var bFirst = Campaign.ApplyRun(runB, new CampPickCommand(table, Team.PlayerB, 1)).NewState;
        bFirst = Campaign.ApplyRun(bFirst, new CampPickCommand(table, Team.PlayerA, 0)).NewState;

        // Order does not matter (I2). The recorded order differs — it is the log — so the squads and
        // the cursor are compared rather than the whole state.
        Assert.Equal(aFirst.Phase, bFirst.Phase);
        Assert.Equal(aFirst.RngState, bFirst.RngState);
        Assert.Equal(aFirst.CampsHeld, bFirst.CampsHeld);
        Assert.Equal(aFirst.Squad, bFirst.Squad);
    }

    [Fact]
    public void ACampWithAPickOutstanding_TakesNothingElse_AndSaysThereIsNoSkip()
    {
        var run = AtACamp(out var table);
        var half = Campaign.ApplyRun(run, new CampPickCommand(table, Team.PlayerA, 0)).NewState;

        var refusal = Assert.Throws<InvalidOperationException>(
            () => Campaign.ApplyRun(half, new EnterNodeCommand()));

        Assert.Contains("camp", refusal.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no skip", refusal.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ---- the log, which is the regression instrument (I3) -----------------------------------------

    [Fact]
    public void EveryCamp_EmitsOneLinePerPlayer_NamingThePlayerTheCardAndTheDuck()
    {
        // THE INSTRUMENT. Under D-247 a camp that produced one line is a bug report: the missing line
        // means either a table went unspent or a pick went unrecorded, and neither is legal. This
        // fails the moment that happens, without anybody watching a screen.
        var run = RunFixture.Start(4242);
        int camps = 0;

        for (int leg = 0; leg < 40 && run.Phase != RunPhase.Complete; leg++)
        {
            run = RunFixture.PlayForward(run, until: r => r.Phase == RunPhase.AtCamp).State;
            if (run.Phase != RunPhase.AtCamp)
            {
                break;
            }

            {
                var table = Camp.Draw(run);
                var lines = new List<CampTaken>();

                // Who is OWED a line, worked out from the squad rather than from the table — so a
                // table that quietly went missing cannot also lower the bar it is measured against.
                var owed = Sides.Where(p => Camp.DucksFor(run, p).Count > 0).ToArray();

                foreach (var seat in table.Seats)
                {
                    var step = Campaign.ApplyRun(run, new CampPickCommand(table, seat.Player, 0));
                    lines.AddRange(step.All<CampTaken>());
                    run = step.NewState;
                }

                camps++;

                Assert.Equal(owed.Length, lines.Count);
                Assert.Equal(owed, lines.Select(l => l.Player).OrderBy(t => t).ToArray());

                foreach (var line in lines)
                {
                    Assert.NotNull(run.FindUnit(line.Duck));
                    Assert.NotEmpty(line.Name);
                    Assert.NotEmpty(line.Summary);

                    // The recipient duck is the one named on the card, and it is that player's.
                    Assert.Equal(line.Offer.Duck, line.Duck);
                    Assert.Equal(line.Player, DefaultTeams.SideFor(line.Kind));
                }
            }
        }

        Assert.True(camps > 0, "The run walked no camps, so the instrument measured nothing.");
    }

    [Fact]
    public void AtACamp_TheOnlyThingTheRunTakesIsAPick_AndItSaysThereIsNoSkip()
    {
        var run = AtACamp(out _);

        var refusal = Assert.Throws<InvalidOperationException>(
            () => Campaign.ApplyRun(run, new EnterNodeCommand()));

        Assert.Contains("camp", refusal.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no skip", refusal.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EveryCardIsForADuckOfTheTableItSitsOn()
    {
        var run = AtACamp(out var table);

        // D-092's loadout is ownership: Player A holds the Vanguard and the Fisher, Player B the
        // Wardbearer and the Archer. Since D-247 a table is addressed to its owner's ducks, so what
        // has to hold is stronger than "the card can say whose duck it is" — the card is on the right
        // table, and there is no way to be handed one that is not.
        foreach (var seat in table.Seats)
        {
            Assert.True(seat.Player.IsPlayer());

            foreach (var offer in seat.Offers)
            {
                var duck = run.FindUnit(offer.Duck)!;
                Assert.Equal(seat.Player, DefaultTeams.SideFor(duck.Kind));
                Assert.Equal(seat.Player, CampDirector.OwnerOf(run, offer));
            }
        }
    }

    [Fact]
    public void TheBoss_RunsNoCamp_BecauseItsRewardIsTheMoltAndTheMoltIsNotBuilt()
    {
        var run = MapFixture.Rigged(MapFixture.Start(), stopAt: "c7-quarry-king");
        Assert.True(Assert.IsType<FightNode>(run.CurrentNode).Boss);

        var step = RunFixture.EndFightInAWin(MapFixture.Enter(run));

        Assert.Empty(step.All<CampOffered>());
        Assert.Equal(RunPhase.Complete, step.NewState.Phase);
    }

    [Fact]
    public void AnActWonEndToEnd_HandsOutTwoCardsPerCamp_AndTheCountIsPinned()
    {
        // THE NUMBER. Act 1's shortest route plays four combat nodes, so it holds four camps. Under
        // D-154 that was four cards for the whole flock and a player could be dealt none of them;
        // under D-247 it is EIGHT — two per camp, one per player, every camp.
        int camps = 0;
        int cards = 0;
        var perPlayer = new Dictionary<Team, int> { [Team.PlayerA] = 0, [Team.PlayerB] = 0 };

        var run = MapFixture.Start(4242);
        int guard = 0;

        while (run.Phase != RunPhase.Complete && guard++ < 400)
        {
            if (run.Phase == RunPhase.AtCamp)
            {
                var table = Camp.Draw(run);
                camps++;

                foreach (var seat in table.Seats)
                {
                    var step = Campaign.ApplyRun(run, new CampPickCommand(table, seat.Player, 0));
                    foreach (var line in step.All<CampTaken>())
                    {
                        cards++;
                        perPlayer[line.Player]++;
                    }

                    run = step.NewState;
                }

                continue;
            }

            if (run.Phase == RunPhase.AtVote)
            {
                run = Campaign.ApplyRun(run, VoteCommand.Agreed(run.Doors()[0])).NewState;
                continue;
            }

            if (run.Phase == RunPhase.AtNode)
            {
                run = MapFixture.Enter(run);
                continue;
            }

            if (run.Phase == RunPhase.InFight)
            {
                run = RunFixture.EndFightInAWin(run).NewState;
                continue;
            }

            run = Campaign.ApplyRun(run, Campaign.LegalRunCommands(run)[0]).NewState;
        }

        Assert.Equal(RunOutcome.Won, run.Outcome);
        Assert.Equal(4, camps);
        Assert.Equal(8, cards);
        Assert.Equal(camps * 2, cards);

        // And the halves are even, which is the whole ruling: neither player watches the other's
        // ducks improve.
        Assert.Equal(camps, perPlayer[Team.PlayerA]);
        Assert.Equal(camps, perPlayer[Team.PlayerB]);
    }

    // ---- the draw -----------------------------------------------------------------------------------

    [Fact]
    public void TheDraw_IsSeeded_SoTheSameRunDealsTheSameTableTwice()
    {
        AtACamp(out var first, seed: 4242);
        AtACamp(out var second, seed: 4242);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void TheDraw_IsNotTheSameAtEverySeed()
    {
        // Not a distribution claim — only that the cursor is actually consulted. A camp that dealt
        // the same two cards to every run in the game would pass every other test here.
        var tables = new List<CampTable>();
        foreach (int seed in new[] { 1, 2, 3, 7, 11, 4242 })
        {
            AtACamp(out var table, seed);
            tables.Add(table);
        }

        Assert.True(tables.Distinct().Count() > 1);
    }

    [Fact]
    public void TheTable_IsDerivedFromTheCursor_SoItSurvivesASaveAndARestore()
    {
        var run = AtACamp(out var table, seed: 99);

        var restored = Campaign.Restore(
            run.Campaign, run.Seed, run.NodeIndex, run.Squad, run.FightsWon, run.Outcome,
            run.MapState, run.RngState, atVote: false, atCamp: true);

        Assert.Equal(RunPhase.AtCamp, restored.Phase);
        Assert.Equal(table, Camp.Draw(restored));
    }

    [Fact]
    public void TheCursorMovesWhenTheCampCLOSES_NotWhenTheFirstPlayerPicks()
    {
        // The tables have to stay a pure function of the state while the camp is open, or the second
        // player's own recorded table would be refused as one the seed never dealt (D-251). So the
        // cursor and the camp count both move at the exit, and neither moves at the first pick.
        var run = AtACamp(out var table, seed: 4242);

        Assert.NotEqual(run.RngState, table.RngState);

        var half = Campaign.ApplyRun(run, new CampPickCommand(table, Team.PlayerA, 0)).NewState;

        Assert.Equal(run.RngState, half.RngState);
        Assert.Equal(run.CampsHeld, half.CampsHeld);
        Assert.Equal(table, Camp.Draw(half));

        var after = Campaign.ApplyRun(half, new CampPickCommand(table, Team.PlayerB, 0)).NewState;

        Assert.Equal(table.RngState, after.RngState);
        Assert.Equal(run.CampsHeld + 1, after.CampsHeld);
    }

    [Fact]
    public void ATableTheSeedDidNotDeal_IsRefusedByName()
    {
        var run = AtACamp(out var table);

        // The same rule a move's route obeys (D-097), one level up: the log records what was dealt,
        // and Core will not take a log that hands the squad cards it never drew.
        var seat = table.Seats[0];
        var forged = table with
        {
            Seats = new[]
            {
                seat with
                {
                    Offers = new[] { CampOffer.Of(new RunUnitId(0), Mod.Heavier), seat.Offers[1] },
                },
                table.Seats[1],
            },
        };

        var refusal = Assert.Throws<InvalidOperationException>(
            () => Campaign.ApplyRun(run, new CampPickCommand(forged, seat.Player, 0)));

        Assert.Contains("not the camp Core would have dealt", refusal.Message, StringComparison.Ordinal);
    }

    // ---- the constraints --------------------------------------------------------------------------

    [Fact]
    public void AFullSpender_IsOfferedNoMoreModsForThatDuck()
    {
        var run = AtACamp(out _);
        var fisher = run.Squad.Single(u => u.Kind == UnitKind.Threadcaster);

        // Three per slot, all classes (D-226). Cast's whole pool is these three.
        var full = fisher with
        {
            Loadout = DuckLoadout.Empty.With(Mod.LightLine).With(Mod.LongRod).With(Mod.BigSplash),
        };

        Assert.True(Kits.SlotIsFull(full.Loadout, KitEntry.Cast));
        Assert.DoesNotContain(
            CampCatalogue.EligibleFor(full), o => o.Category == OfferCategory.Mod);

        // And it is the run's answer too, not just the catalogue's: no camp can deal her a fourth.
        var loaded = run.WithUnit(full);
        Assert.DoesNotContain(
            CampDirector.Pool(loaded),
            o => o.Category == OfferCategory.Mod && o.Duck.Equals(fisher.Id));
    }

    [Fact]
    public void AModIsOnlyEverOfferedToTheClassWhoseSpenderItFits()
    {
        for (int seed = 1; seed <= 60; seed++)
        {
            var run = AtACamp(out var table, seed);

            foreach (var offer in table.Offers)
            {
                var duck = run.FindUnit(offer.Duck)!;

                if (offer.Category == OfferCategory.Mod)
                {
                    Assert.Equal(duck.Kind, CampCatalogue.KindOf(offer.AsMod));
                }
                else if (offer.Category == OfferCategory.SecondWind)
                {
                    Assert.Equal(duck.Kind, CampCatalogue.KindOf(offer.AsSecondWind));
                }
            }
        }
    }

    [Fact]
    public void NoOfferOutsideTheImplementedSet_CanEverBeDrawn()
    {
        // The pool is §8.5's five categories minus the two that are not built: Learn/Replace/Swap
        // (kit surgery, needs a multi-spender slot surface) and the stats tier, which is purged.
        // Nothing here draws a legendary either — those are destinations.
        var built = new HashSet<OfferCategory>(CampCatalogue.DrawableCategories());

        Assert.Equal(
            new[]
            {
                OfferCategory.Mod, OfferCategory.SecondWind,
                OfferCategory.Unlock, OfferCategory.Consumable,
                OfferCategory.Technique,
            },
            CampCatalogue.DrawableCategories());

        for (int seed = 1; seed <= 60; seed++)
        {
            AtACamp(out var table, seed);

            foreach (var offer in table.Offers)
            {
                Assert.Contains(offer.Category, built);
                Assert.True(IsKnown(offer), offer + " is not in any built pool.");
                Assert.NotEmpty(offer.Name);
                Assert.NotEmpty(offer.Summary);
            }
        }
    }

    [Fact]
    public void TheCatalogueHoldsTheWholeV1Pool_AndNothingBeyondIt()
    {
        // 24 hosted on spenders + 8 hosted on actions (D-243). §8.6 prints 24 and needs the widening
        // carried into the next stamp; the count is asserted here so the doc gap cannot go quiet.
        Assert.Equal(32, CampCatalogue.ModPool().Count);
        Assert.Equal(8, CampCatalogue.SecondWindPool().Count);
        Assert.Equal(3, CampCatalogue.UnlockPool().Count);
        Assert.Equal(10, CampCatalogue.ConsumablePool().Count);

        // Three mods per spender (MASTER_DESIGN §8.6), and since G4 there are eight spenders: the
        // four §5 prints and the four alternates that can replace them. The rule the pool is sized
        // by is unchanged — it is the number of spenders that doubled, not the number per spender.
        foreach (var spend in new[]
                 {
                     VerveSpend.WreckingWeight, VerveSpend.Cast,
                     VerveSpend.DoubleNock, VerveSpend.Preen,
                     VerveSpend.Retort, VerveSpend.Whirl,
                     VerveSpend.Skyfall, VerveSpend.Breakwater,
                 })
        {
            Assert.Equal(3, CampCatalogue.ModsFor(Kits.EntryOf(spend)).Count);
        }

        // And the three alternate ACTIONS that shipped carry theirs on the same three axes. Overrun
        // and Punt carry three each; Interpose carries two, because its third — Shield Arm — was not
        // commissioned. Grounding Shot's are absent with the ability (D-236).
        Assert.Equal(3, CampCatalogue.ModsFor(KitEntry.Overrun).Count);
        Assert.Equal(3, CampCatalogue.ModsFor(KitEntry.Punt).Count);
        Assert.Equal(2, CampCatalogue.ModsFor(KitEntry.Interpose).Count);

        // Two conditions per class.
        foreach (var kind in new[]
                 {
                     UnitKind.Vanguard, UnitKind.Threadcaster,
                     UnitKind.Archer, UnitKind.Wardbearer,
                 })
        {
            Assert.Equal(2, CampCatalogue.SecondWindPool().Count(w => CampCatalogue.KindOf(w) == kind));
        }
    }

    [Fact]
    public void ACampNeverOffersTheSameThingTwice()
    {
        for (int seed = 1; seed <= 60; seed++)
        {
            AtACamp(out var table, seed);

            Assert.Equal(table.Offers.Count, table.Offers.Distinct().Count());
        }
    }

    // ---- picking ------------------------------------------------------------------------------------

    [Fact]
    public void EachPick_HangsItsCardOnTheDuckItWasDrawnFor_AndReportsIt()
    {
        var run = AtACamp(out var table);

        var a = table.For(Team.PlayerA)[1];
        var b = table.For(Team.PlayerB)[0];

        var first = Campaign.ApplyRun(run, new CampPickCommand(table, Team.PlayerA, 1));
        var lineA = Assert.Single(first.All<CampTaken>());

        Assert.Equal(Team.PlayerA, lineA.Player);
        Assert.Equal(a, lineA.Offer);

        // Full payloads: a renderer draws the card from the event and asks the run nothing (CLAUDE.md).
        Assert.Equal(a.Name, lineA.Name);
        Assert.Equal(a.Summary, lineA.Summary);

        var second = Campaign.ApplyRun(first.NewState, new CampPickCommand(table, Team.PlayerB, 0));
        var lineB = Assert.Single(second.All<CampTaken>());

        Assert.Equal(Team.PlayerB, lineB.Player);
        Assert.Equal(b, lineB.Offer);

        // Both cards land, and each on its own duck. Under D-154 one of these two ducks got nothing.
        AssertCarries(second.NewState.FindUnit(a.Duck)!, a);
        AssertCarries(second.NewState.FindUnit(b.Duck)!, b);
    }

    [Fact]
    public void PickingACardThatIsNotOnYourTable_IsRefused()
    {
        var run = AtACamp(out var table);

        Assert.Throws<InvalidOperationException>(
            () => Campaign.ApplyRun(run, new CampPickCommand(table, Team.PlayerA, 2)));

        // And so is declining, which is what an out-of-range index would have to mean.
        Assert.Throws<InvalidOperationException>(
            () => Campaign.ApplyRun(run, new CampPickCommand(table, Team.PlayerA, CampPickCommand.NoPick)));
    }

    [Fact]
    public void SettlingBothTables_MovesTheRunOnToTheNextNode()
    {
        var run = AtACamp(out var table);
        int before = run.NodeIndex;

        var after = Campaign.ApplyRun(run, new CampPickCommand(table, Team.PlayerA, 0)).NewState;
        Assert.Equal(before, after.NodeIndex);

        after = Campaign.ApplyRun(after, new CampPickCommand(table, Team.PlayerB, 0)).NewState;

        Assert.NotEqual(RunPhase.AtCamp, after.Phase);
        Assert.Equal(before + 1, after.NodeIndex);
        Assert.Empty(after.CampPicks);
    }

    // ---- determinism --------------------------------------------------------------------------------

    [Fact]
    public void ARunWithCampsInItsLog_ReplaysToAnIdenticalStateAndHash()
    {
        var (played, log) = RunFixture.PlayWholeRun(seed: 4242);

        Assert.Contains(log, c => c is CampPickCommand);

        var replayed = Campaign.Replay(CampaignLibrary.Faultline, 4242, log);

        Assert.Equal(played, replayed);
        Assert.Equal(played.GetHashCode(), replayed.GetHashCode());
    }

    [Fact]
    public void TheLogRecordsBothTheDrawAndThePick_AndCarriesTwoPicksPerCamp()
    {
        var (_, log) = RunFixture.PlayWholeRun(seed: 4242);
        var picks = log.OfType<CampPickCommand>().ToList();

        Assert.NotEmpty(picks);

        // Each pick is its own command in the replay log (I2), so the picks come in owner-labelled
        // pairs — never a lone command that closed a camp on one player.
        Assert.Equal(0, picks.Count % 2);
        for (int i = 0; i < picks.Count; i += 2)
        {
            Assert.Equal(picks[i].Drawn, picks[i + 1].Drawn);
            Assert.NotEqual(picks[i].Player, picks[i + 1].Player);
        }

        var pick = picks[0];

        // The draw is on the command, not only the pick: a log entry that said "index 1" without
        // saying what was on offer would be a record nobody could read back.
        Assert.NotNull(pick.Drawn);
        Assert.NotEmpty(pick.Drawn.Offers);
        Assert.NotNull(pick.Chosen);
        Assert.Contains(pick.Chosen!.Value, pick.Drawn.For(pick.Player));
    }

    [Fact]
    public void AHalfPickedCamp_SurvivesARestore_SoTheOtherTableIsStillThereToPick()
    {
        // A camp with one table spent IS a state. Five shipped bugs are Core growing one and the save
        // dropping it (D-125, D-127, D-222, D-231, D-234) — this is the Core half of not making six.
        var run = AtACamp(out var table, seed: 99);
        var half = Campaign.ApplyRun(run, new CampPickCommand(table, Team.PlayerA, 1)).NewState;

        var restored = Campaign.Restore(
            half.Campaign, half.Seed, half.NodeIndex, half.Squad, half.FightsWon, half.Outcome,
            half.MapState, half.RngState, atVote: false, atCamp: true, campsHeld: half.CampsHeld,
            atDestination: false, campPicks: half.CampPicks);

        Assert.Equal(RunPhase.AtCamp, restored.Phase);
        Assert.Equal(table, Camp.Draw(restored));
        Assert.True(Camp.HasPicked(restored, Team.PlayerA));
        Assert.False(Camp.HasPicked(restored, Team.PlayerB));

        var legal = Campaign.LegalRunCommands(restored);
        Assert.All(legal, c => Assert.Equal(Team.PlayerB, Assert.IsType<CampPickCommand>(c).Player));

        // And finishing it from the restore hands out BOTH cards, not only the one taken after it.
        var done = Campaign.ApplyRun(restored, new CampPickCommand(table, Team.PlayerB, 0)).NewState;

        AssertCarries(done.FindUnit(table.For(Team.PlayerA)[1].Duck)!, table.For(Team.PlayerA)[1]);
        AssertCarries(done.FindUnit(table.For(Team.PlayerB)[0].Duck)!, table.For(Team.PlayerB)[0]);
    }

    [Fact]
    public void CampsCompound_SoADuckCanCarryMoreThanOneThingByTheEndOfARun()
    {
        var (played, _) = RunFixture.PlayWholeRun(seed: 4242);

        Assert.Contains(played.Squad, u => !u.Loadout.IsEmpty);
    }

    // ---- Bedraggled × mods --------------------------------------------------------------------------

    [Fact]
    public void ADuckThatDownsAndComesBackBedraggled_ComesBackWithItsModsIntact()
    {
        // Being downed costs three quarters of your health and a slot. It does not cost you what a
        // camp gave you — the loadout is not part of the price (MASTER_DESIGN §8.5).
        var run = RunFixture.Start();
        var vanguard = run.Squad.Single(u => u.Kind == UnitKind.Vanguard).Id;

        // Hung on before the fight starts, the way a camp would have: the loadout is carried onto the
        // board by the run, and the board carries it back out again.
        run = run.WithUnit(run.FindUnit(vanguard)! with
        {
            Loadout = DuckLoadout.Empty.With(Mod.Heavier).With(Unlock.LongBoot),
        });

        run = RunFixture.Deploy(RunFixture.Enter(run));
        Assert.True(RunFixture.OnBoard(run, vanguard).Has(Mod.Heavier));

        run = RunFixture.HurtTo(run, vanguard, 0);
        run = RunFixture.WinTheFight(run);

        var carried = run.FindUnit(vanguard)!;
        Assert.Equal(RunUnitStatus.Downed, carried.Status);
        Assert.True(carried.ReturnsBedraggled);
        Assert.True(carried.Loadout.Has(Mod.Heavier));
        Assert.True(carried.Loadout.Has(Unlock.LongBoot));

        // And they are on the board it walks back onto, at quarter health and with no round-1 slot.
        var next = RunFixture.Enter(run);
        var onBoard = RunFixture.OnBoard(next, vanguard);

        Assert.True(onBoard.Bedraggled);
        Assert.Equal(Bedraggled.ReturningHp(carried.MaxHp), onBoard.Hp);
        Assert.True(onBoard.Has(Mod.Heavier));
        Assert.True(onBoard.Has(Unlock.LongBoot));
    }

    [Fact]
    public void AnEmptiedPocket_IsEmptyForTheRestOfTheRun()
    {
        // The pocket is the one thing in the loadout a fight can change, so it is the one thing the
        // fight-to-run handover has to carry back.
        var run = RunFixture.Start();
        var wardbearer = run.Squad.Single(u => u.Kind == UnitKind.Wardbearer).Id;

        run = run.WithUnit(run.FindUnit(wardbearer)! with
        {
            Loadout = DuckLoadout.Empty.WithPocket(Consumable.DriedMinnow),
        });

        run = RunFixture.Deploy(RunFixture.Enter(run));
        Assert.Equal(Consumable.DriedMinnow, RunFixture.OnBoard(run, wardbearer).Loadout.Pocket);

        var board = RunFixture.OnBoard(run, wardbearer);
        run = run with
        {
            Fight = run.Fight!.WithUnit(board with
            {
                Loadout = board.Loadout.WithEmptyPocket(),
            }),
        };

        // Read at the handover, before the camp opens. The camp is entitled to hand him a fresh
        // one-shot — an empty pocket is exactly what makes a consumable drawable — so asserting after
        // it would be asking whether the *camp* refilled the pocket, which is a different question.
        run = RunFixture.EndFightInAWin(run).NewState;

        Assert.Null(run.FindUnit(wardbearer)!.Loadout.Pocket);
    }

    // ---- the model ----------------------------------------------------------------------------------

    /// <summary>
    /// Three mods on one slot, and the pool runs out at exactly the same number — §8.6 prints three
    /// mods per spender, so a spender slot's ceiling is never the thing that stops a fourth. The
    /// ceiling is asserted through <see cref="Kits.RefusalFor(DuckLoadout, Mod)"/> rather than by
    /// fitting a fourth, because <b>there is no fourth mod for any spender to fit</b> (D-226).
    /// </summary>
    [Fact]
    public void ADuckTakesThreeModsOnOneSlot_AndTheSameOneNeverTwice()
    {
        var loadout = DuckLoadout.Empty.With(Mod.Heavier).With(Mod.Freight).With(Mod.Echo);

        Assert.Equal(Kits.ModsPerSlot, loadout.Mods.Count);
        Assert.Equal(Kits.ModsPerSlot, Kits.ModsOn(loadout, KitEntry.WreckingWeight));
        Assert.True(Kits.SlotIsFull(loadout, KitEntry.WreckingWeight));

        // The pool holds no fourth Wrecking Weight mod, so the ceiling and the pool agree.
        Assert.Equal(
            Kits.ModsPerSlot,
            CampCatalogue.ModPool().Count(m => Kits.HostOf(m) == KitEntry.WreckingWeight));

        // A refusal names its reason rather than returning a quiet false.
        Assert.Contains(
            "which is the ceiling for one slot",
            Kits.RefusalFor(loadout, Mod.Heavier) ?? string.Empty,
            StringComparison.Ordinal);

        Assert.Throws<InvalidOperationException>(
            () => DuckLoadout.Empty.With(Mod.Heavier).With(Mod.Heavier));
    }

    [Fact]
    public void AnOfferOnlyReadsBackAsWhatItIs()
    {
        var offer = CampOffer.Of(new RunUnitId(0), Unlock.LongBoot);

        Assert.Equal(Unlock.LongBoot, offer.AsUnlock);
        Assert.Throws<InvalidOperationException>(() => offer.AsMod);
        Assert.Throws<InvalidOperationException>(() => offer.AsSecondWind);
        Assert.Throws<InvalidOperationException>(() => offer.AsConsumable);
    }

    // ---- fixtures -----------------------------------------------------------------------------------

    /// <summary>A run standing at the camp its first won fight opened, with the table it was dealt.</summary>
    private static RunState AtACamp(out CampTable table, int seed = RunFixture.Seed)
    {
        var run = RunFixture.Enter(RunFixture.Start(seed));
        run = RunFixture.EndFightInAWin(run).NewState;

        TestPlay.Require(run.Phase == RunPhase.AtCamp, "The won fight did not open a camp.");

        table = Camp.Draw(run);
        return run;
    }

    private static void AssertCarries(RunUnit duck, CampOffer offer)
    {
        switch (offer.Category)
        {
            case OfferCategory.Mod:
                Assert.True(duck.Loadout.Has(offer.AsMod));
                break;
            case OfferCategory.SecondWind:
                Assert.True(duck.Loadout.Has(offer.AsSecondWind));
                break;
            case OfferCategory.Unlock:
                Assert.True(duck.Loadout.Has(offer.AsUnlock));
                break;
            case OfferCategory.Technique:
                Assert.True(duck.Loadout.Has(offer.AsTechnique));
                break;
            default:
                Assert.Equal(offer.AsConsumable, duck.Loadout.Pocket);
                break;
        }
    }

    private static bool IsKnown(CampOffer offer) => offer.Category switch
    {
        OfferCategory.Mod => CampCatalogue.ModPool().Contains(offer.AsMod),
        OfferCategory.SecondWind => CampCatalogue.SecondWindPool().Contains(offer.AsSecondWind),
        OfferCategory.Unlock => CampCatalogue.UnlockPool().Contains(offer.AsUnlock),
        OfferCategory.Consumable => CampCatalogue.ConsumablePool().Contains(offer.AsConsumable),
        OfferCategory.Technique => CampCatalogue.TechniquePool().Contains(offer.AsTechnique),
        _ => false,
    };
}
