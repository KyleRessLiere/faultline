namespace Faultline.Core
{
    /// <summary>
    /// A unit lost Footing: <b>spent</b>, to refuse a whole displacement instance or a Cast, or
    /// <b>stripped</b> — by a collision it suffered, by ending a round beside a drain, or by a Cast
    /// overwhelming its last token (D-143).
    /// </summary>
    /// <param name="UnitId">Unit that braced, or lost its grip on a token.</param>
    /// <param name="Remaining">Footing left this fight.</param>
    public sealed record FootingSpent(UnitId UnitId, int Remaining) : GameEvent;
}
