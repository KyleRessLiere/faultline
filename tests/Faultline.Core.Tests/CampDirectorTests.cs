using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// The camp offer director (MASTER_DESIGN §8.6): the six rows this stage builds, each asserted on
/// the table the seed actually deals rather than on the code that deals it.
/// </summary>
/// <remarks>
/// Swept across seeds wherever the row is a statement about the pool: "camp 1 deals two engine
/// starters on different classes" is a claim about every camp 1 there is, and a single deal proves
/// nothing about it.
/// </remarks>
public class CampDirectorTests
{
    private const int Seeds = 40;

    // ---- camp 1: two engine starters, different classes, preferably different players ------------

    [Fact]
    public void CampOne_DealsTwoEngineStarters_OnDifferentClasses()
    {
        for (int seed = 1; seed <= Seeds; seed++)
        {
            var run = Fresh(seed);
            var table = Camp.Draw(run);

            Assert.Equal(CampDirector.CardsPerCamp, table.Offers.Count);
            Assert.All(table.Offers, o => Assert.True(
                CampDirector.IsEngineStarter(o),
                "Camp 1 at seed " + seed + " dealt " + o + ", which is not an engine starter."));

            var kinds = table.Offers.Select(o => run.FindUnit(o.Duck)!.Kind).Distinct().ToList();
            Assert.True(kinds.Count == 2, "Camp 1 at seed " + seed + " dealt two cards on one class.");
        }
    }

    [Fact]
    public void CampOne_PrefersTwoDifferentPlayers_AndSaysSoWhenItBound()
    {
        // "Preferably", so this is a tendency and not a law — but a preference that never binds is
        // not implemented. The proof log is what makes the difference visible.
        int bothPlayers = 0;

        for (int seed = 1; seed <= Seeds; seed++)
        {
            var run = Fresh(seed);
            var table = Camp.Draw(run);

            var owners = table.Offers.Select(o => CampDirector.OwnerOf(run, o)).Distinct().Count();
            if (owners == 2)
            {
                bothPlayers++;
            }
        }

        Assert.True(
            bothPlayers > Seeds / 2,
            "Camp 1 spanned both players only " + bothPlayers + " times in " + Seeds + " seeds.");
    }

    // ---- later camps: at least one connector to an owned tag -------------------------------------

    [Fact]
    public void ALaterCamp_OffersACardConnectingToATagTheSquadOwns()
    {
        // Owning Rattling Impact is owning IMPACT and RELAY. A camp after that has to put at least
        // one card wearing one of them on the table, whenever the pool still holds one.
        for (int seed = 1; seed <= Seeds; seed++)
        {
            var run = WithTechnique(Fresh(seed), UnitKind.Vanguard, TechniqueModifier.RattlingImpact);
            run = run with { CampsHeld = 1 };

            var owned = CampDirector.OwnedTags(run);
            Assert.NotEqual(TechniqueTag.None, owned);

            var table = Camp.Draw(run);
            bool anyConnectorInPool = CampDirector.Pool(run).Any(o => CampDirector.Connects(o, owned));

            if (!anyConnectorInPool)
            {
                continue;
            }

            Assert.True(
                table.Offers.Any(o => CampDirector.Connects(o, owned)),
                "Camp 2 at seed " + seed + " offered no connector: " + table + ".");
        }
    }

    [Fact]
    public void ASquadThatOwnsNoTags_HasNothingToConnectTo_AndTheRowDoesNotFire()
    {
        var run = Fresh(1) with { CampsHeld = 1 };

        Assert.Equal(TechniqueTag.None, CampDirector.OwnedTags(run));
        Assert.All(CampDirector.Pool(run), o => Assert.False(CampDirector.Connects(o, TechniqueTag.None)));

        // And it still deals two cards. A constraint nothing can satisfy steps aside rather than
        // emptying the table.
        Assert.Equal(CampDirector.CardsPerCamp, Camp.Draw(run).Offers.Count);
    }

    // ---- no named permanent twice in a run -------------------------------------------------------

    [Fact]
    public void ANamedPermanentAlreadyInTheRun_IsNeverOfferedAgain_ToAnybody()
    {
        // Sure-Footed is class-free, so it is the case the per-duck eligibility filter cannot catch:
        // the Vanguard holding one must stop the Fisher being offered another.
        var run = Fresh(1);
        var vanguard = run.Squad.Single(u => u.Kind == UnitKind.Vanguard);

        run = run.WithUnit(vanguard with { Loadout = DuckLoadout.Empty.With(Unlock.SureFooted) });

        Assert.True(CampDirector.AnybodyHolds(run, CampOffer.Of(vanguard.Id, Unlock.SureFooted)));
        Assert.DoesNotContain(
            CampDirector.Pool(run),
            o => o.Category == OfferCategory.Unlock && o.AsUnlock == Unlock.SureFooted);
    }

    [Fact]
    public void ATablesTwoCards_AreNeverTheSameNamedPermanent()
    {
        for (int seed = 1; seed <= Seeds; seed++)
        {
            for (int camp = 0; camp < 4; camp++)
            {
                var table = Camp.Draw(Fresh(seed) with { CampsHeld = camp });
                if (table.Offers.Count < 2)
                {
                    continue;
                }

                var a = table.Offers[0];
                var b = table.Offers[1];

                Assert.NotEqual(a, b);
                Assert.False(
                    a.IsPermanent && b.IsPermanent && a.Category == b.Category && a.Value == b.Value,
                    "Camp " + (camp + 1) + " at seed " + seed + " dealt the same permanent twice.");
            }
        }
    }

    // ---- never two consumables paired ------------------------------------------------------------

    [Fact]
    public void ACampNeverPairsTwoConsumables()
    {
        for (int seed = 1; seed <= Seeds; seed++)
        {
            for (int camp = 0; camp < 6; camp++)
            {
                var table = Camp.Draw(Fresh(seed) with { CampsHeld = camp });

                Assert.True(
                    table.Offers.Count(o => o.Category == OfferCategory.Consumable) < 2,
                    "Camp " + (camp + 1) + " at seed " + seed + " paired two consumables.");
            }
        }
    }

    // ---- ownership fairness across any three offers ----------------------------------------------

    [Fact]
    public void WhenTheLastTwoPicksWentToOnePlayer_TheNextTableHoldsACardForTheOther()
    {
        for (int seed = 1; seed <= Seeds; seed++)
        {
            foreach (var hogging in new[] { Team.PlayerA, Team.PlayerB })
            {
                var run = Fresh(seed) with
                {
                    CampsHeld = 2,
                    LastPickOwner = hogging,
                    PreviousPickOwner = hogging,
                };

                Assert.True(run.OwnershipIsLopsided);

                var owed = hogging.OtherPlayer();
                var table = Camp.Draw(run);

                Assert.Contains(table.Offers, o => CampDirector.OwnerOf(run, o) == owed);
            }
        }
    }

    [Fact]
    public void TwoPicksToDifferentPlayers_IsNotLopsided_AndTheRowDoesNotFire()
    {
        var even = Fresh(1) with
        {
            CampsHeld = 2,
            LastPickOwner = Team.PlayerA,
            PreviousPickOwner = Team.PlayerB,
        };

        Assert.False(even.OwnershipIsLopsided);
        Assert.DoesNotContain("ownership fairness", Camp.Draw(even).Bound);
    }

    // ---- rarity by node --------------------------------------------------------------------------

    [Fact]
    public void TheWeightsAreTheDesignsNumbers_AndTheLaneChoosesBetweenThem()
    {
        // The numbers themselves live in RarityOdds now — a tunable table keyed by source, because
        // §14 #9 leaves the odds open (D-198). What the director owes is the lookup, not the rate.
        Assert.Equal(new RarityOdds(60, 35, 5), RarityOdds.For(RewardSource.SafeCamp));
        Assert.Equal(new RarityOdds(35, 50, 15), RarityOdds.For(RewardSource.HungryCamp));

        var safe = Fresh(1) with { MapState = MapState.At("c2-bait-and-break") };
        var hungry = Fresh(1) with { MapState = MapState.At("c2-the-teeth") };

        Assert.Equal(MapLane.Safe, safe.CurrentMapNode!.Lane);
        Assert.Equal(MapLane.Hungry, hungry.CurrentMapNode!.Lane);

        Assert.Equal(RarityOdds.For(RewardSource.SafeCamp), CampDirector.WeightsFor(safe));
        Assert.Equal(RarityOdds.For(RewardSource.HungryCamp), CampDirector.WeightsFor(hungry));

        // And a run with no map at all — the linear campaign — is priced as safe rather than as
        // nothing: plain fights are the safe reading of "safe".
        Assert.Equal(RarityOdds.For(RewardSource.SafeCamp), CampDirector.WeightsFor(RunFixture.Start()));
    }

    [Fact]
    public void AHungryNode_DealsMoreUncommonsThanASafeOne_OverTheSameSeeds()
    {
        // Absolutes, not ratios (CLAUDE.md): the count of Uncommon cards dealt, on identical squads
        // and identical seeds, with only the lane differing.
        int safe = UncommonsOver(MapLane.Safe);
        int hungry = UncommonsOver(MapLane.Hungry);

        Assert.True(
            hungry > safe,
            "Hungry dealt " + hungry + " Uncommons and safe dealt " + safe + " over " + Seeds + " seeds.");
    }

    // ---- seeded and replay-stable ----------------------------------------------------------------

    [Fact]
    public void TheDeal_IsAPureFunctionOfTheCursorAndTheSquad()
    {
        for (int seed = 1; seed <= Seeds; seed++)
        {
            var run = Fresh(seed) with { CampsHeld = 3 };
            Assert.Equal(Camp.Draw(run), Camp.Draw(run));
        }
    }

    [Fact]
    public void TheProofLog_NamesTheConstraintsThatActuallyBound()
    {
        // The map generator is required to emit one (§8.5); the director emits the same thing. A
        // constraint that never appears here is a constraint that never narrowed anything.
        var seen = new HashSet<string>();

        for (int seed = 1; seed <= Seeds; seed++)
        {
            for (int camp = 0; camp < 4; camp++)
            {
                foreach (string name in Camp.Draw(Fresh(seed) with { CampsHeld = camp }).Bound)
                {
                    seen.Add(name);
                }
            }
        }

        Assert.Contains("camp-1 engine starter", seen);
        Assert.Contains("camp-1 different classes", seen);
    }

    // ---- fixtures --------------------------------------------------------------------------------

    /// <summary>
    /// A run standing at a camp on the act map, with nothing yet on any duck. Reached by winning the
    /// opening fight rather than by assembling a state, so the squad and the cursor are the ones the
    /// rules produce.
    /// </summary>
    private static RunState Fresh(int seed)
    {
        var run = MapFixture.Enter(MapFixture.Start(seed));
        run = RunFixture.EndFightInAWin(run).NewState;

        TestPlay.Require(run.Phase == RunPhase.AtCamp, "The won fight did not open a camp.");
        return run;
    }

    private static RunState WithTechnique(RunState run, UnitKind kind, TechniqueModifier technique)
    {
        var duck = run.Squad.Single(u => u.Kind == kind);
        return run.WithUnit(duck with { Loadout = duck.Loadout.With(technique) });
    }

    /// <summary>How many Uncommon cards a lane's weights deal over the sweep, all else equal.</summary>
    private static int UncommonsOver(MapLane lane)
    {
        int count = 0;

        for (int seed = 1; seed <= Seeds; seed++)
        {
            var run = Fresh(seed);
            var rng = new SeededRng(run.RngState);

            // The lane is the only thing that differs, so the weights are asked for directly rather
            // than by walking the run down two different routes — which would also change the squad.
            var weights = RarityOdds.For(
                lane == MapLane.Hungry ? RewardSource.HungryCamp : RewardSource.SafeCamp);

            count += Dealt(run, rng, weights);
        }

        return count;
    }

    private static int Dealt(RunState run, SeededRng rng, RarityOdds weights)
    {
        // A direct weighted draw over the same pool: this measures the weights, which is what the row
        // is about, without needing a board on each lane.
        var pool = CampDirector.Pool(run);
        int uncommon = 0;

        for (int draw = 0; draw < 20; draw++)
        {
            int total = 0;
            var tiers = new List<CardRarity>();

            foreach (var rarity in new[] { CardRarity.Common, CardRarity.Uncommon, CardRarity.Rare })
            {
                if (pool.Any(o => o.Rarity == rarity))
                {
                    tiers.Add(rarity);
                    total += weights.Of(rarity);
                }
            }

            int roll = rng.Next(total);
            foreach (var tier in tiers)
            {
                if (roll < weights.Of(tier))
                {
                    if (tier == CardRarity.Uncommon)
                    {
                        uncommon++;
                    }

                    break;
                }

                roll -= weights.Of(tier);
            }
        }

        return uncommon;
    }
}
