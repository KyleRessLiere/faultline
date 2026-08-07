using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Faultline.Core
{
    /// <summary>
    /// Turns the event stream into the tab-separated transcript described in docs/COMBAT_LOG.md.
    /// </summary>
    /// <remarks>
    /// A pure function from events to text: no state is queried that the event did not already carry,
    /// no wall-clock is read, and no collection is iterated in hash order. The same seed and command
    /// log therefore produce a byte-identical log, which is the entire point — a log that differs run
    /// to run cannot be diffed against another run. Core does no file IO, so everything here returns
    /// a string and the caller decides where it goes.
    /// </remarks>
    public static class CombatLog
    {
        /// <summary>Column separator. Tabs, not aligned spaces, so <c>cut</c> and spreadsheets work.</summary>
        public const string ColumnSeparator = "\t";

        /// <summary>Line separator. Pinned to LF so the log is byte-identical on every platform.</summary>
        public const string LineSeparator = "\n";

        /// <summary>Number of columns every line has, header included.</summary>
        public const int ColumnCount = 5;

        /// <summary>The column header line.</summary>
        public const string Header = "round\tslot\tactor\tevent\tdetail";

        /// <summary>
        /// Event-column value for a <see cref="GameEvent"/> this formatter does not know about. A
        /// silently unlogged event type is the failure mode that matters, so it is named loudly and
        /// asserted against in the tests.
        /// </summary>
        public const string UnknownEvent = "UnknownEvent";

        /// <summary>Actor-column value for an event that is about no particular unit.</summary>
        public const string NoActor = "-";

        /// <summary>Formats one event as a complete log line.</summary>
        /// <param name="round">Round the event belongs to; zero during deployment.</param>
        /// <param name="slot">Activation slot, from <see cref="Slot(Team, UnitId?)"/>.</param>
        /// <param name="evt">Event to format.</param>
        /// <param name="state">Any state from the same fight, used only to name units.</param>
        /// <returns>One tab-separated line, without a trailing newline.</returns>
        public static string Line(int round, string slot, GameEvent evt, GameState state)
        {
            var actorId = ActorOf(evt);

            return Number(round)
                + ColumnSeparator + slot
                + ColumnSeparator + (actorId == UnitId.None ? NoActor : Actor(state, actorId))
                + ColumnSeparator + EventName(evt)
                + ColumnSeparator + Detail(evt, state);
        }

        /// <summary>Formats a run of events as lines, without a header.</summary>
        /// <param name="events">Events in resolution order.</param>
        /// <param name="round">Round they belong to.</param>
        /// <param name="slot">Activation slot they belong to.</param>
        /// <param name="state">Any state from the same fight, used only to name units.</param>
        /// <returns>One line per event, in the order given.</returns>
        public static IReadOnlyList<string> Lines(
            IEnumerable<GameEvent> events,
            int round,
            string slot,
            GameState state)
        {
            var lines = new List<string>();
            if (events is null)
            {
                return lines;
            }

            foreach (var evt in events)
            {
                lines.Add(Line(round, slot, evt, state));
            }

            return lines;
        }

        /// <summary>
        /// The event column: the concrete event type's name. Never guessed from reflection, so an
        /// event type nobody taught this formatter about reports <see cref="UnknownEvent"/> instead
        /// of quietly looking fine.
        /// </summary>
        /// <param name="evt">Event to name.</param>
        /// <returns>The event name, or <see cref="UnknownEvent"/>.</returns>
        public static string EventName(GameEvent evt) => evt switch
        {
            FightStarted => nameof(FightStarted),
            UnitDeployed => nameof(UnitDeployed),
            DeploymentCompleted => nameof(DeploymentCompleted),
            RoundStarted => nameof(RoundStarted),
            IntentDeclared => nameof(IntentDeclared),
            RoundEnded => nameof(RoundEnded),
            ActivationStarted => nameof(ActivationStarted),
            ActivationEnded => nameof(ActivationEnded),
            UnitMoved => nameof(UnitMoved),
            AbilityUsed => nameof(AbilityUsed),
            UnitAttacked => nameof(UnitAttacked),
            UnitDamaged => nameof(UnitDamaged),
            UnitDowned => nameof(UnitDowned),
            UnitPushed => nameof(UnitPushed),
            Collision => nameof(Collision),
            SpikeHit => nameof(SpikeHit),
            Staggered => nameof(Staggered),
            Rattled => nameof(Rattled),
            ChalkMarked => nameof(ChalkMarked),
            BramblesGrew => nameof(BramblesGrew),
            BramblesFaded => nameof(BramblesFaded),
            GreasedFeatherSpent => nameof(GreasedFeatherSpent),
            HandOffGranted => nameof(HandOffGranted),
            StepBanked => nameof(StepBanked),
            CrossingShotFired => nameof(CrossingShotFired),
            Clinging => nameof(Clinging),
            Rescued => nameof(Rescued),
            Voided => nameof(Voided),
            FootingSpent => nameof(FootingSpent),
            FootingChoiceRequested => nameof(FootingChoiceRequested),
            DisplacementRefused => nameof(DisplacementRefused),
            CastRefused => nameof(CastRefused),
            GuardStanceChanged => nameof(GuardStanceChanged),
            GuardIntercepted => nameof(GuardIntercepted),
            GuardShielded => nameof(GuardShielded),
            UnitTrampled => nameof(UnitTrampled),
            VerveCharged => nameof(VerveCharged),
            UnitHealed => nameof(UnitHealed),
            VerveSpent => nameof(VerveSpent),
            ConsumableUsed => nameof(ConsumableUsed),
            DebrisPlaced => nameof(DebrisPlaced),
            FightWon => nameof(FightWon),
            FightLost => nameof(FightLost),
            ObjectiveDeclared => nameof(ObjectiveDeclared),
            StructureAttacked => nameof(StructureAttacked),
            StructureDamaged => nameof(StructureDamaged),
            StructureDestroyed => nameof(StructureDestroyed),
            ReinforcementScheduled => nameof(ReinforcementScheduled),
            ReinforcementArrived => nameof(ReinforcementArrived),
            ReinforcementDelayed => nameof(ReinforcementDelayed),
            _ => UnknownEvent,
        };

        /// <summary>Whether this formatter has a line for the given event type.</summary>
        /// <param name="evt">Event to test.</param>
        /// <returns>False when the event would fall through to the unknown branch.</returns>
        public static bool IsHandled(GameEvent evt) => EventName(evt) != UnknownEvent;

        /// <summary>
        /// The detail column: everything the payload carries, spelled out. "Down to the line" means a
        /// shove's tile-by-tile route is visible here, not just where it ended.
        /// </summary>
        /// <param name="evt">Event to describe.</param>
        /// <param name="state">Any state from the same fight, used only to name units.</param>
        /// <returns>A single-line, tab-free description.</returns>
        public static string Detail(GameEvent evt, GameState state) => evt switch
        {
            FightStarted e => "fight " + Number(e.FightNumber) + " " + Clean(e.Name),

            UnitDeployed e => e.Team + " " + UnitTemplate.For(e.Kind).Name + " placed at " + e.At,

            DeploymentCompleted => "every unit placed, battle opens",

            RoundStarted e => "round " + Number(e.Round) + " begins",

            RoundEnded e => "round " + Number(e.Round) + " ends",

            IntentDeclared e => Intent(e, state),

            ActivationStarted e => e.Team + " takes the activation slot",

            ActivationEnded e => e.Passed
                ? "activation ends with a half unspent"
                : "activation ends, move and action both spent",

            UnitMoved e => e.From + " -> " + e.To
                + " cost " + Number(e.Cost)
                + " via " + Route(e.Path),

            AbilityUsed e => e.Ability
                + " from " + e.At
                + (e.TargetId.HasValue ? " on " + Actor(state, e.TargetId.Value) : " with no unit target"),

            UnitAttacked e => "hits " + Actor(state, e.TargetId)
                + " " + e.From + " -> " + e.To
                + " for " + Number(e.Damage)
                + (e.FromHighGround ? $" (high ground +{Combat.HighGroundBonus})" : string.Empty),

            UnitDamaged e => "-" + Number(e.Amount)
                + (e.Overkill > 0 ? " (" + Number(e.Removed) + " taken, " + Number(e.Overkill) + " over)" : string.Empty)
                + " " + e.Source
                + " at " + e.At
                + ", hp " + Number(e.RemainingHp),

            UnitDowned e => "down at " + e.At + ", off the board",

            UnitPushed e => e.Kind == DisplacementKind.Throw
                ? "thrown " + Number(e.Distance) + " " + e.From + " -> " + e.To + ", over everything between"
                : e.Kind + " " + Number(e.Distance)
                    + " " + e.From + " -> " + e.To
                    + " via " + Route(e.Path),

            Collision e => "into "
                + (e.ObstacleId.HasValue ? Actor(state, e.ObstacleId.Value) : "terrain")
                + " at " + e.At
                + ", " + Number(e.Damage) + " damage"
                + (e.ObstacleId.HasValue ? " to both" : string.Empty),

            SpikeHit e => "spikes at " + e.At
                + ", " + Number(e.Damage) + " damage, "
                + (e.Voluntary ? "walked in" : "displaced onto"),

            Staggered => "staggered until end of round, next displacement +1",

            Rattled e => "rattled by " + Actor(state, e.ByUnitId)
                + ", " + e.ForFlock + "'s next displacement of it +"
                + Number(Techniques.RattledDistanceBonus),

            ChalkMarked e => "chalked by " + Actor(state, e.ByUnitId)
                + ", " + e.ForFlock + "'s next displacement of it +"
                + Number(Techniques.RattledDistanceBonus),

            // Named even when the tile it bought was eaten by resistance or a refusal: the feather is
            // spent by the attempt, and a player who saw nothing move is owed the reason (D-190).
            GreasedFeatherSpent e => "greased feather spent on " + Actor(state, e.TargetId)
                + ", displacement +" + Number(Consumables.GreaseDistanceBonus),

            HandOffGranted e => "handed off by " + Actor(state, e.ByUnitId)
                + ": next basic attack on " + Actor(state, e.AgainstId)
                + " gains push " + Number(Techniques.HandOffPush) + " if " + e.Owner + " takes it",

            StepBanked e => "offered the tile " + Actor(state, e.ByGuard) + " left at " + e.To
                + ", " + e.Owner + " to answer",

            CrossingShotFired e => "crossing shot from " + e.From + " at " + e.Crossing
                + ", " + Number(e.Damage) + " damage",

            Clinging e => "clinging to the lip of the pit at " + e.At,

            Rescued e => "hauled out by " + Actor(state, e.RescuerId) + " to " + e.To,

            Voided e => "voided at " + e.At + ", " + Clean(e.Reason) + ", gone for the run",

            FootingSpent e => "digs in, " + Number(e.Remaining) + " footing left",

            FootingChoiceRequested e => "is asked whether to refuse " + e.Kind.ToString().ToLowerInvariant()
                + " " + Number(e.Distance) + " at " + e.At + " for " + Number(e.Cost)
                + " of its " + Number(e.Held) + " footing",

            DisplacementRefused e => "refuses the " + e.Kind.ToString().ToLowerInvariant() + " outright at "
                + e.At + " — nothing lands — " + Number(e.Remaining) + " footing left",

            CastRefused e => "braces against the cast at " + e.At + " for " + Number(e.Cost)
                + " footing, " + Number(e.Remaining) + " left — the throw fails",

            GuardStanceChanged e => e.Active
                ? "takes guard stance at " + e.At + ", covering adjacent allies until its next activation"
                : "drops guard stance at " + e.At,

            UnitTrampled e => "shoulders through " + Actor(state, e.VictimId)
                + " at " + e.At + ", knocking it " + e.Aside + " for " + e.Damage,

            GuardShielded e => "steps in front of the structure at " + e.StructureAt
                + ", taking " + Actor(state, e.AttackerId) + "'s claw on " + e.At
                + " — the structure keeps " + e.Spared,

            GuardIntercepted e => "intercepts " + Actor(state, e.AttackerId)
                + " for " + Actor(state, e.AllyId)
                + " at " + e.AllyAt
                + ", it lands on " + e.At + " instead"
                + (e.Redirected > 0
                    ? " (" + Number(e.Redirected) + " spared, " + Number(Guard.Halve(e.Redirected)) + " taken)"
                    : string.Empty),

            VerveCharged e => (e.Wasted ? "+0 " : "+1 ") + Naming.MeterLower + " at "
                + e.At
                + " from " + Naming.Of(e.Source)
                + ", " + Number(e.NewTotal) + "/" + Number(Verve.Cap)
                + (e.Wasted ? ", full, discarded" : string.Empty),

            VerveSpent e => "spends " + Number(e.Cost) + " " + Naming.MeterLower
                + " on " + Naming.Of(e.Spend)
                + " at " + e.At
                + ", " + Number(e.Remaining) + " left",

            UnitHealed e => "patched up +" + Number(e.Amount) + " at " + e.At
                + ", hp " + Number(e.RemainingHp),

            ConsumableUsed e => "uses " + CampCatalogue.NameOf(e.Item) + " at " + e.At
                + (e.To.HasValue ? " on " + e.To.Value : string.Empty)
                + ", pocket empty",

            DebrisPlaced e => "drops debris on " + e.At + " with " + Number(e.Hp) + " hp",

            BramblesGrew e => "scatters brambles on " + e.At
                + ", until the end of round " + Number(e.ThroughRound),

            BramblesFaded e => "brambles on " + e.At + " die back to " + e.Now,

            FightWon e => "fight " + Number(e.FightNumber) + " won",

            FightLost e => "fight " + Number(e.FightNumber) + " lost, " + Clean(e.Reason),

            ObjectiveDeclared e => "objective " + Objective.KeywordFor(e.Kind)
                + (e.Tiles is not null && e.Tiles.Count > 0 ? " at " + Route(e.Tiles) : string.Empty)
                + (e.Rounds > 0 ? " on round " + Number(e.Rounds) : string.Empty)
                + (e.Hp > 0 ? ", structure " + Number(e.Hp) + " hp" : string.Empty)
                + (e.TurnLimit > 0 ? ", turn limit " + Number(e.TurnLimit) : ", no turn limit"),

            StructureAttacked e => "claws the structure at " + e.At
                + " from " + e.From
                + " for " + Number(e.Damage),

            StructureDamaged e => Objective.KeywordFor(e.Role) + " structure at " + e.At
                + " -" + Number(e.Amount)
                + " " + e.Source
                + ", hp " + Number(e.RemainingHp),

            StructureDestroyed e => Objective.KeywordFor(e.Role) + " structure at " + e.At
                + " is rubble, tile is clear",

            ReinforcementScheduled e => UnitTemplate.For(e.Kind).Name
                + " due on round " + Number(e.Round) + " at " + e.At,

            ReinforcementArrived e => UnitTemplate.For(e.Kind).Name
                + " arrives at " + e.At
                + (e.At == e.Scheduled ? " as scheduled" : ", slid from " + e.Scheduled),

            ReinforcementDelayed e => UnitTemplate.For(e.Kind).Name
                + " cannot land on " + e.At + ", waits for the next round",

            _ => "unhandled event type " + evt.GetType().Name,
        };

        /// <summary>
        /// Names a unit readably and unambiguously — <c>Vanguard [E] u5</c>. Three Husks look
        /// identical without the id.
        /// </summary>
        /// <param name="state">State to resolve the unit against.</param>
        /// <param name="id">Unit to name.</param>
        /// <returns>The unit's label, falling back to the bare id when it cannot be resolved.</returns>
        public static string Actor(GameState state, UnitId id)
        {
            if (id == UnitId.None)
            {
                return NoActor;
            }

            var unit = state?.FindUnit(id);
            return unit is null
                ? id.ToString()
                : unit.Name + " [" + TeamTag(unit.Team) + "] " + id;
        }

        /// <summary>The unit an event is primarily about.</summary>
        /// <param name="evt">Event to inspect.</param>
        /// <returns>The subject unit, or <see cref="UnitId.None"/> for fight- and round-level events.</returns>
        public static UnitId ActorOf(GameEvent evt) => evt switch
        {
            UnitDeployed e => e.UnitId,
            IntentDeclared e => e.Intent.UnitId,
            ActivationStarted e => e.UnitId,
            ActivationEnded e => e.UnitId,
            UnitMoved e => e.UnitId,
            AbilityUsed e => e.UnitId,
            UnitAttacked e => e.AttackerId,
            UnitDamaged e => e.UnitId,
            UnitDowned e => e.UnitId,
            UnitPushed e => e.UnitId,
            Collision e => e.UnitId,
            SpikeHit e => e.UnitId,
            Staggered e => e.UnitId,
            Rattled e => e.UnitId,
            ChalkMarked e => e.UnitId,
            BramblesGrew e => e.UnitId,
            GreasedFeatherSpent e => e.UnitId,
            HandOffGranted e => e.UnitId,
            StepBanked e => e.UnitId,
            CrossingShotFired e => e.ArcherId,
            Clinging e => e.UnitId,
            Rescued e => e.UnitId,
            Voided e => e.UnitId,
            FootingSpent e => e.UnitId,
            FootingChoiceRequested e => e.TargetId,
            DisplacementRefused e => e.TargetId,
            CastRefused e => e.TargetId,
            GuardStanceChanged e => e.UnitId,
            GuardIntercepted e => e.UnitId,
            GuardShielded e => e.UnitId,
            UnitTrampled e => e.UnitId,
            VerveCharged e => e.UnitId,
            UnitHealed e => e.UnitId,
            VerveSpent e => e.UnitId,
            ConsumableUsed e => e.UnitId,
            DebrisPlaced e => e.UnitId,
            StructureAttacked e => e.AttackerId,
            ReinforcementScheduled e => e.UnitId,
            ReinforcementArrived e => e.UnitId,
            ReinforcementDelayed e => e.UnitId,
            _ => UnitId.None,
        };

        /// <summary>The slot column: which activation the line belongs to, e.g. <c>PlayerA:u0</c>.</summary>
        /// <param name="team">Team holding the slot.</param>
        /// <param name="unitId">Unit mid-activation, or <c>null</c> between activations.</param>
        /// <returns>The slot label.</returns>
        public static string Slot(Team team, UnitId? unitId) =>
            team + ":" + (unitId.HasValue ? unitId.Value.ToString() : NoActor);

        /// <summary>Single-letter side label: "A", "B" or "E".</summary>
        /// <param name="team">Team to label.</param>
        /// <returns>The label.</returns>
        public static string TeamTag(Team team) => team switch
        {
            Team.PlayerA => "A",
            Team.PlayerB => "B",
            _ => "E",
        };

        /// <summary>Joins tiles into a comma-separated route, or "-" when nothing was entered.</summary>
        /// <param name="path">Tiles entered in order.</param>
        /// <returns>The route.</returns>
        public static string Route(IReadOnlyList<Coord> path)
        {
            if (path is null || path.Count == 0)
            {
                return NoActor;
            }

            var text = new StringBuilder();
            for (int i = 0; i < path.Count; i++)
            {
                if (i > 0)
                {
                    text.Append(',');
                }

                text.Append(path[i].ToString());
            }

            return text.ToString();
        }

        /// <summary>Strips anything that would break the column or line structure.</summary>
        /// <param name="value">Free text from an event payload.</param>
        /// <returns>The same text with tabs and newlines flattened to spaces.</returns>
        public static string Clean(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
        }

        // An intent is a telegraph, so the log spells the whole plan out: what it committed to, who
        // it committed to, where it walks and exactly what the shove would do.
        private static string Intent(IntentDeclared evt, GameState state)
        {
            var intent = evt.Intent;
            var text = new StringBuilder();

            text.Append(evt.Replanned ? "re-plans " : "plans ")
                .Append(intent.Action)
                .Append(" from ")
                .Append(intent.From);

            if (intent.TargetId.HasValue)
            {
                text.Append(" on ").Append(Actor(state, intent.TargetId.Value));
                if (intent.TargetPosition.HasValue)
                {
                    text.Append(" at ").Append(intent.TargetPosition.Value);
                }
            }
            else
            {
                text.Append(" with no target");
            }

            text.Append(intent.MoveTo.HasValue ? " move to " + intent.MoveTo.Value : " no move");

            // The shoulder is part of the walk and costs somebody a hit point, so it is logged with
            // the walk rather than left to be inferred from the events that follow (D-100).
            if (intent.TrampleVictim.HasValue && intent.TrampleAt.HasValue)
            {
                text.Append(" through ").Append(Actor(state, intent.TrampleVictim.Value))
                    .Append(" at ").Append(intent.TrampleAt.Value);

                if (intent.TrampleAside.HasValue)
                {
                    text.Append(" knocked ").Append(intent.TrampleAside.Value);
                }
            }

            if (intent.Displacement.HasValue)
            {
                text.Append(", ").Append(intent.Displacement.Value)
                    .Append(' ').Append(Number(intent.DisplacementDistance));

                if (intent.DisplacementDirection.HasValue)
                {
                    text.Append(' ').Append(intent.DisplacementDirection.Value);
                }

                if (intent.DisplacementTo.HasValue)
                {
                    text.Append(" to ").Append(intent.DisplacementTo.Value);
                }
            }

            if (intent.RedirectedTo.HasValue)
            {
                text.Append(", intercepted by ").Append(Actor(state, intent.RedirectedTo.Value));
            }

            text.Append(", damage ").Append(Number(intent.Damage));
            return text.ToString();
        }

        // Culture-invariant so the log is identical whichever machine renders it.
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
