using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// The fight's win condition, published at setup so the players see what they are being asked
    /// for before they place a single unit.
    /// </summary>
    /// <param name="Kind">Which objective this fight uses.</param>
    /// <param name="Rounds">The objective's own deadline round, or zero when it has none.</param>
    /// <param name="Hp">Structure hit points, or zero when the objective has no structure.</param>
    /// <param name="TurnLimit">The fight's round cap, or zero when it has none.</param>
    /// <param name="Tiles">Tiles the objective names: ground to hold, tiles to reach, where a structure stands.</param>
    public sealed record ObjectiveDeclared(
        ObjectiveKind Kind,
        int Rounds,
        int Hp,
        int TurnLimit,
        IReadOnlyList<Coord> Tiles) : GameEvent;
}
