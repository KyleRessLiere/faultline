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
    /// toward, push resistance eating the tile, Footing refusing the instance — any of them and the
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
        /// is doing the work: it is how push resistance, a Footing refusal and a body already standing
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
            return victim is null ? null : SideFor(state, victim, tile, heading, Distance);
        }

        /// <summary>
        /// The side a body standing on <paramref name="tile"/> would be knocked toward by something
        /// walking through it on <paramref name="heading"/>, or <c>null</c> when it cannot vacate and
        /// is therefore a wall.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The Shoulder's geometry, without the Shoulder's owner.</b> <see cref="Side"/> asks this
        /// after establishing that the mover is a trampler at all; the Vanguard's Overrun asks it
        /// directly, because a player verb that reproduces this resolution rather than calling it is
        /// exactly the second copy that ends up disagreeing about which way a body goes.
        /// </para>
        /// <para>
        /// Sides are the two directions perpendicular to the heading, considered in the fixed order
        /// N/E/S/W, and a side counts only when the body ends up standing on it. That clause is doing
        /// the work: push resistance, a Footing refusal and a body already standing in the way all turn
        /// the shove into a halt without any of them needing a rule here, because it asks
        /// <see cref="Displacement.PreviewAuto"/> and reads where the body really stops.
        /// </para>
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="victim">Body standing in the way.</param>
        /// <param name="tile">Tile it is standing on.</param>
        /// <param name="heading">Direction the mover is walking.</param>
        /// <param name="distance">Tiles to knock it.</param>
        /// <returns>The side it is knocked toward, or <c>null</c>.</returns>
        public static Direction? SideFor(
            GameState state, Unit victim, Coord tile, Direction heading, int distance)
        {
            if (state is null || victim is null || distance <= 0)
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
                    state, victim.Id, From(tile, side), DisplacementKind.Push, distance);

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
        /// Knocks one body aside and reports what it cost it: the trample event, the contact damage
        /// when there is any, and then the shove through the shared pipeline.
        /// </summary>
        /// <remarks>
        /// <b>One resolution, two callers.</b> The Husk's Shoulder passes
        /// <see cref="ContactDamage"/>; the Vanguard's Overrun passes zero, because §4 gives his
        /// charge base contact damage 0 and the alternate action does not change that. Everything
        /// after the damage — collisions, drain entries, Stagger, resistance and Footing — comes from
        /// <see cref="Displacement.ResolveAuto"/> and from nowhere else.
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="moverId">Unit walking through.</param>
        /// <param name="victimId">Body being knocked aside.</param>
        /// <param name="tile">Tile it is standing on.</param>
        /// <param name="heading">Direction the mover is walking.</param>
        /// <param name="side">Side it is knocked toward.</param>
        /// <param name="contactDamage">Hit points the contact takes; zero for a contactless shoulder.</param>
        /// <param name="distance">Tiles to knock it.</param>
        /// <param name="events">Sink for the resulting events.</param>
        /// <returns>The state after the shoulder.</returns>
        public static GameState Shoulder(
            GameState state,
            UnitId moverId,
            UnitId victimId,
            Coord tile,
            Direction heading,
            Direction side,
            int contactDamage,
            int distance,
            List<GameEvent> events)
        {
            events.Add(new UnitTrampled(moverId, victimId, tile, heading, side, contactDamage));

            // Contact first, then the shove. A body the contact finishes off has already left the
            // doorway and there is nothing to shove — the walk simply continues over it.
            if (contactDamage > 0)
            {
                state = Combat.ApplyDamage(state, victimId, contactDamage, DamageSource.Trample, events);

                if (!state.UnitById(victimId).IsOnBoard)
                {
                    return state;
                }
            }

            return Displacement.ResolveAuto(
                state, victimId, From(tile, side), DisplacementKind.Push, distance, events, by: moverId);
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
            return side is null || victim is null
                ? state
                : Shoulder(
                    state, mover.Id, victim.Id, tile, heading, side.Value, ContactDamage, Distance,
                    events);
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
