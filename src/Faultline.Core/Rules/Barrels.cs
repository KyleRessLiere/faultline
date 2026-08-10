using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// The barrel's one rider: <b>it pops on collision or death</b> — 6 to the collision target, 2 to
    /// every tile adjacent to the pop (MASTER_DESIGN §6).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Nothing here moves a barrel.</b> The roll down a lane is the ordinary displacement pipeline
    /// doing what it does to any body: shove, tile by tile, collide. This reads the finished event
    /// stream afterwards and adds what the pop costs — the same shape as <see cref="Verve.Charge"/>
    /// and <see cref="CampListeners"/>, and for the same reason: a rule that listens cannot be
    /// threaded through thirteen files that emit events, and the pipeline never has to learn that
    /// barrels exist.
    /// </para>
    /// <para>
    /// <b>The pipeline never checks who pushed</b>, which is the whole of "a Husk's jostle sets it
    /// off". A barrel shoved by a duck, by the Cooper, or by an enemy stumbling into it pops
    /// identically, and the blast is allegiance-blind.
    /// </para>
    /// <para>
    /// <b>A body in the lane IS the plug.</b> That falls out of the pipeline rather than being coded
    /// here: a shove stops at the first occupied tile, so the barrel collides there, pops there, and
    /// the lane behind it is never entered.
    /// </para>
    /// </remarks>
    public static class Barrels
    {
        /// <summary>What the thing a popping barrel struck takes.</summary>
        public const int PopDamage = 6;

        /// <summary>What every tile adjacent to the pop takes.</summary>
        public const int BlastDamage = 2;

        /// <summary>Whether this unit is a barrel.</summary>
        /// <param name="unit">Unit to test, or <c>null</c>.</param>
        /// <returns>Whether it pops.</returns>
        public static bool IsBarrel(Unit? unit) => unit is not null && unit.Kind == UnitKind.Barrel;

        /// <summary>
        /// Fires every pop the finished command earned, appending its damage to the same event list.
        /// </summary>
        /// <remarks>
        /// Run once per command, after everything else has resolved, so a barrel that was shoved into
        /// a wall and a barrel that was shot to pieces both arrive here as facts rather than as
        /// interrupts. A pop that kills a second barrel pops that one too — chains resolve because the
        /// scan continues over the events the earlier pop appended.
        /// </remarks>
        /// <param name="state">State after the command resolved.</param>
        /// <param name="events">The command's events; pop damage is appended to it.</param>
        /// <returns>The state with every pop applied.</returns>
        public static GameState Fire(GameState state, List<GameEvent> events)
        {
            if (state is null || events is null)
            {
                return state!;
            }

            var popped = new List<UnitId>();

            // Not a foreach: a pop appends events, and a chain reaction is those events being read by
            // the same pass rather than by a second one.
            for (int i = 0; i < events.Count; i++)
            {
                if (!Popped(state, events[i], out var barrelId, out var struckId))
                {
                    continue;
                }

                if (popped.Contains(barrelId))
                {
                    continue;
                }

                popped.Add(barrelId);
                state = Pop(state, barrelId, struckId, events);
            }

            return state;
        }

        /// <summary>Whether one event set a barrel off, and what it struck.</summary>
        private static bool Popped(
            GameState state, GameEvent produced, out UnitId barrelId, out UnitId? struckId)
        {
            barrelId = UnitId.None;
            struckId = null;

            switch (produced)
            {
                // The barrel arrived at something. Whatever it hit takes the 6; a barrel that hit a
                // wall struck nothing, and only the blast lands.
                case Collision e when IsBarrel(state.FindUnit(e.UnitId)):
                    barrelId = e.UnitId;
                    struckId = e.ObstacleId;
                    return true;

                // Shot to pieces, or caught in another barrel's blast. It goes off where it stood.
                case UnitDowned e when IsBarrel(state.FindUnit(e.UnitId)):
                    barrelId = e.UnitId;
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>Applies one pop: the struck body, then every tile around it.</summary>
        private static GameState Pop(
            GameState state, UnitId barrelId, UnitId? struckId, List<GameEvent> events)
        {
            var barrel = state.FindUnit(barrelId);
            if (barrel is null)
            {
                return state;
            }

            var at = barrel.Position;
            events.Add(new BarrelPopped(barrelId, at, struckId));

            if (struckId is { } struck && state.FindUnit(struck) is { IsOnBoard: true })
            {
                state = Combat.ApplyDamage(state, struck, PopDamage, DamageSource.Collision, events);
            }

            // Allegiance-blind, and the barrel's own tile is not in it: the thing that exploded is
            // gone, and what it caught is what stood around it.
            foreach (var tile in Around(at))
            {
                if (!state.Board.InBounds(tile))
                {
                    continue;
                }

                var caught = state.UnitAt(tile);

                // Whatever it arrived at takes the 6 and not also the 2: the pop's damage to the
                // thing it hit IS its share of the blast, not a second helping of it.
                if (caught is null || caught.Id.Equals(barrelId) || caught.Id.Equals(struckId))
                {
                    continue;
                }

                state = Combat.ApplyDamage(state, caught.Id, BlastDamage, DamageSource.Collision, events);
            }

            // The barrel is spent whether or not it was already down.
            var spent = state.FindUnit(barrelId);
            if (spent is { IsOnBoard: true })
            {
                state = Combat.ApplyDamage(state, barrelId, spent.Hp, DamageSource.Collision, events);
            }

            return state;
        }

        /// <summary>The four tiles a blast reaches. Orthogonal, like every other distance here.</summary>
        private static IEnumerable<Coord> Around(Coord at)
        {
            yield return new Coord(at.X + 1, at.Y);
            yield return new Coord(at.X - 1, at.Y);
            yield return new Coord(at.X, at.Y + 1);
            yield return new Coord(at.X, at.Y - 1);
        }
    }
}
