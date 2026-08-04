using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// The v1 camp pools (MASTER_DESIGN §8.6) as data: which mod belongs to which spender, which
    /// Second Wind to which class, what each thing is called and what it says on the card.
    /// </summary>
    /// <remarks>
    /// <para>
    /// One table, in Core, for the same reason <see cref="Verve.DescriptionOf"/> is here: the card
    /// and the rule must not be able to drift apart. A shell that wrote its own offer text would be
    /// a second, unversioned copy of the pool.
    /// </para>
    /// <para>
    /// <b>Everything drawable is in this file.</b> The camp draws from these four lists and nothing
    /// else, which is what makes "no offer type outside the implemented set can be drawn" an
    /// assertion about one table rather than about the whole run layer.
    /// </para>
    /// </remarks>
    public static class CampCatalogue
    {
        private static readonly Mod[] AllMods =
        {
            Mod.Heavier, Mod.Freight, Mod.Echo,
            Mod.LightLine, Mod.LongRod, Mod.BigSplash,
            Mod.FletchersRhythm, Mod.LongDraw, Mod.HuntersRefund,
            Mod.Thorough, Mod.Neighborly, Mod.Quick,
        };

        private static readonly SecondWind[] AllSecondWinds =
        {
            SecondWind.StaggerAnEnemy, SecondWind.BullRushConnects,
            SecondWind.ChumTheWater, SecondWind.DisplacedAdjacent,
            SecondWind.LongKill, SecondWind.Roost,
            SecondWind.Patience, SecondWind.SpearTip,
        };

        private static readonly Unlock[] AllUnlocks =
        {
            Unlock.SureFooted, Unlock.Climber, Unlock.SteadyHands, Unlock.LongBoot,
        };

        private static readonly Consumable[] AllConsumables =
        {
            Consumable.DriedMinnow,
            Consumable.BrambleSalve,
            Consumable.OldRope,
            Consumable.DuckFeatherCharm,
            Consumable.CrateOfDebris,
        };

        private static readonly OfferCategory[] Categories =
        {
            OfferCategory.Mod, OfferCategory.SecondWind, OfferCategory.Unlock, OfferCategory.Consumable,
        };

        /// <summary>Every mod in the v1 pool, in pool order.</summary>
        /// <returns>The twelve mods.</returns>
        public static IReadOnlyList<Mod> ModPool() => AllMods;

        /// <summary>Every Second Wind condition in the v1 pool, in pool order.</summary>
        /// <returns>The eight conditions.</returns>
        public static IReadOnlyList<SecondWind> SecondWindPool() => AllSecondWinds;

        /// <summary>Every tactical unlock built, in pool order.</summary>
        /// <returns>The four unlocks.</returns>
        public static IReadOnlyList<Unlock> UnlockPool() => AllUnlocks;

        /// <summary>Every tactical consumable, in pool order.</summary>
        /// <returns>The five one-shots.</returns>
        public static IReadOnlyList<Consumable> ConsumablePool() => AllConsumables;

        /// <summary>The categories a camp can draw from, in draw order.</summary>
        /// <returns>The four implemented categories.</returns>
        public static IReadOnlyList<OfferCategory> DrawableCategories() => Categories;

        /// <summary>Which spender a mod bolts onto.</summary>
        /// <param name="mod">Mod to place.</param>
        /// <returns>The spender it modifies.</returns>
        public static VerveSpend SpenderOf(Mod mod) => mod switch
        {
            Mod.Heavier or Mod.Freight or Mod.Echo => VerveSpend.WreckingWeight,
            Mod.LightLine or Mod.LongRod or Mod.BigSplash => VerveSpend.Cast,
            Mod.FletchersRhythm or Mod.LongDraw or Mod.HuntersRefund => VerveSpend.DoubleNock,
            Mod.Thorough or Mod.Neighborly or Mod.Quick => VerveSpend.Preen,
            _ => throw new ArgumentOutOfRangeException(nameof(mod), mod, "No spender for that mod."),
        };

        /// <summary>Which class holds the spender a mod bolts onto.</summary>
        /// <param name="mod">Mod to place.</param>
        /// <returns>The archetype that can carry it.</returns>
        public static UnitKind KindOf(Mod mod) => SpenderOf(mod) switch
        {
            VerveSpend.WreckingWeight => UnitKind.Vanguard,
            VerveSpend.Cast => UnitKind.Threadcaster,
            VerveSpend.DoubleNock => UnitKind.Archer,
            _ => UnitKind.Wardbearer,
        };

        /// <summary>Which class a Second Wind condition belongs to. Class-bound, always.</summary>
        /// <param name="wind">Condition to place.</param>
        /// <returns>The archetype that can earn from it.</returns>
        public static UnitKind KindOf(SecondWind wind) => wind switch
        {
            SecondWind.StaggerAnEnemy or SecondWind.BullRushConnects => UnitKind.Vanguard,
            SecondWind.ChumTheWater or SecondWind.DisplacedAdjacent => UnitKind.Threadcaster,
            SecondWind.LongKill or SecondWind.Roost => UnitKind.Archer,
            SecondWind.Patience or SecondWind.SpearTip => UnitKind.Wardbearer,
            _ => throw new ArgumentOutOfRangeException(nameof(wind), wind, "No class for that condition."),
        };

        /// <summary>Display name.</summary>
        /// <param name="mod">Mod to name.</param>
        /// <returns>Its name.</returns>
        public static string NameOf(Mod mod) => mod switch
        {
            Mod.Heavier => "Heavier",
            Mod.Freight => "Freight",
            Mod.Echo => "Echo",
            Mod.LightLine => "Light Line",
            Mod.LongRod => "Long Rod",
            Mod.BigSplash => "Big Splash",
            Mod.FletchersRhythm => "Fletcher's Rhythm",
            Mod.LongDraw => "Long Draw",
            Mod.HuntersRefund => "Hunter's Refund",
            Mod.Thorough => "Thorough",
            Mod.Neighborly => "Neighborly",
            _ => "Quick",
        };

        /// <summary>Display name.</summary>
        /// <param name="wind">Condition to name.</param>
        /// <returns>Its name.</returns>
        public static string NameOf(SecondWind wind) => wind switch
        {
            SecondWind.StaggerAnEnemy => "Rattle",
            SecondWind.BullRushConnects => "Impact",
            SecondWind.ChumTheWater => "Chum the Water",
            SecondWind.DisplacedAdjacent => "Undertow",
            SecondWind.LongKill => "Long Shot",
            SecondWind.Roost => "Roost",
            SecondWind.Patience => "Patience",
            _ => "Spear Tip",
        };

        /// <summary>Display name.</summary>
        /// <param name="unlock">Unlock to name.</param>
        /// <returns>Its name.</returns>
        public static string NameOf(Unlock unlock) => unlock switch
        {
            Unlock.SureFooted => "Sure-Footed",
            Unlock.Climber => "Climber",
            Unlock.SteadyHands => "Steady Hands",
            _ => "Long Boot",
        };

        /// <summary>Display name.</summary>
        /// <param name="consumable">One-shot to name.</param>
        /// <returns>Its name.</returns>
        public static string NameOf(Consumable consumable) => consumable switch
        {
            Consumable.DriedMinnow => "Dried Minnow",
            Consumable.BrambleSalve => "Bramble Salve",
            Consumable.OldRope => "Old Rope",
            Consumable.DuckFeatherCharm => "Duck Feather Charm",
            _ => "Crate of Debris",
        };

        /// <summary>What a mod does, in plain words.</summary>
        /// <param name="mod">Mod to describe.</param>
        /// <returns>Its card text.</returns>
        public static string SummaryOf(Mod mod) => mod switch
        {
            Mod.Heavier => "Contact damage " + Verve.HeavierContactDamage + ".",
            Mod.Freight => "+" + Verve.FreightDistanceBonus + " distance instead of +"
                + Verve.ContactDistanceBonus + ".",
            Mod.Echo => "If the charged push collides, refund 1 " + Naming.Meter + ".",
            Mod.LightLine => "Cost " + Verve.LightLineCost + ".",
            Mod.LongRod => "Grab range " + Throw.LongRodGrabRange + ".",
            Mod.BigSplash => "The landing also deals " + Throw.SplashDamage
                + " to enemies adjacent to the landing tile.",
            Mod.FletchersRhythm => "Cost " + Verve.FletchersRhythmCost + ".",
            Mod.LongDraw => "Both shots range " + Combat.LongDrawRange + ".",
            Mod.HuntersRefund => "A killing shot refunds 1.",
            Mod.Thorough => "Also clears his Stagger.",
            Mod.Neighborly => "May target an adjacent ally.",
            _ => "Cost " + Verve.QuickPreenCost + ".",
        };

        /// <summary>What a Second Wind earns from, in plain words.</summary>
        /// <param name="wind">Condition to describe.</param>
        /// <returns>Its card text.</returns>
        public static string SummaryOf(SecondWind wind) => wind switch
        {
            SecondWind.StaggerAnEnemy => "+1 when he Staggers an enemy.",
            SecondWind.BullRushConnects => "+1 when Bull Rush connects.",
            SecondWind.ChumTheWater =>
                "+1 when an enemy she displaced this round is killed by anyone.",
            SecondWind.DisplacedAdjacent =>
                "+1 the first time each round an enemy ends a displacement adjacent to her.",
            SecondWind.LongKill => "+1 on kills at range " + Verve.LongKillRange + ".",
            SecondWind.Roost => "+1 the first time each fight she ends a round on high ground.",
            SecondWind.Patience => "+1 when Guard Stance expires unabsorbed — patience pays.",
            _ => "+1 when the Spear's tip tile hits.",
        };

        /// <summary>What an unlock changes, in one sentence.</summary>
        /// <param name="unlock">Unlock to describe.</param>
        /// <returns>Its card text.</returns>
        public static string SummaryOf(Unlock unlock) => unlock switch
        {
            Unlock.SureFooted => "Brambles cost this duck " + Activation.StepCost + " AP.",
            Unlock.Climber => "High ground costs this duck " + Activation.StepCost + " AP.",
            Unlock.SteadyHands => "Rescue costs this duck " + Activation.SteadyHandsRescueCost + " AP.",
            _ => "May Kick-in at range " + Pits.LongBootKickRange + ".",
        };

        /// <summary>What a one-shot does, in one sentence.</summary>
        /// <param name="consumable">One-shot to describe.</param>
        /// <returns>Its card text.</returns>
        public static string SummaryOf(Consumable consumable) => consumable switch
        {
            Consumable.DriedMinnow => "Gain " + Consumables.MinnowPluck + " " + Naming.Meter + " now.",
            Consumable.BrambleSalve => "Heal " + Consumables.SalveHeal + ", never past your maximum.",
            Consumable.OldRope => "Rescue an adjacent clinger as a free action.",
            Consumable.DuckFeatherCharm => "Refill Footing " + Consumables.CharmFooting + ".",
            _ => "Place debris on an adjacent open tile.",
        };

        /// <summary>Display name of whatever an offer holds.</summary>
        /// <param name="offer">Offer to name.</param>
        /// <returns>Its name.</returns>
        public static string NameOf(CampOffer offer) => offer.Category switch
        {
            OfferCategory.Mod => NameOf(offer.AsMod),
            OfferCategory.SecondWind => NameOf(offer.AsSecondWind),
            OfferCategory.Unlock => NameOf(offer.AsUnlock),
            _ => NameOf(offer.AsConsumable),
        };

        /// <summary>Card text of whatever an offer holds.</summary>
        /// <param name="offer">Offer to describe.</param>
        /// <returns>Its card text.</returns>
        public static string SummaryOf(CampOffer offer) => offer.Category switch
        {
            OfferCategory.Mod => SummaryOf(offer.AsMod),
            OfferCategory.SecondWind => SummaryOf(offer.AsSecondWind),
            OfferCategory.Unlock => SummaryOf(offer.AsUnlock),
            _ => SummaryOf(offer.AsConsumable),
        };

        /// <summary>
        /// Every offer this duck could be handed right now, in a fixed order: mods its spender has
        /// room for, its class's unheld conditions, its unheld unlocks, and — while its pocket is
        /// empty — the tactical one-shots.
        /// </summary>
        /// <remarks>
        /// An offer that cannot be taken is not an offer, which is why the two capacity rules are
        /// filters here rather than errors at the pick: a full spender contributes no mods
        /// (MASTER_DESIGN §8.6's slot ceiling) and a full pocket contributes no consumables (§8.5's
        /// one pocket per duck).
        /// </remarks>
        /// <param name="duck">Squad member to draw for.</param>
        /// <returns>The candidate offers, in a reproducible order.</returns>
        public static IReadOnlyList<CampOffer> EligibleFor(RunUnit duck)
        {
            var offers = new List<CampOffer>();
            if (duck is null || !duck.IsAvailable)
            {
                return offers;
            }

            var loadout = duck.Loadout;

            if (!loadout.SpenderIsFull)
            {
                foreach (var mod in AllMods)
                {
                    if (KindOf(mod) == duck.Kind && !loadout.Has(mod))
                    {
                        offers.Add(CampOffer.Of(duck.Id, mod));
                    }
                }
            }

            foreach (var wind in AllSecondWinds)
            {
                if (KindOf(wind) == duck.Kind && !loadout.Has(wind))
                {
                    offers.Add(CampOffer.Of(duck.Id, wind));
                }
            }

            foreach (var unlock in AllUnlocks)
            {
                if (!loadout.Has(unlock))
                {
                    offers.Add(CampOffer.Of(duck.Id, unlock));
                }
            }

            if (loadout.Pocket is null)
            {
                foreach (var consumable in AllConsumables)
                {
                    offers.Add(CampOffer.Of(duck.Id, consumable));
                }
            }

            return offers;
        }
    }
}
