namespace Faultline.Core
{
    /// <summary>
    /// A clinging unit went into the pit for good. Brief §2: permanently dead for the run — unlike
    /// being downed, this is not undone between fights.
    /// </summary>
    /// <param name="UnitId">Unit lost.</param>
    /// <param name="Team">Its allegiance.</param>
    /// <param name="At">The pit it fell into.</param>
    /// <param name="Reason">What finished it.</param>
    public sealed record Voided(UnitId UnitId, Team Team, Coord At, string Reason) : GameEvent;
}
