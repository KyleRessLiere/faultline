namespace Faultline.Core
{
    /// <summary>The fight's loss condition was met.</summary>
    /// <param name="FightNumber">One-based index into the run.</param>
    /// <param name="Reason">Why the fight was lost.</param>
    public sealed record FightLost(int FightNumber, string Reason) : GameEvent;
}
