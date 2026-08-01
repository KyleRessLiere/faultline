namespace Faultline.Core
{
    /// <summary>A fight has been set up and deployment is open.</summary>
    /// <param name="FightNumber">One-based index into the five-fight run.</param>
    /// <param name="Name">Display name of the fight.</param>
    public sealed record FightStarted(int FightNumber, string Name) : GameEvent;
}
