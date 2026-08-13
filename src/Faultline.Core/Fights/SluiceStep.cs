using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// One step of a board's water level: the sluice that holds it back, and the tiles the canal
    /// takes when that sluice comes down. Read from a <c>sluice: x,y = x,y x,y ...</c> line.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The schedule is authored data and is published from fight start</b>, exactly like the wave
    /// timetable and for the identical reason (D-035): a hidden timetable is dread, a published one is
    /// planning. Every step's gate and every tile it floods is inspectable before anything is clicked
    /// — see <see cref="Sluice.Next"/> and <see cref="Sluice.Pending"/>.
    /// </para>
    /// <para>
    /// <b>A gate is a <see cref="Structure"/>; the water is a <see cref="TileType"/>.</b> Those are
    /// two orthogonal axes and mixing them is the documented error: terrain is a dense array, and
    /// structures are a sparse hit-point-bearing occupant list whose tile underneath stays walkable
    /// once the masonry is rubble. So this record names a gate by the tile its structure stands on and
    /// never carries the gate itself.
    /// </para>
    /// </remarks>
    /// <param name="Gate">Tile carrying the sluice structure that holds this step back.</param>
    /// <param name="Tiles">Tiles the canal takes when it opens, in the order the line wrote them.</param>
    public sealed record SluiceStep(Coord Gate, IReadOnlyList<Coord> Tiles)
    {
        /// <inheritdoc/>
        /// <remarks>
        /// Hand-written for the reason <see cref="FightDefinition"/>'s is: the generated equality
        /// compares <see cref="Tiles"/> by reference, so two identical steps parsed from the same text
        /// would come back unequal and the writer round-trip could never be asserted.
        /// </remarks>
        public bool Equals(SluiceStep? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (Gate != other.Gate || Tiles.Count != other.Tiles.Count)
            {
                return false;
            }

            for (int i = 0; i < Tiles.Count; i++)
            {
                if (Tiles[i] != other.Tiles[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Gate.GetHashCode();
                foreach (var tile in Tiles)
                {
                    hash = (hash * 31) + tile.GetHashCode();
                }

                return hash;
            }
        }
    }
}
