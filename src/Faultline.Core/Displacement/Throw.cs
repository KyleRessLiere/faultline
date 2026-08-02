using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// The third displacement verb: a unit is picked up and put down somewhere, rather than shoved
    /// along a line. Only the Fisher's Cast produces one (D-091).
    /// </summary>
    /// <remarks>
    /// <para>
    /// A throw is a <b>lob</b>. Nothing between the thrower and the landing tile matters — not walls,
    /// not bodies, not hazards — because the target goes over them. That is the whole reason it is a
    /// separate verb rather than a long push: a push is a conversation with every tile it crosses,
    /// and this is a conversation with exactly one.
    /// </para>
    /// <para>
    /// <b>Push resistance does not apply.</b> An Anchor shrugs off a shove because it is braced
    /// against the ground; it has nothing to brace against once it is in the air. This is what makes
    /// Cast the answer to the units the rest of the displacement system cannot move, and it is
    /// deliberate rather than an oversight.
    /// </para>
    /// <para>
    /// <b>Footing still helps, differently.</b> A token buys one tile back along the throw line
    /// toward the thrower — digging in mid-flight is nonsense, but scrabbling short of where you
    /// were aimed is not, and it keeps the token meaningful against the one displacement it cannot
    /// otherwise resist.
    /// </para>
    /// </remarks>
    public static class Throw
    {
        /// <summary>How far off the Fisher can pluck somebody. The grab is a lob and ignores everything between.</summary>
        public const int GrabRange = 3;

        /// <summary>
        /// How far from the Fisher a target may be put down: her four orthogonal tiles and nothing
        /// else. She reaches out a long way and brings them in close.
        /// </summary>
        public const int LandingRadius = 1;

        /// <summary>Tiles a Footing token buys back along the throw line.</summary>
        public const int FootingShortens = 1;

        /// <summary>
        /// Every tile this thrower could put a target down on: in range, in bounds, walkable and
        /// empty. In a fixed scan order so the list is reproducible.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="thrower">Unit doing the throwing.</param>
        /// <param name="targetId">Unit being thrown; its own tile counts as vacated.</param>
        /// <returns>The landing tiles.</returns>
        public static IReadOnlyList<Coord> Landings(GameState state, Unit thrower, UnitId targetId)
        {
            var tiles = new List<Coord>();
            if (state is null || thrower is null || !thrower.IsOnBoard)
            {
                return tiles;
            }

            // Her four orthogonal tiles, in the fixed direction order. Not a radius scan: the game is
            // 4-way everywhere else (D-002) and a diagonal landing would be the only exception.
            foreach (var direction in Directions.All)
            {
                var tile = thrower.Position.Step(direction);
                if (IsLanding(state, tile, targetId))
                {
                    tiles.Add(tile);
                }
            }

            return tiles;
        }

        /// <summary>
        /// Enemies this Fisher could pluck: within grab range, on the board, and not already on a
        /// ledge. Nothing between her and them is consulted — that is what makes it a lob, and it is
        /// how she pulls a Lobber out from behind its own screen.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="thrower">The Fisher.</param>
        /// <returns>The grabbable enemies, in unit-id order.</returns>
        public static IReadOnlyList<Unit> Grabbable(GameState state, Unit thrower)
        {
            var targets = new List<Unit>();
            if (state is null || thrower is null || !thrower.IsOnBoard)
            {
                return targets;
            }

            foreach (var unit in state.Units)
            {
                if (unit.Team != Team.Enemy || !unit.IsOnBoard || unit.Clinging)
                {
                    continue;
                }

                int distance = thrower.Position.DistanceTo(unit.Position);
                if (distance == 0 || distance > GrabRange)
                {
                    continue;
                }

                // A grab with nowhere to put it is not a spend anybody should be offered.
                if (Landings(state, thrower, unit.Id).Count > 0)
                {
                    targets.Add(unit);
                }
            }

            return targets;
        }

        /// <summary>Whether a tile can be landed on: walkable, in bounds and nobody else there.</summary>
        /// <param name="state">Current state.</param>
        /// <param name="tile">Tile to test.</param>
        /// <param name="targetId">Unit being thrown, whose own tile is about to be vacated.</param>
        /// <returns>Whether it is a legal landing.</returns>
        public static bool IsLanding(GameState state, Coord tile, UnitId targetId)
        {
            if (state is null || !state.Board.InBounds(tile))
            {
                return false;
            }

            // "Unoccupied and non-wall" — which is looser than walkable on purpose. A drain is not
            // somewhere you may walk (D-004) and is very much somewhere you may be put, so testing
            // walkability here would quietly delete the best thing Cast does.
            if (state.Board.At(tile) == TileType.Wall || state.StructureAt(tile) is not null)
            {
                return false;
            }

            var occupant = state.UnitAt(tile);
            return occupant is null || occupant.Id == targetId;
        }

        /// <summary>
        /// Picks a unit up and puts it down, applying whatever the landing tile does to something
        /// that arrives on it displaced.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="throwerId">Unit doing the throwing.</param>
        /// <param name="targetId">Unit being thrown.</param>
        /// <param name="landing">Tile to put it down on.</param>
        /// <param name="events">Sink for the resulting events.</param>
        /// <returns>The state after the throw resolved.</returns>
        public static GameState Resolve(
            GameState state,
            UnitId throwerId,
            UnitId targetId,
            Coord landing,
            List<GameEvent> events)
        {
            var thrower = state.UnitById(throwerId);
            var before = state.UnitById(targetId);

            var destination = landing;
            var shortened = Shortened(state, before, landing);
            bool spendFooting = shortened is not null;

            if (spendFooting)
            {
                destination = shortened!.Value;
                var updated = before with { Footing = before.Footing - 1 };
                state = state.WithUnit(updated);
                events.Add(new FootingSpent(targetId, updated.Footing));
            }

            state = state.WithUnit(state.UnitById(targetId) with { Position = destination });

            // Distance is how far it actually travelled, so a shortened throw says so.
            events.Add(new UnitPushed(
                targetId,
                before.Position,
                destination,
                new[] { destination },
                DisplacementKind.Throw,
                before.Position.DistanceTo(destination)));

            return Land(state, targetId, destination, events);
        }

        /// <summary>
        /// What the ground does to something that arrives on it. The same outcomes a shove produces,
        /// because being put down hard is being put down hard.
        /// </summary>
        private static GameState Land(
            GameState state, UnitId targetId, Coord destination, List<GameEvent> events)
        {
            switch (state.Board.At(destination))
            {
                case TileType.Spikes:
                    events.Add(new SpikeHit(targetId, destination, Displacement.SpikeDamage, false));
                    state = Combat.ApplyDamage(
                        state, targetId, Displacement.SpikeDamage, DamageSource.Spikes, events);

                    if (state.UnitById(targetId).IsOnBoard)
                    {
                        state = state.WithUnit(state.UnitById(targetId) with { Staggered = true });
                        events.Add(new Staggered(targetId));
                    }

                    return state;

                case TileType.Pit:
                    state = state.WithUnit(state.UnitById(targetId) with
                    {
                        Clinging = true,
                        ClingingSinceRound = state.Round,
                    });
                    events.Add(new Clinging(targetId, destination));
                    return state;

                default:
                    // Open ground, or high ground the lob went over the lip of. A push cannot go up
                    // onto a ledge; a throw is not travelling along the ground to be stopped by one.
                    return state;
            }
        }

        /// <summary>
        /// Where a Footing token would put the target instead, or <c>null</c> when the token buys
        /// nothing and is therefore not spent.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The spec words this as shortening the throw "toward the Fisher", which held when she threw
        /// people away from herself. She now grabs at range and lands them on her own doorstep, so
        /// the flight travels toward her and one more tile toward her is the tile she is standing on.
        /// The reading that leaves the token meaning anything is the literal one: the throw is
        /// <em>shortened</em>, so it stops a tile early, back toward where the target was grabbed
        /// from (D-091).
        /// </para>
        /// <para>
        /// A short flight is rarely straight, so "a tile early" has two candidates — one per axis.
        /// The larger offset gives first and x breaks a tie, and if that tile is occupied or a wall
        /// the other is tried. Without the second try the token would do nothing whenever the first
        /// candidate happened to be the Fisher herself, which is most diagonal throws.
        /// </para>
        /// <para>
        /// Deterministic, and the same shape as the enemy's shove rule (Brief §2): spend only when it
        /// changes the outcome for the better, never as a reflex.
        /// </para>
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="target">Unit being thrown, at its original tile.</param>
        /// <param name="landing">Where it was aimed.</param>
        /// <returns>The shortened tile, or null when no token is spent.</returns>
        public static Coord? Shortened(GameState state, Unit target, Coord landing)
        {
            if (state is null || target is null || target.Footing <= 0 || target.Team != Team.Enemy)
            {
                return null;
            }

            int aimed = Harm(state, landing);

            foreach (var candidate in ShortCandidates(target.Position, landing))
            {
                if (candidate == landing || !IsLanding(state, candidate, target.Id))
                {
                    continue;
                }

                if (Harm(state, candidate) < aimed)
                {
                    return candidate;
                }
            }

            return null;
        }

        /// <summary>
        /// The tiles one step short of the landing, along each axis of the flight, larger offset
        /// first and x breaking a tie.
        /// </summary>
        /// <param name="from">Where the throw started: the target's own tile.</param>
        /// <param name="landing">Where it was aimed.</param>
        /// <returns>Up to two candidates, in a fixed order.</returns>
        public static IReadOnlyList<Coord> ShortCandidates(Coord from, Coord landing)
        {
            var candidates = new List<Coord>();

            int dx = landing.X - from.X;
            int dy = landing.Y - from.Y;

            var alongX = new Coord(landing.X - System.Math.Sign(dx), landing.Y);
            var alongY = new Coord(landing.X, landing.Y - System.Math.Sign(dy));

            bool xFirst = System.Math.Abs(dx) >= System.Math.Abs(dy);

            if (dx != 0)
            {
                candidates.Add(alongX);
            }

            if (dy != 0)
            {
                if (xFirst)
                {
                    candidates.Add(alongY);
                }
                else
                {
                    candidates.Insert(0, alongY);
                }
            }

            return candidates;
        }

        /// <summary>How bad a landing tile is, for the deterministic Footing decision.</summary>
        private static int Harm(GameState state, Coord tile) => state.Board.At(tile) switch
        {
            TileType.Pit => 2,
            TileType.Spikes => 1,
            _ => 0,
        };

    }
}
