using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// The enemy planner. Brief §2 gives each archetype an ordered priority list; this is that list
    /// and nothing else.
    /// </summary>
    /// <remarks>
    /// Every choice here is a pure function of <see cref="GameState"/>. There is no
    /// <see cref="IRng"/> anywhere in this file and there must never be one: enemy behaviour is part
    /// of what a seed plus a command log reproduces. Ties break on a fixed ladder — the criterion the
    /// brief names, then lowest unit id, then row-major coordinate order — so two runs of the same
    /// board cannot disagree.
    /// </remarks>
    public static class Ai
    {
        // The empty candidate list handed to a plan that has no clause about player units. Shared
        // so that planning one allocates nothing.
        private static readonly Unit[] NoUnits = new Unit[0];

        /// <summary>
        /// The single command this enemy will submit next. Callers apply it through
        /// <see cref="Game.Apply(GameState, Command)"/> like any other command, which is what keeps
        /// the command log a complete recording.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="enemy">Enemy holding the activation slot.</param>
        /// <returns>A legal command, or an <see cref="EndActivationCommand"/> when nothing is left to do.</returns>
        public static Command Plan(GameState state, Unit enemy)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (enemy is null)
            {
                throw new ArgumentNullException(nameof(enemy));
            }

            var end = new EndActivationCommand(enemy.Id);
            if (!enemy.IsOnBoard || enemy.Clinging)
            {
                return end;
            }

            // Brief §2: an adjacent enemy of a Clinging unit may finish it as a free action, "and
            // enemies with attacks will, per AI". It costs neither half, so it happens before the
            // priority list rather than instead of it. The free finish is a clause about player units,
            // and an archetype whose list has no clause about player units does not get one either:
            // a Raider walks past a clinging player exactly as it walks past a standing one (D-041).
            if (enemy.Template.Attack != AttackKind.None && !IgnoresUnits(enemy))
            {
                foreach (var clinging in state.Units)
                {
                    if (Pits.CanFinish(state, enemy, clinging))
                    {
                        return new FinishClingingCommand(enemy.Id, clinging.Id);
                    }
                }
            }

            var intent = Live(state, enemy);

            if (!enemy.HasMoved
                && intent.MoveTo.HasValue
                && intent.MoveTo.Value != enemy.Position
                && Movement.TryGetMove(state, enemy, intent.MoveTo.Value, out _))
            {
                return new MoveCommand(enemy.Id, intent.MoveTo.Value);
            }

            if (!enemy.HasActed && intent.TargetId.HasValue)
            {
                var target = state.FindUnit(intent.TargetId.Value);
                if (target is not null)
                {
                    switch (intent.Action)
                    {
                        // The planner has no player to ask which side to set an ally down on, so it
                        // takes the fixed-order default. Reproducible, which is what its rescues have
                        // to be (D-082).
                        case IntentAction.Rescue when !enemy.HasSpentMovement
                            && Pits.CanRescue(state, enemy, target)
                            && Pits.DefaultRescueDestination(state, enemy) is { } landing:
                            return new RescueCommand(enemy.Id, target.Id, landing);

                        case IntentAction.Attack when Combat.CanAttack(state, enemy, target, out _):
                            return new AttackCommand(enemy.Id, target.Id);

                        // The aim is read back off the telegraph rather than chosen again here. An
                        // intent that named one tile and resolved to another would be worse than the
                        // silent pick this replaces (MASTER_DESIGN §3, locked v), and the only way
                        // to be sure of that is for the declaration and the command to carry the
                        // same axis rather than two computations that agree today.
                        case IntentAction.Pull when Combat.CanPull(state, enemy, target):
                            return new AttackCommand(
                                enemy.Id, target.Id, AttackMode.Pull, intent.DisplacementAim);

                        case IntentAction.Push when Combat.CanPush(state, enemy, target):
                            return new AttackCommand(
                                enemy.Id, target.Id, AttackMode.Push, intent.DisplacementAim);
                    }
                }
            }

            return end;
        }

        /// <summary>
        /// Runs the archetype's priority list from scratch, ignoring any intent already on record.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="enemy">Enemy to plan for.</param>
        /// <returns>The plan it would declare right now.</returns>
        public static EnemyIntent Declare(GameState state, Unit enemy)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (enemy is null)
            {
                throw new ArgumentNullException(nameof(enemy));
            }

            return WithTrample(state, enemy, Compute(state, enemy, null));
        }

        /// <summary>
        /// Adds the trample a plan's walk would perform, when its route crosses a body it can shift.
        /// </summary>
        /// <remarks>
        /// Decorated onto the finished intent rather than threaded through the dozen places that
        /// build one. Every planner branch that produces a walk gets it for free, and a branch added
        /// later cannot forget to telegraph a hit it is about to deal — which is the failure mode
        /// this is guarding against, not the arithmetic.
        /// </remarks>
        private static EnemyIntent WithTrample(GameState state, Unit enemy, EnemyIntent intent)
        {
            if (intent.MoveTo is not { } destination || !enemy.Template.Tramples)
            {
                return intent;
            }

            if (!Movement.TryGetMove(state, enemy, destination, out var option))
            {
                return intent;
            }

            return Trample.FirstOnRoute(
                state, enemy, enemy.Position, option.Path, out var victim, out var at, out var aside)
                ? intent with { TrampleVictim = victim, TrampleAt = at, TrampleAside = aside }
                : intent;
        }

        /// <summary>The intent currently on record for a unit, if it has one.</summary>
        /// <param name="state">Current state.</param>
        /// <param name="unitId">Unit to look up.</param>
        /// <returns>Its declared intent, or <c>null</c>.</returns>
        public static EnemyIntent? IntentFor(GameState state, UnitId unitId)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            foreach (var intent in state.Intents)
            {
                if (intent.UnitId == unitId)
                {
                    return intent;
                }
            }

            return null;
        }

        /// <summary>
        /// Declares an intent for every enemy that can act. Brief §2 puts this at round start, before
        /// the first activation, so the players see the whole enemy round before committing to theirs.
        /// </summary>
        /// <param name="state">State at the top of the round.</param>
        /// <param name="events">Sink for the <see cref="IntentDeclared"/> events.</param>
        /// <returns>The state carrying the round's intents.</returns>
        public static GameState DeclareAll(GameState state, List<GameEvent> events)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (events is null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            var intents = new List<EnemyIntent>();
            foreach (var unit in state.Units)
            {
                if (unit.Team != Team.Enemy || !unit.IsOnBoard || unit.Clinging)
                {
                    continue;
                }

                var intent = Compute(state, unit, null);
                intents.Add(intent);
                events.Add(new IntentDeclared(intent, false));
            }

            return state with { Intents = intents };
        }

        /// <summary>
        /// Swaps in any second stat block that has just come due, then re-runs the priority list for
        /// any enemy whose declared target has died or been removed, and drops the intents of enemies
        /// that are no longer on the board.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Brief §2: an intent is locked and re-planned "only if its target becomes invalid". A target
        /// that merely moved does not trigger this — the enemy still chases the unit it named.
        /// </para>
        /// <para>
        /// A stat-block swap is the second thing that invalidates a plan, and for the same reason: the
        /// intent on the table was worked out by a unit with different numbers. It is announced as a
        /// re-declaration, exactly as a dead target is (D-040).
        /// </para>
        /// </remarks>
        /// <param name="state">State just after a command resolved.</param>
        /// <param name="events">Sink for any re-declaration events.</param>
        /// <returns>The state with its intents brought back in line.</returns>
        public static GameState ReplanInvalidated(GameState state, List<GameEvent> events)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (events is null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            state = SwapPhases(state, events);

            if (state.Intents.Count == 0)
            {
                return state;
            }

            bool live = state.Phase == Phase.Battle && state.Outcome == FightOutcome.InProgress;
            var updated = new List<EnemyIntent>(state.Intents.Count);
            bool changed = false;

            // A guard changes who a declared plan lands on without changing whom it names, so the
            // check only runs when a stance could possibly be in play (D-058). With nobody guarding
            // and no intent already redirected, this is exactly the behaviour it always was.
            bool guardsInPlay = Guard.AnyGuarding(state) || AnyRedirected(state);

            // The rescue slot opens and closes without the declared target changing at all: an ally
            // shoved into a pit during the players' half of the round outranks whatever the enemy
            // announced at round start. Gated on there being anything hanging off a lip, so a board
            // with no clinging unit on it plans bit-identically to before.
            bool lipsInPlay = AnyClinging(state);

            foreach (var intent in state.Intents)
            {
                var enemy = state.FindUnit(intent.UnitId);
                if (enemy is null || !enemy.IsOnBoard || enemy.Clinging)
                {
                    changed = true;
                    continue;
                }

                if (!intent.TargetId.HasValue || IsValidTarget(state, intent))
                {
                    bool replan = live && !enemy.HasActivated
                        && ((guardsInPlay && RedirectMoved(state, intent))
                            || (lipsInPlay && RescueChanged(state, intent, enemy)));

                    if (!replan)
                    {
                        updated.Add(intent);
                        continue;
                    }

                    // The target is unchanged; only who eats the blow is, or whether there is now
                    // somebody to haul out. Re-declare with the target still locked — a telegraph
                    // that lies is worse than no telegraph.
                    changed = true;
                    var refreshed = Compute(state, enemy, intent.TargetId);
                    updated.Add(refreshed);
                    events.Add(new IntentDeclared(refreshed, true));
                    continue;
                }

                changed = true;
                if (!live || enemy.HasActivated)
                {
                    continue;
                }

                var replacement = Compute(state, enemy, null);
                updated.Add(replacement);
                events.Add(new IntentDeclared(replacement, true));
            }

            return changed ? state with { Intents = updated } : state;
        }

        // True when the plan's standing on the rescue slot has changed since it was declared: an ally
        // has started clinging beside this enemy, or the ally it named has been hauled out, finished
        // off, or outranked by a kill that has walked into reach.
        private static bool RescueChanged(GameState state, EnemyIntent intent, Unit enemy) =>
            (intent.Action == IntentAction.Rescue) != (PlanRescue(state, enemy, intent.TargetId) is not null);

        private static bool AnyClinging(GameState state)
        {
            foreach (var unit in state.Units)
            {
                if (unit.Clinging && unit.IsOnBoard)
                {
                    return true;
                }
            }

            return false;
        }

        // True when the guard that would take this plan is no longer the one the plan was declared
        // against — a stance raised, dropped, or simply walked out of reach since the declaration.
        private static bool RedirectMoved(GameState state, EnemyIntent intent)
        {
            // Nobody steps in front of a unit hanging off a lip, so a rescue is never redirected.
            if (!intent.TargetId.HasValue || intent.Action == IntentAction.Rescue)
            {
                return false;
            }

            var guard = Guard.Interceptor(state, state.FindUnit(intent.TargetId.Value));
            return guard?.Id != intent.RedirectedTo;
        }

        private static bool AnyRedirected(GameState state)
        {
            foreach (var intent in state.Intents)
            {
                if (intent.RedirectedTo.HasValue)
                {
                    return true;
                }
            }

            return false;
        }

        // ---- phase swaps ------------------------------------------------------------------------

        // A two-phase archetype changes stat block the moment its hit points reach the threshold on
        // the template. The swap is a flag on the unit rather than a recomputation from hit points so
        // that it is a single, announced instant — and so a healed unit, if the game ever grows one,
        // does not un-enrage (D-040).
        private static GameState SwapPhases(GameState state, List<GameEvent> events)
        {
            List<UnitId>? swapping = null;

            foreach (var unit in state.Units)
            {
                var template = UnitTemplate.For(unit.Kind);
                if (template.Enraged is null || unit.Enraged || !unit.IsOnBoard || unit.Hp > template.EnrageAt)
                {
                    continue;
                }

                swapping ??= new List<UnitId>();
                swapping.Add(unit.Id);
            }

            if (swapping is null)
            {
                return state;
            }

            bool live = state.Phase == Phase.Battle && state.Outcome == FightOutcome.InProgress;

            foreach (var id in swapping)
            {
                state = state.WithUnit(state.UnitById(id) with { Enraged = true });

                var swapped = state.UnitById(id);
                if (!live || swapped.Team != Team.Enemy || swapped.Clinging || swapped.HasActivated)
                {
                    continue;
                }

                state = Redeclare(state, swapped, events);
            }

            return state;
        }

        // Replaces one enemy's intent in place and announces it as a re-plan. Adds nothing when the
        // enemy has no intent on the table — the round's declaration will read the new block anyway.
        private static GameState Redeclare(GameState state, Unit enemy, List<GameEvent> events)
        {
            var intents = new List<EnemyIntent>(state.Intents.Count);
            bool replaced = false;

            foreach (var existing in state.Intents)
            {
                if (existing.UnitId != enemy.Id)
                {
                    intents.Add(existing);
                    continue;
                }

                var intent = Compute(state, enemy, null);
                intents.Add(intent);
                events.Add(new IntentDeclared(intent, true));
                replaced = true;
            }

            return replaced ? state with { Intents = intents } : state;
        }

        // ---- planning -------------------------------------------------------------------------

        // The intent locks the target, not the route. Re-deriving the geometry each time keeps the
        // command legal after the players have shuffled the board underneath it (D-021).
        private static EnemyIntent Live(GameState state, Unit enemy)
        {
            var declared = IntentFor(state, enemy.Id);
            UnitId? locked = null;

            if (declared is not null
                && declared.TargetId.HasValue
                && IsValidTarget(state, declared))
            {
                locked = declared.TargetId.Value;
            }

            return WithTrample(state, enemy, Compute(state, enemy, locked));
        }

        private static EnemyIntent Compute(GameState state, Unit enemy, UnitId? locked)
        {
            // One slot on the front of every priority list: below a lethal attack on a player unit,
            // above everything the archetype would otherwise do. It is here rather than copied into
            // each Plan* method because it is the same clause for all of them — the Raider included,
            // whose silence is about players and not about its own side (D-045).
            var rescue = PlanRescue(state, enemy, locked);
            if (rescue is not null)
            {
                return rescue;
            }

            // Dispatch on the plan named by the stat block, not on the archetype: a Heavy Husk and a
            // Husk are the same priority list with different numbers, so there is only ever one copy
            // of each list (docs/ENEMY_ROSTER.md). The list is looked up rather than switched on, so
            // the registration that names the planner is the same registration that describes it and
            // the two cannot drift (component review, step 8). EnemyPlan.None — every player class —
            // has no registration and holds, exactly as the old switch's default did.
            var plan = EnemyPlanDefinition.ForPlan(enemy.Template.Plan);
            if (plan is null)
            {
                return Hold(enemy);
            }

            // A list with no clause about player units is planned before anybody asks who the player
            // units are. Running the candidate search first would make an empty board of players
            // silence an enemy that was never listening to them (D-041).
            if (plan.IgnoresPlayerUnits)
            {
                return plan.Planner(state, enemy, NoUnits, NoUnits);
            }

            var all = Candidates(state, enemy);
            if (all.Count == 0)
            {
                return Hold(enemy);
            }

            IReadOnlyList<Unit> choices = all;
            if (locked.HasValue)
            {
                foreach (var candidate in all)
                {
                    if (candidate.Id == locked.Value)
                    {
                        choices = new List<Unit> { candidate };
                        break;
                    }
                }
            }

            return plan.Planner(state, enemy, all, choices);
        }

        // The clinging ally this enemy would haul out, or null when it would not. Nothing in the
        // rescue rules is enemy-specific — Pits.CanRescue has always been team-agnostic and the
        // command has always been on offer — so this is only the choice, never the rule.
        private static EnemyIntent? PlanRescue(GameState state, Unit enemy, UnitId? locked)
        {
            // Brief §2: a rescue is the entire activation, both halves. An enemy that has already
            // spent either has no whole activation left to give.
            //
            // HasSpentMovement, not HasMoved: "has no movement left" and "has spent its movement"
            // are the same question only for a unit with legs. The Warden has Move 0, so HasMoved
            // was true for it before it did anything, and the slot every list carries was refused to
            // the one archetype built to stand still beside its friends.
            if (enemy.HasSpentMovement || enemy.HasActed)
            {
                return null;
            }

            Unit? pick = null;
            foreach (var unit in state.Units)
            {
                if (!Pits.CanRescue(state, enemy, unit))
                {
                    continue;
                }

                // A plan already declared against one ally keeps it, so the telegraph stays true
                // even when a lower id starts hanging off a lip in the meantime (D-061).
                if (locked.HasValue && unit.Id == locked.Value)
                {
                    pick = unit;
                    break;
                }

                if (pick is null || unit.Id.Value < pick.Id.Value)
                {
                    pick = unit;
                }
            }

            if (pick is null || Lethal(state, enemy))
            {
                return null;
            }

            return new EnemyIntent(
                enemy.Id, enemy.Kind, enemy.Position, IntentAction.Rescue,
                pick.Id, pick.Position, null, null, null, 0,
                Pits.DefaultRescueDestination(state, enemy), 0);
        }

        // Whether this enemy could put a player unit on 0 with its basic attack this activation —
        // from where it stands, or from any tile it could still walk to first (D-022). Only the
        // attack counts: an archetype that deals no damage can never have one, and a shove that
        // happens to kill is the board's doing rather than a blow the planner is choosing.
        private static bool Lethal(GameState state, Unit enemy)
        {
            var template = enemy.Template;
            if (template.Attack == AttackKind.None || template.Damage <= 0 || IgnoresUnits(enemy))
            {
                return false;
            }

            var candidates = Candidates(state, enemy);
            if (candidates.Count == 0)
            {
                return false;
            }

            int reach = template.Attack == AttackKind.Melee ? 1 : template.Range;
            var reachable = Movement.Reachable(state, enemy);

            foreach (var target in candidates)
            {
                // D-058: whoever would actually take the blow is who has to be killed by it.
                var victim = Guard.Interceptor(state, target) ?? target;

                if (Kills(state, enemy, enemy.Position, target, victim, reach))
                {
                    return true;
                }

                foreach (var pair in reachable)
                {
                    if (Kills(state, enemy, pair.Key, target, victim, reach))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool Kills(
            GameState state, Unit enemy, Coord from, Unit target, Unit victim, int reach)
        {
            if (from.DistanceTo(target.Position) > reach)
            {
                return false;
            }

            int damage = enemy.Template.Damage;
            if (enemy.Template.Attack == AttackKind.Ranged
                && state.Board.At(from) == TileType.HighGround)
            {
                damage += Combat.HighGroundBonus;
            }

            return Guard.Mitigate(state, victim.Id, damage, DamageSource.Attack) >= victim.Hp;
        }

        // docs/archive/CURATED_SET.md §5A, Raider: "1. Adjacent to the Protect structure → claw it; 2. else
        // path to it, Husk rules." That is the whole list. There is no third clause, no clause about
        // player units, and no self-defence clause: an enemy that ignores you is the entire point of
        // the archetype, and the pressure it applies is the clock of its walk (D-041).
        internal static EnemyIntent PlanRaider(
            GameState state, Unit enemy, IReadOnlyList<Unit> all, IReadOnlyList<Unit> choices)
        {
            var shrines = SiegeTiles(state);
            if (shrines.Count == 0)
            {
                // Nothing to besiege — a fallen shrine, or a board that never had one. It stands
                // still rather than inventing a target, and re-runs this list every activation.
                return Hold(enemy);
            }

            var reach = Adjoining(enemy.Position, shrines);
            if (reach.HasValue)
            {
                return Claw(enemy, reach.Value, null);
            }

            // The same breadth-first field every other archetype walks by, grown from the structure
            // tiles instead of from a unit (D-029). The structure blocks its own tile, so the tiles
            // that measure 1 are exactly the ring the Raider is trying to reach.
            var field = PathField.ToAnyOf(state, enemy, shrines);
            var destination = BestTile(
                state, enemy, coord => new Score(field.At(coord), NearestTileDistance(coord, shrines)));
            var moveTo = destination == enemy.Position ? (Coord?)null : destination;

            var arrival = Adjoining(destination, shrines);
            if (arrival.HasValue)
            {
                return Claw(enemy, arrival.Value, moveTo);
            }

            return March(enemy, NearestTile(enemy.Position, shrines), moveTo);
        }

        // docs/archive/CURATED_SET.md §5B, Quarry King: the melee list with a Bull Rush branch on the front of
        // it — the player's own opener, aimed back. Which branches exist is read off the stat block
        // rather than off the archetype: the enraged block is the one carrying a standalone shove, so
        // the phase swap adds the branch without there being two lists to keep in step (D-040).
        internal static EnemyIntent PlanQuarryKing(
            GameState state, Unit enemy, IReadOnlyList<Unit> all, IReadOnlyList<Unit> choices)
        {
            if (enemy.Template.BasicPush > 0)
            {
                var rush = PlanRush(state, enemy, choices);
                if (rush is not null)
                {
                    return rush;
                }
            }

            return PlanMelee(state, enemy, all, choices);
        }

        // Bull Rush as the Vanguard has it: a straight run of up to Move tiles that stops adjacent to
        // the first player unit on the line, then a shove. A run of zero tiles is legal and is exactly
        // what the ability does against something already adjacent. It is only taken when the shove
        // beats the swing it replaces — a plain push across open ground is worth less than 3 damage,
        // so he punches; a push into a body, or over a lip, is worth more, so he charges.
        private static EnemyIntent? PlanRush(GameState state, Unit enemy, IReadOnlyList<Unit> choices)
        {
            var reachable = Movement.Reachable(state, enemy);
            var board = state.Board;
            int distance = enemy.Template.BasicPush;

            Unit? best = null;
            Coord? bestMove = null;
            int bestScore = enemy.Template.Damage;

            foreach (var direction in Directions.All)
            {
                var position = enemy.Position;

                for (int step = 0; step <= enemy.Move; step++)
                {
                    if (position != enemy.Position && !reachable.ContainsKey(position))
                    {
                        break;
                    }

                    var next = position.Step(direction);
                    if (!board.InBounds(next) || state.StructureAt(next) is not null)
                    {
                        break;
                    }

                    var occupant = state.UnitAt(next);
                    if (occupant is not null)
                    {
                        if (Holds(choices, occupant.Id))
                        {
                            var from = position == enemy.Position ? (Coord?)null : position;
                            int score = RushScore(state, enemy, occupant, from, distance);
                            if (score > bestScore)
                            {
                                bestScore = score;
                                best = occupant;
                                bestMove = from;
                            }
                        }

                        break;
                    }

                    var tile = board.At(next);
                    if (!Movement.IsWalkable(tile) || tile == TileType.HighGround)
                    {
                        break;
                    }

                    position = next;
                }
            }

            return best is null
                ? null
                : Displace(state, enemy, best, bestMove, DisplacementKind.Push, distance);
        }

        // What a charge is worth, in hit points: what the shove deals to the target, what it deals to
        // whatever it slams the target into, and a large bonus for the two endings a swing cannot
        // produce. Compared against the archetype's own attack damage, so the branch turns itself off
        // whenever punching is simply better. Integers only — Core does no float maths (Brief §1).
        private static int RushScore(
            GameState state, Unit enemy, Unit target, Coord? moveTo, int distance)
        {
            var from = moveTo ?? enemy.Position;
            var view = moveTo.HasValue ? state.WithUnit(enemy with { Position = moveTo.Value }) : state;
            var preview = Displacement.PreviewAuto(view, target.Id, from, DisplacementKind.Push, distance);

            // Distance 0 is the only true no-op: anything that travels at all either moves the target
            // or collides, and a collision that leaves the target where it stood is still 2 apiece.
            if (preview.EffectiveDistance <= 0)
            {
                return 0;
            }

            int score = preview.DamageToUnit + preview.DamageToObstacle;
            if (preview.WouldCling)
            {
                score += 100;
            }

            if (preview.WouldDown)
            {
                score += 50;
            }

            return score;
        }

        // Brief §2, Husk: "1. Adjacent player unit → attack. 2. Else move toward nearest player unit."
        // The Anchor's list is the same shape with Move 1 and 2 damage.
        internal static EnemyIntent PlanMelee(
            GameState state, Unit enemy, IReadOnlyList<Unit> all, IReadOnlyList<Unit> choices)
        {
            var adjacent = FirstAdjacent(enemy.Position, choices);
            if (adjacent is not null)
            {
                return Strike(state, enemy, adjacent, null);
            }

            var target = Nearest(enemy.Position, choices);
            if (target is null)
            {
                return Hold(enemy);
            }

            var destination = ClosingTile(state, enemy, target.Position);
            var moveTo = destination == enemy.Position ? (Coord?)null : destination;

            // An activation is Move plus one Action, so a walk that ends in reach still swings (D-022).
            return destination.IsAdjacentTo(target.Position)
                ? Strike(state, enemy, target, moveTo)
                : Advance(enemy, target, moveTo);
        }

        // Brief §2, Lobber: "1. Player unit in range 3 and no player unit adjacent → ranged attack.
        // 2. Player adjacent → move away (maximize distance). 3. Else advance to range."
        internal static EnemyIntent PlanLobber(
            GameState state, Unit enemy, IReadOnlyList<Unit> all, IReadOnlyList<Unit> choices)
        {
            int range = enemy.Template.Range;

            if (FirstAdjacent(enemy.Position, all) is not null)
            {
                return BreakContact(state, enemy, all, choices, range);
            }

            var shot = NearestWithin(enemy.Position, choices, range);
            if (shot is not null)
            {
                return Strike(state, enemy, shot, null);
            }

            var target = Nearest(enemy.Position, choices);
            if (target is null)
            {
                return Hold(enemy);
            }

            // "Advance to range" is a band, not a beeline: the Lobber wants to be inside range 3 and
            // outside melee, so it never walks itself into the front line (D-023).
            var destination = ApproachTile(state, enemy, target.Position, BandLow(enemy), range);
            var step = destination == enemy.Position ? (Coord?)null : destination;

            if (FirstAdjacent(destination, all) is null)
            {
                var arrived = NearestWithin(destination, choices, range);
                if (arrived is not null)
                {
                    return Strike(state, enemy, arrived, step);
                }
            }

            return Advance(enemy, target, step);
        }

        // The retreat clause the ranged archetypes share: get out of contact, and shoot after moving
        // if the retreat actually broke it. Factored out so the Perch runs the identical rule.
        private static EnemyIntent BreakContact(
            GameState state, Unit enemy, IReadOnlyList<Unit> all, IReadOnlyList<Unit> choices, int range)
        {
            var away = BestTile(state, enemy, coord => -Spread(coord, all));
            var moveTo = away == enemy.Position ? (Coord?)null : away;

            if (FirstAdjacent(away, all) is null)
            {
                var opening = NearestWithin(away, choices, range);
                if (opening is not null)
                {
                    return Strike(state, enemy, opening, moveTo);
                }
            }

            var fled = FirstAdjacent(enemy.Position, choices) ?? Nearest(enemy.Position, choices);
            return new EnemyIntent(
                enemy.Id, enemy.Kind, enemy.Position, IntentAction.Retreat,
                fled?.Id, fled?.Position, moveTo, null, null, 0, null, 0);
        }

        // docs/ENEMY_ROSTER.md, Warden: "1. Attack anything adjacent. 2. Otherwise nothing — Move 0."
        // There is deliberately no closing branch: a Warden is a door, and a door does not chase.
        internal static EnemyIntent PlanWarden(
            GameState state, Unit enemy, IReadOnlyList<Unit> all, IReadOnlyList<Unit> choices)
        {
            var adjacent = FirstAdjacent(enemy.Position, choices);
            return adjacent is not null ? Strike(state, enemy, adjacent, null) : Hold(enemy);
        }

        // docs/ENEMY_ROSTER.md, Perch: "1. Shoot from where it stands. 2. Adjacent → break contact.
        // 3. Else climb to the nearest HighGround it can reach. 4. On a ledge, hold it; off one,
        // advance to the Lobber's band." Strike already adds the +1 for a shot fired from HighGround,
        // so the elevation bonus needs no special case here.
        internal static EnemyIntent PlanPerch(
            GameState state, Unit enemy, IReadOnlyList<Unit> all, IReadOnlyList<Unit> choices)
        {
            int range = enemy.Template.Range;

            if (FirstAdjacent(enemy.Position, all) is not null)
            {
                return BreakContact(state, enemy, all, choices, range);
            }

            var shot = NearestWithin(enemy.Position, choices, range);
            if (shot is not null)
            {
                return Strike(state, enemy, shot, null);
            }

            // A Perch that has taken a ledge never comes down of its own accord — holding it is the
            // whole archetype. Coming off it is something the players have to do to it.
            if (state.Board.At(enemy.Position) == TileType.HighGround)
            {
                return Hold(enemy);
            }

            var ledges = TilesOfType(state, TileType.HighGround);
            if (ledges.Count > 0)
            {
                var field = PathField.ToAnyOf(state, enemy, ledges);
                var climb = BestTile(state, enemy, coord => new Score(field.At(coord), 0));

                if (climb != enemy.Position)
                {
                    if (FirstAdjacent(climb, all) is null)
                    {
                        var arrived = NearestWithin(climb, choices, range);
                        if (arrived is not null)
                        {
                            return Strike(state, enemy, arrived, climb);
                        }
                    }

                    var pick = Nearest(enemy.Position, choices);
                    return pick is null ? Hold(enemy) : Advance(enemy, pick, climb);
                }
            }

            return PlanLobber(state, enemy, all, choices);
        }

        // docs/ENEMY_ROSTER.md, Harrier: the Stalker's flank-and-shove loop with separation in place
        // of hazards. It scores a shove by how much further from its nearest ally the target lands, so
        // it pulls a party apart instead of executing anyone. A shove that does not move the target —
        // a wall, a body, an unclimbable ledge — is worth nothing to it and never chosen, which is
        // what keeps it off the board edge the Stalker lives on.
        internal static EnemyIntent PlanHarrier(
            GameState state, Unit enemy, IReadOnlyList<Unit> all, IReadOnlyList<Unit> choices)
        {
            var reachable = Movement.Reachable(state, enemy);
            int distance = enemy.Template.BasicPush;

            Unit? best = null;
            Coord? bestMove = null;
            int bestGain = 0;

            foreach (var target in choices)
            {
                int before = AllyDistance(state, target, target.Position);

                foreach (var direction in Directions.All)
                {
                    var flank = target.Position.Step(direction.Opposite());
                    Coord? moveTo;
                    if (flank == enemy.Position)
                    {
                        moveTo = null;
                    }
                    else if (reachable.ContainsKey(flank))
                    {
                        moveTo = flank;
                    }
                    else
                    {
                        continue;
                    }

                    var from = moveTo ?? enemy.Position;
                    var view = moveTo.HasValue
                        ? state.WithUnit(enemy with { Position = moveTo.Value })
                        : state;
                    var preview = Displacement.PreviewAuto(
                        view, target.Id, from, DisplacementKind.Push, distance);

                    if (preview.Destination == target.Position)
                    {
                        continue;
                    }

                    // A target with nobody left to be separated from still gets shoved: any tile it
                    // did not choose is disruption, which is the whole point of the archetype.
                    int gain = before < 0 ? 1 : AllyDistance(state, target, preview.Destination) - before;
                    if (gain > bestGain)
                    {
                        bestGain = gain;
                        best = target;
                        bestMove = moveTo;
                    }
                }
            }

            if (best is not null)
            {
                return Displace(state, enemy, best, bestMove, DisplacementKind.Push, distance);
            }

            var nearest = Nearest(enemy.Position, choices);
            if (nearest is null)
            {
                return Hold(enemy);
            }

            var closing = ClosingTile(state, enemy, nearest.Position);
            return Advance(enemy, nearest, closing == enemy.Position ? (Coord?)null : closing);
        }

        // Brief §2, Grappler: "1. Player unit within range 3 → Pull 2 toward self, preferring
        // (a) units on HighGround, (b) Archer. 2. Else advance toward the Archer, else nearest."
        internal static EnemyIntent PlanGrappler(
            GameState state, Unit enemy, IReadOnlyList<Unit> all, IReadOnlyList<Unit> choices)
        {
            int range = enemy.Template.Range;
            int distance = enemy.Template.BasicPull;

            var grab = PickGrab(state, enemy.Position, choices, range);
            if (grab is not null)
            {
                return Displace(state, enemy, grab, null, DisplacementKind.Pull, distance);
            }

            var target = FirstOfKind(choices, UnitKind.Archer) ?? Nearest(enemy.Position, choices);
            if (target is null)
            {
                return Hold(enemy);
            }

            var destination = ApproachTile(state, enemy, target.Position, BandLow(enemy), range);
            var moveTo = destination == enemy.Position ? (Coord?)null : destination;

            var arrived = PickGrab(state, destination, choices, range);
            return arrived is not null
                ? Displace(state, enemy, arrived, moveTo, DisplacementKind.Pull, distance)
                : Advance(enemy, target, moveTo);
        }

        // Brief §2, Stalker: "1. Player unit adjacent to Pit/Spikes/edge and reachable → move to
        // flank, Push 1 toward the hazard. 2. Else move toward nearest player unit that is within 2
        // of a hazard; else hold position."
        internal static EnemyIntent PlanStalker(
            GameState state, Unit enemy, IReadOnlyList<Unit> all, IReadOnlyList<Unit> choices)
        {
            var reachable = Movement.Reachable(state, enemy);

            // How far down the ladder this shover is willing to go is a number on the stat block: the
            // Stalker takes all three tiers, a Blunted Stalker stops at spikes and never trades on the
            // board edge that is always available (docs/ENEMY_ROSTER.md).
            int maxRank = enemy.Template.HazardRanks - 1;
            if (maxRank > HazardRankEdge)
            {
                maxRank = HazardRankEdge;
            }

            bool edgeCounts = maxRank >= HazardRankEdge;

            // Hazards are ranked pit, then spikes, then a solid edge — a pit ends the fight for the
            // unit it swallows, so it outranks 3 damage, which outranks 2 (D-024).
            for (int rank = 0; rank <= maxRank; rank++)
            {
                foreach (var target in choices)
                {
                    foreach (var direction in Directions.All)
                    {
                        var hazard = target.Position.Step(direction);
                        if (HazardRank(state, hazard) != rank)
                        {
                            continue;
                        }

                        var flank = target.Position.Step(direction.Opposite());
                        Coord? moveTo;
                        if (flank == enemy.Position)
                        {
                            moveTo = null;
                        }
                        else if (reachable.ContainsKey(flank))
                        {
                            moveTo = flank;
                        }
                        else
                        {
                            continue;
                        }

                        return Displace(
                            state, enemy, target, moveTo, DisplacementKind.Push, enemy.Template.BasicPush);
                    }
                }
            }

            Unit? loiterer = null;
            int best = int.MaxValue;
            foreach (var candidate in choices)
            {
                if (HazardDistance(state, candidate.Position, edgeCounts) > 2)
                {
                    continue;
                }

                int distance = enemy.Position.DistanceTo(candidate.Position);
                if (distance < best)
                {
                    best = distance;
                    loiterer = candidate;
                }
            }

            if (loiterer is null)
            {
                return Hold(enemy);
            }

            var destination = ClosingTile(state, enemy, loiterer.Position);
            return Advance(enemy, loiterer, destination == enemy.Position ? (Coord?)null : destination);
        }

        // The escort duckling: a neutral that is not trying to fight (component review, step 8's
        // proof that a new priority list costs one registration and one planner). 1. Break away from
        // the nearest hostile. 2. Otherwise hold. It reads no terrain and names no victim, so there
        // is nothing here for a hazard or a structure to change.
        internal static EnemyIntent PlanEscort(
            GameState state, Unit enemy, IReadOnlyList<Unit> all, IReadOnlyList<Unit> choices)
        {
            // The kiter's retreat scoring, unchanged: furthest from the nearest hostile, ties broken
            // by the greater total distance to all of them. Standing still is always a candidate and
            // wins every tie on cost, so a duckling with nowhere better to be does not shuffle.
            var away = BestTile(state, enemy, coord => -Spread(coord, all));
            if (away == enemy.Position)
            {
                return Hold(enemy);
            }

            var fled = FirstAdjacent(enemy.Position, choices) ?? Nearest(enemy.Position, choices);
            return new EnemyIntent(
                enemy.Id, enemy.Kind, enemy.Position, IntentAction.Retreat,
                fled?.Id, fled?.Position, away, null, null, 0, null, 0);
        }

        // ---- intent construction --------------------------------------------------------------

        private static EnemyIntent Hold(Unit enemy) => new EnemyIntent(
            enemy.Id, enemy.Kind, enemy.Position, IntentAction.Hold,
            null, null, null, null, null, 0, null, 0);

        // A plan aimed at a structure rather than at a unit. The telegraph carries the tile instead of
        // a target id, which is a shape the intent record already supports — a Lobber that cannot find
        // anyone to flee from declares a Retreat with no target in exactly the same way.
        private static EnemyIntent Claw(Unit enemy, Coord structure, Coord? moveTo) => new EnemyIntent(
            enemy.Id, enemy.Kind, enemy.Position, IntentAction.Attack,
            null, structure, moveTo, null, null, 0, null, enemy.Template.Damage);

        private static EnemyIntent March(Unit enemy, Coord? structure, Coord? moveTo) => new EnemyIntent(
            enemy.Id, enemy.Kind, enemy.Position,
            moveTo.HasValue ? IntentAction.Advance : IntentAction.Hold,
            null, structure, moveTo, null, null, 0, null, 0);

        private static EnemyIntent Advance(Unit enemy, Unit target, Coord? moveTo) => new EnemyIntent(
            enemy.Id, enemy.Kind, enemy.Position, moveTo.HasValue ? IntentAction.Advance : IntentAction.Hold,
            target.Id, target.Position, moveTo, null, null, 0, null, 0);

        private static EnemyIntent Strike(GameState state, Unit enemy, Unit target, Coord? moveTo)
        {
            var from = moveTo ?? enemy.Position;
            var template = enemy.Template;

            int damage = template.Damage;
            if (template.Attack == AttackKind.Ranged && state.Board.At(from) == TileType.HighGround)
            {
                damage += Combat.HighGroundBonus;
            }

            // D-058: a guard beside the target takes this instead, halved. The telegraph says so,
            // and says how much, because the resolution is going to do exactly that.
            var guard = Guard.Interceptor(state, target);
            var victimId = guard?.Id ?? target.Id;
            damage = Guard.Mitigate(state, victimId, damage, DamageSource.Attack);

            return new EnemyIntent(
                enemy.Id, enemy.Kind, enemy.Position, IntentAction.Attack,
                target.Id, target.Position, moveTo, null, null, 0, null, damage, guard?.Id);
        }

        /// <summary>
        /// Which of two tiles an enemy sends a body to when the vector is diagonal.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The published priority order and nothing else — no new AI inputs, no randomness
        /// (MASTER_DESIGN §3, locked v). It is the same order <see cref="RushScore"/> already
        /// publishes for choosing whom to shove: a sweep first, then a kill, then hit points; a tie
        /// falls back on the fixed direction order, which is the first candidate.
        /// </para>
        /// <para>
        /// Deliberately not a new judgement about position. Anything cleverer would need inputs the
        /// enemy does not have, and would make the telegraph harder to read for no gain the player
        /// can see.
        /// </para>
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="targetId">Unit that would be displaced.</param>
        /// <param name="from">Tile the displacement originates from.</param>
        /// <param name="kind">Push or Pull.</param>
        /// <param name="distance">Requested distance.</param>
        /// <returns>The aim the enemy declares and resolves with.</returns>
        internal static DisplacementAim PreferredAim(
            GameState state, UnitId targetId, Coord from, DisplacementKind kind, int distance)
        {
            var candidates = Displacement.Candidates(state, targetId, from, kind, distance);
            if (candidates.Count < 2)
            {
                return DisplacementAim.Default;
            }

            // Strictly greater, so the fixed-order candidate keeps the tie: the enemy's pick has to
            // be reproducible from the board alone, and "whichever we looked at first" is not.
            return AimScore(candidates[1]) > AimScore(candidates[0])
                ? candidates[1].Aim
                : candidates[0].Aim;
        }

        private static int AimScore(DisplacementPreview preview)
        {
            int score = preview.DamageToUnit + preview.DamageToObstacle + preview.DamageToStructure;

            if (preview.WouldCling)
            {
                score += 100;
            }

            if (preview.WouldDown)
            {
                score += 50;
            }

            return score;
        }

        private static EnemyIntent Displace(
            GameState state,
            Unit enemy,
            Unit target,
            Coord? moveTo,
            DisplacementKind kind,
            int distance)
        {
            var from = moveTo ?? enemy.Position;

            // Project from where the enemy will be standing, not where it is now, so the telegraph
            // matches what the shove actually does.
            var view = moveTo.HasValue ? state.WithUnit(enemy with { Position = moveTo.Value }) : state;

            // D-058: and project onto whoever will actually travel. A guard beside the target takes
            // the shove on its own tile, with its own resistance, so the direction, the distance and
            // the destination the telegraph carries are the guard's, not the target's.
            var guard = Guard.Interceptor(view, target);
            var victimId = guard?.Id ?? target.Id;

            // The declared tile is the one the priority order picks, not the one a fixed direction
            // order happens to reach first (MASTER_DESIGN §3, locked v).
            var aim = PreferredAim(
                view, victimId, Guard.SourceFor(view, from, target, victimId, kind), kind, distance);

            var preview = Guard.PreviewAimed(view, from, target, victimId, kind, distance, aim);

            return new EnemyIntent(
                enemy.Id,
                enemy.Kind,
                enemy.Position,
                kind == DisplacementKind.Push ? IntentAction.Push : IntentAction.Pull,
                target.Id,
                target.Position,
                moveTo,
                kind,
                preview.Direction,
                preview.EffectiveDistance,
                preview.Destination,
                0,
                guard?.Id,
                DisplacementAim: aim);
        }

        // ---- target selection -----------------------------------------------------------------

        // Clinging units are not targeted by the priority lists: they cannot be reached by an ordinary
        // attack line and the free finish already handles them (D-025).
        private static List<Unit> Candidates(GameState state, Unit enemy)
        {
            var list = new List<Unit>();
            foreach (var unit in state.Units)
            {
                if (unit.IsOnBoard && !unit.Clinging && enemy.Team.IsHostileTo(unit.Team))
                {
                    list.Add(unit);
                }
            }

            return list;
        }

        // True for an archetype whose priority list contains no clause about player units at all.
        // A flag would be a second way of saying what the plan already says (D-032, D-041).
        // The closest tile a ranged plan is willing to fight from, off the plan's own parameters
        // rather than a literal repeated in two planners. The far end of the band is the stat
        // block's Range and is never restated anywhere (D-023).
        private static int BandLow(Unit enemy) =>
            EnemyPlanDefinition.For(enemy.Template.Plan).Parameters.PreferredRangeLow;

        private static bool IgnoresUnits(Unit enemy) =>
            EnemyPlanDefinition.ForPlan(enemy.Template.Plan)?.IgnoresPlayerUnits == true;

        private static bool Holds(IReadOnlyList<Unit> units, UnitId id)
        {
            foreach (var unit in units)
            {
                if (unit.Id == id)
                {
                    return true;
                }
            }

            return false;
        }

        // The tiles a Raider is here for: standing Protect structures. The same test Objectives.Besiege
        // runs, so the thing it walks toward is exactly the thing it claws when it gets there — a
        // Destroy structure is the players' problem to bring down and is not a Raider's business.
        private static List<Coord> SiegeTiles(GameState state)
        {
            var tiles = new List<Coord>();
            foreach (var structure in state.Structures)
            {
                if (structure.IsStanding && structure.IsSiegeTarget)
                {
                    tiles.Add(structure.At);
                }
            }

            return tiles;
        }

        private static Coord? Adjoining(Coord from, List<Coord> tiles)
        {
            foreach (var tile in tiles)
            {
                if (from.IsAdjacentTo(tile))
                {
                    return tile;
                }
            }

            return null;
        }

        private static Coord? NearestTile(Coord from, List<Coord> tiles)
        {
            Coord? best = null;
            int bestDistance = int.MaxValue;

            foreach (var tile in tiles)
            {
                int distance = from.DistanceTo(tile);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = tile;
                }
            }

            return best;
        }

        private static int NearestTileDistance(Coord from, List<Coord> tiles)
        {
            int best = int.MaxValue;
            foreach (var tile in tiles)
            {
                int distance = from.DistanceTo(tile);
                if (distance < best)
                {
                    best = distance;
                }
            }

            return best;
        }

        private static bool IsValidTarget(GameState state, UnitId id)
        {
            var unit = state.FindUnit(id);
            return unit is not null && unit.IsOnBoard && !unit.Clinging;
        }

        // A rescue names a unit that is clinging, which is exactly what an attack plan calls invalid.
        // The lock is the same idea read the other way round: the plan holds while the ally is still
        // hanging there and this enemy can still reach it.
        private static bool IsValidTarget(GameState state, EnemyIntent intent)
        {
            var id = intent.TargetId!.Value;
            if (intent.Action != IntentAction.Rescue)
            {
                return IsValidTarget(state, id);
            }

            var rescuer = state.FindUnit(intent.UnitId);
            var clinging = state.FindUnit(id);
            return rescuer is not null && clinging is not null
                && Pits.CanRescue(state, rescuer, clinging);
        }

        private static Unit? FirstAdjacent(Coord from, IReadOnlyList<Unit> units)
        {
            foreach (var unit in units)
            {
                if (from.IsAdjacentTo(unit.Position))
                {
                    return unit;
                }
            }

            return null;
        }

        private static Unit? Nearest(Coord from, IReadOnlyList<Unit> units)
        {
            Unit? best = null;
            int bestDistance = int.MaxValue;

            foreach (var unit in units)
            {
                int distance = from.DistanceTo(unit.Position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = unit;
                }
            }

            return best;
        }

        private static Unit? NearestWithin(Coord from, IReadOnlyList<Unit> units, int range)
        {
            Unit? best = null;
            int bestDistance = int.MaxValue;

            foreach (var unit in units)
            {
                int distance = from.DistanceTo(unit.Position);
                if (distance == 0 || distance > range || distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                best = unit;
            }

            return best;
        }

        private static Unit? FirstOfKind(IReadOnlyList<Unit> units, UnitKind kind)
        {
            foreach (var unit in units)
            {
                if (unit.Kind == kind)
                {
                    return unit;
                }
            }

            return null;
        }

        // Brief §2 ranks the Grappler's picks: units on HighGround first, then the Archer, then
        // whoever is left. A target already adjacent has nowhere to be pulled to (D-020).
        private static Unit? PickGrab(GameState state, Coord from, IReadOnlyList<Unit> choices, int range)
        {
            Unit? best = null;
            int bestTier = int.MaxValue;

            foreach (var candidate in choices)
            {
                int distance = from.DistanceTo(candidate.Position);
                if (distance < 2 || distance > range)
                {
                    continue;
                }

                int tier = state.Board.At(candidate.Position) == TileType.HighGround
                    ? 0
                    : candidate.Kind == UnitKind.Archer ? 1 : 2;

                if (tier < bestTier)
                {
                    bestTier = tier;
                    best = candidate;
                }
            }

            return best;
        }

        // ---- geometry -------------------------------------------------------------------------

        private const int HazardRankPit = 0;
        private const int HazardRankSpikes = 1;
        private const int HazardRankEdge = 2;

        // Brief §2 names "Pit/Spikes/board edge"; a wall sits with the edge because the brief also
        // says the board edge acts as a wall, so both produce the identical Displacement.CollisionDamage.
        private static int HazardRank(GameState state, Coord tile)
        {
            if (!state.Board.InBounds(tile))
            {
                return HazardRankEdge;
            }

            var terrain = state.Board.At(tile);
            if (terrain == TileType.Wall)
            {
                return HazardRankEdge;
            }

            // A unit standing in the way turns the shove into an ordinary collision, so the hazard
            // is not actually on offer.
            if (state.IsOccupied(tile))
            {
                return int.MaxValue;
            }

            if (terrain == TileType.Pit)
            {
                return HazardRankPit;
            }

            return terrain == TileType.Spikes ? HazardRankSpikes : int.MaxValue;
        }

        private static int HazardDistance(GameState state, Coord from, bool edgeCounts)
        {
            var board = state.Board;
            int best = int.MaxValue;

            if (edgeCounts)
            {
                // Off-board is one step past the outermost tile in each direction.
                best = from.X + 1;
                best = Math.Min(best, from.Y + 1);
                best = Math.Min(best, board.Width - from.X);
                best = Math.Min(best, board.Height - from.Y);
            }

            foreach (var coord in board.AllCoords())
            {
                var terrain = board.At(coord);
                if (terrain != TileType.Pit && terrain != TileType.Spikes
                    && (terrain != TileType.Wall || !edgeCounts))
                {
                    continue;
                }

                int distance = from.DistanceTo(coord);
                if (distance < best)
                {
                    best = distance;
                }
            }

            return best;
        }

        private static List<Coord> TilesOfType(GameState state, TileType terrain)
        {
            var tiles = new List<Coord>();
            foreach (var coord in state.Board.AllCoords())
            {
                if (state.Board.At(coord) == terrain)
                {
                    tiles.Add(coord);
                }
            }

            return tiles;
        }

        // How isolated a unit would be standing on a tile: the distance to the nearest other unit on
        // its own side, or -1 when it has none left to be separated from.
        private static int AllyDistance(GameState state, Unit unit, Coord at)
        {
            int best = -1;

            foreach (var other in state.Units)
            {
                if (other.Id == unit.Id || other.Team != unit.Team || !other.IsOnBoard || other.Clinging)
                {
                    continue;
                }

                int distance = at.DistanceTo(other.Position);
                if (best < 0 || distance < best)
                {
                    best = distance;
                }
            }

            return best;
        }

        private static int Spread(Coord from, IReadOnlyList<Unit> units)
        {
            int nearest = int.MaxValue;
            int total = 0;

            foreach (var unit in units)
            {
                int distance = from.DistanceTo(unit.Position);
                total += distance;
                if (distance < nearest)
                {
                    nearest = distance;
                }
            }

            return nearest == int.MaxValue ? 0 : (nearest * 100) + total;
        }

        // How badly a distance sits outside the band a ranged archetype wants to fight from. Being
        // too close is penalised twice as hard as being too far: closing again is one move, but
        // standing in melee is where a Lobber dies.
        private static int Band(int distance, int low, int high)
        {
            if (distance < low)
            {
                return (low - distance) * 32;
            }

            if (distance > high)
            {
                return (distance - high) * 16;
            }

            return high - distance;
        }

        // Real path distance, not Manhattan. Scoring the walk by "how many steps is that tile from the
        // target" is what stops a wall being a local minimum an enemy can never climb out of (D-029).
        // Manhattan survives only as the tie-break, which keeps the open-board line identical.
        private static Coord ClosingTile(GameState state, Unit enemy, Coord target)
        {
            var field = PathField.To(state, enemy, target);
            return BestTile(state, enemy, coord => new Score(field.At(coord), coord.DistanceTo(target)));
        }

        // The ranged archetypes want a band of distances, not a tile, so the field is grown from every
        // tile in that band at once: the primary score is "how far to the nearest tile I would be happy
        // standing on", and the band score only decides between tiles once one has been reached.
        private static Coord ApproachTile(GameState state, Unit enemy, Coord target, int low, int high)
        {
            var field = PathField.ToAnyOf(state, enemy, BandTiles(state, target, low, high));
            return BestTile(
                state, enemy, coord => new Score(field.At(coord), Band(coord.DistanceTo(target), low, high)));
        }

        private static List<Coord> BandTiles(GameState state, Coord target, int low, int high)
        {
            var board = state.Board;
            var tiles = new List<Coord>();

            foreach (var coord in board.AllCoords())
            {
                if (!Movement.IsWalkable(board.At(coord)))
                {
                    continue;
                }

                int distance = coord.DistanceTo(target);
                if (distance >= low && distance <= high)
                {
                    tiles.Add(coord);
                }
            }

            return tiles;
        }

        /// <summary>
        /// Picks the tile the unit should walk to. Ranked by the caller's score, then by spike tiles
        /// crossed, then by movement spent, then by row-major coordinate order. Standing still is
        /// always a candidate and wins every tie on cost, so an enemy that is already where it wants
        /// to be does not shuffle.
        /// </summary>
        private static Coord BestTile(GameState state, Unit enemy, Func<Coord, int> score) =>
            BestTile(state, enemy, coord => new Score(0, score(coord)));

        private static Coord BestTile(GameState state, Unit enemy, Func<Coord, Score> score)
        {
            var bestTile = enemy.Position;
            var bestScore = score(enemy.Position);
            int bestSpikes = 0;
            int bestCost = 0;

            foreach (var pair in Movement.Reachable(state, enemy))
            {
                var candidate = score(pair.Key);
                var option = pair.Value;

                if (IsBetterTile(candidate, option.SpikeTiles, option.Cost, pair.Key,
                        bestScore, bestSpikes, bestCost, bestTile))
                {
                    bestTile = pair.Key;
                    bestScore = candidate;
                    bestSpikes = option.SpikeTiles;
                    bestCost = option.Cost;
                }
            }

            return bestTile;
        }

        private static bool IsBetterTile(
            Score score,
            int spikes,
            int cost,
            Coord tile,
            Score bestScore,
            int bestSpikes,
            int bestCost,
            Coord bestTile)
        {
            if (score.Primary != bestScore.Primary)
            {
                return score.Primary < bestScore.Primary;
            }

            if (score.Secondary != bestScore.Secondary)
            {
                return score.Secondary < bestScore.Secondary;
            }

            if (spikes != bestSpikes)
            {
                return spikes < bestSpikes;
            }

            if (cost != bestCost)
            {
                return cost < bestCost;
            }

            return tile.Y != bestTile.Y ? tile.Y < bestTile.Y : tile.X < bestTile.X;
        }

        // How good a tile is, lowest first: how far it is from where the enemy is trying to get to,
        // then how good a place it is to stand once there. Both are plain integers — Core does no
        // float maths (Brief §1).
        private readonly struct Score
        {
            public Score(int primary, int secondary)
            {
                Primary = primary;
                Secondary = secondary;
            }

            public int Primary { get; }

            public int Secondary { get; }
        }
    }
}
