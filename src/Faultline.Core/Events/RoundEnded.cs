namespace Faultline.Core
{
    /// <summary>Every unit has activated. Collapse clock, Clinging resolution and Stagger clear here.</summary>
    /// <param name="Round">The round that just finished.</param>
    public sealed record RoundEnded(int Round) : GameEvent;
}
