using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// Everything one <see cref="Game.Apply(GameState, Command)"/> call produces. Brief §1: the
    /// renderer holds the new state, animates the events in order, and offers the legal next commands.
    /// </summary>
    /// <param name="NewState">State after the command resolved.</param>
    /// <param name="Events">What happened, in resolution order.</param>
    /// <param name="LegalNext">Commands that are legal against <paramref name="NewState"/>.</param>
    public sealed record StepResult(
        GameState NewState,
        IReadOnlyList<GameEvent> Events,
        IReadOnlyList<Command> LegalNext);
}
