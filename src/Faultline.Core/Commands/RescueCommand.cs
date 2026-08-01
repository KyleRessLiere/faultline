namespace Faultline.Core
{
    /// <summary>
    /// Pulls an adjacent clinging ally out of a pit. Brief §2: this costs the rescuer its entire
    /// activation, both halves.
    /// </summary>
    /// <param name="UnitId">Unit spending its activation.</param>
    /// <param name="ClingingId">Clinging ally to pull out.</param>
    public sealed record RescueCommand(UnitId UnitId, UnitId ClingingId) : Command;
}
