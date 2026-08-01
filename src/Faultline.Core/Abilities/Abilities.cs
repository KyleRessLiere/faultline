using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// The four class abilities. Everything they do to the board goes through
    /// <see cref="Displacement"/>, so both sides obey identical physics (Brief §6 prior 2).
    /// </summary>
    public static class Abilities
    {
        /// <summary>The ability a unit brings, or <c>null</c> for enemies.</summary>
        /// <param name="unit">Unit to inspect.</param>
        /// <returns>Its ability descriptor.</returns>
        public static AbilityDescriptor? Of(Unit unit) => AbilityDescriptor.ForKind(unit.Kind);

        /// <summary>True when the unit could use its ability right now, ignoring target choice.</summary>
        /// <param name="unit">Unit to inspect.</param>
        /// <returns>Whether the ability is usable at all.</returns>
        public static bool IsUsable(Unit unit)
        {
            var descriptor = Of(unit);
            return descriptor is not null
                && descriptor.Targeting != AbilityTargeting.Passive
                && unit.IsOnBoard
                && !unit.Clinging;
        }

        /// <summary>Enemies a targeted ability may be aimed at.</summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Acting unit.</param>
        /// <returns>Legal target ids, in stable order.</returns>
        public static IReadOnlyList<UnitId> LegalTargets(GameState state, Unit unit)
        {
            var targets = new List<UnitId>();
            var descriptor = Of(unit);

            if (descriptor is null || descriptor.Targeting != AbilityTargeting.Enemy || !IsUsable(unit))
            {
                return targets;
            }

            foreach (var candidate in state.Units)
            {
                if (!candidate.IsOnBoard || !unit.Team.IsHostileTo(candidate.Team))
                {
                    continue;
                }

                int distance = unit.Position.DistanceTo(candidate.Position);
                if (distance == 0 || distance > descriptor.Range)
                {
                    continue;
                }

                // Reel needs somewhere to reel to; a target already adjacent has nowhere to go.
                if (descriptor.PullsToAdjacent && distance <= 1)
                {
                    continue;
                }

                targets.Add(candidate.Id);
            }

            return targets;
        }

        /// <summary>Directions a charge ability would actually accomplish something in.</summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Acting unit.</param>
        /// <returns>Legal charge directions.</returns>
        public static IReadOnlyList<Direction> LegalDirections(GameState state, Unit unit)
        {
            var directions = new List<Direction>();
            var descriptor = Of(unit);

            if (descriptor is null || descriptor.Targeting != AbilityTargeting.Direction || !IsUsable(unit))
            {
                return directions;
            }

            foreach (var direction in Directions.All)
            {
                if (!PreviewCharge(state, unit, direction).IsNoOp)
                {
                    directions.Add(direction);
                }
            }

            return directions;
        }

        /// <summary>
        /// Every tile the ability can reach, for the shell to highlight before a target is picked.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Acting unit.</param>
        /// <returns>Tiles within the ability's reach.</returns>
        public static IReadOnlyList<Coord> RangeTiles(GameState state, Unit unit)
        {
            var tiles = new List<Coord>();
            var descriptor = Of(unit);

            if (descriptor is null || !IsUsable(unit))
            {
                return tiles;
            }

            if (descriptor.Targeting == AbilityTargeting.Direction)
            {
                foreach (var direction in Directions.All)
                {
                    var charge = PreviewCharge(state, unit, direction);
                    foreach (var tile in charge.Path)
                    {
                        tiles.Add(tile);
                    }

                    if (charge.Contact is not null)
                    {
                        tiles.Add(state.UnitById(charge.Contact.UnitId).Position);
                    }
                }

                return tiles;
            }

            foreach (var coord in state.Board.AllCoords())
            {
                int distance = unit.Position.DistanceTo(coord);
                if (distance > 0 && distance <= descriptor.Range)
                {
                    tiles.Add(coord);
                }
            }

            return tiles;
        }

        /// <summary>What a targeted ability would do to a specific enemy.</summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Acting unit.</param>
        /// <param name="targetId">Enemy to aim at.</param>
        /// <returns>The projected displacement, or <c>null</c> when the ability does not displace.</returns>
        public static DisplacementPreview? PreviewTarget(GameState state, Unit unit, UnitId targetId)
        {
            var descriptor = Of(unit);
            if (descriptor is null)
            {
                return null;
            }

            var target = state.UnitById(targetId);

            if (descriptor.PullsToAdjacent)
            {
                int distance = unit.Position.DistanceTo(target.Position) - 1;
                return distance <= 0
                    ? null
                    : Displacement.PreviewAuto(state, targetId, unit.Position, DisplacementKind.Pull, distance);
            }

            return descriptor.Push <= 0
                ? null
                : Displacement.PreviewAuto(state, targetId, unit.Position, DisplacementKind.Push, descriptor.Push);
        }

        /// <summary>What a charge along a line would do.</summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Charging unit.</param>
        /// <param name="direction">Line to charge along.</param>
        /// <returns>The projected charge.</returns>
        public static ChargePreview PreviewCharge(GameState state, Unit unit, Direction direction)
        {
            var descriptor = Of(unit);
            var path = new List<Coord>();
            var board = state.Board;
            var position = unit.Position;
            int selfDamage = 0;
            Unit? contact = null;

            int reach = descriptor?.Range ?? 0;
            for (int step = 0; step < reach; step++)
            {
                var next = position.Step(direction);
                if (!board.InBounds(next))
                {
                    break;
                }

                var occupant = state.UnitAt(next);
                if (occupant is not null)
                {
                    // Brief §2: the charge stops adjacent to the first enemy it reaches. An ally in
                    // the way simply blocks it.
                    if (unit.Team.IsHostileTo(occupant.Team))
                    {
                        contact = occupant;
                    }

                    break;
                }

                var tile = board.At(next);
                if (!Movement.IsWalkable(tile) || tile == TileType.HighGround)
                {
                    break;
                }

                position = next;
                path.Add(next);

                if (tile == TileType.Spikes)
                {
                    selfDamage += 1;
                }
            }

            DisplacementPreview? shove = null;
            if (contact is not null && descriptor is not null && descriptor.Push > 0)
            {
                shove = Displacement.PreviewAuto(state, contact.Id, position, DisplacementKind.Push, descriptor.Push);
            }

            return new ChargePreview(unit.Id, direction, path, position, selfDamage, shove);
        }

        /// <summary>Applies an ability and emits everything it caused.</summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Acting unit.</param>
        /// <param name="command">The ability command.</param>
        /// <param name="events">Sink for the resulting events.</param>
        /// <returns>The state after the ability resolved.</returns>
        public static GameState Resolve(GameState state, Unit unit, AbilityCommand command, List<GameEvent> events)
        {
            var descriptor = AbilityDescriptor.For(command.Ability);
            events.Add(new AbilityUsed(unit.Id, command.Ability, command.TargetId, unit.Position));

            if (descriptor.Targeting == AbilityTargeting.Direction)
            {
                return ResolveCharge(state, unit, command.Direction!.Value, descriptor, events);
            }

            var targetId = command.TargetId!.Value;

            if (descriptor.Damage > 0)
            {
                var target = state.UnitById(targetId);
                events.Add(new UnitAttacked(
                    unit.Id, targetId, unit.Position, target.Position, descriptor.Damage, false));
                state = Combat.ApplyDamage(state, targetId, descriptor.Damage, DamageSource.Attack, events);

                if (!state.UnitById(targetId).IsOnBoard)
                {
                    return state;
                }
            }

            if (descriptor.PullsToAdjacent)
            {
                int distance = unit.Position.DistanceTo(state.UnitById(targetId).Position) - 1;
                if (distance > 0)
                {
                    state = Displacement.ResolveAuto(
                        state, targetId, unit.Position, DisplacementKind.Pull, distance, events);
                }

                return state;
            }

            if (descriptor.Push > 0)
            {
                state = Displacement.ResolveAuto(
                    state, targetId, unit.Position, DisplacementKind.Push, descriptor.Push, events);
            }

            return state;
        }

        private static GameState ResolveCharge(
            GameState state,
            Unit unit,
            Direction direction,
            AbilityDescriptor descriptor,
            List<GameEvent> events)
        {
            var charge = PreviewCharge(state, unit, direction);

            if (charge.Path.Count > 0)
            {
                state = state.WithUnit(state.UnitById(unit.Id) with { Position = charge.Destination });
                events.Add(new UnitMoved(
                    unit.Id, unit.Position, charge.Destination, charge.Path, charge.Path.Count));

                foreach (var tile in charge.Path)
                {
                    if (state.Board.At(tile) != TileType.Spikes)
                    {
                        continue;
                    }

                    events.Add(new SpikeHit(unit.Id, tile, 1, true));
                    state = Combat.ApplyDamage(state, unit.Id, 1, DamageSource.Spikes, events);

                    if (!state.UnitById(unit.Id).IsOnBoard)
                    {
                        return state;
                    }
                }
            }

            if (charge.Contact is not null)
            {
                state = Displacement.ResolveAuto(
                    state,
                    charge.Contact.UnitId,
                    charge.Destination,
                    DisplacementKind.Push,
                    descriptor.Push,
                    events);
            }

            return state;
        }
    }
}
