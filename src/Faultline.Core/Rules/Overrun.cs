using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// Overrun: the Vanguard's alternate action. He runs up to three tiles in a straight line and
    /// shoulders <em>every</em> enemy in the path one tile aside, ending where the run stops
    /// (MASTER_DESIGN §5's parked spender list, promoted to an action).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is the Husk's Shoulder as a player verb, and it is the same code.</b> The side a body
    /// is knocked toward, the fixed N/E/S/W order, the "both blocked and he stops" clause and the
    /// whole of the resolution are <see cref="Trample.SideFor"/> and <see cref="Trample.Shoulder"/>,
    /// called rather than restated. The one number that differs is the contact damage: §4 gives the
    /// Vanguard's charge base contact damage 0, so he passes zero where a Husk passes
    /// <see cref="Trample.ContactDamage"/>.
    /// </para>
    /// <para>
    /// <b>Every displacement is the shared pipeline's.</b> Nothing here decides what a collision
    /// costs, whether a drain takes somebody, whether resistance shortens the tile or whether Footing
    /// refuses it — those are <see cref="Displacement"/>'s, and asking <c>PreviewAuto</c> where the
    /// body really stops is exactly how the "must actually vacate" test stays true without this file
    /// knowing any of them.
    /// </para>
    /// <para>
    /// <b>The projection resolves in the order the action does</b> (D-184). Each shove is computed
    /// against the board the previous shove left behind, because the second body's escape route
    /// genuinely depends on whether the first one is still standing in it. A preview computed against
    /// the untouched board would promise a run he cannot make.
    /// </para>
    /// </remarks>
    public static class Overrun
    {
        /// <summary>Tiles each body is knocked aside — one, exactly as the Shoulder knocks.</summary>
        public const int ShoveDistance = 1;

        /// <summary>Damage the runner takes per bramble tile he crosses, as Bull Rush takes it.</summary>
        public const int BrambleSelfDamage = 1;

        /// <summary>
        /// What the run would do, projected in resolution order.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">The running unit.</param>
        /// <param name="direction">Line to run along.</param>
        /// <param name="descriptor">The Overrun definition being aimed.</param>
        /// <returns>The projection; a no-op when the first tile already stops him.</returns>
        public static OverrunPreview Preview(
            GameState state, Unit unit, Direction direction, AbilityDefinition? descriptor)
        {
            var path = new List<Coord>();
            var shoves = new List<DisplacementPreview>();
            var position = unit.Position;
            int selfDamage = 0;

            int reach = descriptor is not null && descriptor.CustomRule == AbilityRule.Overrun
                ? descriptor.Range
                : 0;

            int distance = ShoveDistance;

            // The board as the run leaves it, shove by shove. Events are discarded: a projection must
            // not announce anything and must not damage anybody the caller can see.
            var projected = state;
            var discarded = new List<GameEvent>();

            for (int step = 0; step < reach; step++)
            {
                var next = position.Step(direction);
                if (!projected.Board.InBounds(next))
                {
                    break;
                }

                // A structure stops the run dead, the way it stops a charge. The run is a run, not a
                // shove, so it does the masonry no damage.
                if (projected.StructureAt(next) is not null)
                {
                    break;
                }

                var tile = projected.Board.At(next);
                if (!Movement.IsWalkable(tile) || tile == TileType.HighGround)
                {
                    break;
                }

                if (projected.UnitAt(next) is { } occupant)
                {
                    // An ally in the way simply blocks him — the same clause Bull Rush makes. The
                    // brief's "every enemy in the path" is the whole of who gets shouldered.
                    if (!unit.Team.IsHostileTo(occupant.Team))
                    {
                        break;
                    }

                    var side = Trample.SideFor(projected, occupant, next, direction, distance);
                    if (side is null)
                    {
                        // Nowhere to put it. The body is a wall and the run stops short of it —
                        // Trample's rule, and the reason a Wardbearer is a door.
                        break;
                    }

                    shoves.Add(Displacement.PreviewAuto(
                        projected,
                        occupant.Id,
                        SourceTile(next, side.Value),
                        DisplacementKind.Push,
                        distance,
                        by: unit.Id));

                    projected = Trample.Shoulder(
                        projected, unit.Id, occupant.Id, next, direction, side.Value,
                        contactDamage: 0, distance, discarded);
                }

                position = next;
                path.Add(next);

                if (tile == TileType.Spikes)
                {
                    selfDamage += BrambleSelfDamage;
                }
            }

            return new OverrunPreview(unit.Id, direction, path, position, selfDamage, shoves);
        }

        /// <summary>
        /// Runs the line for real: shoulder, step, shoulder, step, paying for brambles underfoot.
        /// </summary>
        /// <remarks>
        /// Resolution walks the board rather than replaying <see cref="Preview"/>'s list, because the
        /// projection and the run ask the identical questions of the identical rules in the identical
        /// order — so they agree by construction, and a preview that replayed a stale list would be
        /// the second copy this design keeps refusing.
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="unit">The running unit.</param>
        /// <param name="direction">Line to run along.</param>
        /// <param name="descriptor">The Overrun definition being resolved.</param>
        /// <param name="events">Sink for the resulting events.</param>
        /// <returns>The state after the run.</returns>
        public static GameState Resolve(
            GameState state,
            Unit unit,
            Direction direction,
            AbilityDefinition descriptor,
            List<GameEvent> events)
        {
            var position = unit.Position;
            var from = unit.Position;
            var walked = new List<Coord>();

            int reach = descriptor.Range;
            int distance = ShoveDistance;

            for (int step = 0; step < reach; step++)
            {
                var next = position.Step(direction);
                if (!state.Board.InBounds(next) || state.StructureAt(next) is not null)
                {
                    break;
                }

                var tile = state.Board.At(next);
                if (!Movement.IsWalkable(tile) || tile == TileType.HighGround)
                {
                    break;
                }

                if (state.UnitAt(next) is { } occupant)
                {
                    if (!unit.Team.IsHostileTo(occupant.Team))
                    {
                        break;
                    }

                    var side = Trample.SideFor(state, occupant, next, direction, distance);
                    if (side is null)
                    {
                        break;
                    }

                    state = Trample.Shoulder(
                        state, unit.Id, occupant.Id, next, direction, side.Value,
                        contactDamage: 0, distance, events);

                    // The shove can take the runner off the board too — a collision hurts both ends,
                    // and the body he shouldered may have gone into him. Nothing continues after that.
                    if (!state.UnitById(unit.Id).IsOnBoard)
                    {
                        return state;
                    }

                    // The tile has to be genuinely clear before he walks onto it. It normally is,
                    // because the side test already proved the body vacates — but a wave, a
                    // collision chain or a Footing refusal resolved in between is exactly the sort of
                    // thing that makes "normally" wrong, so it is asked rather than assumed.
                    if (state.UnitAt(next) is not null)
                    {
                        break;
                    }
                }

                position = next;
                walked.Add(next);
            }

            if (walked.Count > 0)
            {
                state = state.WithUnit(state.UnitById(unit.Id) with { Position = position });
                events.Add(new UnitMoved(unit.Id, from, position, walked, walked.Count));

                foreach (var tile in walked)
                {
                    if (state.Board.At(tile) != TileType.Spikes)
                    {
                        continue;
                    }

                    events.Add(new SpikeHit(unit.Id, tile, BrambleSelfDamage, true));
                    state = Combat.ApplyDamage(
                        state, unit.Id, BrambleSelfDamage, DamageSource.Spikes, events);

                    if (!state.UnitById(unit.Id).IsOnBoard)
                    {
                        return state;
                    }
                }
            }

            return state;
        }

        // A push travels away from its source, so the synthetic source is the tile opposite the side.
        private static Coord SourceTile(Coord tile, Direction side) => tile.Step(side.Opposite());
    }
}
