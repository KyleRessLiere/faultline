using System;
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
        /// Every tile the unit can still walk to this activation, with the route Core would take.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The fastest route wins (D-097).</b> Routes are ranked by movement points first, damage
        /// taken second, and the fixed direction order N/E/S/W last — so the answer is one route, and
        /// always the same route. A damaging tile on the fastest way through is walked over and its
        /// entry effect applies: the player who wants around it says so with a second click, which is
        /// what segmented movement is for. This supersedes D-009, which routed around spikes first
        /// and left a unit taking the long way without being asked.
        /// </para>
        /// <para>
        /// Budgeted from <see cref="Unit.MoveRemaining"/>, not the full stat line, so a unit part-way
        /// through its move gets the tiles it can still reach from where it now stands.
        /// </para>
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Unit that would move.</param>
        /// <returns>Reachable destinations keyed by tile, excluding the unit's own tile.</returns>
        public static IReadOnlyDictionary<Coord, MoveOption> Reachable(GameState state, Unit unit)
        {
            var result = new Dictionary<Coord, MoveOption>();
            if (!unit.IsOnBoard || unit.MoveRemaining <= 0)
            {
                return result;
            }

            var board = state.Board;
            var best = new Dictionary<Coord, Node>();
            var settled = new HashSet<Coord>();
            best[unit.Position] = new Node(0, 0, null, Array.Empty<int>(), true);

            while (true)
            {
                if (!TryPickCheapest(best, settled, out var current))
                {
                    break;
                }

                settled.Add(current);
                var node = best[current];

                // N/E/S/W, which is the order Directions.All is held in. Iterating it is half the
                // tie-break: the earlier direction reaches the tile first and the later one has to
                // beat it outright to take it (D-097).
                for (int d = 0; d < Directions.All.Count; d++)
                {
                    var next = current.Step(Directions.All[d]);
                    if (!board.InBounds(next) || settled.Contains(next))
                    {
                        continue;
                    }

                    var tile = board.At(next);
                    if (!IsWalkable(tile))
                    {
                        continue;
                    }

                    // A body is a wall to everyone except something that shoulders through, and to
                    // that it is a wall only when it cannot be knocked aside from this heading
                    // (D-100). Terrain is unaffected either way: nothing tramples masonry.
                    bool trampling = false;
                    if (state.IsOccupied(next))
                    {
                        if (Trample.Side(state, unit, next, Directions.All[d]) is null)
                        {
                            continue;
                        }

                        trampling = true;
                    }

                    int cost = node.Cost + StepCost(tile, unit) + (trampling ? Trample.ExtraCost : 0);
                    if (cost > unit.MoveRemaining)
                    {
                        continue;
                    }

                    int spikes = node.Spikes + (tile == TileType.Spikes ? 1 : 0);
                    var candidate = new Node(spikes, cost, current, Extend(node.Steps, d), false);

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

                // A trampler walks *through* a body, never stops on one (D-100). The tile stays in
                // the search so routes may cross it, and leaves the answer so nothing can plan to
                // finish its move standing in somebody else's square — which is what an enemy
                // asked to close on a target would otherwise do, trampling the very unit it came to
                // hit and forfeiting the attack.
                if (state.IsOccupied(pair.Key))
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

        /// <summary>
        /// Movement points to enter a tile, from its terrain alone. A body standing on it costs
        /// <see cref="Trample.ExtraCost"/> more again, which the caller adds because only the search
        /// knows which heading the tile is being crossed on.
        /// </summary>
        /// <param name="tile">Terrain being entered.</param>
        /// <param name="unit">Unit doing the entering.</param>
        /// <returns>The cost in movement points.</returns>
        public static int StepCost(TileType tile, Unit unit)
        {
            // Brief §2: climbing onto HighGround costs +1, except for the Archer. Under the AP
            // turn that surcharge is denominated in AP like every other one (MASTER_DESIGN §3).
            if (tile == TileType.HighGround && !unit.Template.FreeClimb)
            {
                return Activation.ClimbCost;
            }

            // Brambles cost double to wade into, for AP users only (MASTER_DESIGN §3). Enemies keep
            // movement-point semantics, so terrain prices them exactly as it always did. The damage
            // for entering is unchanged and separate — this is the price of the step, not the wound.
            if (tile == TileType.Spikes && Activation.UsesActionPoints(unit))
            {
                return Activation.BrambleCost;
            }

            return Activation.StepCost;
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

        // D-097's ranking, in order: movement points, then damage, then the direction the step came
        // in on. Cost leads because the fastest route is the one the player asked for by clicking;
        // damage only separates routes that are equally fast, and a hazard on the quickest way
        // through is entered rather than dodged.
        private static bool IsBetter(Node candidate, Node existing)
        {
            if (candidate.Cost != existing.Cost)
            {
                return candidate.Cost < existing.Cost;
            }

            if (candidate.Spikes != existing.Spikes)
            {
                return candidate.Spikes < existing.Spikes;
            }

            // Equally fast and equally safe: N/E/S/W decides, compared from the first step rather
            // than the last, so "north then east" beats "east then north" the way a reader of the
            // rule would expect. Same route on any machine, in any order, every time.
            int order = CompareSteps(candidate.Steps, existing.Steps);
            if (order != 0)
            {
                return order < 0;
            }

            if (candidate.Prev.HasValue && existing.Prev.HasValue)
            {
                return Precedes(candidate.Prev.Value, existing.Prev.Value);
            }

            return false;
        }

        // Lexicographic over the direction sequence walked so far. Shorter wins a prefix tie, which
        // only arises between routes of equal cost across terrain of differing price.
        private static int CompareSteps(IReadOnlyList<int> a, IReadOnlyList<int> b)
        {
            int shared = a.Count < b.Count ? a.Count : b.Count;
            for (int i = 0; i < shared; i++)
            {
                if (a[i] != b[i])
                {
                    return a[i] < b[i] ? -1 : 1;
                }
            }

            return a.Count == b.Count ? 0 : (a.Count < b.Count ? -1 : 1);
        }

        private static IReadOnlyList<int> Extend(IReadOnlyList<int> steps, int direction)
        {
            var next = new int[steps.Count + 1];
            for (int i = 0; i < steps.Count; i++)
            {
                next[i] = steps[i];
            }

            next[steps.Count] = direction;
            return next;
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
            public Node(int spikes, int cost, Coord? prev, IReadOnlyList<int> steps, bool isStart)
            {
                Spikes = spikes;
                Cost = cost;
                Prev = prev;
                Steps = steps;
                IsStart = isStart;
            }

            public int Spikes { get; }

            public int Cost { get; }

            public Coord? Prev { get; }

            /// <summary>
            /// Indices into <see cref="Directions.All"/> of every step taken to reach this tile, in
            /// order. The whole sequence, because the tie-break reads from the first step.
            /// </summary>
            public IReadOnlyList<int> Steps { get; }

            public bool IsStart { get; }
        }
    }
}
