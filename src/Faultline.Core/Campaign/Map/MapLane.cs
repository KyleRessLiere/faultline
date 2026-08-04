namespace Faultline.Core
{
    /// <summary>
    /// Which lane of the comfort gradient a node stands in (MASTER_DESIGN §8.5: lanes are unequal by
    /// design — risk buys rarity as geography).
    /// </summary>
    /// <remarks>
    /// A tag on the node rather than a property of a path, because a lane is not a route: the act's
    /// crossing point belongs to neither side, and the pre-boss Rest belongs to both. Tagging the node
    /// lets the map say "this door is the hungry one" without pretending the graph is two lists.
    /// </remarks>
    public enum MapLane
    {
        /// <summary>Belongs to no lane: the start, the crossing, the floor Rest, the boss.</summary>
        Neutral = 0,

        /// <summary>More campfires, plainer fights, thinner rewards.</summary>
        Safe = 1,

        /// <summary>Elites, no mid-lane Rest, and where the map's reward marks live.</summary>
        Hungry = 2,
    }
}
