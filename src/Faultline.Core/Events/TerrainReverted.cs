namespace Faultline.Core
{
    /// <summary>
    /// A mutated tile's booking has run out and the terrain is what it was before (D-210).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Emitted by <see cref="TerrainMutation.FadeExpired"/> rather than by whatever caused the
    /// mutation, because reversion has no cause standing over it to speak — a Thorn Pouch narrates
    /// its own brambles with <see cref="BramblesGrew"/>, and the collapse clock will narrate its own
    /// cracks, but both of them change back through the same seam and say the same thing when they do.
    /// </para>
    /// <para>
    /// Carries what the tile became, not just where: a renderer that has been drawing thorns needs to
    /// know what to draw instead, and asking state for it is the query an event exists to avoid.
    /// </para>
    /// </remarks>
    /// <param name="At">The tile.</param>
    /// <param name="Now">What it is again.</param>
    /// <param name="Beneath">
    /// The unit that was standing on the tile when it changed back, or <c>null</c> when it was empty.
    /// MASTER_DESIGN §14 #16 has not ruled what that should cost the unit and
    /// <see cref="TerrainMutation.ExpiryBeneathUnit"/> currently charges it nothing — so this field is
    /// how the case stops being invisible while the answer is outstanding.
    /// </param>
    public sealed record TerrainReverted(Coord At, TileType Now, UnitId? Beneath) : GameEvent;
}
