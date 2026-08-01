namespace Faultline.Core
{
    /// <summary>The fight's win condition was met.</summary>
    /// <param name="FightNumber">One-based index into the run.</param>
    public sealed record FightWon(int FightNumber) : GameEvent;
}
