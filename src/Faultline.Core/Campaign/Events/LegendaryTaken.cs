namespace Faultline.Core
{
    /// <summary>
    /// A duck earned its epithet: one permanent legendary, taken at a gilt destination
    /// (MASTER_DESIGN §8.6).
    /// </summary>
    /// <param name="Player">The player whose duck took it, by the default loadout split (D-092).</param>
    /// <param name="Duck">The squad member wearing it.</param>
    /// <param name="Kind">That duck's archetype, so a renderer need not look it up.</param>
    /// <param name="Card">What was taken.</param>
    /// <param name="Name">Its display name — the epithet.</param>
    /// <param name="Summary">Its one-line rules text.</param>
    public sealed record LegendaryTaken(
        Team? Player,
        RunUnitId Duck,
        UnitKind Kind,
        Legendary Card,
        string Name,
        string Summary) : RunEvent;
}
