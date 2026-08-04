using System;
using System.Collections.Generic;
using System.Linq;
using Faultline.Core;
using Faultline.Web.Shell.Playtest;

namespace Faultline.Web.Shell;

/// <summary>
/// Holds the current <see cref="GameState"/> and the action the player is aiming.
/// </summary>
/// <remarks>
/// This type answers no rules questions of its own. Legality comes from the
/// <see cref="StepResult.LegalNext"/> list Core hands back, and every preview comes from a Core
/// query — <see cref="Displacement.Preview"/>, <see cref="Abilities.PreviewCharge"/> and friends.
/// CLAUDE.md: duplicated rule logic in the shell is a bug.
/// </remarks>
public sealed class GameSession
{
    private readonly List<string> _log = new();

    // Every command applied to this board since it started, with what its step crossed. Undo is
    // built on Core's determinism guarantee: seed + command log replays to the same state, so
    // dropping the tail of the log and replaying is an exact rewind rather than an approximation.
    private readonly List<Applied> _applied = new();

    // True while Core's planner is having its own command submitted, so the command log can tell an
    // enemy activation from a player's decision. Undo rewinds to before a decision, never to the
    // middle of an enemy turn the screen would immediately play forward again.
    private bool _submittingEnemy;

    // True while a rewind is replaying the command log. The board is being rebuilt, not played, so
    // the steps it produces are not offered to anything that would put them on screen one at a time.
    private bool _silent;

    // docs/COMBAT_LOG.md: the flag belongs in the shell, not in Core. Core always emits its events;
    // it has no idea whether anyone is writing them down. Null means "not recording, retain nothing".
    private CombatRecorder? _recorder;

    // Where finished fights are written. Null in a session nobody wired one to, which is every test
    // that only cares about the board.
    private readonly SessionLog? _sink;

    // Position of the current fight in this sitting, so the folder sorts in play order rather than
    // alphabetically. Counts fights that were started, not fights that were won.
    private int _fightNumber;

    // Recording is on unless somebody turned it off, and that decision has to survive a reload:
    // switching it off is what you do to bug-hunt, and a bug hunt outlives one page load.
    private bool _recordingWanted = true;

    /// <summary>Creates a session already sitting on a fresh fight.</summary>
    /// <param name="sink">
    /// Folder to write finished fight logs into, when the player has pointed at one. Optional, so a
    /// session can be built with nothing but a board.
    /// </param>
    public GameSession(SessionLog? sink = null)
    {
        _sink = sink;
        StartFight(FightLibrary.Fight1(), DefaultSeed);

        // The constructor's fight is a placeholder, not a choice. Saying so lets a page restore a
        // campaign run after a reload without ever stepping on a battle the player actually picked.
        Untouched = true;
    }

    /// <summary>Seed used when the shell starts with no explicit choice.</summary>
    public const int DefaultSeed = 1;

    /// <summary>
    /// True while the loaded fight is the one the constructor picked and nobody has chosen or played
    /// anything since — the state a fresh page load leaves the session in.
    /// </summary>
    public bool Untouched { get; private set; }

    // Set while a run owns the board. Non-null means every command leaves through the run layer
    // instead of Game.Apply, so the run sees its own fight resolve.
    private IRunBoardDriver? _run;

    /// <summary>
    /// True when the board belongs to a campaign run, so commands travel through
    /// <see cref="Campaign.ApplyRun(RunState, RunCommand)"/> rather than straight to Core's fight
    /// layer, and a win advances the run.
    /// </summary>
    public bool InRun => _run is not null;

    /// <summary>Current state.</summary>
    public GameState State { get; private set; } = null!;

    /// <summary>Commands Core says are legal right now.</summary>
    public IReadOnlyList<Command> Legal { get; private set; } = Array.Empty<Command>();

    /// <summary>Seed the current fight was started from.</summary>
    public int Seed { get; private set; }

    /// <summary>
    /// Raised whenever anything a screen draws has changed. The shell is a store, not a component
    /// tree: without this, a panel only redraws when its own button was the one pressed.
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// Raised once per step with the events Core emitted for it, and whether the command was the
    /// enemy planner's rather than a person's. This is the renderer's script — CLAUDE.md: receive a
    /// StepResult, animate its events in order, then render the new state.
    /// </summary>
    /// <remarks>
    /// Silent while a rewind replays the command log: those steps rebuild a position rather than
    /// play one, and nothing subscribed here is required for the board to be correct.
    /// </remarks>
    public event Action<IReadOnlyList<GameEvent>, bool>? Stepped;

    /// <summary>Human-readable event history, newest last.</summary>
    public IReadOnlyList<string> Log => _log;

    /// <summary>
    /// Empties the on-screen transcript. The recording, if one is running, is untouched: this
    /// clears what is being read, not what is being kept.
    /// </summary>
    public void ClearLog()
    {
        _log.Clear();
        Changed?.Invoke();
    }

    /// <summary>
    /// Whether the full combat log is being recorded. <b>On by default.</b>
    /// </summary>
    /// <remarks>
    /// It used to be off, on the grounds that memory grows with the fight and a player who is not
    /// analysing anything should not pay for it. That had the trade backwards: the fights worth
    /// analysing are the ones nobody expected to be interesting, and a log you have to remember to
    /// switch on before the interesting thing happens is a log you do not have. A few hundred lines
    /// of text is not a cost worth losing evidence over.
    ///
    /// Switching it <em>off</em> is now the deliberate act, and it is what you do to bug-hunt: no
    /// recorder, no writes to disk, nothing on the board but the board.
    /// </remarks>
    public bool Recording => _recorder is not null;

    /// <summary>Event-log lines recorded so far; zero when recording is off.</summary>
    public int RecordedLineCount => _recorder?.LineCount ?? 0;

    /// <summary>
    /// True when recording covers the whole fight. False after switching it on mid-fight, which
    /// leaves the command log unable to replay from the seed.
    /// </summary>
    public bool RecordingIsComplete => _recorder?.FromFightStart ?? false;

    /// <summary>Suggested filename for an export, from the fight's slug.</summary>
    public string LogFileName => (_recorder ?? new CombatRecorder(Fight, Seed)).FileName;

    /// <summary>Unit the player is currently looking at, if any.</summary>
    public UnitId? Selected { get; private set; }

    /// <summary>Which action the player is aiming.</summary>
    public ActionMode Mode { get; private set; } = ActionMode.Move;

    /// <summary>
    /// Which ability is armed while <see cref="Mode"/> is <see cref="ActionMode.Ability"/>.
    /// </summary>
    /// <remarks>
    /// A unit may bring more than one ability, so "aiming the ability" is not a mode on its own — it
    /// is a mode plus a choice. The choice is carried here rather than by growing
    /// <see cref="ActionMode"/> one value per ability, because <see cref="ActionMode"/> enumerates
    /// kinds of action the shell knows how to aim, and abilities are content Core owns: a mode per
    /// ability would mean the shell's enum had to be edited every time Core gained one.
    /// </remarks>
    public Ability? ArmedAbility { get; private set; }

    /// <summary>Tile the pointer is over, if any.</summary>
    public Coord? Hovered { get; private set; }

    /// <summary>The selected unit, resolved.</summary>
    public Unit? SelectedUnit => Selected is null ? null : State.FindUnit(Selected.Value);

    /// <summary>Every ability the selected unit brings, in the order Core offers them.</summary>
    public IReadOnlyList<AbilityDescriptor> SelectedAbilities =>
        SelectedUnit is null
            ? Array.Empty<AbilityDescriptor>()
            : AbilityDescriptor.AllForKind(SelectedUnit.Kind);

    /// <summary>The armed ability's descriptor, or <c>null</c> when no ability is armed.</summary>
    public AbilityDescriptor? ArmedDescriptor =>
        ArmedAbility is null ? null : AbilityDescriptor.For(ArmedAbility.Value);

    /// <summary>
    /// The rules text to show beside the action row: the armed ability's, or the unit's first when
    /// nothing is armed yet.
    /// </summary>
    public AbilityDescriptor? SelectedAbility =>
        ArmedDescriptor ?? (SelectedAbilities.Count > 0 ? SelectedAbilities[0] : null);

    /// <summary>Enemy the player has opened a dossier on, if any.</summary>
    /// <remarks>
    /// Inspection is a view, not a command: it aims nothing, submits nothing and changes no state
    /// Core can see. It lives here rather than on the page only so it survives a re-render and can
    /// be dropped when the unit it points at leaves the board.
    /// </remarks>
    public UnitId? Inspected { get; private set; }

    /// <summary>The inspected unit, resolved.</summary>
    public Unit? InspectedUnit => Inspected is null ? null : State.FindUnit(Inspected.Value);

    /// <summary>
    /// Tile the player has opened the inspector on, when they clicked ground rather than a body.
    /// </summary>
    /// <remarks>
    /// A view, like <see cref="Inspected"/>: what a drain does to whoever is shoved onto it is a rule
    /// a player has to be able to read <em>before</em> aiming at it, and the alternative to a panel
    /// that says so is a tooltip nobody hovers in time.
    /// </remarks>
    public Coord? InspectedTile { get; private set; }

    /// <summary>Reads a tile in the inspector. Never a command.</summary>
    /// <param name="tile">Tile to read.</param>
    public void InspectTile(Coord tile)
    {
        if (State.Board.InBounds(tile))
        {
            Inspected = null;
            InspectedTile = tile;
        }

        Changed?.Invoke();
    }

    /// <summary>
    /// The enemy whose declared plan is being singled out on the board, from hovering its card in
    /// the activation strip.
    /// </summary>
    /// <remarks>
    /// Every intent is drawn at once and faintly; this one is drawn at full opacity. Painting them
    /// all loudly is the same mistake the threat union made (D-089) — a board covered in red arrows
    /// says only "something is coming", which is not a fact anybody can plan against.
    /// </remarks>
    public UnitId? Telegraphed { get; private set; }

    /// <summary>Singles out one enemy's telegraph, or drops the singling out.</summary>
    /// <param name="unitId">Enemy to highlight, or null for none.</param>
    public void Telegraph(UnitId? unitId)
    {
        if (Telegraphed.Equals(unitId))
        {
            return;
        }

        Telegraphed = unitId;
        Changed?.Invoke();
    }

    /// <summary>Core's description of the inspected unit's archetype.</summary>
    public EnemyBehaviour? InspectedBehaviour =>
        InspectedUnit is null ? null : EnemyBehaviour.ForKind(InspectedUnit.Kind);

    /// <summary>
    /// Whether a unit can be inspected: <b>any unit on the board</b>, either side (D-103).
    /// </summary>
    /// <remarks>
    /// It used to be enemy-only, on the grounds that "player units are served by the action panel".
    /// That was true of the board and false of the activation strip, where half the portraits are
    /// yours and clicked into nothing. Inspect is universal and read-only; <see cref="Select"/> stays
    /// gated on whose slot it is, and where a clicked unit is one you may command both fire — that
    /// coincidence is not a licence to merge them.
    ///
    /// A player kind has no <see cref="EnemyBehaviour"/> and never will; the panel handles the null
    /// rather than this guard turning it into "not inspectable", which is what made the strip dead
    /// on its own side.
    /// </remarks>
    /// <param name="unit">Unit to test.</param>
    /// <returns>Whether inspecting it would show anything.</returns>
    public static bool CanInspect(Unit? unit) => unit is not null && unit.IsOnBoard;

    /// <summary>Reads a unit's character sheet, in the reference panel. Anything else is ignored.</summary>
    /// <param name="id">Unit to inspect.</param>
    public void Inspect(UnitId id)
    {
        if (CanInspect(State.FindUnit(id)))
        {
            Inspected = id;
            InspectedTile = null;
            Tab = ReferenceTab.Unit;
        }

        Changed?.Invoke();
    }

    /// <summary>Forgets whatever the inspector was reading, leaving it on its empty state.</summary>
    public void ClearInspection()
    {
        Inspected = null;
        InspectedTile = null;
        Changed?.Invoke();
    }

    /// <summary>Which reference the one reference panel is showing.</summary>
    /// <remarks>
    /// The same kind of flag as <see cref="Inspected"/>, and for the same reason: reading what a
    /// board or a unit is made of is a view, not a command. It aims nothing, submits nothing, and
    /// Core never sees it.
    /// </remarks>
    public ReferenceTab Tab { get; private set; } = ReferenceTab.Abilities;

    /// <summary>Shows a reference tab.</summary>
    /// <param name="tab">Tab to show.</param>
    public void ShowTab(ReferenceTab tab)
    {
        Tab = tab;
        Changed?.Invoke();
    }

    /// <summary>Whether the reference panel is on the design notes.</summary>
    public bool DesignOpen => Tab == ReferenceTab.Battle;

    /// <summary>
    /// Shows the design notes, or puts the panel back on the abilities it defaults to. The inspected
    /// unit is remembered either way, so the unit tab is still there to go back to.
    /// </summary>
    public void ToggleDesign() =>
        ShowTab(DesignOpen ? ReferenceTab.Abilities : ReferenceTab.Battle);

    /// <summary>Leaves the design notes, if they are what is showing.</summary>
    public void CloseDesign()
    {
        if (DesignOpen)
        {
            Tab = ReferenceTab.Abilities;
        }

        Changed?.Invoke();
    }

    /// <summary>The fight currently loaded, authored or hand-built.</summary>
    public FightDefinition Fight { get; private set; } = null!;

    /// <summary>
    /// Every authored fight, with its lints, for a picker to display. Lints are deliberately kept
    /// alongside so a deviation from the brief is visible at selection time.
    /// </summary>
    public static IReadOnlyList<FightParseResult> Library => FightLibrary.LoadAll();

    /// <summary>Restarts the current fight on a new seed.</summary>
    /// <param name="seed">Run seed.</param>
    public void NewRun(int seed) => StartFight(Fight ?? FightLibrary.Fight1(), seed);

    /// <summary>Loads a fight and starts it from the beginning, outside any campaign run.</summary>
    /// <param name="fight">Authored or hand-built fight.</param>
    /// <param name="seed">Run seed.</param>
    public void StartFight(FightDefinition fight, int seed)
    {
        _run = null;
        Reset(fight, seed);

        var start = Game.Start(fight, seed);
        _recorder?.RecordStart(start);
        Adopt(start);
    }

    /// <summary>
    /// Hands the board to a run. The opening state comes from the run rather than from
    /// <see cref="Game.Start(FightDefinition, int)"/>, because the run has already started the
    /// fight with the squad's carried hit points.
    /// </summary>
    /// <param name="driver">Who the board's commands go to from now on.</param>
    /// <param name="fight">The authored fight, for the header, the notes and the log.</param>
    /// <param name="seed">The run's seed.</param>
    /// <param name="opening">The board as the run started it.</param>
    public void AttachRun(IRunBoardDriver driver, FightDefinition fight, int seed, StepResult opening)
    {
        _run = driver;
        Reset(fight, seed);
        _recorder?.RecordStart(opening);
        Adopt(opening);
    }

    /// <summary>Gives the board back, so the next command goes straight to Core's fight layer.</summary>
    public void DetachRun()
    {
        _run = null;
        Changed?.Invoke();
    }

    /// <summary>Folds a step the run resolved on the session's behalf back into the board.</summary>
    /// <param name="command">The combat command that was played.</param>
    /// <param name="before">The board it was played against.</param>
    /// <param name="result">What came back, unwrapped from the run's result.</param>
    /// <param name="silent">
    /// True while the run is replaying its command log for a rewind, so the step is folded in
    /// without being offered to <see cref="Stepped"/>.
    /// </param>
    public void AdoptRunStep(Command command, GameState before, StepResult result, bool silent = false)
    {
        _recorder?.RecordStep(command, before, result);

        bool was = _silent;
        _silent = silent;
        try
        {
            Adopt(result);
        }
        finally
        {
            _silent = was;
        }

        Hovered = null;
        Mode = DefaultModeFor(SelectedUnit);
        ArmedAbility = null;
    }

    /// <summary>Applies a command and folds the result into the session.</summary>
    /// <param name="command">Command drawn from <see cref="Legal"/>.</param>
    public void Submit(Command command)
    {
        Untouched = false;
        ClearCast();
        ClearRescue();
        ClearPocketAim();

        if (_run is not null)
        {
            // A run's fight has one command stream and it is the run's. The driver calls back into
            // AdoptRunStep with what Core produced.
            _run.Play(command);
            return;
        }

        Play(command, !_submittingEnemy);
    }

    /// <summary>
    /// True while <see cref="ResolveEnemyActivation"/> is submitting Core's planned command, so the
    /// run layer can log it as the planner's rather than a player's.
    /// </summary>
    public bool SubmittingEnemyCommand => _submittingEnemy;

    /// <summary>
    /// Whether the last command can be taken back: it was this player's own, it is still inside the
    /// activation they are holding, and nothing has resolved on top of it. Always false inside a
    /// run — there, the run owns the command stream and <see cref="RunSession.CanUndo"/> answers.
    /// </summary>
    public bool CanUndo => Blocked == UndoBlock.None;

    /// <summary>
    /// Why the last command cannot be taken back, in the player's words, or <c>null</c> when it can.
    /// Non-null exactly when <see cref="CanUndo"/> is false.
    /// </summary>
    public string? UndoBlockedReason
    {
        get
        {
            var block = Blocked;
            return block == UndoBlock.None
                ? null
                : UndoWords(block, _applied.Count == 0 ? Team.PlayerA : _applied[_applied.Count - 1].ActorTeam);
        }
    }

    /// <summary>
    /// What one press would take back — "undo move segment to D4". Non-empty exactly when
    /// <see cref="CanUndo"/> is true, because a button that names nothing has nothing to promise.
    /// </summary>
    public string UndoDescription =>
        CanUndo ? DescribeUndo(State, _applied[_applied.Count - 1].Command) : string.Empty;

    /// <summary>Why undo is refused right now, wordlessly. <see cref="UndoBlock.None"/> means it is not.</summary>
    public UndoBlock Blocked
    {
        get
        {
            if (_run is not null)
            {
                return UndoBlock.InRun;
            }

            if (_applied.Count == 0)
            {
                return UndoBlock.Nothing;
            }

            var last = _applied[_applied.Count - 1];
            return BlockOn(last, State.ActiveTeam);
        }
    }

    /// <summary>
    /// Takes back exactly one command — the last one — by replaying the fight from its seed with
    /// that one entry dropped. One press, one command: a walk made of three segments takes three.
    /// </summary>
    /// <remarks>
    /// Refuses rather than doing nothing quietly whenever <see cref="CanUndo"/> is false, and
    /// <see cref="UndoBlockedReason"/> says which boundary is in the way.
    /// </remarks>
    /// <returns>Whether anything was undone.</returns>
    public bool Undo()
    {
        if (!CanUndo)
        {
            return false;
        }

        var kept = _applied.GetRange(0, _applied.Count - 1);

        // The reference panel is a view of the fight, not of the position, so a rewind leaves it on
        // whichever tab the player put it.
        var tab = Tab;

        _silent = true;
        try
        {
            Reset(Fight, Seed);

            var start = Game.Start(Fight, Seed);
            _recorder?.RecordStart(start);
            Adopt(start);

            foreach (var entry in kept)
            {
                Play(entry.Command, entry.Chosen);
            }
        }
        finally
        {
            _silent = false;
        }

        Tab = tab;
        Changed?.Invoke();
        return true;
    }

    private void Play(Command command, bool chosen)
    {
        var before = State;
        var result = Game.Apply(before, command);

        _recorder?.RecordStep(command, before, result);
        _applied.Add(Record(command, chosen, before, result));

        Adopt(result);
        Hovered = null;
        Mode = DefaultModeFor(SelectedUnit);
        ArmedAbility = null;
    }

    // ---- undo policy: which commands the shell still offers to take back -----------------------

    /// <summary>
    /// Why the shell refuses to take a command back. Wordless on purpose, the way
    /// <see cref="TargetingBlock"/> is: this is a policy talking, and the words belong to whoever is
    /// drawing the button.
    /// </summary>
    public enum UndoBlock
    {
        /// <summary>Nothing is in the way; the last command can be taken back.</summary>
        None = 0,

        /// <summary>The board belongs to a run, which owns its own command stream.</summary>
        InRun = 1,

        /// <summary>Nothing has been played on this board yet.</summary>
        Nothing = 2,

        /// <summary>The last thing on the log is the enemy's, not a person's.</summary>
        EnemyActed = 3,

        /// <summary>The step consumed a seeded draw, so its result is on the table.</summary>
        Randomised = 4,

        /// <summary>The step turned the round, which re-declares every enemy plan.</summary>
        RoundTurned = 5,

        /// <summary>The player closed the activation deliberately.</summary>
        TurnEnded = 6,

        /// <summary>The slot has passed to the other player, and it is theirs now.</summary>
        NotYours = 7,
    }

    /// <summary>
    /// The whole rule, as a pure function of one applied command and who holds the slot after it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The order is the precedence, and it is deliberate: the loudest boundary wins, so a player who
    /// crossed two of them at once is told about the one that actually cost them the rewind.
    /// </para>
    /// <para>
    /// <b>Deployment is one open segment.</b> Core hands the placement slot back and forth after
    /// every single placement, so an activation-shaped gate would make undo dead for the whole of
    /// setup in any two-player fight; nothing has closed an activation during deployment, so
    /// <see cref="UndoBlock.NotYours"/> cannot fire there.
    /// </para>
    /// </remarks>
    /// <param name="chosen">Whether a person chose the command rather than Core's planner.</param>
    /// <param name="drewFromTheSeed">Whether the step advanced the generator.</param>
    /// <param name="turnedTheRound">Whether the step ended the round and declared fresh plans.</param>
    /// <param name="endedTheTurn">Whether the command was the deliberate end of an activation.</param>
    /// <param name="closedTheActivation">Whether the step closed the activation, however it did it.</param>
    /// <param name="actorTeam">Side that issued the command.</param>
    /// <param name="slotTeam">Side holding the slot now.</param>
    /// <returns>The boundary in the way, or <see cref="UndoBlock.None"/>.</returns>
    public static UndoBlock BlockOn(
        bool chosen,
        bool drewFromTheSeed,
        bool turnedTheRound,
        bool endedTheTurn,
        bool closedTheActivation,
        Team actorTeam,
        Team slotTeam)
    {
        if (!chosen)
        {
            return UndoBlock.EnemyActed;
        }

        if (drewFromTheSeed)
        {
            return UndoBlock.Randomised;
        }

        if (turnedTheRound)
        {
            return UndoBlock.RoundTurned;
        }

        if (endedTheTurn)
        {
            return UndoBlock.TurnEnded;
        }

        // A closed activation whose slot went to the enemy is still the acting player's to take
        // back until the enemy actually moves; a closed activation whose slot went to the *other
        // player* is not, because the button is one button and the next person is already on it.
        return closedTheActivation && slotTeam.IsPlayer() && slotTeam != actorTeam
            ? UndoBlock.NotYours
            : UndoBlock.None;
    }

    /// <summary>Core's refusal in the player's words. Empty when nothing is refusing.</summary>
    /// <param name="block">Why undo is refused.</param>
    /// <param name="actorTeam">Side whose command it was, for the wrong-player case.</param>
    /// <returns>One phrase, lower-case, for a tooltip.</returns>
    public static string UndoWords(UndoBlock block, Team actorTeam) => block switch
    {
        UndoBlock.InRun => "the run owns the rewind — undo the run instead",
        UndoBlock.Nothing => "nothing to undo",
        UndoBlock.EnemyActed => "enemy has acted — round is committed",
        UndoBlock.Randomised => "a seeded roll has been made — its result is on the table",
        UndoBlock.RoundTurned => "the round has turned — the new plans are on the table",
        UndoBlock.TurnEnded => "end turn is committed — a closed activation cannot be reopened",
        UndoBlock.NotYours => "that was " + TeamName(actorTeam) + "'s activation — the slot has passed on",
        _ => string.Empty,
    };

    /// <summary>
    /// What one press would take back, named so the button promises something specific.
    /// </summary>
    /// <param name="state">Board as it stands, for naming whoever the command was aimed at.</param>
    /// <param name="command">The command that would go back.</param>
    /// <returns>A short phrase beginning "undo".</returns>
    public static string DescribeUndo(GameState? state, Command? command) => command switch
    {
        MoveCommand move => "undo move segment to " + BoardCoords.Of(move.To),
        DeployCommand deploy => "undo placing " + NameOf(state, deploy.UnitId) + " on " + BoardCoords.Of(deploy.At),
        AttackCommand { Mode: AttackMode.Pull } pull => "undo pull on " + NameOf(state, pull.TargetId),
        AttackCommand attack => "undo attack on " + NameOf(state, attack.TargetId),
        AbilityCommand ability => "undo " + AbilityDescriptor.For(ability.Ability).Name,
        SpendVerveCommand spend => "undo " + Naming.Of(spend.Spend),
        RescueCommand rescue => "undo rescue of " + NameOf(state, rescue.ClingingId),
        FinishClingingCommand finish => "undo kicking " + NameOf(state, finish.ClingingId) + " off the ledge",
        EndActivationCommand => "undo end turn",
        null => string.Empty,
        _ => "undo the last command",
    };

    private static string TeamName(Team team) => team switch
    {
        Team.PlayerA => "Player A",
        Team.PlayerB => "Player B",
        _ => "the enemy",
    };

    private static string NameOf(GameState? state, UnitId id) =>
        state?.FindUnit(id)?.Name ?? "the unit";

    /// <summary>One applied command, and everything the undo rule needs to know about its step.</summary>
    private readonly record struct Applied(
        Command Command,
        bool Chosen,
        Team ActorTeam,
        bool ClosedTheActivation,
        bool TurnedTheRound,
        bool DrewFromTheSeed);

    private static UndoBlock BlockOn(Applied entry, Team slotTeam) =>
        BlockOn(
            entry.Chosen,
            entry.DrewFromTheSeed,
            entry.TurnedTheRound,
            entry.Command is EndActivationCommand,
            entry.ClosedTheActivation,
            entry.ActorTeam,
            slotTeam);

    private static Applied Record(Command command, bool chosen, GameState before, StepResult result)
    {
        bool closed = false;
        bool turned = false;

        foreach (var evt in result.Events)
        {
            if (evt is ActivationEnded)
            {
                closed = true;
            }
            else if (evt is RoundEnded)
            {
                // The rollover, not the opening of round 1: coming out of deployment declares plans
                // for the first time, and taking the placement that caused it back takes the plans
                // with it. A round *turning* mid-fight is the reveal that cannot be un-seen.
                turned = true;
            }
        }

        return new Applied(
            command,
            chosen,
            TeamOf(before, command),
            closed,
            turned,
            before.RngState != result.NewState.RngState);
    }

    private static Team TeamOf(GameState before, Command command)
    {
        var id = ActorOf(command);
        return id != UnitId.None && before.FindUnit(id) is { } unit ? unit.Team : before.ActiveTeam;
    }

    // Deliberately not UnitOf: that one drives Selectable and the target maps, and a Verve spend
    // must not turn into a selectable unit just because undo wanted to know whose it was.
    private static UnitId ActorOf(Command command) => command switch
    {
        SpendVerveCommand s => s.UnitId,
        _ => UnitOf(command),
    };

    private void Reset(FightDefinition fight, int seed)
    {
        Untouched = false;
        Fight = fight;
        Seed = seed;
        _log.Clear();
        _applied.Clear();
        Selected = null;
        Inspected = null;
        InspectedTile = null;
        Telegraphed = null;
        Tab = ReferenceTab.Abilities;
        Hovered = null;
        Mode = ActionMode.Move;
        ArmedAbility = null;

        // A half-aimed cast, rescue or one-shot is aimed at a position that is about to stop
        // existing. It used to survive a rewind, which left the board drawing landing tiles for a
        // grab that had been taken back.
        ClearCast();
        ClearRescue();
        ClearPocketAim();

        // Keyed to what the player asked for, not to whether a recorder happens to exist: a new
        // fight in a session that is recording is a new fight that records.
        _recorder = _recordingWanted ? new CombatRecorder(fight, seed) : null;
        _fightNumber++;
    }

    /// <summary>
    /// Turns combat logging on or off. Switching it on mid-fight records from that point; the export
    /// says so rather than offering a command log that cannot be replayed. Switching it off drops
    /// everything recorded.
    /// </summary>
    /// <param name="on">Whether to record.</param>
    public void SetRecording(bool on)
    {
        _recordingWanted = on;

        if (on == Recording)
        {
            return;
        }

        _recorder = on ? new CombatRecorder(Fight, Seed) : null;
        Changed?.Invoke();
    }

    /// <summary>
    /// Restores the recording preference from an earlier visit. Only ever turns recording
    /// <em>off</em>: on is the default and needs no restoring, and a stored "on" that arrived from a
    /// corrupt key should not be able to switch recording on behind somebody mid-bug-hunt.
    /// </summary>
    /// <param name="on">What was stored.</param>
    public void RestoreRecording(bool on)
    {
        if (!on)
        {
            SetRecording(false);
        }
    }

    /// <summary>
    /// Writes the fight in progress to the sitting's folder, when there is a folder and a recorder.
    /// Called at activation boundaries and when a fight resolves.
    /// </summary>
    /// <returns>A task that completes when the write has finished or been skipped.</returns>
    public Task FlushFightLogAsync() =>
        _sink is null || _recorder is null
            ? Task.CompletedTask
            : _sink.WriteFightLogAsync(_fightNumber, Fight.Id, _recorder.Export());

    /// <summary>
    /// The full export — command log first so the file is re-runnable, then the event log. Empty
    /// when recording is off.
    /// </summary>
    /// <returns>The export text.</returns>
    public string RenderCombatLog() => _recorder?.Export() ?? string.Empty;

    /// <summary>Selects a unit to act with, when Core allows it.</summary>
    /// <param name="id">Unit to select.</param>
    public void Select(UnitId id)
    {
        if (!Selectable.Contains(id))
        {
            return;
        }

        Selected = id;
        Hovered = null;
        Mode = DefaultModeFor(SelectedUnit);
        ArmedAbility = null;
        Changed?.Invoke();
    }

    /// <summary>Switches which action the player is aiming.</summary>
    /// <param name="mode">Mode to aim.</param>
    /// <remarks>
    /// Asking for <see cref="ActionMode.Ability"/> without naming one arms the first the unit can
    /// use, so a caller that does not care which ability still gets a coherent mode.
    /// </remarks>
    public void SetMode(ActionMode mode)
    {
        if (mode == ActionMode.Ability)
        {
            foreach (var descriptor in SelectedAbilities)
            {
                if (IsAbilityAvailable(descriptor.Ability))
                {
                    SetAbility(descriptor.Ability);
                    return;
                }
            }

            Changed?.Invoke();
            return;
        }

        if (IsAvailable(mode))
        {
            Mode = mode;
            ArmedAbility = null;
            Hovered = null;
        }

        Changed?.Invoke();
    }

    /// <summary>Arms one named ability, so the board aims that ability and no other.</summary>
    /// <param name="ability">Ability to arm.</param>
    public void SetAbility(Ability ability)
    {
        if (IsAbilityAvailable(ability))
        {
            Mode = ActionMode.Ability;
            ArmedAbility = ability;
            Hovered = null;
        }

        Changed?.Invoke();
    }

    /// <summary>Records the tile under the pointer, for previewing.</summary>
    /// <param name="coord">Tile hovered, or <c>null</c> when the pointer leaves the board.</param>
    public void Hover(Coord? coord)
    {
        Hovered = coord;
        Changed?.Invoke();
    }

    /// <summary>Units that have at least one legal command right now.</summary>
    public IReadOnlyCollection<UnitId> Selectable =>
        Legal.Select(UnitOf).Where(id => id != UnitId.None).Distinct().ToList();

    /// <summary>Whether an action mode has any legal target for the selected unit.</summary>
    /// <param name="mode">Mode to test.</param>
    /// <returns>Whether it can be aimed.</returns>
    /// <remarks>
    /// Availability of an ability is asked of Core's legal list rather than of the tile map, because
    /// a <see cref="AbilityTargeting.Self"/> ability is perfectly usable while covering no tile.
    /// </remarks>
    public bool IsAvailable(ActionMode mode) =>
        mode == ActionMode.Ability
            ? AbilityCommands().Any()
            : TargetsFor(mode, null).Count > 0;

    /// <summary>Whether Core would accept this specific ability from the selected unit right now.</summary>
    /// <param name="ability">Ability to test.</param>
    /// <returns>Whether it can be armed.</returns>
    public bool IsAbilityAvailable(Ability ability) =>
        AbilityCommands().Any(a => a.Ability == ability);

    /// <summary>
    /// The command a <see cref="AbilityTargeting.Self"/> ability would submit, when one is armed.
    /// </summary>
    /// <remarks>
    /// A stance aims at nothing, so there is no tile to click and the board can never issue it. The
    /// action panel shows this as an explicit confirm button instead of firing on the arming click:
    /// every other action in the shell takes a second, deliberate click to commit, and a button that
    /// spent the activation's action half the instant it was pressed would be the one exception.
    /// </remarks>
    public AbilityCommand? SelfAbilityCommand =>
        Mode == ActionMode.Ability && ArmedDescriptor is { Targeting: AbilityTargeting.Self }
            ? AbilityCommands().FirstOrDefault(a => a.Ability == ArmedAbility!.Value)
            : null;

    /// <summary>
    /// The Verve spend the selected unit could make right now, or <c>null</c>.
    /// </summary>
    /// <remarks>
    /// Read off <see cref="StepResult.LegalNext"/> rather than worked out from the unit's class and
    /// meter. Half of Verve's legality is not visible on the unit at all — Cast needs an enemy in
    /// reach with somewhere to put it, and Preen needs hit points missing — and a shell that
    /// re-derived it would be a second, disagreeing copy of the rule.
    /// </remarks>
    public SpendVerveCommand? VerveSpendCommand =>
        Selected is null
            ? null
            : Legal.OfType<SpendVerveCommand>().FirstOrDefault(s => s.UnitId == Selected.Value);

    /// <summary>Whether the selected unit has a Verve spend available this instant.</summary>
    public bool CanSpendVerve => VerveSpendCommand is not null;

    /// <summary>Submits the selected unit's Verve spend, if Core is offering one.</summary>
    /// <remarks>
    /// Cast is not submitted this way — it aims twice, so it has its own arming flow below.
    /// </remarks>
    public void SpendVerve()
    {
        if (VerveSpendCommand is { Spend: not VerveSpend.Cast } command)
        {
            Submit(command);
        }
    }

    // ---- Cast: grab somebody, then choose where they land ------------------------------------

    /// <summary>The enemy a cast is currently aimed at, once one has been picked.</summary>
    public UnitId? CastTarget { get; private set; }

    /// <summary>True while a cast is being aimed, at either stage.</summary>
    public bool AimingCast { get; private set; }

    /// <summary>
    /// Every cast Core is offering the selected unit, which is one command per grab-and-place pair.
    /// </summary>
    private IEnumerable<SpendVerveCommand> CastCommands =>
        Selected is null
            ? Array.Empty<SpendVerveCommand>()
            : Legal.OfType<SpendVerveCommand>()
                .Where(c => c.UnitId == Selected.Value && c.Spend == VerveSpend.Cast);

    /// <summary>Enemies the selected Fisher could pluck, for the first stage of aiming.</summary>
    public IReadOnlyCollection<Coord> CastGrabTiles =>
        !AimingCast || CastTarget is not null
            ? Array.Empty<Coord>()
            : CastCommands
                .Select(c => State.FindUnit(c.TargetId!.Value)?.Position)
                .Where(p => p is not null)
                .Select(p => p!.Value)
                .Distinct()
                .ToList();

    /// <summary>
    /// Landing tiles for the grabbed enemy, keyed to the command each would submit. The second
    /// stage of aiming.
    /// </summary>
    public IReadOnlyDictionary<Coord, Command> CastLandings
    {
        get
        {
            var tiles = new Dictionary<Coord, Command>();
            if (!AimingCast || CastTarget is null)
            {
                return tiles;
            }

            foreach (var command in CastCommands)
            {
                if (command.TargetId == CastTarget.Value && command.To is { } to)
                {
                    tiles[to] = command;
                }
            }

            return tiles;
        }
    }

    /// <summary>
    /// Which way a landing tile lies from the Fisher — the side she would put them down on.
    /// </summary>
    /// <remarks>
    /// The four landings are her four neighbours, so choosing one is choosing a side rather than a
    /// square. The board draws them as a cone fanning off her, and this is the direction each one
    /// points (D-093).
    /// </remarks>
    /// <param name="landing">A tile from <see cref="CastLandings"/>.</param>
    /// <returns>The direction from the Fisher, or null when the tile is not one of hers.</returns>
    public Direction? CastLandingSide(Coord landing)
    {
        if (SelectedUnit is not { } fisher)
        {
            return null;
        }

        foreach (var direction in Directions.All)
        {
            if (fisher.Position.Step(direction) == landing)
            {
                return direction;
            }
        }

        return null;
    }

    /// <summary>Starts aiming a cast, or puts it away again.</summary>
    public void ToggleCast()
    {
        AimingCast = !AimingCast;
        CastTarget = null;
        Changed?.Invoke();
    }

    /// <summary>Picks the enemy to pluck, moving aiming on to the landing stage.</summary>
    /// <param name="targetId">Enemy to grab.</param>
    public void AimCastAt(UnitId targetId)
    {
        CastTarget = targetId;
        Changed?.Invoke();
    }

    /// <summary>Stops aiming, whether a cast was thrown or abandoned.</summary>
    public void ClearCast()
    {
        AimingCast = false;
        CastTarget = null;
    }

    // ---- Rescue: haul them out, then choose which side they come up on -----------------------

    /// <summary>The clinging ally a rescue is currently aimed at, if one has been chosen.</summary>
    public UnitId? RescueTarget { get; private set; }

    /// <summary>True while a rescue is choosing its destination.</summary>
    public bool AimingRescue => RescueTarget is not null;

    /// <summary>
    /// Where the rescued ally could be set down, keyed to the command each tile would submit.
    /// </summary>
    /// <remarks>
    /// D-082 made the destination the rescuer's choice, and the shell was quietly throwing that away:
    /// every destination for a given ally was keyed to the <em>ally's</em> tile, so all but one were
    /// overwritten and clicking picked whichever survived. The tiles are the rescuer's own
    /// neighbours, so this is a side rather than a square — drawn as a cone, like Cast (D-093).
    /// </remarks>
    public IReadOnlyDictionary<Coord, Command> RescueDestinations
    {
        get
        {
            var tiles = new Dictionary<Coord, Command>();
            if (RescueTarget is null || Selected is null)
            {
                return tiles;
            }

            foreach (var command in Legal.OfType<RescueCommand>())
            {
                // Stand-still rescues only. Core offers routed ones too since the rescue fused with
                // its run-up, but this surface is a cone of the rescuer's own neighbours and has no
                // way to ask which route she took - keying a routed drop tile here would silently
                // pick one approach on the player's behalf. Surfacing the run-up is its own job.
                if (command.UnitId == Selected.Value
                    && command.ClingingId == RescueTarget.Value
                    && command.Path.Count == 0)
                {
                    tiles[command.To] = command;
                }
            }

            return tiles;
        }
    }

    /// <summary>Which way a rescue destination lies from the rescuer.</summary>
    /// <param name="destination">A tile from <see cref="RescueDestinations"/>.</param>
    /// <returns>The direction, or null when the tile is not one of the rescuer's neighbours.</returns>
    public Direction? RescueSide(Coord destination)
    {
        if (SelectedUnit is not { } rescuer)
        {
            return null;
        }

        foreach (var direction in Directions.All)
        {
            if (rescuer.Position.Step(direction) == destination)
            {
                return direction;
            }
        }

        return null;
    }

    /// <summary>Aims a rescue at one clinging ally, or puts it away if it was already aimed there.</summary>
    /// <param name="clingingId">The ally on the ledge.</param>
    public void ToggleRescue(UnitId clingingId)
    {
        RescueTarget = RescueTarget == clingingId ? null : clingingId;
        Changed?.Invoke();
    }

    /// <summary>Stops aiming a rescue, whether one was made or abandoned.</summary>
    public void ClearRescue() => RescueTarget = null;

    /// <summary>Clickable tiles for the current mode, each mapped to the command it submits.</summary>
    public IReadOnlyDictionary<Coord, Command> Targets => TargetsFor(Mode, ArmedAbility);

    private IEnumerable<AbilityCommand> AbilityCommands() =>
        Selected is null
            ? Array.Empty<AbilityCommand>()
            : Legal.OfType<AbilityCommand>().Where(a => a.UnitId == Selected.Value);

    /// <summary>Tiles to tint as "within reach" for the current mode.</summary>
    public IReadOnlyCollection<Coord> RangeTiles
    {
        get
        {
            var unit = SelectedUnit;
            if (unit is null || !unit.IsOnBoard)
            {
                return Array.Empty<Coord>();
            }

            switch (Mode)
            {
                case ActionMode.Attack:
                case ActionMode.Pull:
                    return Combat.RangeTiles(State, unit).ToList();
                case ActionMode.Ability:
                    return Abilities.RangeTiles(State, unit, ArmedDescriptor).ToList();
                default:
                    return Array.Empty<Coord>();
            }
        }
    }

    /// <summary>Tiles the hovered action would travel through, for drawing the projected route.</summary>
    public IReadOnlyCollection<Coord> ProjectedPath
    {
        get
        {
            var unit = SelectedUnit;
            if (unit is null || Hovered is null || !Targets.ContainsKey(Hovered.Value))
            {
                return Array.Empty<Coord>();
            }

            var tiles = new List<Coord>();

            if (Mode == ActionMode.Move && Movement.TryGetMove(State, unit, Hovered.Value, out var option))
            {
                tiles.AddRange(option.Path);
                return tiles;
            }

            // A line is drawn as the whole run it covers, not only the tiles that happen to have
            // somebody on them, so the shape of the ability is visible before it is fired.
            var line = HoveredLineDirection();
            if (line is not null)
            {
                tiles.AddRange(Abilities.LineTiles(State, unit, line.Value, ArmedDescriptor));
                return tiles;
            }

            var charge = HoveredCharge();
            if (charge is not null)
            {
                tiles.AddRange(charge.Path);
                if (charge.Contact is not null)
                {
                    tiles.AddRange(charge.Contact.Path);
                }

                return tiles;
            }

            var displacement = HoveredDisplacement();
            if (displacement is not null)
            {
                tiles.AddRange(displacement.Path);
            }

            return tiles;
        }
    }

    /// <summary>
    /// What the hovered action would do, tile by tile, so the outcome can be drawn on the board
    /// rather than only written beside it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every figure is read out of a Core preview — <see cref="Combat.CanAttack"/>,
    /// <see cref="Abilities.PreviewLine"/>, <see cref="Abilities.PreviewCharge"/>,
    /// <see cref="Abilities.PreviewTarget"/> and <see cref="Displacement.PreviewAuto"/>. The shell
    /// adds no arithmetic of its own; it only decides which tile each number belongs on.
    /// </para>
    /// <para>
    /// The board carries the outcome and the panel supports it, not the other way round: an aim that
    /// only reads correctly in a sentence beside the board is an aim the player has to look away to
    /// understand.
    /// </para>
    /// </remarks>
    public IReadOnlyList<PreviewMark> PreviewMarks
    {
        get
        {
            var marks = new List<PreviewMark>();
            var unit = SelectedUnit;

            if (unit is null || Hovered is null || !Targets.TryGetValue(Hovered.Value, out var command))
            {
                return marks;
            }

            switch (command)
            {
                case AttackCommand { Mode: AttackMode.Damage } attack:
                {
                    var target = State.UnitById(attack.TargetId);
                    Combat.CanAttack(State, unit, target, out int damage);
                    Add(marks, target.Position, damage, DisplacementStop.RanOut, damage >= target.Hp);
                    AddDisplacement(marks, HoveredDisplacement());
                    break;
                }

                case AttackCommand:
                    AddDisplacement(marks, HoveredDisplacement());
                    break;

                case AbilityCommand ability:
                {
                    var descriptor = AbilityDescriptor.For(ability.Ability);

                    if (descriptor.Targeting == AbilityTargeting.Line && ability.Direction.HasValue)
                    {
                        foreach (var hit in Abilities.PreviewLine(State, unit, ability.Direction.Value, ability.Ability))
                        {
                            var struck = hit.UnitId is null ? null : State.FindUnit(hit.UnitId.Value);
                            Add(marks, hit.At, hit.Damage, DisplacementStop.RanOut,
                                struck is not null && hit.Damage >= struck.Hp);
                        }

                        break;
                    }

                    if (descriptor.Targeting == AbilityTargeting.Direction && ability.Direction.HasValue)
                    {
                        var charge = Abilities.PreviewCharge(State, unit, ability.Direction.Value);

                        // The lane's own cost to the charger, on the tile it ends on. A dash that
                        // hurts is still a legal dash, and the number is why a player takes it anyway.
                        Add(marks, charge.Destination, charge.SelfDamage, DisplacementStop.RanOut, false);
                        AddDisplacement(marks, charge.Contact);
                        break;
                    }

                    if (ability.TargetId is { } targetId && State.FindUnit(targetId) is { } aimed)
                    {
                        Add(marks, aimed.Position, descriptor.Damage, DisplacementStop.RanOut,
                            descriptor.Damage > 0 && descriptor.Damage >= aimed.Hp);
                    }

                    AddDisplacement(marks, HoveredDisplacement());
                    break;
                }
            }

            return marks;
        }
    }

    private void AddDisplacement(List<PreviewMark> marks, DisplacementPreview? preview)
    {
        if (preview is null || preview.IsNoOp)
        {
            return;
        }

        Add(marks, preview.Destination, preview.DamageToUnit, preview.Stop, preview.WouldDown);

        // A collision hurts both bodies, and the second one is the half a player forgets.
        if (preview.ObstacleId is { } obstacleId
            && State.FindUnit(obstacleId) is { } obstacle
            && preview.DamageToObstacle > 0)
        {
            Add(marks, obstacle.Position, preview.DamageToObstacle, DisplacementStop.Collision,
                preview.DamageToObstacle >= obstacle.Hp);
        }

        if (preview.StructureAt is { } structureAt && preview.DamageToStructure > 0)
        {
            Add(marks, structureAt, preview.DamageToStructure, DisplacementStop.Collision, false);
        }
    }

    // One mark per tile: the loudest claim wins, so a tile that both takes a hit and is a landing
    // reads as the hit rather than as two overlapping labels.
    private static void Add(
        List<PreviewMark> marks, Coord at, int damage, DisplacementStop stop, bool fatal)
    {
        if (damage <= 0 && stop is DisplacementStop.RanOut or DisplacementStop.Immovable)
        {
            return;
        }

        for (int i = 0; i < marks.Count; i++)
        {
            if (marks[i].At.Equals(at))
            {
                if (damage > marks[i].Damage)
                {
                    marks[i] = new PreviewMark(at, damage, stop, fatal);
                }

                return;
            }
        }

        marks.Add(new PreviewMark(at, damage, stop, fatal));
    }

    /// <summary>
    /// Plain-language description of what the hovered action would do, built entirely from Core
    /// previews so it can never promise something the rules will not deliver.
    /// </summary>
    public string? PreviewText
    {
        get
        {
            var unit = SelectedUnit;
            if (unit is null || Hovered is null || !Targets.TryGetValue(Hovered.Value, out var command))
            {
                return null;
            }

            switch (command)
            {
                case MoveCommand:
                    return DescribeMove(unit, Hovered.Value);
                case AttackCommand attack when attack.Mode == AttackMode.Damage:
                    return DescribeAttack(unit, attack);
                case AttackCommand:
                    return Describe("Pull 1", HoveredDisplacement());
                case AbilityCommand ability:
                    return DescribeAbility(unit, ability);
                case RescueCommand:
                    return "Haul the clinging ally out. Costs your action — and closes your move.";
                case FinishClingingCommand:
                    return "Kick it off the ledge — gone for the run. Free action.";
                case UseConsumableCommand use:
                    return DescribePocket(unit, use);
                default:
                    return null;
            }
        }
    }

    /// <summary>The command that ends the selected unit's activation, when it exists.</summary>
    public EndActivationCommand? EndCommand =>
        Selected is null
            ? null
            : Legal.OfType<EndActivationCommand>().FirstOrDefault(e => e.UnitId == Selected.Value);

    /// <summary>
    /// Every way the selected duck could empty its pocket right now, straight off Core's legal list.
    /// </summary>
    /// <remarks>
    /// Read off <see cref="StepResult.LegalNext"/> rather than worked out from the item and the
    /// board, for the same reason <see cref="VerveSpendCommand"/> is: half of a one-shot's legality
    /// is invisible on the unit — an Old Rope needs somebody hanging within reach and a Crate needs
    /// an open tile beside it — and a shell that re-derived it would be a second, disagreeing copy
    /// of <see cref="Consumables.Legal"/>.
    /// </remarks>
    public IReadOnlyList<UseConsumableCommand> PocketCommands =>
        Selected is null
            ? Array.Empty<UseConsumableCommand>()
            : Legal.OfType<UseConsumableCommand>().Where(u => u.UnitId == Selected.Value).ToList();

    /// <summary>Whether the selected duck may use what is in its pocket this instant.</summary>
    public bool CanUsePocket => PocketCommands.Count > 0;

    /// <summary>True while a one-shot is being aimed on the board.</summary>
    /// <remarks>
    /// Keyed to the selection as well as the mode: with nothing selected the pocket section is
    /// drawing whatever duck the inspector is reading, and an armed badge on that one would be
    /// pointing at an aim it does not own.
    /// </remarks>
    public bool AimingPocket => Mode == ActionMode.Pocket && Selected is not null;

    /// <summary>
    /// Who an Old Rope is aimed at, once the ally has been chosen. Null for an item that aims at
    /// tiles only, and null in the first stage of aiming a Rope at one of several hanging allies.
    /// </summary>
    public UnitId? PocketTarget { get; private set; }

    /// <summary>
    /// Which stage the aim is at: true while the click still chooses <em>who</em> rather than where.
    /// </summary>
    public bool PocketPicksWho =>
        AimingPocket && PocketTarget is null && PocketCommands.Any(u => u.TargetId is not null);

    /// <summary>
    /// Presses the one-shot. <b>Never a no-op.</b> One way to use it submits it; several enter
    /// aiming so the board can draw them; pressing it again while aiming puts it away.
    /// </summary>
    /// <remarks>
    /// It used to submit only when Core offered exactly one command and do nothing at all otherwise,
    /// which made a Crate of Debris beside open ground unusable from the obvious control — the
    /// choice was a column of coordinate buttons in the sidebar and the item itself was dead
    /// (D-136). Aiming is the same surface an ability uses: <see cref="Mode"/> plus
    /// <see cref="Targets"/>, highlighted on the board, committed by clicking a tile.
    /// </remarks>
    public void UsePocket()
    {
        var commands = PocketCommands;

        // Nothing to press. The row is disabled and carries Core's own reason, so this is only
        // reachable by code — never by a player looking at a live button.
        if (commands.Count == 0)
        {
            return;
        }

        if (commands.Count == 1)
        {
            Submit(commands[0]);
            return;
        }

        if (AimingPocket)
        {
            ClearPocketAim();
            Changed?.Invoke();
            return;
        }

        Mode = ActionMode.Pocket;
        ArmedAbility = null;
        Hovered = null;

        // One hanging ally is not a choice, so a Rope with exactly one skips straight to the side
        // it sets them down on. Making the player click the only candidate first would be ceremony.
        PocketTarget = SoleTarget(commands);
        Changed?.Invoke();
    }

    /// <summary>
    /// Takes a board click as the first half of a two-stage pocket aim — which hanging ally the Rope
    /// hauls — and says whether it did.
    /// </summary>
    /// <remarks>
    /// The same shape as <see cref="AimCastAt"/>: an item that aims at a unit and then at a tile
    /// cannot commit on the first click, so that click chooses instead of submitting. False leaves
    /// the click to whatever else the board would have done with it.
    /// </remarks>
    /// <param name="coord">Tile clicked.</param>
    /// <returns>Whether the click was consumed by the aim.</returns>
    public bool AimPocketAt(Coord coord)
    {
        if (!PocketPicksWho)
        {
            return false;
        }

        foreach (var use in PocketCommands)
        {
            if (use.TargetId is { } id
                && State.FindUnit(id) is { IsOnBoard: true } ally
                && ally.Position == coord)
            {
                PocketTarget = id;
                Hovered = null;
                Changed?.Invoke();
                return true;
            }
        }

        return false;
    }

    /// <summary>Stops aiming the pocket, whether the one-shot was used or the aim abandoned.</summary>
    public void ClearPocketAim()
    {
        if (Mode == ActionMode.Pocket)
        {
            Mode = DefaultModeFor(SelectedUnit);
        }

        PocketTarget = null;
    }

    /// <summary>The one unit every offered use names, or null when they name none or several.</summary>
    private static UnitId? SoleTarget(IReadOnlyList<UseConsumableCommand> commands)
    {
        UnitId? only = null;

        foreach (var use in commands)
        {
            if (use.TargetId is not { } id)
            {
                return null;
            }

            if (only is null)
            {
                only = id;
            }
            else if (only.Value != id)
            {
                return null;
            }
        }

        return only;
    }

    /// <summary>Tiles the current player may deploy onto.</summary>
    public IReadOnlyDictionary<Coord, DeployCommand> DeployTargets =>
        Legal.OfType<DeployCommand>()
            .Where(d => PendingDeployUnit is null || d.UnitId == PendingDeployUnit.Value)
            .GroupBy(d => d.At)
            .ToDictionary(g => g.Key, g => g.First());

    /// <summary>The unit that will be placed by the next deployment click.</summary>
    public UnitId? PendingDeployUnit =>
        Legal.OfType<DeployCommand>().Select(d => (UnitId?)d.UnitId).FirstOrDefault();

    /// <summary>True when the enemy side owes an activation.</summary>
    public bool AwaitingEnemy => Game.IsEnemyTurn(State) && Legal.Count > 0;

    /// <summary>
    /// Resolves one step of the pending enemy activation by submitting whatever Core's planner
    /// chose. The command goes through <see cref="Game.Apply"/> like any other, so the log stays a
    /// complete recording of the fight.
    /// </summary>
    public void ResolveEnemyActivation()
    {
        var command = Game.NextEnemyCommand(State);
        if (command is null)
        {
            return;
        }

        _submittingEnemy = true;
        try
        {
            Submit(command);
        }
        finally
        {
            _submittingEnemy = false;
        }
    }

    private IReadOnlyDictionary<Coord, Command> TargetsFor(ActionMode mode, Ability? ability)
    {
        var map = new Dictionary<Coord, Command>();
        if (Selected is null)
        {
            return map;
        }

        var id = Selected.Value;

        foreach (var command in Legal)
        {
            if (UnitOf(command) != id)
            {
                continue;
            }

            switch (command)
            {
                case MoveCommand move when mode == ActionMode.Move:
                    map[move.To] = move;
                    break;

                case AttackCommand attack when mode == ActionMode.Attack && attack.Mode == AttackMode.Damage:
                    Add(map, attack.TargetId, attack);
                    break;

                case AttackCommand attack when mode == ActionMode.Pull && attack.Mode == AttackMode.Pull:
                    Add(map, attack.TargetId, attack);
                    break;

                // Only the armed ability is aimed. Merging every ability's commands into one map is
                // what made a two-ability unit unaimable: there was no way to say which one you meant.
                case AbilityCommand armed when mode == ActionMode.Ability && armed.Ability == ability:
                    AddAbility(map, armed);
                    break;

                case RescueCommand rescue when mode == ActionMode.Rescue:
                    Add(map, rescue.ClingingId, rescue);
                    break;

                case FinishClingingCommand finish when mode == ActionMode.Finish:
                    Add(map, finish.ClingingId, finish);
                    break;

                case UseConsumableCommand use when mode == ActionMode.Pocket:
                    AddPocket(map, use);
                    break;
            }
        }

        return map;
    }

    // Which tile issues a one-shot is the item's own shape, read off the command Core handed over:
    // a Crate names a tile and nothing else, a Rope names somebody and a tile. The Rope's two
    // halves are two clicks — the ally's own tile first, then the side she comes up on — so the
    // first stage draws whoever is hanging and the second draws the cone.
    private void AddPocket(Dictionary<Coord, Command> map, UseConsumableCommand use)
    {
        if (use.TargetId is { } targetId)
        {
            if (PocketTarget is null)
            {
                Add(map, targetId, use);
                return;
            }

            if (targetId != PocketTarget.Value)
            {
                return;
            }
        }

        if (use.To is { } tile)
        {
            map[tile] = use;
        }
    }

    // Which tiles issue this command is decided by the ability's shape, never by which of the
    // command's optional fields happens to be set: a Line also carries a Direction, and treating any
    // Direction as a charge aimed Spear Thrust at a charge destination it does not have.
    private void AddAbility(Dictionary<Coord, Command> map, AbilityCommand ability)
    {
        var unit = SelectedUnit;
        if (unit is null)
        {
            return;
        }

        var descriptor = AbilityDescriptor.For(ability.Ability);

        switch (descriptor.Targeting)
        {
            case AbilityTargeting.Enemy when ability.TargetId.HasValue:
                Add(map, ability.TargetId.Value, ability);
                break;

            // A charge is aimed by clicking where it lands: the enemy it would hit, or the tile it
            // would end on when the lane is empty.
            case AbilityTargeting.Direction when ability.Direction.HasValue:
                var charge = Abilities.PreviewCharge(State, unit, ability.Direction.Value);
                var tile = charge.Contact is not null
                    ? State.UnitById(charge.Contact.UnitId).Position
                    : charge.Destination;
                map[tile] = ability;
                break;

            // A line is aimed by clicking anything it would hit. Each direction is its own command,
            // and the four directions cover disjoint tiles, so a tile names exactly one of them.
            case AbilityTargeting.Line when ability.Direction.HasValue:
                foreach (var hit in Abilities.PreviewLine(State, unit, ability.Direction.Value, ability.Ability))
                {
                    map[hit.At] = ability;
                }

                break;

            // Self aims at nothing and must not be clickable on the board. SelfAbilityCommand offers
            // it in the action panel instead.
        }
    }

    private Direction? HoveredLineDirection()
    {
        if (Hovered is null
            || ArmedDescriptor is not { Targeting: AbilityTargeting.Line }
            || !Targets.TryGetValue(Hovered.Value, out var command))
        {
            return null;
        }

        return command is AbilityCommand { Direction: { } direction } ? direction : null;
    }

    private void Add(Dictionary<Coord, Command> map, UnitId unitId, Command command)
    {
        var unit = State.FindUnit(unitId);
        if (unit is not null && unit.IsOnBoard)
        {
            map[unit.Position] = command;
        }
    }

    private ChargePreview? HoveredCharge()
    {
        var unit = SelectedUnit;
        if (unit is null || Hovered is null || !Targets.TryGetValue(Hovered.Value, out var command))
        {
            return null;
        }

        return command is AbilityCommand { Direction: { } direction } ac
            && AbilityDescriptor.For(ac.Ability).Targeting == AbilityTargeting.Direction
            ? Abilities.PreviewCharge(State, unit, direction)
            : null;
    }

    private DisplacementPreview? HoveredDisplacement()
    {
        var unit = SelectedUnit;
        if (unit is null || Hovered is null || !Targets.TryGetValue(Hovered.Value, out var command))
        {
            return null;
        }

        switch (command)
        {
            case AbilityCommand { TargetId: { } targetId }:
                return Abilities.PreviewTarget(State, unit, targetId);

            case AttackCommand { Mode: AttackMode.Pull } pull:
                return Displacement.PreviewAuto(State, pull.TargetId, unit.Position, DisplacementKind.Pull, 1);

            case AttackCommand attack when unit.Template.AttackPush > 0:
                return Displacement.PreviewAuto(
                    State, attack.TargetId, unit.Position, DisplacementKind.Push, unit.Template.AttackPush);

            default:
                return null;
        }
    }

    private string DescribeMove(Unit unit, Coord destination)
    {
        if (!Movement.TryGetMove(State, unit, destination, out var option))
        {
            return string.Empty;
        }

        string text = $"Move {option.Cost} MP";
        if (option.SpikeTiles > 0)
        {
            text += $" · {option.SpikeTiles} damage from spikes";
        }

        // What is left after this click, because since D-097 the click is a segment rather than the
        // whole move: a player deciding whether to detour needs to know what the detour costs them.
        int left = unit.MoveRemaining - option.Cost;
        text += left > 0
            ? $" · {left} MP left after"
            : " · ends your move";

        return text;
    }

    private string DescribeAttack(Unit unit, AttackCommand attack)
    {
        var target = State.UnitById(attack.TargetId);
        Combat.CanAttack(State, unit, target, out int damage);

        string text = $"{damage} damage to {target.Name}";
        if (Combat.IsElevatedShot(State, unit))
        {
            text += " (high ground +1)";
        }

        if (damage >= target.Hp)
        {
            return text + " — kills it";
        }

        var push = HoveredDisplacement();
        return push is null ? text : text + ", then " + Describe("push " + unit.Template.AttackPush, push);
    }

    private string DescribeAbility(Unit unit, AbilityCommand command)
    {
        var descriptor = AbilityDescriptor.For(command.Ability);

        // Two abilities are aimed by direction and they are nothing alike, so the shape decides —
        // not the mere presence of a Direction, which used to render Spear Thrust as "Charge 3 to…".
        if (descriptor.Targeting == AbilityTargeting.Line && command.Direction.HasValue)
        {
            var hits = Abilities.PreviewLine(State, unit, command.Direction.Value, command.Ability);
            if (hits.Count == 0)
            {
                return descriptor.Name;
            }

            var parts = new List<string>(hits.Count);
            foreach (var hit in hits)
            {
                var struck = hit.UnitId is null ? null : State.FindUnit(hit.UnitId.Value);
                string who = struck is not null
                    ? struck.Name
                    : hit.HitsStructure ? "the structure" : hit.At.ToString();

                parts.Add($"{hit.Damage} to {who}");
            }

            return string.Join(", ", parts);
        }

        if (command.Direction.HasValue)
        {
            var charge = Abilities.PreviewCharge(State, unit, command.Direction.Value);
            string text = charge.Path.Count > 0 ? $"Charge {charge.Path.Count} to {charge.Destination}" : "Shove from here";

            if (charge.SelfDamage > 0)
            {
                text += $" · you take {charge.SelfDamage} from spikes";
            }

            return charge.Contact is null ? text : text + ", then " + Describe("push 2", charge.Contact);
        }

        var displacement = HoveredDisplacement();
        string head = descriptor.Damage > 0 ? $"{descriptor.Damage} damage" : descriptor.Name;
        return displacement is null ? head : head + ", " + Describe(descriptor.PullsToAdjacent ? "reel in" : "push", displacement);
    }

    /// <summary>
    /// What emptying the pocket onto this tile would do. Every figure is Core's — the crate's hit
    /// points come from <see cref="Consumables.DebrisHp"/>, never from a number written here.
    /// </summary>
    private string DescribePocket(Unit unit, UseConsumableCommand use)
    {
        string name = unit.Loadout.Pocket is { } item ? CampCatalogue.NameOf(item) : "the one-shot";

        if (PocketPicksWho)
        {
            var hanging = use.TargetId is { } id ? State.FindUnit(id) : null;
            return hanging is null
                ? name + " — pick who to haul out."
                : name + " — haul " + hanging.Name + " out, then pick the side.";
        }

        if (use.TargetId is { } targetId && State.FindUnit(targetId) is { } ally)
        {
            return "Haul " + ally.Name + " out and set them down here. Costs no points.";
        }

        return name + " — a blocker with " + Consumables.DebrisHp(State) + " hit points, here. Costs no points.";
    }

    private string Describe(string verb, DisplacementPreview? preview)
    {
        if (preview is null)
        {
            return verb;
        }

        if (preview.IsNoOp)
        {
            return $"{verb} — it does not budge";
        }

        var target = State.FindUnit(preview.UnitId);
        string name = target?.Name ?? "target";

        switch (preview.Stop)
        {
            case DisplacementStop.Collision:
                string into = preview.ObstacleId.HasValue
                    ? State.UnitById(preview.ObstacleId.Value).Name
                    : "the wall";
                string both = preview.ObstacleId.HasValue ? " to both" : string.Empty;
                return $"{verb} {preview.EffectiveDistance} into {into} — {preview.DamageToUnit} damage{both}, staggered"
                    + (preview.WouldDown ? ", kills it" : string.Empty);

            case DisplacementStop.Spikes:
                return $"{verb} onto spikes — {preview.DamageToUnit} damage, staggered"
                    + (preview.WouldDown ? ", kills it" : string.Empty);

            case DisplacementStop.Pit:
                return $"{verb} into the pit — {name} is left clinging";

            default:
                string tail = preview.DamageToUnit > 0 ? $" · {preview.DamageToUnit} falling damage" : string.Empty;
                return $"{verb} {preview.EffectiveDistance} to {preview.Destination}{tail}";
        }
    }

    private static ActionMode DefaultModeFor(Unit? unit)
    {
        if (unit is null)
        {
            return ActionMode.Move;
        }

        return unit.HasMoved ? ActionMode.Attack : ActionMode.Move;
    }

    // The boundaries worth writing at: an activation finishing, and the fight itself finishing.
    private static bool WorthCheckpointing(IReadOnlyList<GameEvent> events)
    {
        foreach (var evt in events)
        {
            if (evt is ActivationEnded or FightWon or FightLost)
            {
                return true;
            }
        }

        return false;
    }

    private void Adopt(StepResult result)
    {
        State = result.NewState;
        Legal = result.LegalNext;

        foreach (var evt in result.Events)
        {
            _log.Add(EventText.Describe(evt, State));
        }

        // Checkpointed at activation boundaries and again when the fight ends, never per command:
        // one write per activation bounds what a closed tab loses to the activation in progress,
        // which is the same bound the board itself has. A rewind is not a checkpoint - it is the
        // log being rebuilt, and writing each replayed step would put the fight on disk N times.
        if (!_silent && WorthCheckpointing(result.Events))
        {
            // Deliberately not awaited. The write is the folder's business and the board must not
            // wait on a file handle to redraw; a failure lands in SessionLog.Status, which the notes
            // panel shows, rather than being thrown into a click handler.
            _ = FlushFightLogAsync();
        }

        // A dossier on something that is no longer on the board is a lie about the live half, so it
        // closes with the unit rather than freezing its last hit points on screen.
        if (Inspected.HasValue && !(State.FindUnit(Inspected.Value)?.IsOnBoard ?? false))
        {
            Inspected = null;
        }

        // Same rule for the singled-out telegraph: an arrow drawn from a body that is no longer
        // there is the loudest kind of lie a board can tell.
        if (Telegraphed.HasValue && !(State.FindUnit(Telegraphed.Value)?.IsOnBoard ?? false))
        {
            Telegraphed = null;
        }

        // Core commits a unit to its activation on that unit's first command; follow it so the
        // highlighted unit and the one Core will accept commands for never disagree.
        if (State.ActiveUnitId.HasValue)
        {
            Selected = State.ActiveUnitId;
        }
        else if (Selected.HasValue && !Selectable.Contains(Selected.Value))
        {
            Selected = null;
        }

        // Before Changed, so a renderer that wants to hold a unit back for a slide has already said
        // so by the time the board redraws.
        if (!_silent && result.Events.Count > 0)
        {
            Stepped?.Invoke(result.Events, _submittingEnemy);
        }

        Changed?.Invoke();
    }

    private static UnitId UnitOf(Command command) => command switch
    {
        MoveCommand m => m.UnitId,
        AttackCommand a => a.UnitId,
        AbilityCommand a => a.UnitId,
        RescueCommand r => r.UnitId,
        FinishClingingCommand f => f.UnitId,
        EndActivationCommand e => e.UnitId,
        DeployCommand d => d.UnitId,
        UseConsumableCommand u => u.UnitId,
        _ => UnitId.None,
    };
}
