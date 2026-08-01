namespace Faultline.Core
{
    /// <summary>A new round began. Enemy intents are declared immediately after this (M3).</summary>
    /// <param name="Round">One-based round number.</param>
    public sealed record RoundStarted(int Round) : GameEvent;
}
