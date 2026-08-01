using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Faultline.Core
{
    /// <summary>
    /// Accumulates a fight's two logs as it is played: the command log that makes it re-runnable and
    /// the event log that makes it readable without re-running it.
    /// </summary>
    /// <remarks>
    /// This is the only mutable type in Core and it holds no rules state — it is a transcript of a
    /// stream that already exists (docs/COMBAT_LOG.md). Core does no file IO, so
    /// <see cref="Export"/> hands back a string and the shell decides where it goes. Recording costs
    /// memory that grows with the length of a fight, which is why the shell keeps it opt-in.
    /// </remarks>
    public sealed class CombatRecorder
    {
        private readonly List<string> _lines = new List<string>();
        private readonly List<Command> _commands = new List<Command>();
        private readonly FightDefinition _fight;
        private readonly int _seed;

        private int _round;
        private Team _slotTeam;
        private UnitId? _slotUnit;

        /// <summary>Starts a recorder for one fight on one seed.</summary>
        /// <param name="fight">Fight being played.</param>
        /// <param name="seed">Run seed.</param>
        public CombatRecorder(FightDefinition fight, int seed)
        {
            _fight = fight ?? new FightDefinition();
            _seed = seed;
        }

        /// <summary>
        /// True when recording began at <see cref="Game.Start(FightDefinition, int)"/>. When false the
        /// recorder was switched on mid-fight and the command log cannot be replayed from the seed;
        /// the export says so rather than pretending otherwise.
        /// </summary>
        public bool FromFightStart { get; private set; }

        /// <summary>Event-log lines, oldest first, without the header.</summary>
        public IReadOnlyList<string> Lines => _lines;

        /// <summary>Number of event-log lines recorded so far.</summary>
        public int LineCount => _lines.Count;

        /// <summary>Commands recorded so far, in order.</summary>
        public IReadOnlyList<Command> Commands => _commands;

        /// <summary>The replayable record of this run.</summary>
        public RunRecord Run => RunRecord.For(_fight, _seed) with { Commands = _commands.ToArray() };

        /// <summary>Suggested filename for an export, from the fight's slug.</summary>
        public string FileName =>
            (string.IsNullOrEmpty(_fight.Id) ? "fight" : _fight.Id) + ".log";

        /// <summary>Records the opening step, before any command has been applied.</summary>
        /// <param name="start">Result of <see cref="Game.Start(FightDefinition, int)"/>.</param>
        public void RecordStart(StepResult start)
        {
            if (start is null)
            {
                return;
            }

            FromFightStart = true;
            _slotTeam = start.NewState.ActiveTeam;
            _slotUnit = start.NewState.ActiveUnitId;
            Append(start.Events, start.NewState);
        }

        /// <summary>Records one applied command and everything it caused.</summary>
        /// <param name="command">Command that was applied.</param>
        /// <param name="before">State the command was applied to.</param>
        /// <param name="result">What <see cref="Game.Apply(GameState, Command)"/> returned.</param>
        public void RecordStep(Command command, GameState before, StepResult result)
        {
            if (command is null || result is null)
            {
                return;
            }

            _commands.Add(command);

            if (before is not null)
            {
                _slotTeam = before.ActiveTeam;
                _slotUnit = before.ActiveUnitId;
            }

            Append(result.Events, result.NewState);
        }

        /// <summary>Forgets everything recorded so far.</summary>
        public void Clear()
        {
            _lines.Clear();
            _commands.Clear();
            _round = 0;
            _slotUnit = null;
            FromFightStart = false;
        }

        /// <summary>Renders the event log, header first.</summary>
        /// <returns>The event-log text.</returns>
        public string RenderEventLog()
        {
            var text = new StringBuilder();
            text.Append(CombatLog.Header).Append(CombatLog.LineSeparator);

            foreach (var line in _lines)
            {
                text.Append(line).Append(CombatLog.LineSeparator);
            }

            return text.ToString();
        }

        /// <summary>Renders the command log.</summary>
        /// <returns>The command-log text.</returns>
        public string RenderCommandLog() => Run.Render();

        /// <summary>
        /// Renders both logs into one file: the command log first so the file is re-runnable, then the
        /// event log. Nothing here is derived from the clock, so two runs of the same fight from the
        /// same seed export byte-identical text.
        /// </summary>
        /// <returns>The export text.</returns>
        public string Export()
        {
            var text = new StringBuilder();

            text.Append("# Faultline combat log").Append(CombatLog.LineSeparator);
            text.Append("# fight ").Append(_fight.Id)
                .Append(" - ").Append(CombatLog.Clean(_fight.Name))
                .Append(" (#").Append(Number(_fight.Number)).Append(")")
                .Append(CombatLog.LineSeparator);
            text.Append("# seed ").Append(Number(_seed)).Append(CombatLog.LineSeparator);
            text.Append("# events ").Append(Number(_lines.Count)).Append(CombatLog.LineSeparator);

            if (!FromFightStart)
            {
                text.Append("# WARNING recording started mid-fight - the command log below is partial")
                    .Append(CombatLog.LineSeparator);
            }

            text.Append("#").Append(CombatLog.LineSeparator);
            text.Append("# === command log ===").Append(CombatLog.LineSeparator);
            text.Append(RenderCommandLog());
            text.Append("#").Append(CombatLog.LineSeparator);
            text.Append("# === event log ===").Append(CombatLog.LineSeparator);
            text.Append(RenderEventLog());

            return text.ToString();
        }

        private void Append(IReadOnlyList<GameEvent> events, GameState state)
        {
            if (events is null)
            {
                return;
            }

            foreach (var evt in events)
            {
                // Round and slot are tracked from the stream itself rather than read off the state, so
                // the line an activation's first command produces already carries that activation.
                if (evt is RoundStarted started)
                {
                    _round = started.Round;
                }
                else if (evt is ActivationStarted activation)
                {
                    _slotTeam = activation.Team;
                    _slotUnit = activation.UnitId;
                }

                _lines.Add(CombatLog.Line(_round, SlotFor(evt, state), evt, state));

                if (evt is ActivationEnded)
                {
                    _slotUnit = null;
                }
            }
        }

        // Inside an activation every line belongs to it. Outside one — deployment, intent
        // declarations — the line belongs to whoever it is about, and a round- or fight-level line
        // belongs to no slot at all and says so.
        private string SlotFor(GameEvent evt, GameState state)
        {
            if (_slotUnit.HasValue)
            {
                return CombatLog.Slot(_slotTeam, _slotUnit);
            }

            var actorId = CombatLog.ActorOf(evt);
            if (actorId == UnitId.None)
            {
                return CombatLog.NoActor;
            }

            var actor = state?.FindUnit(actorId);
            return CombatLog.Slot(actor is null ? _slotTeam : actor.Team, null);
        }

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
