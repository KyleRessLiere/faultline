using System;
using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// The reward taxonomy MASTER_DESIGN §8.6 locked in v2026-08-06q: <b>two axes, never one</b>. A
/// reward's KIND (Technique · Second Wind · Pocket Item · Legendary) and its TIER (Common ·
/// Uncommon · Rare) are read independently, and the guard the ruling exists to enforce is that no
/// tier may admit a Legendary to a camp pool.
/// </summary>
/// <remarks>
/// <para>
/// The through-line: <b>rarity is metadata, not kind</b>. Every failure this suite is written to
/// catch is the same mistake in a different costume — a <c>Rare</c> that behaves like a category, a
/// <c>Legendary</c> that behaves like a tier, or a camp filter that keeps legendaries out by
/// weighting instead of by law.
/// </para>
/// <para>
/// Camps are reached by winning a fight rather than by assembling a state, per §13's standing
/// practice. The pool assertions are swept over both lanes and forty seeds, because "no tier admits
/// a legendary" is a claim about every camp there is.
/// </para>
/// </remarks>
public class RewardTierTests
{
    private const int Seeds = 40;

    /// <summary>Camps deep enough to have left camp 1's authored floor behind.</summary>
    private const int CampsSwept = 4;

    // ---- the guard: camps and destinations are separated by law, never by rarity ----------------

    /// <summary>
    /// <b>THE GUARD.</b> §8.5: "no tier may place a Legendary into a camp pool. Camps and
    /// destinations are separated by the law below, not by rarity."
    /// </summary>
    /// <remarks>
    /// <para>
    /// Asserted over <em>every tier path</em> rather than over the cards a seed happened to roll:
    /// <see cref="CampDirector.Pool"/> is the whole set of cards any weighting could reach, so
    /// bucketing it by tier and finding no legendary in any bucket is the statement the design makes.
    /// A test that only checked dealt tables would pass on a build where a legendary sat in the pool
    /// behind a 5% weight.
    /// </para>
    /// <para>
    /// <b>How it fails.</b> Give <see cref="OfferCategory"/> a <c>Legendary</c> member and teach
    /// <see cref="CampCatalogue"/> to deal one and this goes red on the first seed, at whatever tier
    /// the card was given — including Common, which is the back door a tier-only guard would miss.
    /// It is deliberately not vacuous: every legendary the build ships is
    /// <see cref="CardRarity.Rare"/>, asserted here, so the top rung really is populated and the
    /// thing keeping those cards out of a camp is their kind.
    /// </para>
    /// </remarks>
    [Fact]
    public void NoTierAdmitsALegendaryToACampPool()
    {
        var legendaries = new HashSet<string>(
            LegendaryCatalogue.All().Select(d => d.Name), StringComparer.Ordinal);

        Assert.NotEmpty(legendaries);
        Assert.All(LegendaryCatalogue.All(), d => Assert.Equal(CardRarity.Rare, d.Tier));

        int examined = 0;

        for (int seed = 1; seed <= Seeds; seed++)
        {
            foreach (var run in BothLanes(seed))
            {
                for (int camp = 0; camp < CampsSwept; camp++)
                {
                    var pool = CampDirector.Pool(run with { CampsHeld = camp });

                    Assert.NotEmpty(pool);
                    examined += pool.Count;

                    foreach (var tier in Ladder)
                    {
                        foreach (var offer in pool.Where(o => o.Rarity == tier))
                        {
                            Assert.False(
                                legendaries.Contains(offer.Name),
                                "Seed " + seed + " camp " + camp + " could deal '" + offer.Name
                                + "' at " + tier + ", and that card is a legendary.");
                        }
                    }
                }
            }
        }

        // The sweep is not allowed to become vacuous by an empty pool: if the fixture ever stops
        // reaching a camp with cards on it, this guard would pass by looking at nothing.
        Assert.True(examined > Seeds, "The sweep examined only " + examined + " candidate cards.");
    }

    /// <summary>
    /// The same law one level down: the camp's offer type cannot even <em>spell</em> a legendary, so
    /// the separation is structural rather than a filter somebody could forget to apply.
    /// </summary>
    /// <remarks>
    /// This is the assertion §8.5's "separated by the law, not by rarity" actually cashes out to. If
    /// a member is ever added to <see cref="OfferCategory"/>, this goes red and whoever added it has
    /// to say out loud which tier they think admits it.
    /// </remarks>
    [Fact]
    public void ACampOfferCannotSpellALegendary_AndTheDrawableSetIsTheWholeEnum()
    {
        Assert.Equal(
            new[]
            {
                OfferCategory.Mod, OfferCategory.SecondWind, OfferCategory.Unlock,
                OfferCategory.Consumable, OfferCategory.Technique,
            },
            Enum.GetValues(typeof(OfferCategory)).Cast<OfferCategory>().ToArray());

        Assert.Equal(
            Enum.GetValues(typeof(OfferCategory)).Length,
            CampCatalogue.DrawableCategories().Count);
    }

    /// <summary>
    /// And the same law from the far side: a legendary that a duck is actually offered comes off the
    /// destination's table, never a camp's — reached by playing to a gilt node rather than by
    /// inspecting the catalogue.
    /// </summary>
    [Fact]
    public void ALegendaryReachesADuckThroughADestination_AndTheCampNeverDealsOne()
    {
        var run = MapFixture.Enter(MapFixture.Start(1));
        run = RunFixture.EndFightInAWin(run).NewState;
        TestPlay.Require(run.Phase == RunPhase.AtCamp, "The won fight did not open a camp.");

        var camp = Camp.Draw(run);

        Assert.NotEmpty(camp.Offers);
        Assert.All(camp.Offers, o => Assert.NotEqual(CardRarity.Rare, o.Rarity));

        // The destination's own table is where the Rare tier is actually reachable from.
        Assert.All(
            LegendaryCatalogue.All(),
            d => Assert.Equal(CardRarity.Rare, new LegendaryOffer(run.Squad[0].Id, d.Card).Rarity));
    }

    // ---- the two axes are read independently -----------------------------------------------------

    /// <summary>
    /// Kind and tier answer separately on both reward types. Neither is derivable from the other,
    /// which is the whole content of "orthogonal".
    /// </summary>
    [Fact]
    public void TierIsReadableWithoutAskingTheKind_OnACampCardAndOnALegendaryAlike()
    {
        var duck = new RunUnitId(0);

        // One kind spanning two tiers: the technique pool carries Common and Uncommon cards, so the
        // tier is not a restatement of the category.
        var techniqueTiers = CampCatalogue.TechniquePool()
            .Select(t => CampOffer.Of(duck, t).Rarity)
            .Distinct()
            .ToList();

        Assert.True(techniqueTiers.Count > 1, "One kind must be able to span more than one tier.");

        // One tier spanning two kinds: Rare is worn by legendaries, and a legendary answers the tier
        // question through the same ladder a camp card does.
        Assert.All(LegendaryCatalogue.All(), d => Assert.Equal(CardRarity.Rare, d.Tier));
        Assert.Equal(CardRarity.Rare, LegendaryCatalogue.RarityOf(Legendary.FollowThrough));

        // And the tier enum holds tiers only — no kind smuggled onto the ladder.
        Assert.Equal(
            new[] { CardRarity.Common, CardRarity.Uncommon, CardRarity.Rare },
            Enum.GetValues(typeof(CardRarity)).Cast<CardRarity>().ToArray());
    }

    /// <summary>
    /// Tempo's "+1 AP" promotion (§3) reads Uncommon, and §8.6 folds it onto this ladder — but the
    /// card is <b>not built</b>, so there is nothing to tag and nothing was invented for it.
    /// </summary>
    [Fact]
    public void TempoIsNotInAnyPool_SoItsUncommonReadingTagsNothing()
    {
        var duck = new RunUnitId(0);

        var names = CampCatalogue.ModPool().Select(m => CampCatalogue.NameOf(m))
            .Concat(CampCatalogue.SecondWindPool().Select(w => CampCatalogue.NameOf(w)))
            .Concat(CampCatalogue.UnlockPool().Select(u => CampCatalogue.NameOf(u)))
            .Concat(CampCatalogue.ConsumablePool().Select(c => CampCatalogue.NameOf(c)))
            .Concat(CampCatalogue.TechniquePool().Select(t => CampCatalogue.NameOf(t)))
            .ToList();

        Assert.DoesNotContain("Tempo", names);
        Assert.DoesNotContain(
            CampCatalogue.ModPool(), m => CampOffer.Of(duck, m).Rarity != CardRarity.Common);
    }

    // ---- the odds are data, per source ------------------------------------------------------------

    /// <summary>
    /// §8.6's numbers, in a table keyed by source. §14 #9 and #15 leave the odds open, so they live
    /// where a tuner can reach them without touching a rule.
    /// </summary>
    [Fact]
    public void TheOddsAreTunableData_PerSource_AndTheDirectorHoldsNoRateOfItsOwn()
    {
        Assert.Equal(new RarityOdds(60, 35, 5), RarityOdds.For(RewardSource.SafeCamp));
        Assert.Equal(new RarityOdds(35, 50, 15), RarityOdds.For(RewardSource.HungryCamp));

        var safe = RarityOdds.For(RewardSource.SafeCamp);
        Assert.Equal(60, safe.Of(CardRarity.Common));
        Assert.Equal(35, safe.Of(CardRarity.Uncommon));
        Assert.Equal(5, safe.Of(CardRarity.Rare));

        // Every source the enum names has a tuned row; a member with no row is an untuned source,
        // and an all-zero row would silently fall through to an unweighted draw.
        foreach (RewardSource source in Enum.GetValues(typeof(RewardSource)))
        {
            var odds = RarityOdds.For(source);
            Assert.True(
                Ladder.Sum(t => odds.Of(t)) > 0,
                "RewardSource." + source + " has no tuned odds behind it.");
        }
    }

    /// <summary>The lane picks the row, and a run with no act map reads as safe.</summary>
    [Fact]
    public void TheLaneChoosesTheSourceAndTheSourceChoosesTheOdds()
    {
        var safe = Fresh(1) with { MapState = MapState.At(SafeNode) };
        var hungry = Fresh(1) with { MapState = MapState.At(HungryNode) };

        Assert.Equal(MapLane.Safe, safe.CurrentMapNode!.Lane);
        Assert.Equal(MapLane.Hungry, hungry.CurrentMapNode!.Lane);

        Assert.Equal(RewardSource.SafeCamp, RarityOdds.SourceAt(safe));
        Assert.Equal(RewardSource.HungryCamp, RarityOdds.SourceAt(hungry));

        Assert.Equal(RarityOdds.For(RewardSource.SafeCamp), CampDirector.WeightsFor(safe));
        Assert.Equal(RarityOdds.For(RewardSource.HungryCamp), CampDirector.WeightsFor(hungry));

        // A linear campaign has no lane at all; "safe" is the honest reading of no gradient.
        Assert.Equal(RewardSource.SafeCamp, RarityOdds.SourceAt(RunFixture.Start()));
        Assert.Equal(RewardSource.SafeCamp, RarityOdds.SourceAt(null));
    }

    // ---- fixtures ---------------------------------------------------------------------------------

    private static readonly CardRarity[] Ladder =
    {
        CardRarity.Common, CardRarity.Uncommon, CardRarity.Rare,
    };

    /// <summary>A safe node of the authored act map, and a hungry one.</summary>
    private const string SafeNode = "c2-bait-and-break";

    /// <summary>The hungry lane's node, for the other half of the comfort gradient.</summary>
    private const string HungryNode = "c2-the-teeth";

    /// <summary>The same run standing on each lane, so a pool claim is made about both gradients.</summary>
    private static IEnumerable<RunState> BothLanes(int seed)
    {
        var run = Fresh(seed);
        yield return run with { MapState = MapState.At(SafeNode) };
        yield return run with { MapState = MapState.At(HungryNode) };
    }

    /// <summary>
    /// A run standing at a camp on the act map, reached by winning the opening fight rather than by
    /// assembling a state — so the squad and the RNG cursor are the ones the rules produce.
    /// </summary>
    private static RunState Fresh(int seed)
    {
        var run = MapFixture.Enter(MapFixture.Start(seed));
        run = RunFixture.EndFightInAWin(run).NewState;

        TestPlay.Require(run.Phase == RunPhase.AtCamp, "The won fight did not open a camp.");
        return run;
    }

}
