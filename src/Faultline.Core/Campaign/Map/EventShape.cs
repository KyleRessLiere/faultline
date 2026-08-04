namespace Faultline.Core
{
    /// <summary>
    /// The two shapes an event comes in (MASTER_DESIGN §8.5). Both print every price before you
    /// choose; the difference is whether one of the exits is free.
    /// </summary>
    public enum EventShape
    {
        /// <summary>Walkable: there is a way out that costs nothing but the scene.</summary>
        Offer = 0,

        /// <summary>
        /// Every exit is priced. None is built in v1 — the Ferryman and the Peddler's Bargain arrive
        /// with the curse and consumable systems — but the shape is named so an Offer is visibly the
        /// one that lets you leave.
        /// </summary>
        Strait = 1,
    }
}
