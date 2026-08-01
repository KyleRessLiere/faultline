namespace Faultline.Core
{
    /// <summary>A unit was placed on the board during deployment.</summary>
    /// <param name="UnitId">Unit placed.</param>
    /// <param name="Team">Its allegiance.</param>
    /// <param name="Kind">Its archetype.</param>
    /// <param name="At">Tile it was placed on.</param>
    public sealed record UnitDeployed(UnitId UnitId, Team Team, UnitKind Kind, Coord At) : GameEvent;
}
