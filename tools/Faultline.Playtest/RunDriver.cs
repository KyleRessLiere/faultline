using Faultline.Core;

namespace Faultline.Playtest;

/// <summary>
/// Walks a campaign run forward, stopping wherever a player has to decide something.
/// </summary>
/// <remarks>
/// <para>
/// One loop, three callers. A policy run, a hand-played session and a replay all have to advance the
/// run identically or the log stops being a log — a replay that entered nodes in a different order
/// from the recording would reproduce a different fight and nobody would notice until the numbers
/// disagreed. So the loop lives here once and the callers differ only in who answers
/// <see cref="Legal"/>.
/// </para>
/// <para>
/// Everything that is not a player decision — entering a node, every enemy command — is applied by
/// <see cref="AutoAdvance"/>, because all of it is a function of the state. That is what lets a run
/// log record player decisions alone.
/// </para>
/// </remarks>
public sealed class RunDriver
{
    private readonly List<GameEvent> _fightEvents = new();
    private readonly List<RunEvent> _runEvents = new();

    private RunDriver(RunState run) => Run = run;

    /// <summary>The run as it stands.</summary>
    public RunState Run { get; private set; }

    /// <summary>Commands applied so far, of every kind. The safety stop counts these.</summary>
    public int Applied { get; private set; }

    /// <summary>Player decisions applied so far.</summary>
    public int Decisions { get; private set; }

    /// <summary>Why the driver stopped, when it stopped for a reason other than a decision.</summary>
    public string Reason { get; private set; } = "in progress";

    /// <summary>Fight events since the last <see cref="ClearEvents"/>.</summary>
    public IReadOnlyList<GameEvent> FightEvents => _fightEvents;

    /// <summary>Run events since the last <see cref="ClearEvents"/>.</summary>
    public IReadOnlyList<RunEvent> RunEvents => _runEvents;

    /// <summary>The last board the run stepped through, for reading unit sides off events.</summary>
    public GameState? LastBoard { get; private set; }

    /// <summary>Whether the run is over, one way or the other.</summary>
    public bool Complete => Run.Phase == RunPhase.Complete;

    /// <summary>Whether the driver is parked on a player decision.</summary>
    public bool AtDecision { get; private set; }

    /// <summary>What the player may choose right now. Empty unless <see cref="AtDecision"/>.</summary>
    public IReadOnlyList<Command> Legal { get; private set; } = Array.Empty<Command>();

    /// <summary>Starts a run.</summary>
    /// <param name="campaign">Campaign to play.</param>
    /// <param name="seed">Run seed.</param>
    /// <returns>A driver parked on the first decision.</returns>
    public static RunDriver Start(CampaignDefinition campaign, int seed)
    {
        var driver = new RunDriver(Campaign.Start(campaign, seed).NewState);
        driver.AutoAdvance();
        return driver;
    }

    /// <summary>Drops the accumulated events, so the next step's events stand alone.</summary>
    public void ClearEvents()
    {
        _fightEvents.Clear();
        _runEvents.Clear();
    }

    /// <summary>
    /// Applies one player decision, then advances to the next one.
    /// </summary>
    /// <param name="command">A command from <see cref="Legal"/>.</param>
    /// <exception cref="InvalidOperationException">The driver is not on a decision.</exception>
    public void Decide(Command command)
    {
        if (!AtDecision)
        {
            throw new InvalidOperationException("The run is not waiting on a player decision.");
        }

        Apply(new PlayCommand(command));
        Decisions++;
        AutoAdvance();
    }

    /// <summary>
    /// Replays a recorded sequence of decisions, then advances to the next open decision.
    /// </summary>
    /// <param name="decisions">Decisions in the order they were made.</param>
    /// <returns>How many were consumed before the run ended.</returns>
    /// <exception cref="InvalidOperationException">
    /// A decision names a unit of a different class from the one recorded, which means the log was
    /// recorded against different content and every id in it has shifted.
    /// </exception>
    public int Replay(IReadOnlyList<RunLog.Decision> decisions)
    {
        int i = 0;
        for (; i < decisions.Count && AtDecision; i++)
        {
            var decision = decisions[i];
            var actorId = RunLog.ActorOf(decision.Command);
            var actor = Run.Fight?.FindUnit(actorId);

            if (actor is not null && actor.Kind != decision.Actor)
            {
                throw new InvalidOperationException(
                    $"Decision {i + 1} was recorded as a {decision.Actor} but {actorId} is a "
                    + $"{actor.Kind} in this build. The log was recorded against different content — "
                    + "a roster edit renumbers every unit — so it cannot be replayed here.");
            }

            Decide(decision.Command);
        }

        return i;
    }

    /// <summary>
    /// Applies everything that is not a player decision, and parks on the first one that is.
    /// </summary>
    /// <param name="maxCommands">Safety stop, counted across the driver's whole life.</param>
    public void AutoAdvance(int maxCommands = 200000)
    {
        AtDecision = false;
        Legal = Array.Empty<Command>();

        while (!Complete)
        {
            if (Applied >= maxCommands)
            {
                Reason = "command budget exhausted";
                return;
            }

            if (Run.Fight is { Round: > RunHarness.StallRound })
            {
                Reason = $"stalled: {Run.Fight.Fight.Id} reached round {Run.Fight.Round} "
                    + "with neither side able to finish it";
                return;
            }

            if (Run.Phase == RunPhase.AtNode)
            {
                Apply(new EnterNodeCommand());
                continue;
            }

            // The enemy is Core's own planner; a player never chooses for it.
            var enemy = Game.NextEnemyCommand(Run.Fight!);
            if (enemy is not null)
            {
                Apply(new PlayCommand(enemy));
                continue;
            }

            var legal = Game.LegalCommands(Run.Fight!);
            if (legal.Count == 0)
            {
                Reason = "no legal command on node " + Run.NodeIndex;
                return;
            }

            AtDecision = true;
            Legal = legal;
            return;
        }

        Reason = Run.Outcome == RunOutcome.Won ? "campaign cleared" : Reason;
    }

    private void Apply(RunCommand command)
    {
        var step = Campaign.ApplyRun(Run, command);
        Applied++;

        LastBoard = step.FinalBoard ?? Run.Fight ?? LastBoard;
        _fightEvents.AddRange(step.FightEvents);
        _runEvents.AddRange(step.Events);

        foreach (var e in step.Events)
        {
            if (e is RunLost lost)
            {
                Reason = lost.Reason;
            }
        }

        Run = step.NewState;
    }
}
