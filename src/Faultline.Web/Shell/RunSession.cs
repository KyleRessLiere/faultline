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

    // Every run command this browser has applied since the run was started, with what its step
    // crossed. Undo replays the run from its seed with the last entry dropped, which is exact for
    // the same reason the fight-level undo is: Core is deterministic on seed plus command log.
    private readonly List<Applied> _commands = new();

    // True only when _commands covers the run from Campaign.Start. A run read back out of storage
    // arrives as a state, not as a log, so there is nothing to replay and undo stays off.
    private bool _logFromStart;

    // Suppresses storage writes while a replay walks the log back up to where it was.
    private bool _replaying;

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

    /// <summary>
    /// Raised whenever the run changed. The board screen's panels are siblings, not a chain, so a
    /// change has to be announced rather than fall out of whose button was pressed.
    /// </summary>
    public event Action? Changed;

    /// <summary>The run in this browser, or <c>null</c> when nobody has started one.</summary>
    public RunState? State { get; private set; }

    /// <summary>True once <see cref="LoadAsync"/> has run at least once.</summary>
    public bool Loaded { get; private set; }

    /// <summary>What the last run command produced, for the screen to draw.</summary>
    public IReadOnlyList<RunEvent> LastEvents { get; private set; } = Array.Empty<RunEvent>();

    /// <summary>Every run event so far in this browser session, oldest first.</summary>
    public IReadOnlyList<string> Journal => _journal;

    /// <summary>
    /// How the last fight ended, until the next one begins — or <c>null</c> before the first.
    /// </summary>
    /// <remarks>
    /// Remembered rather than read off <see cref="LastEvents"/>, because a won fight is no longer the
    /// last thing that happened: the Camp stands between it and the next node (MASTER_DESIGN §8.5),
    /// so a band that asked only the last command would stop reporting the fight the moment the cards
    /// were dealt. Cleared when a fight begins, which is the moment the summary stops being true.
    /// </remarks>
    public FightResolved? LastResolution { get; private set; }

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
    /// The act map this run is walking, or <c>null</c> when it is walking the linear ten. Which of
    /// the two a run is on is the campaign id it was started with, and nothing else.
    /// </summary>
    public ActMap? Map => State?.Map;

    /// <summary>True when the run is standing at a fork with more than one door out.</summary>
    public bool AtVote => State is { Phase: RunPhase.AtVote };

    /// <summary>True when the node the run is on is asking it a question — a campfire, an event.</summary>
    public bool AtChoice => State is { Phase: RunPhase.AtChoice };

    /// <summary>True when the run is at a Camp with both players' cards on the table.</summary>
    public bool AtCamp => State is { Phase: RunPhase.AtCamp };

    /// <summary>
    /// The camp's two-per-player draw, or <c>null</c> when the run is not at one. Dealt by Core from
    /// the run RNG, never by this session.
    /// </summary>
    public CampTable? Camp => AtCamp ? Faultline.Core.Camp.Draw(State!) : null;

    /// <summary>Everything legal from here, straight from Core.</summary>
    public IReadOnlyList<RunCommand> Legal =>
        State is null ? Array.Empty<RunCommand>() : Campaign.LegalRunCommands(State);

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
            _commands.Clear();
            _logFromStart = false;
            LastResolution = null;
        }

        Loaded = true;
        Changed?.Invoke();
    }

    /// <summary>Throws away any current run and starts a new one.</summary>
    /// <remarks>
    /// <paramref name="campaignId"/> is the flag, and the only one: the linear ten and Act 1's lane
    /// graph are both shipped, both in <see cref="CampaignLibrary.All"/>, and which a run walks is
    /// which id it was started with. It defaults to the linear ten because that is the sequence the
    /// playtests are tuned against (MASTER_DESIGN §8).
    /// </remarks>
    /// <param name="seed">Run seed.</param>
    /// <param name="campaignId">Which campaign to walk.</param>
    /// <returns>A task that completes when the new run is stored.</returns>
    public async Task StartAsync(int seed, string? campaignId = null)
    {
        await _store.ClearAsync();

        var definition = CampaignLibrary.ById(campaignId ?? CampaignLibrary.FaultlineId);
        var result = Campaign.Start(definition, seed);

        _journal.Clear();
        _commands.Clear();
        _logFromStart = true;
        LastResolution = null;
        _pushed = null;
        _restoredNode = null;
        Problem = null;
        State = result.NewState;
        LastEvents = result.Events;
        Record(result.Events);
        Loaded = true;

        await _store.WriteAsync(result.NewState);
        Changed?.Invoke();
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
        _commands.Clear();
        _logFromStart = false;
        LastResolution = null;
        _session.DetachRun();
        Changed?.Invoke();
    }

    /// <summary>Enters the node the run is standing on: begins its fight, or takes its rest.</summary>
    public void Enter() => Apply(new EnterNodeCommand());

    /// <summary>
    /// Sends both blind picks to Core as one command, which reveals them and settles the fork.
    /// </summary>
    /// <remarks>
    /// Both picks travel together because there is no half-voted state to send one into, and a fork
    /// takes exactly one of these. Whether the picks match, and which way a split falls, is Core's —
    /// the coin is the run RNG's only draw.
    /// </remarks>
    /// <param name="choiceA">The door Player A picked.</param>
    /// <param name="choiceB">The door Player B picked.</param>
    public void Vote(string choiceA, string choiceB) => Apply(new VoteCommand(choiceA, choiceB));

    /// <summary>Spends the campfire on healing — the only thing a v1 campfire can be spent on.</summary>
    public void Heal() => Apply(new RestHealCommand());

    /// <summary>
    /// Sends the camp pick to Core, which applies it and moves the run on.
    /// </summary>
    /// <remarks>
    /// The table is Core's, redealt from the run RNG, so this session cannot hand the squad a card the
    /// seed did not deal. One pick, not two: §8.6's director deals one table across the whole squad
    /// and the flock takes one card off it (D-154).
    /// </remarks>
    /// <param name="pick">Index of the card taken, or -1 when the camp dealt none.</param>
    public void PickCamp(int pick)
    {
        if (Camp is not { } table)
        {
            return;
        }

        Apply(new CampPickCommand(table, pick));
    }

    /// <summary>
    /// Takes an event's offer, with one named duck paying for it.
    /// </summary>
    /// <remarks>
    /// The payer is named because bodily consent is per duck (MASTER_DESIGN §8.5): the vote governs
    /// where the run goes and never what a duck pays, so there is no command that accepts on the
    /// party's behalf and this screen must have asked that duck's owner.
    /// </remarks>
    /// <param name="payer">The duck that pays.</param>
    public void PayEvent(RunUnitId payer) => Apply(new EventPayCommand(payer));

    /// <summary>Walks away from an Offer. Never legal at a Strait, and Core is the judge of which.</summary>
    public void WalkAwayFromEvent() => Apply(new EventWalkAwayCommand());

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

    /// <summary>
    /// Whether the last run command can be taken back. False for a run that came back out of
    /// storage: a save is a state, not a command log, so there is nothing to replay from and
    /// pretending otherwise would land on a different board.
    /// </summary>
    public bool CanUndo => _logFromStart && State is not null && Blocked == GameSession.UndoBlock.None;

    /// <summary>Why undo is refused right now, wordlessly.</summary>
    public GameSession.UndoBlock Blocked
    {
        get
        {
            if (_commands.Count == 0)
            {
                return GameSession.UndoBlock.Nothing;
            }

            var last = _commands[_commands.Count - 1];
            return GameSession.BlockOn(
                last.Chosen,
                last.DrewFromTheSeed,
                last.TurnedTheRound,
                last.Command is PlayCommand { Command: EndActivationCommand },
                last.ClosedTheActivation,
                last.ActorTeam,
                _session.State?.ActiveTeam ?? last.ActorTeam);
        }
    }

    /// <summary>
    /// Why undo is unavailable, for the button's tooltip, or <c>null</c> when it is available.
    /// Non-null exactly when <see cref="CanUndo"/> is false.
    /// </summary>
    public string? UndoBlockedReason =>
        CanUndo ? null
        : State is null ? "There is no run to rewind."
        : !_logFromStart
            ? "This run came back out of browser storage as a saved position, not as a command log, "
              + "so there is nothing to replay from. Undo works again on a run started in this tab."
            : Blocked == GameSession.UndoBlock.Nothing
                ? "Nothing has been played on this run yet."
                : GameSession.UndoWords(Blocked, _commands[_commands.Count - 1].ActorTeam);

    /// <summary>
    /// What one press would take back — "undo move segment to D4". Non-empty exactly when
    /// <see cref="CanUndo"/> is true.
    /// </summary>
    public string UndoDescription
    {
        get
        {
            if (!CanUndo)
            {
                return string.Empty;
            }

            var last = _commands[_commands.Count - 1].Command;
            return last is PlayCommand play
                ? GameSession.DescribeUndo(_session.State, play.Command)
                : "undo entering " + (CurrentNode?.Describe() ?? "this node");
        }
    }

    /// <summary>
    /// Takes back exactly one run command — the last one — by replaying the run from its seed with
    /// that one entry dropped.
    /// </summary>
    /// <returns>Whether anything was undone.</returns>
    public bool Undo()
    {
        if (!CanUndo)
        {
            return false;
        }

        var campaign = State!.Campaign;
        int seed = State.Seed;
        var kept = _commands.GetRange(0, _commands.Count - 1);

        _commands.Clear();
        _journal.Clear();
        _pushed = null;
        Problem = null;
        LastResolution = null;

        var start = Campaign.Start(campaign, seed);
        State = start.NewState;
        LastEvents = start.Events;
        Record(start.Events);

        _replaying = true;
        try
        {
            foreach (var entry in kept)
            {
                Apply(entry.Command, entry.Chosen);
            }
        }
        finally
        {
            _replaying = false;
        }

        _ = _store.WriteAsync(State!);
        Changed?.Invoke();
        return true;
    }

    /// <summary>One applied run command, and what the undo rule needs to know about its step.</summary>
    private readonly record struct Applied(
        RunCommand Command,
        bool Chosen,
        Team ActorTeam,
        bool ClosedTheActivation,
        bool TurnedTheRound,
        bool DrewFromTheSeed);

    private void Apply(RunCommand command) => Apply(command, !_session.SubmittingEnemyCommand);

    private void Apply(RunCommand command, bool chosen)
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
            Changed?.Invoke();
            return;
        }

        Problem = null;
        State = result.NewState;
        LastEvents = result.Events;
        Record(result.Events);
        _commands.Add(Record(command, chosen, board, result));

        PushBoard(command, board, result);

        // Run events fire only on run-level changes — a node entered, a fight resolved, a rest
        // taken — so an ordinary combat command costs no storage write.
        if (result.Events.Count > 0 && !_replaying)
        {
            _ = _store.WriteAsync(result.NewState);
        }

        Changed?.Invoke();
    }

    // The same reading of a step the fight-level undo makes, over the board the run unwrapped.
    private static Applied Record(RunCommand command, bool chosen, GameState? before, RunStepResult result)
    {
        bool closed = false;
        bool turned = false;

        foreach (var evt in result.FightEvents)
        {
            if (evt is ActivationEnded)
            {
                closed = true;
            }
            else if (evt is RoundEnded)
            {
                turned = true;
            }
        }

        var team = before is null ? Team.PlayerA : before.ActiveTeam;
        if (command is PlayCommand play && before is not null)
        {
            team = TeamOf(before, play.Command);
        }

        bool drew = before is not null
            && result.FinalBoard is not null
            && before.RngState != result.FinalBoard.RngState;

        return new Applied(command, chosen, team, closed, turned, drew);
    }

    private static Team TeamOf(GameState before, Command command)
    {
        var id = ActorOf(command);
        return id != UnitId.None && before.FindUnit(id) is { } unit ? unit.Team : before.ActiveTeam;
    }

    private static UnitId ActorOf(Command command) => command switch
    {
        DeployCommand c => c.UnitId,
        MoveCommand c => c.UnitId,
        AttackCommand c => c.UnitId,
        AbilityCommand c => c.UnitId,
        RescueCommand c => c.UnitId,
        FinishClingingCommand c => c.UnitId,
        SpendVerveCommand c => c.UnitId,
        EndActivationCommand c => c.UnitId,
        _ => UnitId.None,
    };

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
                play.Command,
                before,
                new StepResult(board, result.FightEvents, Unwrap(result.LegalNext)),
                _replaying);
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

            if (e is FightResolved resolved)
            {
                LastResolution = resolved;
            }
            else if (e is FightBegan)
            {
                LastResolution = null;
            }
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
