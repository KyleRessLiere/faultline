namespace Faultline.Core
{
    /// <summary>
    /// One tile whose terrain has been changed for a while and will change back — today only the
    /// brambles a Thorn Pouch scatters (MASTER_DESIGN §8.6).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The board carries the change; this carries the way back.</b> A tile that has grown brambles
    /// is genuinely <see cref="TileType.Spikes"/> in <see cref="GameState.Board"/>, so every rule that
    /// already knows what brambles cost — walking on, being displaced onto, Sure-Footed — needs no new
    /// case and cannot disagree with a parallel list of "pretend" hazards. What cannot live on the
    /// board is what the tile <em>used to be</em>, and that is the whole of this record.
    /// </para>
    /// <para>
    /// <see cref="Was"/> rather than an assumption of <see cref="TileType.Open"/>: the fade has to be
    /// a restore, not a reset. Today the only placeable tile is open ground, so the two agree — but a
    /// fade that wrote <c>Open</c> would quietly repair a drain the day the filter widens, and a
    /// hazard deleted by a one-shot is exactly the trick <see cref="Consumables.DebrisTiles"/> was
    /// narrowed to refuse.
    /// </para>
    /// </remarks>
    /// <param name="At">The tile.</param>
    /// <param name="Was">What it was before, and what it becomes again when it fades.</param>
    /// <param name="ThroughRound">The last round it holds. It fades at the end of this round.</param>
    public readonly record struct TemporaryTerrain(Coord At, TileType Was, int ThroughRound);
}
