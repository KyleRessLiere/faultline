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
                if (distance == 0 || distance > descriptor.Range || distance < descriptor.MinRange)
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
        /// Directions a Line ability would hit something in. A line with nothing on it does nothing,
        /// so it is never offered — the same rule Bull Rush follows. A structure counts as something:
        /// an attack chips it (D-060), so a line covering only a structure is still worth aiming.
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
                if (LineHits(state, unit, direction, descriptor).Count > 0)
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
        /// The enemies a Line ability would hit, nearest first. Order is presentation only — a Line
        /// displaces nothing, so no target can affect another (D-068).
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Acting unit.</param>
        /// <param name="direction">Direction to face.</param>
        /// <param name="descriptor">Ability being aimed.</param>
        /// <returns>Target ids, nearest first.</returns>
        public static IReadOnlyList<UnitId> LineTargets(
            GameState state, Unit unit, Direction direction, AbilityDescriptor? descriptor)
        {
            var targets = new List<UnitId>();

            foreach (var hit in LineHits(state, unit, direction, descriptor))
            {
                if (hit.UnitId is not null)
                {
                    targets.Add(hit.UnitId.Value);
                }
            }

            return targets;
        }

        /// <summary>
        /// Everything a Line ability would hit and for how much, nearest tile first: the enemies on
        /// its tiles and any objective structure standing on one.
        /// </summary>
        /// <remarks>
        /// This is the whole ability. Resolution walks exactly this list, so a preview and a
        /// resolution are the same projection read twice rather than two implementations of one rule.
        /// A tile the line delivers nothing to produces no hit, and so does an empty or allied tile.
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Acting unit.</param>
        /// <param name="direction">Direction to face.</param>
        /// <param name="descriptor">Ability being aimed.</param>
        /// <returns>The projected hits, nearest first.</returns>
        public static IReadOnlyList<LineHit> LineHits(
            GameState state, Unit unit, Direction direction, AbilityDescriptor? descriptor)
        {
            var hits = new List<LineHit>();
            if (descriptor is null || descriptor.Targeting != AbilityTargeting.Line)
            {
                return hits;
            }

            var tiles = LineTiles(state, unit, direction, descriptor);

            for (int i = 0; i < tiles.Count; i++)
            {
                int damage = descriptor.DamageOnTile(i);
                if (damage <= 0)
                {
                    continue;
                }

                var occupant = state.UnitAt(tiles[i]);
                if (occupant is not null && unit.Team.IsHostileTo(occupant.Team))
                {
                    hits.Add(new LineHit(tiles[i], damage, occupant.Id, false));
                    continue;
                }

                // D-060: an attack takes 1 off a structure whatever the weapon, so the line reports
                // the 1 it will actually deliver rather than the number it deals a body.
                var structure = state.StructureAt(tiles[i]);
                if (structure is not null && structure.IsStanding)
                {
                    hits.Add(new LineHit(tiles[i], Objectives.AttackDamageToStructure, null, true));
                }
            }

            return hits;
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
        /// What a Line ability would do: one hit per tile it damages, nearest first. Nothing moves,
        /// so nothing needs projecting against a board that has already changed.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Acting unit.</param>
        /// <param name="direction">Direction to face.</param>
        /// <param name="ability">
        /// Which Line ability is being aimed. Named rather than assumed: this used to hard-code
        /// Spear Thrust, so a second Line ability would silently have previewed as the first one —
        /// and a preview that quietly describes a different ability is worse than no preview.
        /// </param>
        /// <returns>The projected hits, nearest first.</returns>
        public static IReadOnlyList<LineHit> PreviewLine(
            GameState state, Unit unit, Direction direction, Ability ability) =>
            LineHits(state, unit, direction, DescriptorFor(unit, ability));

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
                    state, targetId, unit.Position, DisplacementKind.Push, descriptor.Push, events,
                    by: unit.Id);
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

        // D-068: a Line is damage and nothing else — it displaces nobody, so the far-first ordering
        // rule the ability shipped with is gone rather than reversed. The near tile resolves first
        // because that is how the ability reads; with nothing moving, the order is not load-bearing
        // and no hit can change what another hit finds.
        private static GameState ResolveLine(
            GameState state,
            Unit unit,
            Direction direction,
            AbilityDescriptor descriptor,
            List<GameEvent> events)
        {
            foreach (var hit in LineHits(state, unit, direction, descriptor))
            {
                state = StepLineHit(state, unit, hit, events);
            }

            return state;
        }

        // One tile of a Line ability. Everything on the tile goes through the shared damage path for
        // its kind — Combat for a unit, Objectives for a structure — so a rule about what an attack
        // does to a thing lives with that thing rather than being restated here.
        private static GameState StepLineHit(
            GameState state, Unit unit, LineHit hit, List<GameEvent> events)
        {
            if (hit.UnitId is { } targetId)
            {
                var target = state.FindUnit(targetId);
                if (target is null || !target.IsOnBoard)
                {
                    return state;
                }

                events.Add(new UnitAttacked(
                    unit.Id,
                    targetId,
                    unit.Position,
                    target.Position,
                    Guard.Mitigate(state, targetId, hit.Damage, DamageSource.Attack),
                    false));

                return Combat.ApplyDamage(state, targetId, hit.Damage, DamageSource.Attack, events);
            }

            if (hit.HitsStructure)
            {
                events.Add(new StructureAttacked(unit.Id, unit.Position, hit.At, hit.Damage));
                return Objectives.Damage(state, hit.At, hit.Damage, DamageSource.Attack, events);
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
                    events,
                    by: unit.Id);
            }

            return state;
        }
    }
}
