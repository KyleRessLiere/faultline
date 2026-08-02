namespace Faultline.Core
{
    /// <summary>
    /// Hauls an adjacent clinging ally out of a pit onto a tile of the rescuer's choosing.
    /// </summary>
    /// <remarks>
    /// An <b>action</b> requiring adjacency, not a whole activation (D-082). Walk into reach and
    /// then haul: the move half pays for the walk and the action half pays for the rescue, exactly
    /// as move-then-attack does. The old rule wanted both halves unspent, which made "I can see them
    /// and I am two tiles away" a turn where nothing could be done.
    /// </remarks>
    /// <param name="UnitId">Unit spending its action.</param>
    /// <param name="ClingingId">Clinging ally to pull out.</param>
    /// <param name="To">
    /// Tile to set them down on: open, unoccupied and adjacent to the rescuer. The rescuer's player
    /// picks it — which side of you somebody comes up on is a real decision when the board is the
    /// weapon.
    /// </param>
    public sealed record RescueCommand(UnitId UnitId, UnitId ClingingId, Coord To) : Command;
}
