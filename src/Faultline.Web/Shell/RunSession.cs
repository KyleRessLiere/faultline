using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Faultline.Core;

namespace Faultline.Web.Shell;

/// <summary>
/// Holds the current <see cref="RunState"/> and sends <see cref="RunCommand"/>s to Core.
/// </summary>
/// <remarks>
/// <para>
/// The run layer's twin of <see cref="GameSession"/>, and just as thin: it answers no questions
/// about carrying, resting, downing or voiding. Those are rules and they live in
/// <see cref="Campaign"/>. What lives here is the three things a browser adds — which run this tab
/// is playing, what Core said last so the screen can draw it, and writing the run to localStorage
/// so a reload does not cost it.
/// </para>
/// <para>
/// Combat commands reach the fight through <see cref="Play(Command)"/>, wrapped in a
/// <see cref="PlayCommand"/>, so a run has exactly one command stream. The board screen never calls
/// <see cref="Game.Apply(GameState, Command)"/> while a run owns the board.
/// </para>
/// </remarks>
public sealed class RunSession : IRunBoardDriver
{
    private readonly RunStore _store;
    private readonly GameSession _session;
    private readonly List<string> _journal = new();

    private GameState? _pushed;

    // The node a restored run was standing on when it came back out of storage, while it is still
    // standing on it. That is what lets the board say the reload sent this fight to deployment.
    private int? _restoredNode;

    /// <summary>Creates the session.</summary>
    /// <param name="store">Where the run is persisted.</param>
    /// <param name="session">The board the run's fights are played on.</param>
    public RunSession(RunStore store, GameSession session)
    {
        _store = store;
        _session = session;
    }

    /// <summary>The run in this browser, or <c>null</c> when nobody has started one.</summary>
    public RunState? State { get; private set; }

    /// <summary>True once <see cref="LoadAsync"/> has run at least once.</summary>
    public bool Loaded { get; private set; }

    /// <summary>What the last run command produced, for the screen to draw.</summary>
    public IReadOnlyList<RunEvent> LastEvents { get; private set; } = Array.Empty<RunEvent>();

    /// <summary>Every run event so far in this browser session, oldest first.</summary>
    public IReadOnlyList<string> Journal => _journal;

    /// <summary>
    /// True when the run came back out of storage inside a fight, so that fight went back to
    /// deployment (DECISIONS.md D-050). Stays true while the run is still on that node.
    /// </summary>
    public bool RestartedByReload => _restoredNode is int node && State?.NodeIndex == node;

    /// <summary>The last thing Core refused, if anything. Cleared by the next accepted command.</summary>
    public string? Problem { get; private set; }

    /// <summary>True when there is a run that has not finished.</summary>
    public bool InProgress => State is { Outcome: RunOutcome.InProgress };

    /// <summary>True while a fight is on the board and taking commands.</summary>
    public bool InFight => State is { Phase: RunPhase.InFight };

    /// <summary>The node the run is standing on, or <c>null</c>.</summary>
    public CampaignNode? CurrentNode => State?.CurrentNode;

    /// <summary>The campaign being played, or the shipped one when no run has started.</summary>
    public CampaignDefinition Definition => State?.Campaign ?? CampaignLibrary.Faultline;

    /// <summary>
    /// True when the board the session is showing is this run's and still worth drawing: the fight in
    /// progress, or the board the last command finished on — including the winning one, which the run
    /// has already left behind (<see cref="RunStepResult.FinalBoard"/>).
    /// </summary>
    public bool ShowsBoard => _pushed is not null;

    /// <summary>Reads the run back out of browser storage.</summary>
    /// <returns>A task that completes when <see cref="State"/> is current.</returns>
    public async Task LoadAsync()
    {
        if (Loaded)
        {
            return;
        }

        var save = await _store.ReadAsync();
        if (save is not null)
        {
            try
            {
                State = save.Restore();
                _restoredNode = save.WasInFight ? save.NodeIndex : (int?)null;
            }
            catch (ArgumentException ex)
            {
                // Core validates the save against the campaign. A record that fails is reported and
                // dropped: a locked screen or an unhandled exception is a worse answer than saying
                // the stored run could not be read.
                State = null;
                Problem = "The stored run could not be read: " + Sentence(ex);
            }

            _journal.Clear();
        }

        Loaded = true;
    }

    /// <summary>Throws away any current run and starts a new one on the shipped campaign.</summary>
    /// <param name="seed">Run seed.</param>
    /// <returns>A task that completes when the new run is stored.</returns>
    public async Task StartAsync(int seed)
    {
        await _store.ClearAsync();

        var result = Campaign.Start(CampaignLibrary.Faultline, seed);

        _journal.Clear();
        _pushed = null;
        _restoredNode = null;
        Problem = null;
        State = result.NewState;
        LastEvents = result.Events;
        Record(result.Events);
        Loaded = true;

        await _store.WriteAsync(result.NewState);
    }

    /// <summary>Forgets the run entirely.</summary>
    /// <returns>A task that completes when storage is clear.</returns>
    public async Task AbandonAsync()
    {
        await _store.ClearAsync();

        State = null;
        LastEvents = Array.Empty<RunEvent>();
        _restoredNode = null;
        Problem = null;
        _pushed = null;
        _journal.Clear();
        _session.DetachRun();
    }

    /// <summary>Enters the node the run is standing on: begins its fight, or takes its rest.</summary>
    public void Enter() => Apply(new EnterNodeCommand());

    /// <inheritdoc/>
    public void Play(Command command) => Apply(new PlayCommand(command));

    /// <summary>
    /// Puts the run's fight back on the board after a navigation or a reload. A run standing on a
    /// fight node it has not entered enters it, which is what restarts the fight from deployment.
    /// </summary>
    /// <returns>True when the board now belongs to the run.</returns>
    public bool ResumeBoard()
    {
        var state = State;
        if (state is null || state.Outcome != RunOutcome.InProgress)
        {
            return false;
        }

        if (state.Phase == RunPhase.InFight && state.Fight is not null)
        {
            // Re-attach when the board has wandered off — a one-off battle from the picker takes it
            // away, and coming back to the run must not leave the player looking at that board.
            if (!_session.InRun || !ReferenceEquals(_pushed, state.Fight))
            {
                Attach(state, state.Fight, Array.Empty<GameEvent>(), Campaign.LegalRunCommands(state));
            }

            return true;
        }

        if (state.Phase == RunPhase.AtNode && state.CurrentNode is FightNode)
        {
            Enter();
            return InFight;
        }

        return false;
    }

    private void Apply(RunCommand command)
    {
        var before = State;
        if (before is null || before.Phase == RunPhase.Complete)
        {
            return;
        }

        var board = before.Fight;

        RunStepResult result;
        try
        {
            result = Campaign.ApplyRun(before, command);
        }
        catch (Exception ex) when (ex is InvalidOperationException or IllegalCommandException)
        {
            // Core is the only judge of legality; a refusal is shown rather than worked around.
            Problem = "Core refused that: " + ex.Message;
            return;
        }

        Problem = null;
        State = result.NewState;
        LastEvents = result.Events;
        Record(result.Events);

        PushBoard(command, board, result);

        // Run events fire only on run-level changes — a node entered, a fight resolved, a rest
        // taken — so an ordinary combat command costs no storage write.
        if (result.Events.Count > 0)
        {
            _ = _store.WriteAsync(result.NewState);
        }
    }

    private void PushBoard(RunCommand command, GameState? before, RunStepResult result)
    {
        // The board this command finished on, even when the run has already left it — which is what a
        // winning command does: it resolves the fight, the run advances and RunState.Fight is cleared
        // (D-055), so this is the only handle on the board the killing blow landed on.
        var board = result.FinalBoard;

        if (board is null)
        {
            // A node that touched no board at all — a rest. Nothing of this run's is on screen now.
            _pushed = null;
            return;
        }

        if (command is EnterNodeCommand)
        {
            Attach(result.NewState, board, result.FightEvents, result.LegalNext);
            return;
        }

        if (command is PlayCommand play && before is not null)
        {
            _pushed = board;
            _session.AdoptRunStep(
                play.Command, before, new StepResult(board, result.FightEvents, Unwrap(result.LegalNext)));
        }
    }

    private void Attach(
        RunState state, GameState board, IReadOnlyList<GameEvent> events, IReadOnlyList<RunCommand> legal)
    {
        _pushed = board;

        // The board's own definition, not the authored one: inside a run the roster has been trimmed
        // to the squad that can still be fielded, and the header, the notes and the combat log should
        // all say what was actually played.
        _session.AttachRun(this, board.Fight, state.Seed, new StepResult(board, events, Unwrap(legal)));
    }

    private void Record(IReadOnlyList<RunEvent> events)
    {
        foreach (var e in events)
        {
            _journal.Add(RunEventText.Describe(e));
        }
    }

    /// <summary>
    /// An exception's message with the parameter-name tail cut off. WebAssembly ships without the
    /// framework's resource strings, so <see cref="ArgumentException"/> renders that tail as the
    /// resource key itself — which must not reach a screen.
    /// </summary>
    private static string Sentence(Exception ex)
    {
        var message = ex.Message;

        foreach (var marker in new[] { "Arg_ParamName_Name", " (Parameter", "\n" })
        {
            int cut = message.IndexOf(marker, StringComparison.Ordinal);
            if (cut >= 0)
            {
                message = message.Substring(0, cut);
            }
        }

        return message.Trim();
    }

    private static IReadOnlyList<Command> Unwrap(IReadOnlyList<RunCommand> legal)
    {
        var commands = new List<Command>(legal.Count);
        foreach (var command in legal)
        {
            if (command is PlayCommand play)
            {
                commands.Add(play.Command);
            }
        }

        return commands;
    }
}
