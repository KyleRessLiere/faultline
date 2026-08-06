namespace Faultline.Core
{
    /// <summary>
    /// The kind of thing a camp offer is. The camp's one structural constraint is stated in terms of
    /// this: a player's two offers differ in category wherever the pool allows (MASTER_DESIGN §8.5).
    /// </summary>
    /// <remarks>
    /// §8.5 lists a fifth kind — <b>Learn / Replace / Swap</b>, kit surgery — which is not built:
    /// it needs a multi-spender slot surface to pick into, and a category that can never be drawn
    /// would make the "no offer outside the implemented set" assertion vacuous. It is named as
    /// pending in GAMEPLAY.md instead.
    /// </remarks>
    public enum OfferCategory
    {
        /// <summary>A mod on the duck's spender.</summary>
        Mod = 0,

        /// <summary>An extra Pluck charge condition for the duck's class.</summary>
        SecondWind = 1,

        /// <summary>A one-sentence rule addition for the duck.</summary>
        Unlock = 2,

        /// <summary>A one-shot for the duck's pocket.</summary>
        Consumable = 3,

        /// <summary>
        /// A technique modifier on the duck's kit (MASTER_DESIGN §8.6). The v2 pool, and the only
        /// category that carries a rarity and tags of its own.
        /// </summary>
        Technique = 4,
    }
}
