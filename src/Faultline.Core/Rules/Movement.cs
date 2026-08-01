using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// Voluntary movement. Displacement (Push/Pull) is a different system entirely and lands in
    /// Displacement.cs with M2.
    /// </summary>
    public static class Movement
    {
        /// <summary>
        /// Every tile the unit can walk to this activation, with the route Core would take.
        /// </summary>
        /// <remarks>
        /// Routes are chosen by minimising spike tiles entered first and movement cost second, so a
        /// unit never eats avoidable spike damage just because a shorter route ran over them
        /// (DECISIONS.md D-009). Remaining ties break on a fixed coordinate order, which is what makes
        /// the chosen path reproducible.
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Unit that would move.</param>
        /// <returns>Reachable destinations keyed by tile, excluding the unit's own tile.</returns>
        public static IReadOnlyDictionary<Coord, MoveOption> Reachable(GameState state, Unit unit)
        {
            var result = new Dictionary<Coord, MoveOption>();
            if (!unit.IsOnBoard)
            {
                return result;
            }

            var board = state.Board;
            var best = new Dictionary<Coord, Node>();
            var settled = new HashSet<Coord>();
            best[unit.Position] = new Node(0, 0, null, true);

            while (true)
            {
                if (!TryPickCheapest(best, settled, out var current))
                {
                    break;
                }

                settled.Add(current);
                var node = best[current];

                foreach (var direction in Directions.All)
                {
                    var next = current.Step(direction);
                    if (!board.InBounds(next) || settled.Contains(next))
                    {
                        continue;
                    }

                    var tile = board.At(next);
                    if (!IsWalkable(tile) || state.IsOccupied(next))
                    {
                        continue;
                    }

                    int cost = node.Cost + StepCost(tile, unit);
                    if (cost > unit.Move)
                    {
                        continue;
                    }

                    int spikes = node.Spikes + (tile == TileType.Spikes ? 1 : 0);
                    var candidate = new Node(spikes, cost, current, false);

                    if (!best.TryGetValue(next, out var existing) || IsBetter(candidate, existing))
                    {
                        best[next] = candidate;
                    }
                }
            }

            foreach (var pair in best)
            {
                if (pair.Value.IsStart)
                {
                    continue;
                }

                var path = BuildPath(best, unit.Position, pair.Key);
                result[pair.Key] = new MoveOption(pair.Key, path, pair.Value.Cost, pair.Value.Spikes);
            }

            return result;
        }

        /// <summary>Looks up a single destination.</summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Unit that would move.</param>
        /// <param name="destination">Tile to reach.</param>
        /// <param name="option">The route, when reachable.</param>
        /// <returns>Whether the destination is reachable this activation.</returns>
        public static bool TryGetMove(GameState state, Unit unit, Coord destination, out MoveOption option)
        {
            var reachable = Reachable(state, unit);
            return reachable.TryGetValue(destination, out option!);
        }

        /// <summary>True for terrain a unit may voluntarily walk onto.</summary>
        /// <param name="tile">Terrain to test.</param>
        /// <returns>Whether it can be entered on foot.</returns>
        /// <remarks>Pits are not voluntarily enterable — Brief §2 only ever puts units in them by displacement (DECISIONS.md D-004).</remarks>
        public static bool IsWalkable(TileType tile) =>
            tile == TileType.Open || tile == TileType.Spikes || tile == TileType.HighGround
            || tile == TileType.Cracked;

        /// <summary>Movement points to enter a tile.</summary>
        /// <param name="tile">Terrain being entered.</param>
        /// <param name="unit">Unit doing the entering.</param>
        /// <returns>The cost in movement points.</returns>
        public static int StepCost(TileType tile, Unit unit)
        {
            // Brief §2: climbing onto HighGround costs +1, except for the Archer.
            if (tile == TileType.HighGround && !unit.Template.FreeClimb)
            {
                return 2;
            }

            return 1;
        }

        private static bool TryPickCheapest(
            Dictionary<Coord, Node> best,
            HashSet<Coord> settled,
            out Coord chosen)
        {
            bool found = false;
            chosen = default;
            Node bestNode = default;

            foreach (var pair in best)
            {
                if (settled.Contains(pair.Key))
                {
                    continue;
                }

                if (!found || IsBetter(pair.Value, bestNode) || (Ties(pair.Value, bestNode) && Precedes(pair.Key, chosen)))
                {
                    found = true;
                    chosen = pair.Key;
                    bestNode = pair.Value;
                }
            }

            return found;
        }

        private static bool IsBetter(Node candidate, Node existing)
        {
            if (candidate.Spikes != existing.Spikes)
            {
                return candidate.Spikes < existing.Spikes;
            }

            if (candidate.Cost != existing.Cost)
            {
                return candidate.Cost < existing.Cost;
            }

            // Equal cost and equal damage: keep whichever route came from the earlier tile in
            // row-major order, so the path we hand back is always the same one.
            if (candidate.Prev.HasValue && existing.Prev.HasValue)
            {
                return Precedes(candidate.Prev.Value, existing.Prev.Value);
            }

            return false;
        }

        private static bool Ties(Node a, Node b) => a.Spikes == b.Spikes && a.Cost == b.Cost;

        private static bool Precedes(Coord a, Coord b) => a.Y != b.Y ? a.Y < b.Y : a.X < b.X;

        private static IReadOnlyList<Coord> BuildPath(
            Dictionary<Coord, Node> best,
            Coord start,
            Coord destination)
        {
            var reversed = new List<Coord>();
            var cursor = destination;
            while (cursor != start)
            {
                reversed.Add(cursor);
                var prev = best[cursor].Prev;
                if (!prev.HasValue)
                {
                    break;
                }

                cursor = prev.Value;
            }

            reversed.Reverse();
            return reversed;
        }

        private readonly struct Node
        {
            public Node(int spikes, int cost, Coord? prev, bool isStart)
            {
                Spikes = spikes;
                Cost = cost;
                Prev = prev;
                IsStart = isStart;
            }

            public int Spikes { get; }

            public int Cost { get; }

            public Coord? Prev { get; }

            public bool IsStart { get; }
        }
    }
}
