using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// The class abilities. Everything they do to the board goes through
    /// <see cref="Displacement"/>, so both sides obey identical physics (Brief §6 prior 2).
    /// </summary>
    public static class Abilities
    {
        /// <summary>The archetype's headline ability, or <c>null</c> for enemies.</summary>
        /// <param name="unit">Unit to inspect.</param>
        /// <returns>Its first ability descriptor.</returns>
        public static AbilityDescriptor? Of(Unit unit) => AbilityDescriptor.ForKind(unit.Kind);

        /// <summary>
        /// Every ability the unit brings. The Wardbearer has two and picks one each activation
        /// (D-058); everybody else has one or none.
        /// </summary>
        /// <param name="unit">Unit to inspect.</param>
        /// <returns>Its abilities, in the order they should be offered.</returns>
        public static IReadOnlyList<AbilityDescriptor> AllOf(Unit unit) =>
            AbilityDescriptor.AllForKind(unit.Kind);

        /// <summary>True when the unit could use any of its abilities right now, ignoring target choice.</summary>
        /// <param name="unit">Unit to inspect.</param>
        /// <returns>Whether at least one ability is usable at all.</returns>
        public static bool IsUsable(Unit unit)
        {
            foreach (var descriptor in AllOf(unit))
            {
                if (IsUsable(unit, descriptor))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>True when the unit could use this specific ability right now.</summary>
        /// <param name="unit">Unit to inspect.</param>
        /// <param name="descriptor">Ability to test.</param>
        /// <returns>Whether the ability is usable at all.</returns>
        public static bool IsUsable(Unit unit, AbilityDescriptor? descriptor) =>
            descriptor is not null
            && descriptor.Kind == unit.Kind
            && descriptor.Targeting != AbilityTargeting.Passive
            && unit.IsOnBoard
            && !unit.Clinging;

        /// <summary>The unit's descriptor for a named ability, or <c>null</c> when it does not have it.</summary>
        /// <param name="unit">Unit to inspect.</param>
        /// <param name="ability">Ability to look for.</param>
        /// <returns>The descriptor, or <c>null</c>.</returns>
        public static AbilityDescriptor? DescriptorFor(Unit unit, Ability ability)
        {
            foreach (var descriptor in AllOf(unit))
            {
                if (descriptor.Ability == ability)
                {
                    return descriptor;
                }
            }

            return null;
        }

        /// <summary>Enemies the unit's headline targeted ability may be aimed at.</summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Acting unit.</param>
        /// <returns>Legal target ids, in stable order.</returns>
        public static IReadOnlyList<UnitId> LegalTargets(GameState state, Unit unit) =>
            LegalTargets(state, unit, Of(unit));

        /// <summary>Enemies a targeted ability may be aimed at.</summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Acting unit.</param>
        /// <param name="descriptor">Ability being aimed.</param>
        /// <returns>Legal target ids, in stable order.</returns>
        public static IReadOnlyList<UnitId> LegalTargets(
            GameState state, Unit unit, AbilityDescriptor? descriptor)
        {
            var targets = new List<UnitId>();

            if (descriptor is null
                || descriptor.Targeting != AbilityTargeting.Enemy
                || !IsUsable(unit, descriptor))
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
        public static IReadOnlyList<Direction> LegalDirections(GameState state, Unit unit) =>
            LegalDirections(state, unit, Of(unit));

        /// <summary>Directions a charge ability would actually accomplish something in.</summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Acting unit.</param>
        /// <param name="descriptor">Ability being aimed.</param>
        /// <returns>Legal charge directions.</returns>
        public static IReadOnlyList<Direction> LegalDirections(
            GameState state, Unit unit, AbilityDescriptor? descriptor)
        {
            var directions = new List<Direction>();

            if (descriptor is null
                || descriptor.Targeting != AbilityTargeting.Direction
                || !IsUsable(unit, descriptor))
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
        /// Directions a Line ability would hit at least one enemy in. A line with nothing on it does
        /// nothing, so it is never offered — the same rule Bull Rush follows.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Acting unit.</param>
        /// <param name="descriptor">Ability being aimed.</param>
        /// <returns>Legal line directions.</returns>
        public static IReadOnlyList<Direction> LegalLines(
            GameState state, Unit unit, AbilityDescriptor? descriptor)
        {
            var directions = new List<Direction>();

            if (descriptor is null
                || descriptor.Targeting != AbilityTargeting.Line
                || !IsUsable(unit, descriptor))
            {
                return directions;
            }

            foreach (var direction in Directions.All)
            {
                if (LineTargets(state, unit, direction, descriptor).Count > 0)
                {
                    directions.Add(direction);
                }
            }

            return directions;
        }

        /// <summary>
        /// The tiles a Line ability covers in one direction: the fixed run directly ahead, clipped to
        /// the board. Nothing blocks it — there is no line of sight in this game (D-010), so this is a
        /// shape and not a ray-cast.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Acting unit.</param>
        /// <param name="direction">Direction to face.</param>
        /// <param name="descriptor">Ability being aimed.</param>
        /// <returns>The covered tiles, nearest first.</returns>
        public static IReadOnlyList<Coord> LineTiles(
            GameState state, Unit unit, Direction direction, AbilityDescriptor? descriptor)
        {
            var tiles = new List<Coord>();
            if (descriptor is null || descriptor.Targeting != AbilityTargeting.Line)
            {
                return tiles;
            }

            var position = unit.Position;
            for (int step = 0; step < descriptor.Range; step++)
            {
                position = position.Step(direction);
                if (!state.Board.InBounds(position))
                {
                    break;
                }

                tiles.Add(position);
            }

            return tiles;
        }

        /// <summary>
        /// The enemies a Line ability would hit, <em>in resolution order — furthest first</em>. That
        /// order is the rule: the far target moves before the near one, so the near one can follow it
        /// into the tile it vacated, or collide into it when it did not move (D-058).
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Acting unit.</param>
        /// <param name="direction">Direction to face.</param>
        /// <param name="descriptor">Ability being aimed.</param>
        /// <returns>Target ids, furthest first.</returns>
        public static IReadOnlyList<UnitId> LineTargets(
            GameState state, Unit unit, Direction direction, AbilityDescriptor? descriptor)
        {
            var tiles = LineTiles(state, unit, direction, descriptor);
            var targets = new List<UnitId>(tiles.Count);

            for (int i = tiles.Count - 1; i >= 0; i--)
            {
                var occupant = state.UnitAt(tiles[i]);
                if (occupant is not null && unit.Team.IsHostileTo(occupant.Team))
                {
                    targets.Add(occupant.Id);
                }
            }

            return targets;
        }

        /// <summary>
        /// Every tile the unit's headline ability can reach, for the shell to highlight before a
        /// target is picked.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Acting unit.</param>
        /// <returns>Tiles within the ability's reach.</returns>
        public static IReadOnlyList<Coord> RangeTiles(GameState state, Unit unit) =>
            RangeTiles(state, unit, Of(unit));

        /// <summary>
        /// Every tile an ability can reach, for the shell to highlight before a target is picked.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Acting unit.</param>
        /// <param name="descriptor">Ability being aimed.</param>
        /// <returns>Tiles within the ability's reach.</returns>
        public static IReadOnlyList<Coord> RangeTiles(
            GameState state, Unit unit, AbilityDescriptor? descriptor)
        {
            var tiles = new List<Coord>();

            if (descriptor is null || !IsUsable(unit, descriptor))
            {
                return tiles;
            }

            if (descriptor.Targeting == AbilityTargeting.Self)
            {
                return tiles;
            }

            if (descriptor.Targeting == AbilityTargeting.Line)
            {
                foreach (var direction in Directions.All)
                {
                    tiles.AddRange(LineTiles(state, unit, direction, descriptor));
                }

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

        /// <summary>
        /// What a Line ability would do, one projection per enemy hit, in resolution order. Each
        /// projection is taken against the board as it will stand when that target's shove resolves,
        /// so the near target's preview already accounts for the far one having moved or not.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Acting unit.</param>
        /// <param name="direction">Direction to face.</param>
        /// <returns>The projected displacements, furthest target first.</returns>
        public static IReadOnlyList<DisplacementPreview> PreviewLine(
            GameState state, Unit unit, Direction direction)
        {
            var previews = new List<DisplacementPreview>();
            var descriptor = DescriptorFor(unit, Ability.SpearThrust);
            if (descriptor is null)
            {
                return previews;
            }

            var scratch = state;
            var discarded = new List<GameEvent>();

            foreach (var targetId in LineTargets(state, unit, direction, descriptor))
            {
                scratch = StepLineTarget(scratch, unit, targetId, descriptor, discarded, previews);
            }

            return previews;
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

            int reach = descriptor is not null && descriptor.Targeting == AbilityTargeting.Direction
                ? descriptor.Range
                : 0;

            for (int step = 0; step < reach; step++)
            {
                var next = position.Step(direction);
                if (!board.InBounds(next))
                {
                    break;
                }

                // An objective structure stops the charge dead, the same way a wall does. The charge
                // is a run, not a shove, so it does the structure no damage.
                if (state.StructureAt(next) is not null)
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

            if (descriptor.Targeting == AbilityTargeting.Self)
            {
                return ResolveStance(state, unit, events);
            }

            if (descriptor.Targeting == AbilityTargeting.Line)
            {
                return ResolveLine(state, unit, command.Direction!.Value, descriptor, events);
            }

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

        // D-058: Guard Stance costs the action half and nothing else. It lapses at the start of this
        // unit's next activation, which Game.CommitActivation does — not at end of round, because the
        // enemy round it is meant to cover happens after the round it was declared in.
        private static GameState ResolveStance(GameState state, Unit unit, List<GameEvent> events)
        {
            var guarding = state.UnitById(unit.Id) with { Guarding = true };
            events.Add(new GuardStanceChanged(guarding.Id, guarding.Position, true));
            return state.WithUnit(guarding);
        }

        // D-058: the far target resolves completely — damage, then shove — before the near one is
        // touched at all. That is what lets the near target walk into the tile the far one left, and
        // what turns a far target that could not move into the wall the near one collides with.
        private static GameState ResolveLine(
            GameState state,
            Unit unit,
            Direction direction,
            AbilityDescriptor descriptor,
            List<GameEvent> events)
        {
            foreach (var targetId in LineTargets(state, unit, direction, descriptor))
            {
                state = StepLineTarget(state, unit, targetId, descriptor, events, null);
            }

            return state;
        }

        // One target of a Line ability: the damage, then the shove, from the user's own tile so the
        // shove runs along the line. Shared by Resolve and Preview so the projection cannot drift
        // from what actually happens.
        private static GameState StepLineTarget(
            GameState state,
            Unit unit,
            UnitId targetId,
            AbilityDescriptor descriptor,
            List<GameEvent> events,
            List<DisplacementPreview>? previews)
        {
            var target = state.FindUnit(targetId);
            if (target is null || !target.IsOnBoard)
            {
                return state;
            }

            if (descriptor.Damage > 0)
            {
                events.Add(new UnitAttacked(
                    unit.Id,
                    targetId,
                    unit.Position,
                    target.Position,
                    Guard.Mitigate(state, targetId, descriptor.Damage, DamageSource.Attack),
                    false));

                state = Combat.ApplyDamage(state, targetId, descriptor.Damage, DamageSource.Attack, events);

                if (!state.UnitById(targetId).IsOnBoard)
                {
                    return state;
                }
            }

            if (descriptor.Push <= 0)
            {
                return state;
            }

            if (previews is not null)
            {
                previews.Add(Displacement.PreviewAuto(
                    state, targetId, unit.Position, DisplacementKind.Push, descriptor.Push));
            }

            return Displacement.ResolveAuto(
                state, targetId, unit.Position, DisplacementKind.Push, descriptor.Push, events);
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
