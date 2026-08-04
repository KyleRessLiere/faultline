using System;
using System.Collections.Generic;
using System.Linq;
using Faultline.Core;
using Xunit;

namespace Faultline.Core.Tests;

/// <summary>
/// The Camp: after every won Fight or Elite, each player picks 1 of 2 (MASTER_DESIGN §8.5).
/// </summary>
/// <remarks>
/// These are about the <em>offer</em> — that it is dealt from the run seed, that it is the same two
/// cards on a replay, that the two differ in category where they can, and that nothing outside the
/// built pools can ever be on the table. What each card <em>does</em> is pinned in ModTests,
/// SecondWindTests, UnlockTests and ConsumableTests.
/// </remarks>
public class CampTests
{
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
    public void ACamp_OffersTwoCardsToEachPlayer_AndNothingElseIsLegal()
    {
        var run = AtACamp(out var table);

        Assert.Equal(Camp.OffersPerPlayer, table.OffersA.Count);
        Assert.Equal(Camp.OffersPerPlayer, table.OffersB.Count);

        // Every ordered pair, and nothing but pairs. There is deliberately no decline on the list:
        // camps are the reward, and turning one down is not a decision (MASTER_DESIGN §8.5).
        var legal = Campaign.LegalRunCommands(run);
        Assert.Equal(Camp.OffersPerPlayer * Camp.OffersPerPlayer, legal.Count);
        Assert.All(legal, c => Assert.IsType<CampPickCommand>(c));
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
    public void EachDuckPicksForItsOwnOwner_ByTheDefaultSplit()
    {
        var run = AtACamp(out var table);

        // D-092's loadout is ownership: Player A holds the Vanguard and the Fisher, Player B the
        // Wardbearer and the Archer. A camp deals each player cards for their own ducks and nobody
        // else's, which is what makes the two draws independent.
        foreach (var offer in table.OffersA)
        {
            Assert.Equal(Team.PlayerA, DefaultTeams.SideFor(run.FindUnit(offer.Duck)!.Kind));
        }

        foreach (var offer in table.OffersB)
        {
            Assert.Equal(Team.PlayerB, DefaultTeams.SideFor(run.FindUnit(offer.Duck)!.Kind));
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
    public void ThePick_MovesTheCursor_SoTheNextCampDealsFreshCards()
    {
        var run = AtACamp(out var table, seed: 4242);

        Assert.NotEqual(run.RngState, table.RngState);

        var after = Campaign.ApplyRun(run, new CampPickCommand(table, 0, 0)).NewState;

        Assert.Equal(table.RngState, after.RngState);
    }

    [Fact]
    public void ATableTheSeedDidNotDeal_IsRefusedByName()
    {
        var run = AtACamp(out var table);

        // The same rule a move's route obeys (D-097), one level up: the log records what was dealt,
        // and Core will not take a log that hands the squad cards it never drew.
        var forged = table with
        {
            OffersA = new[] { CampOffer.Of(new RunUnitId(0), Mod.Heavier), table.OffersA[1] },
        };

        var refusal = Assert.Throws<InvalidOperationException>(
            () => Campaign.ApplyRun(run, new CampPickCommand(forged, 0, 0)));

        Assert.Contains("not the camp Core would have dealt", refusal.Message, StringComparison.Ordinal);
    }

    // ---- the constraints --------------------------------------------------------------------------

    [Fact]
    public void APlayersTwoOffers_DifferInCategoryWhereverThePoolAllows()
    {
        // Swept over many seeds rather than one, because "where the pool allows" is a statement about
        // the pool and a single deal proves nothing about it.
        for (int seed = 1; seed <= 60; seed++)
        {
            AtACamp(out var table, seed);

            AssertDiffersInCategory(table.OffersA, "Player A", seed);
            AssertDiffersInCategory(table.OffersB, "Player B", seed);
        }
    }

    [Fact]
    public void AFullSpender_IsOfferedNoMoreModsForThatDuck()
    {
        var run = AtACamp(out _);
        var fisher = run.Squad.Single(u => u.Kind == UnitKind.Threadcaster);

        // Two is the ceiling until the Molt's Deep Mastery, which is not built.
        var full = fisher with
        {
            Loadout = DuckLoadout.Empty.With(Mod.LightLine).With(Mod.LongRod),
        };

        Assert.True(full.Loadout.SpenderIsFull);
        Assert.DoesNotContain(
            CampCatalogue.EligibleFor(full), o => o.Category == OfferCategory.Mod);

        // And it is the run's answer too, not just the catalogue's: no camp can deal her a third.
        var loaded = run.WithUnit(full);
        var table = Camp.Draw(loaded);
        Assert.DoesNotContain(
            table.OffersA,
            o => o.Category == OfferCategory.Mod && o.Duck.Equals(fisher.Id));
    }

    [Fact]
    public void AModIsOnlyEverOfferedToTheClassWhoseSpenderItFits()
    {
        for (int seed = 1; seed <= 60; seed++)
        {
            var run = AtACamp(out var table, seed);

            foreach (var offer in table.OffersA.Concat(table.OffersB))
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
            },
            CampCatalogue.DrawableCategories());

        for (int seed = 1; seed <= 60; seed++)
        {
            AtACamp(out var table, seed);

            foreach (var offer in table.OffersA.Concat(table.OffersB))
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
        Assert.Equal(12, CampCatalogue.ModPool().Count);
        Assert.Equal(8, CampCatalogue.SecondWindPool().Count);
        Assert.Equal(4, CampCatalogue.UnlockPool().Count);
        Assert.Equal(5, CampCatalogue.ConsumablePool().Count);

        // Three mods per spender, one spender per class (MASTER_DESIGN §8.6).
        foreach (var spend in new[]
                 {
                     VerveSpend.WreckingWeight, VerveSpend.Cast,
                     VerveSpend.DoubleNock, VerveSpend.Preen,
                 })
        {
            Assert.Equal(3, CampCatalogue.ModPool().Count(m => CampCatalogue.SpenderOf(m) == spend));
        }

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

            Assert.Equal(table.OffersA.Count, table.OffersA.Distinct().Count());
            Assert.Equal(table.OffersB.Count, table.OffersB.Distinct().Count());
        }
    }

    // ---- picking ------------------------------------------------------------------------------------

    [Fact]
    public void ThePick_HangsTheCardOnTheDuckItWasDrawnFor_AndReportsIt()
    {
        var run = AtACamp(out var table);

        var step = Campaign.ApplyRun(run, new CampPickCommand(table, 0, 1));

        var taken = step.All<CampTaken>();
        Assert.Equal(2, taken.Count);
        Assert.Equal(Team.PlayerA, taken[0].Player);
        Assert.Equal(Team.PlayerB, taken[1].Player);

        Assert.Equal(table.OffersA[0], taken[0].Offer);
        Assert.Equal(table.OffersB[1], taken[1].Offer);

        // Full payloads: a renderer draws the card from the event and asks the run nothing (CLAUDE.md).
        Assert.Equal(table.OffersA[0].Name, taken[0].Name);
        Assert.Equal(table.OffersA[0].Summary, taken[0].Summary);

        AssertCarries(step.NewState.FindUnit(table.OffersA[0].Duck)!, table.OffersA[0]);
        AssertCarries(step.NewState.FindUnit(table.OffersB[1].Duck)!, table.OffersB[1]);
    }

    [Fact]
    public void PickingACardThatIsNotOnTheTable_IsRefused()
    {
        var run = AtACamp(out var table);

        Assert.Throws<InvalidOperationException>(
            () => Campaign.ApplyRun(run, new CampPickCommand(table, 2, 0)));

        // And so is declining, which is what an out-of-range index would have to mean.
        Assert.Throws<InvalidOperationException>(
            () => Campaign.ApplyRun(run, new CampPickCommand(table, CampPickCommand.NoPick, 0)));
    }

    [Fact]
    public void SettlingTheCamp_MovesTheRunOnToTheNextNode()
    {
        var run = AtACamp(out var table);
        int before = run.NodeIndex;

        var after = Campaign.ApplyRun(run, new CampPickCommand(table, 0, 0)).NewState;

        Assert.NotEqual(RunPhase.AtCamp, after.Phase);
        Assert.Equal(before + 1, after.NodeIndex);
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
    public void TheLogRecordsBothTheDrawAndThePick()
    {
        var (_, log) = RunFixture.PlayWholeRun(seed: 4242);
        var pick = log.OfType<CampPickCommand>().First();

        // The draw is on the command, not only the pick: a log entry that said "index 1" without
        // saying what was on offer would be a record nobody could read back.
        Assert.NotNull(pick.Drawn);
        Assert.NotEmpty(pick.Drawn.OffersA);
        Assert.NotNull(pick.ChosenA);
        Assert.Contains(pick.ChosenA!.Value, pick.Drawn.OffersA);
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
            Loadout = DuckLoadout.Empty.With(Mod.Heavier).With(Unlock.Climber),
        });

        run = RunFixture.Deploy(RunFixture.Enter(run));
        Assert.True(RunFixture.OnBoard(run, vanguard).Has(Mod.Heavier));

        run = RunFixture.HurtTo(run, vanguard, 0);
        run = RunFixture.WinTheFight(run);

        var carried = run.FindUnit(vanguard)!;
        Assert.Equal(RunUnitStatus.Downed, carried.Status);
        Assert.True(carried.ReturnsBedraggled);
        Assert.True(carried.Loadout.Has(Mod.Heavier));
        Assert.True(carried.Loadout.Has(Unlock.Climber));

        // And they are on the board it walks back onto, at quarter health and with no round-1 slot.
        var next = RunFixture.Enter(run);
        var onBoard = RunFixture.OnBoard(next, vanguard);

        Assert.True(onBoard.Bedraggled);
        Assert.Equal(Bedraggled.ReturningHp(carried.MaxHp), onBoard.Hp);
        Assert.True(onBoard.Has(Mod.Heavier));
        Assert.True(onBoard.Has(Unlock.Climber));
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

        run = RunFixture.WinTheFight(run);

        Assert.Null(run.FindUnit(wardbearer)!.Loadout.Pocket);
    }

    // ---- the model ----------------------------------------------------------------------------------

    [Fact]
    public void ADuckTakesTwoModsAndRefusesAThird_AndRefusesTheSameOneTwice()
    {
        var loadout = DuckLoadout.Empty.With(Mod.Heavier).With(Mod.Freight);

        Assert.True(loadout.SpenderIsFull);
        Assert.Equal(DuckLoadout.ModSlots, loadout.Mods.Count);
        Assert.Throws<InvalidOperationException>(() => loadout.With(Mod.Echo));
        Assert.Throws<InvalidOperationException>(
            () => DuckLoadout.Empty.With(Mod.Heavier).With(Mod.Heavier));
    }

    [Fact]
    public void AnOfferOnlyReadsBackAsWhatItIs()
    {
        var offer = CampOffer.Of(new RunUnitId(0), Unlock.Climber);

        Assert.Equal(Unlock.Climber, offer.AsUnlock);
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

    private static void AssertDiffersInCategory(
        IReadOnlyList<CampOffer> offers, string who, int seed)
    {
        if (offers.Count < 2)
        {
            return;
        }

        // Only when the pool could have done otherwise: a duck with one category left cannot be
        // handed two, and that is not a reason to hand it one card (MASTER_DESIGN §8.5).
        Assert.True(
            offers[0].Category != offers[1].Category,
            who + " was dealt two " + offers[0].Category + " cards at seed " + seed + ".");
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
        _ => false,
    };
}
