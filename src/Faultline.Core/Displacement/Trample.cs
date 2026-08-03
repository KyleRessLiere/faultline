using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// Shoulder: a unit walking its route may barrel through a body in the way rather than stop or
    /// go round it. The blocker is knocked one tile sideways and takes a point of contact damage;
    /// the mover pays a movement point for the trouble and keeps going (D-100).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Movement, not an action.</b> A trample is paid for out of the move half and costs the mover
    /// nothing else, which is what makes it a property of how the thing walks rather than a second
    /// attack. The Husk is the only archetype that does it.
    /// </para>
    /// <para>
    /// <b>Allegiance-blind.</b> A Husk shoulders its own ally aside exactly as readily as it does a
    /// player unit. The rule is about a body being in the way, and a body is a body.
    /// </para>
    /// <para>
    /// <b>The blocker has to actually vacate, or there is no trample at all.</b> No side to shove
    /// toward, push resistance eating the tile, a Footing token cancelling it — any of them and the
    /// blocker is a wall: no damage, no shove, and the mover stops short. That single rule is what
    /// makes a Wardbearer a door rather than a speed bump, and it is checkable in advance, so the
    /// route a Husk plans and the route it walks agree.
    /// </para>
    /// </remarks>
    public static class Trample
    {
        /// <summary>Contact damage the blocker takes, before the shove's own consequences.</summary>
        public const int ContactDamage = 2;

        /// <summary>Movement points a trampled tile costs on top of the terrain's own.</summary>
        public const int ExtraCost = 1;

        /// <summary>How far the blocker is knocked aside.</summary>
        public const int Distance = 1;

        /// <summary>
        /// Which way the blocker on <paramref name="tile"/> would be knocked, or <c>null</c> when it
        /// cannot be trampled and is therefore a wall.
        /// </summary>
        /// <remarks>
        /// Sides are the two directions perpendicular to the heading, considered in the fixed order
        /// N/E/S/W, and a side counts only when the blocker ends up standing on it. That last clause
        /// is doing the work: it is how push resistance, a Footing token and a body already standing
        /// in the way all turn a trample into a halt without any of them needing their own rule here.
        /// A drain or a spike tile is a perfectly good side — being knocked somewhere terrible is the
        /// point of the mechanic, not an exception to it.
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="mover">Unit walking through.</param>
        /// <param name="tile">Tile it wants to enter.</param>
        /// <param name="heading">Direction it is walking.</param>
        /// <returns>The side the blocker is knocked toward, or <c>null</c>.</returns>
        public static Direction? Side(GameState state, Unit mover, Coord tile, Direction heading)
        {
            if (!Blocks(state, mover, tile))
            {
                return null;
            }

            var victim = state.UnitAt(tile);
            if (victim is null)
            {
                return null;
            }

            foreach (var side in Directions.All)
            {
                if (!IsPerpendicular(heading, side))
                {
                    continue;
                }

                var preview = Displacement.PreviewAuto(
                    state, victim.Id, From(tile, side), DisplacementKind.Push, Distance);

                // Vacating is the whole test. A shove that reports a distance but leaves the unit
                // where it stood (D-057) has not cleared the doorway, and the mover is still stopped.
                if (preview.Destination != tile)
                {
                    return side;
                }
            }

            return null;
        }

        /// <summary>
        /// Whether this tile is a body a trampler could be asked about at all: a unit is standing on
        /// it, the mover tramples, and it is not the mover itself.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="mover">Unit walking through.</param>
        /// <param name="tile">Tile to test.</param>
        /// <returns>Whether the trample question applies here.</returns>
        /// <remarks>
        /// A structure is never trampled. It is masonry, it does not step aside, and shouldering one
        /// would be a second way to damage an objective hidden inside the movement rules.
        /// </remarks>
        public static bool Blocks(GameState state, Unit? mover, Coord tile)
        {
            if (state is null || mover is null || !mover.Template.Tramples)
            {
                return false;
            }

            if (state.StructureAt(tile) is not null)
            {
                return false;
            }

            var victim = state.UnitAt(tile);
            return victim is not null && victim.Id != mover.Id;
        }

        /// <summary>
        /// Whether a trampler could shove the blocker on this tile <em>somewhere</em>, whichever way
        /// it arrives from. Used by the routing metric, which prices a tile before it knows the
        /// heading it will be crossed on.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="mover">Unit walking through.</param>
        /// <param name="tile">Tile to price.</param>
        /// <returns>Whether some approach could trample it.</returns>
        public static bool CouldTrample(GameState state, Unit mover, Coord tile)
        {
            if (!Blocks(state, mover, tile))
            {
                return false;
            }

            foreach (var heading in Directions.All)
            {
                if (Side(state, mover, tile, heading) is not null)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Knocks the blocker aside and reports what it cost it. Emits the trample, the contact
        /// damage and then the shove, in that order.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="mover">Unit walking through.</param>
        /// <param name="tile">Tile being entered.</param>
        /// <param name="heading">Direction the mover is walking.</param>
        /// <param name="events">Sink for the resulting events.</param>
        /// <returns>The state after the trample, unchanged when the blocker could not be moved.</returns>
        public static GameState Resolve(
            GameState state, Unit mover, Coord tile, Direction heading, List<GameEvent> events)
        {
            var side = Side(state, mover, tile, heading);
            var victim = state.UnitAt(tile);
            if (side is null || victim is null)
            {
                return state;
            }

            events.Add(new UnitTrampled(mover.Id, victim.Id, tile, heading, side.Value, ContactDamage));

            // Contact first, then the shove. A blocker the contact finishes off has already left the
            // doorway and there is nothing to shove — the walk simply continues over it.
            state = Combat.ApplyDamage(state, victim.Id, ContactDamage, DamageSource.Trample, events);

            if (!state.UnitById(victim.Id).IsOnBoard)
            {
                return state;
            }

            return Displacement.ResolveAuto(
                state, victim.Id, From(tile, side.Value), DisplacementKind.Push, Distance, events,
                by: mover.Id);
        }

        /// <summary>
        /// Every tile this unit could shoulder somebody off, given everywhere it can walk to.
        /// </summary>
        /// <remarks>
        /// A trample lane is a threatened tile in the ordinary sense — standing on one costs a hit
        /// point and a tile of position — so the overlay has to paint it and the round-one damage
        /// guarantee has to count it (D-080/D-089 lineage). Without this the agency law would read a
        /// Husk's reach as its attack alone and call a deployment safe that is not.
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="mover">Unit whose lanes to find.</param>
        /// <param name="stands">Tiles it could be standing on, its own included.</param>
        /// <returns>Tiles occupied by somebody it could shoulder through.</returns>
        public static IReadOnlyCollection<Coord> Lanes(
            GameState state, Unit mover, IReadOnlyCollection<Coord> stands)
        {
            var lanes = new HashSet<Coord>();
            if (state is null || mover is null || !mover.Template.Tramples || stands is null)
            {
                return lanes;
            }

            foreach (var stand in stands)
            {
                var from = mover with { Position = stand };
                foreach (var heading in Directions.All)
                {
                    var tile = stand.Step(heading);
                    if (Side(state, from, tile, heading) is not null)
                    {
                        lanes.Add(tile);
                    }
                }
            }

            return lanes;
        }

        /// <summary>
        /// The first body a planned route would shoulder through, so an intent can telegraph it.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="mover">Unit that would walk.</param>
        /// <param name="from">Tile it starts on.</param>
        /// <param name="path">Route it would walk, in order.</param>
        /// <param name="victim">The unit knocked aside.</param>
        /// <param name="at">Tile it is standing on.</param>
        /// <param name="aside">Direction it would be knocked.</param>
        /// <returns>Whether the route tramples anybody at all.</returns>
        public static bool FirstOnRoute(
            GameState state,
            Unit mover,
            Coord from,
            IReadOnlyList<Coord> path,
            out UnitId victim,
            out Coord at,
            out Direction aside)
        {
            victim = UnitId.None;
            at = from;
            aside = Direction.Up;

            if (state is null || mover is null || path is null || !mover.Template.Tramples)
            {
                return false;
            }

            var position = from;
            foreach (var step in path)
            {
                var heading = Directions.Toward(position, step);
                if (heading is not null && state.UnitAt(step) is { } blocked)
                {
                    var side = Side(state, mover with { Position = position }, step, heading.Value);
                    if (side is not null)
                    {
                        victim = blocked.Id;
                        at = step;
                        aside = side.Value;
                        return true;
                    }
                }

                position = step;
            }

            return false;
        }

        /// <summary>Whether two directions are at right angles.</summary>
        /// <param name="heading">Direction of travel.</param>
        /// <param name="side">Candidate side.</param>
        /// <returns>Whether the side is perpendicular to the heading.</returns>
        public static bool IsPerpendicular(Direction heading, Direction side)
        {
            bool headingVertical = heading == Direction.Up || heading == Direction.Down;
            bool sideVertical = side == Direction.Up || side == Direction.Down;
            return headingVertical != sideVertical;
        }

        // A push travels away from its source, so the synthetic source is the tile opposite the side.
        private static Coord From(Coord tile, Direction side) => tile.Step(side.Opposite());
    }
}
