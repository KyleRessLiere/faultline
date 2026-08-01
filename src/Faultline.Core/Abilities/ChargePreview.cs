using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// What Bull Rush would do: where the Vanguard ends up, and what happens to whatever it runs into.
    /// </summary>
    /// <param name="UnitId">The charging unit.</param>
    /// <param name="Direction">Line being charged along.</param>
    /// <param name="Path">Tiles the charger enters, in order.</param>
    /// <param name="Destination">Tile the charger stops on.</param>
    /// <param name="SelfDamage">Damage the charger takes on the way, from spikes.</param>
    /// <param name="Contact">The shove applied to the first enemy reached, when there is one.</param>
    public sealed record ChargePreview(
        UnitId UnitId,
        Direction Direction,
        IReadOnlyList<Coord> Path,
        Coord Destination,
        int SelfDamage,
        DisplacementPreview? Contact)
    {
        /// <summary>True when the charge neither moves nor connects with anything.</summary>
        public bool IsNoOp => Path.Count == 0 && Contact is null;
    }
}
