namespace Faultline.Core
{
    /// <summary>A unit dropped to zero hit points and left the board.</summary>
    /// <param name="UnitId">Unit that went down.</param>
    /// <param name="Team">Its allegiance.</param>
    /// <param name="At">Tile it was standing on.</param>
    public sealed record UnitDowned(UnitId UnitId, Team Team, Coord At) : GameEvent;
}
