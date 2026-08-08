using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// The camp offer director (MASTER_DESIGN §8.6), as D-247 restates its rows about <b>two tables of
/// two, one pick each</b> — each asserted on the tables the seed actually deals rather than on the
/// code that deals them.
/// </summary>
/// <remarks>
/// Swept across seeds wherever the row is a statement about the pool: "camp 1 deals one engine
/// starter per player on different classes" is a claim about every camp 1 there is, and a single
/// deal proves nothing about it.
/// </remarks>
public class CampDirectorTests
{
    private const int Seeds = 40;

    private static readonly Team[] Sides = { Team.PlayerA, Team.PlayerB };

    // ---- every player gets a table ----------------------------------------------------------------

    [Fact]
    public void EveryCamp_DealsATableOfTwoToEveryPlayerWithAnAvailableDuck()
    {
        // §8.6's ownership-fairness row dissolved into this (D-249): the row's guarantee is now
        // structural, and it is asserted on every camp of every seed rather than fired after the
        // damage. It is also I2's "suppression never reduces a table to one", swept.
        for (int seed = 1; seed <= Seeds; seed++)
        {
            for (int camp = 0; camp < 6; camp++)
            {
                var run = Fresh(seed) with { CampsHeld = camp };
                var table = Camp.Draw(run);

                foreach (var player in Sides)
                {
                    Assert.True(
                        Camp.DucksFor(run, player).Count > 0,
                        "Seed " + seed + " has no available duck for " + player + ".");

                    Assert.Equal(CampDirector.CardsPerTable, table.For(player).Count);

                    // And every card on a table is for one of that player's own ducks.
                    Assert.All(
                        table.For(player),
                        o => Assert.Equal(player, CampDirector.OwnerOf(run, o)));
                }
            }
        }
    }

    // ---- camp 1: one engine starter per player, different classes --------------------------------

    [Fact]
    public void CampOne_DealsOneEngineStarterPerPlayer_OnDifferentClasses()
    {
        for (int seed = 1; seed <= Seeds; seed++)
        {
            var run = Fresh(seed);
            var table = Camp.Draw(run);

            var starters = new List<CampOffer>();

            foreach (var player in Sides)
            {
                var mine = table.For(player);
                Assert.Equal(CampDirector.CardsPerTable, mine.Count);

                var engine = mine.Where(CampDirector.IsEngineStarter).ToList();
                Assert.True(
                    engine.Count > 0,
                    "Camp 1 at seed " + seed + " dealt " + player + " no engine starter: " + table + ".");

                starters.Add(engine[0]);
            }

            var kinds = starters.Select(o => run.FindUnit(o.Duck)!.Kind).Distinct().ToList();
            Assert.True(
                kinds.Count == starters.Count,
                "Camp 1 at seed " + seed + " guaranteed two engine starters on one class.");
        }
    }

    [Fact]
    public void CampOne_SpansBothPlayersByConstruction_SoThePreferenceIsGone()
    {
        // §8.6's "preferably different players" is deleted as a constraint and kept as a structure
        // (D-247): a table belongs to a player, so this holds at every seed rather than most of them.
        for (int seed = 1; seed <= Seeds; seed++)
        {
            var table = Camp.Draw(Fresh(seed));

            Assert.Equal(2, table.Seats.Count);
            Assert.Equal(Sides, table.Seats.Select(s => s.Player).ToArray());
        }
    }

    // ---- later camps: at least one connector to a tag THIS player owns ---------------------------

    [Fact]
    public void ALaterCamp_OffersEachPlayerACardConnectingToATagTheyOwn()
    {
        // Owning Rattling Impact is owning IMPACT and RELAY. The row is keyed to the player's OWN
        // ducks' tags since the camp deals a table each (D-247), so the Vanguard's tags oblige Player
        // A's table and say nothing about Player B's.
        for (int seed = 1; seed <= Seeds; seed++)
        {
            var run = WithTechnique(Fresh(seed), UnitKind.Vanguard, TechniqueModifier.RattlingImpact);
            run = run with { CampsHeld = 1 };

            var owner = DefaultTeams.SideFor(UnitKind.Vanguard)!.Value;
            var owned = CampDirector.OwnedTagsFor(run, owner);
            Assert.NotEqual(TechniqueTag.None, owned);

            // And it is that player's tags, not the squad's: the other flock owns nothing yet.
            Assert.Equal(TechniqueTag.None, CampDirector.OwnedTagsFor(run, owner.OtherPlayer()));

            var table = Camp.Draw(run);
            bool anyConnectorInPool =
                CampDirector.PoolFor(run, owner).Any(o => CampDirector.Connects(o, owned));

            if (!anyConnectorInPool)
            {
                continue;
            }

            Assert.True(
                table.For(owner).Any(o => CampDirector.Connects(o, owned)),
                "Camp 2 at seed " + seed + " offered " + owner + " no connector: " + table + ".");
        }
    }

    [Fact]
    public void APlayerWhoOwnsNoTags_HasNothingToConnectTo_AndTheRowDoesNotFire()
    {
        var run = Fresh(1) with { CampsHeld = 1 };
        var table = Camp.Draw(run);

        foreach (var player in Sides)
        {
            Assert.Equal(TechniqueTag.None, CampDirector.OwnedTagsFor(run, player));
            Assert.All(
                CampDirector.PoolFor(run, player),
                o => Assert.False(CampDirector.Connects(o, TechniqueTag.None)));

            // And it still deals two cards. A constraint nothing can satisfy steps aside rather than
            // emptying the table.
            Assert.Equal(CampDirector.CardsPerTable, table.For(player).Count);
        }
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
    public void NoNamedPermanentAppearsTwice_ACROSS_BothTablesOfOneCamp()
    {
        // D-247's first restated row. Under one table this was a claim about two cards; under two it
        // is a claim about four, and the cross-table half is the part D-154 could not state.
        for (int seed = 1; seed <= Seeds; seed++)
        {
            for (int camp = 0; camp < 6; camp++)
            {
                var table = Camp.Draw(Fresh(seed) with { CampsHeld = camp });
                var all = table.Offers;

                Assert.Equal(all.Count, all.Distinct().Count());

                for (int i = 0; i < all.Count; i++)
                {
                    for (int j = i + 1; j < all.Count; j++)
                    {
                        Assert.False(
                            all[i].IsPermanent && all[j].IsPermanent
                            && all[i].Category == all[j].Category && all[i].Value == all[j].Value,
                            "Camp " + (camp + 1) + " at seed " + seed
                            + " dealt the same permanent twice: " + table + ".");
                    }
                }
            }
        }
    }

    // ---- never two consumables paired — per table (D-248) ----------------------------------------

    [Fact]
    public void NoTablePairsTwoConsumables_AndTheRowIsStatedPerTableNotPerCamp()
    {
        // D-248 is my call: the row is about the decision in front of one player, which is now their
        // own two cards. So "at most one per table" is asserted, and "at most one per camp" is not.
        for (int seed = 1; seed <= Seeds; seed++)
        {
            for (int camp = 0; camp < 6; camp++)
            {
                var table = Camp.Draw(Fresh(seed) with { CampsHeld = camp });

                foreach (var seat in table.Seats)
                {
                    Assert.True(
                        seat.Offers.Count(o => o.Category == OfferCategory.Consumable) < 2,
                        "Camp " + (camp + 1) + " at seed " + seed + " paired two consumables on "
                        + seat.Player + "'s table.");
                }
            }
        }
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
    public void TheProofLog_NamesTheConstraintsThatActuallyBound_PerTableAndAcrossThem()
    {
        // The map generator is required to emit one (§8.5); the director emits the same thing. A
        // constraint that never appears here is a constraint that never narrowed anything.
        var perTable = new HashSet<string>();
        var acrossTables = new HashSet<string>();

        for (int seed = 1; seed <= Seeds; seed++)
        {
            for (int camp = 0; camp < 4; camp++)
            {
                var table = Camp.Draw(Fresh(seed) with { CampsHeld = camp });

                foreach (string name in table.Bound)
                {
                    acrossTables.Add(name);
                }

                foreach (var seat in table.Seats)
                {
                    foreach (string name in seat.Bound)
                    {
                        perTable.Add(name);
                    }
                }
            }
        }

        // Per table, because these are sentences about one player's two cards (D-247).
        Assert.Contains("camp-1 engine starter", perTable);
        Assert.DoesNotContain("camp-1 engine starter", acrossTables);

        // And across them, because "no named permanent twice" now spans the camp.
        Assert.Contains("no duplicate named permanent in this camp", acrossTables);
    }

    [Fact]
    public void CampOnesDifferentClassesRow_IsLiveButCannotBind_BecauseTheTwoFlocksShareNoClass()
    {
        // D-160 says a constraint that never narrows anything is decorative, and that the proof log
        // is what makes the difference visible. This is one: DefaultTeams.SideFor is a total function
        // from class to side, so Player A's ducks and Player B's are disjoint classes and the engine
        // starter guaranteed to each can never be for the same one. The row stays in the director as
        // a live guard on that mapping — it is asserted to be SILENT, not absent.
        var kinds = new Dictionary<Team, HashSet<UnitKind>>
        {
            [Team.PlayerA] = new HashSet<UnitKind>(),
            [Team.PlayerB] = new HashSet<UnitKind>(),
        };

        var start = Fresh(1);
        foreach (var duck in start.Squad)
        {
            if (DefaultTeams.SideFor(duck.Kind) is { } side && kinds.ContainsKey(side))
            {
                kinds[side].Add(duck.Kind);
            }
        }

        Assert.NotEmpty(kinds[Team.PlayerA]);
        Assert.NotEmpty(kinds[Team.PlayerB]);
        Assert.Empty(kinds[Team.PlayerA].Intersect(kinds[Team.PlayerB]));

        for (int seed = 1; seed <= Seeds; seed++)
        {
            Assert.DoesNotContain("camp-1 different classes", Camp.Draw(Fresh(seed)).Bound);
        }
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
