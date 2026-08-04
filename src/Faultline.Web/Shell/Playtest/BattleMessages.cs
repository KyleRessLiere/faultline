using System.Collections.Generic;

namespace Faultline.Web.Shell.Playtest;

/// <summary>
/// Which system messages are true of the battle screen right now.
/// </summary>
/// <remarks>
/// <para>
/// Every one of these used to be a <c>&lt;p class="banner"&gt;</c> above the board, and every one of
/// them was a row the board paid for. They are computed here rather than in the markup for the
/// reason everything else on this screen is: this project renders no components in its tests, so a
/// decision that lives only in a <c>.razor</c> file is a decision nothing can assert.
/// </para>
/// <para>
/// Nothing here is a rule. Each is a plain reading of state the session already publishes.
/// </para>
/// </remarks>
public static class BattleMessages
{
    /// <summary>The key of the mid-run reload notice — D-050's behaviour, said out loud.</summary>
    public const string ReloadKey = "run.reloaded";

    /// <summary>The key of the "Core refused that" notice.</summary>
    public const string ProblemKey = "run.problem";

    /// <summary>The key of the frozen-board notice, shown on a board the run has already left.</summary>
    public const string FrozenKey = "run.frozen";

    /// <summary>
    /// Every message whose condition holds, in the order they should stack.
    /// </summary>
    /// <param name="session">The board.</param>
    /// <param name="runs">The run, when one owns the board.</param>
    /// <returns>The live conditions, oldest concern first.</returns>
    public static IReadOnlyList<SystemMessage> Current(GameSession session, RunSession runs)
    {
        var messages = new List<SystemMessage>();

        // A refusal first: it is the only one of the three that means something went wrong.
        if (runs.Problem is { } problem)
        {
            messages.Add(new SystemMessage(ProblemKey, problem, SystemTone.Warn));
        }

        if (session.InRun && runs.RestartedByReload)
        {
            messages.Add(new SystemMessage(
                ReloadKey,
                "Reloaded mid-run. The run came back — seed, node and everything the squad is "
                + "carrying — but the half-played board did not, so this fight restarts from deployment.",
                SystemTone.Info));
        }

        if (session.InRun && PlaytestFlow.FightIsOver(session, runs) && PlaytestFlow.ShowBoard(session, runs))
        {
            messages.Add(new SystemMessage(
                FrozenKey,
                "The fight is over and the run has already moved on. This is the board it finished "
                + "on, frozen — nothing on it takes another command.",
                SystemTone.Info));
        }

        return messages;
    }
}
