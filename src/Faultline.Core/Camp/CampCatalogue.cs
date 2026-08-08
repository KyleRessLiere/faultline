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
    /// One place to ask, in Core, for the same reason <see cref="Verve.DescriptionOf"/> is here: the
    /// card and the rule must not be able to drift apart. A shell that wrote its own offer text would
    /// be a second, unversioned copy of the pool.
    /// </para>
    /// <para>
    /// <b>Everything drawable is in this file.</b> The camp draws from these four lists and nothing
    /// else, which is what makes "no offer type outside the implemented set can be drawn" an
    /// assertion about one table rather than about the whole run layer.
    /// </para>
    /// <para>
    /// The <em>metadata</em> of what is drawn — name, card text, eligible class or spender — now lives
    /// in <see cref="UpgradeDefinition"/> and <see cref="ConsumableDefinition"/>; the accessors below
    /// read those registries rather than restating them, so there is exactly one source per name and
    /// per number (component review, "Risks of over-engineering"). What stays here is the
    /// <em>pools</em>: which of those things a camp may actually hand out.
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

            // The alternate spenders' mods. They sit in the same pool because they are drawn the
            // same way and filtered the same way: EligibleFor already refuses a mod whose host slot
            // the duck does not hold, so a Vanguard who never took Retort is never offered a Grudge
            // and nothing new had to learn that (G4).
            Mod.HairTrigger, Mod.Backhand, Mod.Grudge,
            Mod.LowSky, Mod.Shatterfall, Mod.Updraft,
            Mod.Riptide, Mod.WideWhirl, Mod.Churn,
            Mod.LowWall, Mod.SeaWall, Mod.Toll,

            // The alternate actions' mods, and the filter above them did not change by one character
            // to take them: Kits.HostOf answers a KitEntry whichever kind of ability the host is, so
            // a Fisher who never learned Punt is never offered a Long Punt by the same line that
            // already refused her a Grudge. One filter over both host kinds is the evidence the
            // widening was the right shape (D-243).
            Mod.Downhill, Mod.Ploughshare, Mod.FullWeight,
            Mod.ShortPole, Mod.LongPunt, Mod.Downstream,
            Mod.LongReach, Mod.ChangingOfTheGuard,
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
            Unlock.SureFooted, Unlock.SteadyHands, Unlock.LongBoot,
        };

        private static readonly Consumable[] AllConsumables =
        {
            // Enum order, and it has to stay that way: ConsumableDefinition's registry is asserted to
            // match this list exactly, so that which card a seed deals never depends on hashing. New
            // one-shots append here for the same reason they append to the enum — a saved run's
            // pocket keeps its meaning.
            Consumable.DriedMinnow,
            Consumable.BrambleSalve,
            Consumable.OldRope,
            Consumable.DuckFeatherCharm,
            Consumable.CrateOfDebris,
            Consumable.GreasedFeather,
            Consumable.ChalkMark,
            Consumable.ThornPouch,
            Consumable.SplitReed,
            Consumable.SignalWhistle,
        };

        private static readonly TechniqueModifier[] AllTechniques =
        {
            TechniqueModifier.FollowIn, TechniqueModifier.RattlingImpact,
            TechniqueModifier.ShortLine, TechniqueModifier.HandOff,
            TechniqueModifier.Spotter, TechniqueModifier.CrossingShot,
            TechniqueModifier.StoredForce, TechniqueModifier.ShelterStep,
        };

        private static readonly OfferCategory[] Categories =
        {
            OfferCategory.Mod, OfferCategory.SecondWind, OfferCategory.Unlock,
            OfferCategory.Consumable, OfferCategory.Technique,
        };

        /// <summary>Every mod in the pool, in pool order.</summary>
        /// <returns>The mods, spender-hosted first.</returns>
        public static IReadOnlyList<Mod> ModPool() => AllMods;

        /// <summary>
        /// The mods the pool hangs on one slot. <b>Empty is a real answer</b> — Guard Stance and the
        /// basic attacks host none — and a surface that drew sockets anyway would be promising a
        /// player cards no camp can deal (D-243).
        /// </summary>
        /// <param name="slot">Slot to ask about.</param>
        /// <returns>Its mods, in pool order.</returns>
        public static IReadOnlyList<Mod> ModsFor(KitEntry slot)
        {
            var mods = new List<Mod>();
            foreach (var mod in AllMods)
            {
                if (Kits.HostOf(mod) == slot)
                {
                    mods.Add(mod);
                }
            }

            return mods;
        }

        /// <summary>Every Second Wind condition in the v1 pool, in pool order.</summary>
        /// <returns>The eight conditions.</returns>
        public static IReadOnlyList<SecondWind> SecondWindPool() => AllSecondWinds;

        /// <summary>Every tactical unlock built, in pool order.</summary>
        /// <returns>The four unlocks.</returns>
        public static IReadOnlyList<Unlock> UnlockPool() => AllUnlocks;

        /// <summary>Every tactical consumable, in pool order.</summary>
        /// <returns>The built one-shots.</returns>
        public static IReadOnlyList<Consumable> ConsumablePool() => AllConsumables;

        /// <summary>Every technique modifier built, in pool order.</summary>
        /// <returns>The eight techniques.</returns>
        public static IReadOnlyList<TechniqueModifier> TechniquePool() => AllTechniques;

        /// <summary>Which class may hold a technique. Class-bound, always.</summary>
        /// <param name="technique">Technique to place.</param>
        /// <returns>The archetype that can carry it.</returns>
        public static UnitKind KindOf(TechniqueModifier technique) =>
            TechniqueDefinition.For(technique).Kind;

        /// <summary>Display name.</summary>
        /// <param name="technique">Technique to name.</param>
        /// <returns>Its name.</returns>
        public static string NameOf(TechniqueModifier technique) =>
            TechniqueDefinition.For(technique).Name;

        /// <summary>What a technique changes, in one sentence.</summary>
        /// <param name="technique">Technique to describe.</param>
        /// <returns>Its card text.</returns>
        public static string SummaryOf(TechniqueModifier technique) =>
            TechniqueDefinition.For(technique).Summary;

        /// <summary>
        /// How often a card comes up. Only the technique pool is labelled by §8.6; the v1 pools are
        /// Common because the director has to weight them somehow (D-159).
        /// </summary>
        /// <param name="offer">Offer to price.</param>
        /// <returns>Its rarity.</returns>
        public static CardRarity RarityOf(CampOffer offer) => offer.Category == OfferCategory.Technique
            ? TechniqueDefinition.For(offer.AsTechnique).Rarity
            : CardRarity.Common;

        /// <summary>
        /// The §8.6 tags a card wears. Only techniques carry any — the v1 pools predate the tag
        /// vocabulary and are deliberately not retro-tagged (D-159).
        /// </summary>
        /// <param name="offer">Offer to read.</param>
        /// <returns>Its tags, or <see cref="TechniqueTag.None"/>.</returns>
        public static TechniqueTag TagsOf(CampOffer offer) => offer.Category == OfferCategory.Technique
            ? TechniqueDefinition.For(offer.AsTechnique).Tags
            : TechniqueTag.None;

        /// <summary>The categories a camp can draw from, in draw order.</summary>
        /// <returns>The four implemented categories.</returns>
        public static IReadOnlyList<OfferCategory> DrawableCategories() => Categories;

        /// <summary>Which spender a mod bolts onto.</summary>
        /// <param name="mod">Mod to place.</param>
        /// <returns>The spender it modifies.</returns>
        public static VerveSpend SpenderOf(Mod mod) =>
            UpgradeDefinition.For(mod).Spender
            ?? throw new ArgumentOutOfRangeException(nameof(mod), mod, "No spender for that mod.");

        /// <summary>Which class holds the spender a mod bolts onto.</summary>
        /// <param name="mod">Mod to place.</param>
        /// <returns>The archetype that can carry it.</returns>
        public static UnitKind KindOf(Mod mod) =>
            UpgradeDefinition.For(mod).Kind
            ?? throw new ArgumentOutOfRangeException(nameof(mod), mod, "No class for that mod.");

        /// <summary>Which class a Second Wind condition belongs to. Class-bound, always.</summary>
        /// <param name="wind">Condition to place.</param>
        /// <returns>The archetype that can earn from it.</returns>
        public static UnitKind KindOf(SecondWind wind) =>
            UpgradeDefinition.For(wind).Kind
            ?? throw new ArgumentOutOfRangeException(nameof(wind), wind, "No class for that condition.");

        /// <summary>Display name.</summary>
        /// <param name="mod">Mod to name.</param>
        /// <returns>Its name.</returns>
        public static string NameOf(Mod mod) => UpgradeDefinition.For(mod).Name;

        /// <summary>Display name.</summary>
        /// <param name="wind">Condition to name.</param>
        /// <returns>Its name.</returns>
        public static string NameOf(SecondWind wind) => UpgradeDefinition.For(wind).Name;

        /// <summary>Display name.</summary>
        /// <param name="unlock">Unlock to name.</param>
        /// <returns>Its name.</returns>
        public static string NameOf(Unlock unlock) => UpgradeDefinition.For(unlock).Name;

        /// <summary>Display name.</summary>
        /// <param name="consumable">One-shot to name.</param>
        /// <returns>Its name.</returns>
        public static string NameOf(Consumable consumable) => ConsumableDefinition.For(consumable).Name;

        /// <summary>What a mod does, in plain words.</summary>
        /// <param name="mod">Mod to describe.</param>
        /// <returns>Its card text.</returns>
        public static string SummaryOf(Mod mod) => UpgradeDefinition.For(mod).Summary;

        /// <summary>What a Second Wind earns from, in plain words.</summary>
        /// <param name="wind">Condition to describe.</param>
        /// <returns>Its card text.</returns>
        public static string SummaryOf(SecondWind wind) => UpgradeDefinition.For(wind).Summary;

        /// <summary>What an unlock changes, in one sentence.</summary>
        /// <param name="unlock">Unlock to describe.</param>
        /// <returns>Its card text.</returns>
        public static string SummaryOf(Unlock unlock) => UpgradeDefinition.For(unlock).Summary;

        /// <summary>What a one-shot does, in one sentence.</summary>
        /// <param name="consumable">One-shot to describe.</param>
        /// <returns>Its card text.</returns>
        public static string SummaryOf(Consumable consumable) =>
            ConsumableDefinition.For(consumable).Summary;

        /// <summary>Display name of whatever an offer holds.</summary>
        /// <param name="offer">Offer to name.</param>
        /// <returns>Its name.</returns>
        public static string NameOf(CampOffer offer) => offer.Category switch
        {
            OfferCategory.Mod => NameOf(offer.AsMod),
            OfferCategory.SecondWind => NameOf(offer.AsSecondWind),
            OfferCategory.Unlock => NameOf(offer.AsUnlock),
            OfferCategory.Technique => NameOf(offer.AsTechnique),
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
            OfferCategory.Technique => SummaryOf(offer.AsTechnique),
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

            foreach (var mod in AllMods)
            {
                if (KindOf(mod) == duck.Kind
                    && !loadout.Has(mod)
                    && Kits.Holds(duck.Kind, loadout, Kits.HostOf(mod))
                    && Kits.RefusalFor(loadout, mod) is null)
                {
                    offers.Add(CampOffer.Of(duck.Id, mod));
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

            foreach (var technique in AllTechniques)
            {
                if (KindOf(technique) != duck.Kind || loadout.Has(technique))
                {
                    continue;
                }

                // The same line that refuses a mod, asking the same question and taking the same kind
                // of answer: a card needs the duck to still own what it modifies. Every technique has
                // a host now, so there is no second branch for the ones that used not to.
                if (!Kits.Holds(duck.Kind, loadout, Kits.HostOf(technique)))
                {
                    continue;
                }

                if (Kits.RefusalFor(loadout, technique) is null)
                {
                    offers.Add(CampOffer.Of(duck.Id, technique));
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
