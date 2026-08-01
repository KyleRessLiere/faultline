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
            // priority list rather than instead of it.
            if (enemy.Template.Attack != AttackKind.None)
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
                        case IntentAction.Attack when Combat.CanAttack(state, enemy, target, out _):
                            return new AttackCommand(enemy.Id, target.Id);

                        case IntentAction.Pull when Combat.CanPull(state, enemy, target):
                            return new AttackCommand(enemy.Id, target.Id, AttackMode.Pull);

                        case IntentAction.Push when Combat.CanPush(state, enemy, target):
                            return new AttackCommand(enemy.Id, target.Id, AttackMode.Push);
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

            return Compute(state, enemy, null);
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
        /// Re-runs the priority list for any enemy whose declared target has died or been removed,
        /// and drops the intents of enemies that are no longer on the board.
        /// </summary>
        /// <remarks>
        /// Brief §2: an intent is locked and re-planned "only if its target becomes invalid". A target
        /// that merely moved does not trigger this — the enemy still chases the unit it named.
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

            if (state.Intents.Count == 0)
            {
                return state;
            }

            bool live = state.Phase == Phase.Battle && state.Outcome == FightOutcome.InProgress;
            var updated = new List<EnemyIntent>(state.Intents.Count);
            bool changed = false;

            foreach (var intent in state.Intents)
            {
                var enemy = state.FindUnit(intent.UnitId);
                if (enemy is null || !enemy.IsOnBoard || enemy.Clinging)
                {
                    changed = true;
                    continue;
                }

                if (!intent.TargetId.HasValue || IsValidTarget(state, intent.TargetId.Value))
                {
                    updated.Add(intent);
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

        // ---- planning -------------------------------------------------------------------------

        // The intent locks the target, not the route. Re-deriving the geometry each time keeps the
        // command legal after the players have shuffled the board underneath it (D-021).
        private static EnemyIntent Live(GameState state, Unit enemy)
        {
            var declared = IntentFor(state, enemy.Id);
            UnitId? locked = null;

            if (declared is not null
                && declared.TargetId.HasValue
                && IsValidTarget(state, declared.TargetId.Value))
            {
                locked = declared.TargetId.Value;
            }

            return Compute(state, enemy, locked);
        }

        private static EnemyIntent Compute(GameState state, Unit enemy, UnitId? locked)
        {
            var all = Candidates(state, enemy);
            if (all.Count == 0)
            {
                return Hold(enemy);
            }

            var choices = all;
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

            switch (enemy.Kind)
            {
                case UnitKind.Husk:
                case UnitKind.Anchor:
                    return PlanMelee(state, enemy, choices);

                case UnitKind.Lobber:
                    return PlanLobber(state, enemy, all, choices);

                case UnitKind.Grappler:
                    return PlanGrappler(state, enemy, choices);

                case UnitKind.Stalker:
                    return PlanStalker(state, enemy, choices);

                default:
                    return Hold(enemy);
            }
        }

        // Brief §2, Husk: "1. Adjacent player unit → attack. 2. Else move toward nearest player unit."
        // The Anchor's list is the same shape with Move 1 and 2 damage.
        private static EnemyIntent PlanMelee(GameState state, Unit enemy, List<Unit> choices)
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
        private static EnemyIntent PlanLobber(GameState state, Unit enemy, List<Unit> all, List<Unit> choices)
        {
            int range = enemy.Template.Range;

            if (FirstAdjacent(enemy.Position, all) is not null)
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
            var destination = BestTile(state, enemy, coord => Band(coord.DistanceTo(target.Position), 2, range));
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

        // Brief §2, Grappler: "1. Player unit within range 3 → Pull 2 toward self, preferring
        // (a) units on HighGround, (b) Archer. 2. Else advance toward the Archer, else nearest."
        private static EnemyIntent PlanGrappler(GameState state, Unit enemy, List<Unit> choices)
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

            var destination = BestTile(state, enemy, coord => Band(coord.DistanceTo(target.Position), 2, range));
            var moveTo = destination == enemy.Position ? (Coord?)null : destination;

            var arrived = PickGrab(state, destination, choices, range);
            return arrived is not null
                ? Displace(state, enemy, arrived, moveTo, DisplacementKind.Pull, distance)
                : Advance(enemy, target, moveTo);
        }

        // Brief §2, Stalker: "1. Player unit adjacent to Pit/Spikes/edge and reachable → move to
        // flank, Push 1 toward the hazard. 2. Else move toward nearest player unit that is within 2
        // of a hazard; else hold position."
        private static EnemyIntent PlanStalker(GameState state, Unit enemy, List<Unit> choices)
        {
            var reachable = Movement.Reachable(state, enemy);

            // Hazards are ranked pit, then spikes, then a solid edge — a pit ends the fight for the
            // unit it swallows, so it outranks 3 damage, which outranks 2 (D-024).
            for (int rank = 0; rank <= HazardRankEdge; rank++)
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
                if (HazardDistance(state, candidate.Position) > 2)
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

        // ---- intent construction --------------------------------------------------------------

        private static EnemyIntent Hold(Unit enemy) => new EnemyIntent(
            enemy.Id, enemy.Kind, enemy.Position, IntentAction.Hold,
            null, null, null, null, null, 0, null, 0);

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
                damage += 1;
            }

            return new EnemyIntent(
                enemy.Id, enemy.Kind, enemy.Position, IntentAction.Attack,
                target.Id, target.Position, moveTo, null, null, 0, null, damage);
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
            var preview = Displacement.PreviewAuto(view, target.Id, from, kind, distance);

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
                0);
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

        private static bool IsValidTarget(GameState state, UnitId id)
        {
            var unit = state.FindUnit(id);
            return unit is not null && unit.IsOnBoard && !unit.Clinging;
        }

        private static Unit? FirstAdjacent(Coord from, List<Unit> units)
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

        private static Unit? Nearest(Coord from, List<Unit> units)
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

        private static Unit? NearestWithin(Coord from, List<Unit> units, int range)
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

        private static Unit? FirstOfKind(List<Unit> units, UnitKind kind)
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
        private static Unit? PickGrab(GameState state, Coord from, List<Unit> choices, int range)
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
        // says the board edge acts as a wall, so both produce the identical 2-damage collision.
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

        private static int HazardDistance(GameState state, Coord from)
        {
            var board = state.Board;

            // Off-board is one step past the outermost tile in each direction.
            int best = from.X + 1;
            best = Math.Min(best, from.Y + 1);
            best = Math.Min(best, board.Width - from.X);
            best = Math.Min(best, board.Height - from.Y);

            foreach (var coord in board.AllCoords())
            {
                var terrain = board.At(coord);
                if (terrain != TileType.Pit && terrain != TileType.Spikes && terrain != TileType.Wall)
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

        private static int Spread(Coord from, List<Unit> units)
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

        private static Coord ClosingTile(GameState state, Unit enemy, Coord target) =>
            BestTile(state, enemy, coord => coord.DistanceTo(target));

        /// <summary>
        /// Picks the tile the unit should walk to. Ranked by the caller's score, then by spike tiles
        /// crossed, then by movement spent, then by row-major coordinate order. Standing still is
        /// always a candidate and wins every tie on cost, so an enemy that is already where it wants
        /// to be does not shuffle.
        /// </summary>
        private static Coord BestTile(GameState state, Unit enemy, Func<Coord, int> score)
        {
            var bestTile = enemy.Position;
            int bestScore = score(enemy.Position);
            int bestSpikes = 0;
            int bestCost = 0;

            foreach (var pair in Movement.Reachable(state, enemy))
            {
                int candidate = score(pair.Key);
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
            int score,
            int spikes,
            int cost,
            Coord tile,
            int bestScore,
            int bestSpikes,
            int bestCost,
            Coord bestTile)
        {
            if (score != bestScore)
            {
                return score < bestScore;
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
    }
}
