namespace Faultline.Core
{
    /// <summary>
    /// Base type for every input Core accepts. Brief §1: the whole game is
    /// <see cref="Game.Apply(GameState, Command)"/>, so the ordered list of commands plus the seed
    /// is a complete recording of a fight.
    /// </summary>
    public abstract record Command
    {
    }
}
