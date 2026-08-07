namespace Faultline.Core
{
    /// <summary>
    /// The kind of thing a camp offer is. The camp's one structural constraint is stated in terms of
    /// this: a player's two offers differ in category wherever the pool allows (MASTER_DESIGN §8.5).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is §8.5's camp-category cut, and it is NOT §8.6's KIND axis.</b> The two look alike
    /// and are different questions. §8.5 lists the camp's five categories — Modify · Learn/Replace/
    /// Swap · Second Wind · Tactical unlocks · Consumables — which is what this enum enumerates.
    /// §8.6's taxonomy names four KINDS — Technique · Second Wind · Pocket Item · Legendary — which
    /// this enum does not: it has no Legendary (deliberately — that is the law), and it splits
    /// <c>Mod</c>, <c>Unlock</c> and <c>Technique</c> where the kind axis might merge them.
    /// </para>
    /// <para>
    /// <b>The mapping between them is owed, not guessed</b> (§14 #13, "the one item on this list
    /// holding up build"). §8.6 says so itself: Technique "covers <em>at most</em> Modify,
    /// Learn/Replace/Swap and Tactical unlocks — but which... is unwritten". So this enum was left
    /// as the cut it already was rather than being quietly redefined into the other one, and the
    /// tier axis was built beside it instead (D-197). Tier is readable on every reward today;
    /// kind waits on the designer's table.
    /// </para>
    /// <para>
    /// §8.5's <b>Learn / Replace / Swap</b> is not built: it needs a multi-spender slot surface to
    /// pick into, and a category that can never be drawn would make the "no offer outside the
    /// implemented set" assertion vacuous. It is named as pending in GAMEPLAY.md instead.
    /// </para>
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
