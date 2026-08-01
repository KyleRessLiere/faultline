using Faultline.Core;

namespace Faultline.Web.Shell;

/// <summary>
/// Something that owns the board a <see cref="GameSession"/> is showing, and takes its commands.
/// </summary>
/// <remarks>
/// The board screen has two callers. A one-off battle from the picker goes straight to
/// <see cref="Game.Apply(GameState, Command)"/>; a battle inside a run must go through
/// <see cref="Campaign.ApplyRun(RunState, RunCommand)"/> wrapped in a <see cref="PlayCommand"/>, so
/// there is one command stream and the run sees the fight resolve. This interface is the seam
/// between the two, and it exists so the one-off path keeps no knowledge of runs at all.
/// </remarks>
public interface IRunBoardDriver
{
    /// <summary>Routes one combat command to whatever owns the board.</summary>
    /// <param name="command">Command drawn from the session's legal list.</param>
    void Play(Command command);
}
