using System;

namespace Faultline.Core
{
    /// <summary>
    /// One card on a camp's table: a named thing, for a named duck. Everything a renderer needs to
    /// draw an offer card is on it or reachable from <see cref="CampCatalogue"/>.
    /// </summary>
    /// <remarks>
    /// The payload is one integer rather than four nullable enums because an offer is exactly one
    /// thing and the category says which — four fields would let three of them be wrong at once. The
    /// typed accessors below are the only way it is ever read back, and each checks the category.
    /// </remarks>
    /// <param name="Duck">The squad member the offer is for.</param>
    /// <param name="Category">Which pool it came out of.</param>
    /// <param name="Value">The enum value of that pool, as an integer.</param>
    public readonly record struct CampOffer(RunUnitId Duck, OfferCategory Category, int Value)
    {
        /// <summary>A mod offer.</summary>
        /// <param name="duck">Duck it is for.</param>
        /// <param name="mod">The mod.</param>
        /// <returns>The offer.</returns>
        public static CampOffer Of(RunUnitId duck, Mod mod) =>
            new CampOffer(duck, OfferCategory.Mod, (int)mod);

        /// <summary>A Second Wind offer.</summary>
        /// <param name="duck">Duck it is for.</param>
        /// <param name="wind">The condition.</param>
        /// <returns>The offer.</returns>
        public static CampOffer Of(RunUnitId duck, SecondWind wind) =>
            new CampOffer(duck, OfferCategory.SecondWind, (int)wind);

        /// <summary>A tactical unlock offer.</summary>
        /// <param name="duck">Duck it is for.</param>
        /// <param name="unlock">The unlock.</param>
        /// <returns>The offer.</returns>
        public static CampOffer Of(RunUnitId duck, Unlock unlock) =>
            new CampOffer(duck, OfferCategory.Unlock, (int)unlock);

        /// <summary>A consumable offer.</summary>
        /// <param name="duck">Duck it is for.</param>
        /// <param name="consumable">The one-shot.</param>
        /// <returns>The offer.</returns>
        public static CampOffer Of(RunUnitId duck, Consumable consumable) =>
            new CampOffer(duck, OfferCategory.Consumable, (int)consumable);

        /// <summary>A technique modifier offer.</summary>
        /// <param name="duck">Duck it is for.</param>
        /// <param name="technique">The technique.</param>
        /// <returns>The offer.</returns>
        public static CampOffer Of(RunUnitId duck, TechniqueModifier technique) =>
            new CampOffer(duck, OfferCategory.Technique, (int)technique);

        /// <summary>The technique, for a <see cref="OfferCategory.Technique"/> offer.</summary>
        public TechniqueModifier AsTechnique => Category == OfferCategory.Technique
            ? (TechniqueModifier)Value
            : throw new InvalidOperationException("That offer is a " + Category + ", not a technique.");

        /// <summary>
        /// True when taking this card puts something on the duck for the rest of the run. Everything
        /// but a one-shot: §8.6's "no named permanent appears twice in a run" is stated over exactly
        /// this set.
        /// </summary>
        public bool IsPermanent => Category != OfferCategory.Consumable;

        /// <summary>How often this card comes up; see <see cref="CampCatalogue.RarityOf(CampOffer)"/>.</summary>
        public CardRarity Rarity => CampCatalogue.RarityOf(this);

        /// <summary>The §8.6 tags this card wears. <see cref="TechniqueTag.None"/> for the v1 pools.</summary>
        public TechniqueTag Tags => CampCatalogue.TagsOf(this);

        /// <summary>The mod, for a <see cref="OfferCategory.Mod"/> offer.</summary>
        public Mod AsMod => Category == OfferCategory.Mod
            ? (Mod)Value
            : throw new InvalidOperationException("That offer is a " + Category + ", not a mod.");

        /// <summary>The condition, for a <see cref="OfferCategory.SecondWind"/> offer.</summary>
        public SecondWind AsSecondWind => Category == OfferCategory.SecondWind
            ? (SecondWind)Value
            : throw new InvalidOperationException("That offer is a " + Category + ", not a Second Wind.");

        /// <summary>The unlock, for a <see cref="OfferCategory.Unlock"/> offer.</summary>
        public Unlock AsUnlock => Category == OfferCategory.Unlock
            ? (Unlock)Value
            : throw new InvalidOperationException("That offer is a " + Category + ", not an unlock.");

        /// <summary>The one-shot, for a <see cref="OfferCategory.Consumable"/> offer.</summary>
        public Consumable AsConsumable => Category == OfferCategory.Consumable
            ? (Consumable)Value
            : throw new InvalidOperationException("That offer is a " + Category + ", not a consumable.");

        /// <summary>Display name, for the offer card.</summary>
        public string Name => CampCatalogue.NameOf(this);

        /// <summary>One-line rules text, for the offer card.</summary>
        public string Summary => CampCatalogue.SummaryOf(this);

        /// <inheritdoc/>
        public override string ToString() => Duck + " " + Category + " " + Name;
    }
}
