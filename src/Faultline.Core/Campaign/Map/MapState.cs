using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// Where a run stands on its act map: the node it is on, the route it took to get there, and
    /// whether the act is finished.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The visited set is kept as an ordered <see cref="Route"/> rather than a set, because the order
    /// <em>is</em> the interesting fact: two runs that visited the same six nodes in different orders
    /// are different runs, and <see cref="RouteHash"/> — the thing a determinism test compares — has
    /// to tell them apart. Membership is the cheap question on top of it, not the other way round.
    /// </para>
    /// <para>
    /// Immutable, like everything else on the run seam. <see cref="MoveTo"/> is the only transition.
    /// </para>
    /// </remarks>
    public sealed record MapState
    {
        /// <summary>Node the run is standing on, or has entered.</summary>
        public string CurrentNodeId { get; init; } = string.Empty;

        /// <summary>Every node the run has stood on, oldest first, current last.</summary>
        public IReadOnlyList<string> Route { get; init; } = Array.Empty<string>();

        /// <summary>True once the act's terminal node has been cleared.</summary>
        public bool Completed { get; init; }

        /// <summary>How deep into the act the run is: how many nodes it has stood on.</summary>
        public int Depth => Route.Count;

        /// <summary>A run standing on the first node of a map.</summary>
        /// <param name="startNodeId">Id of the map's start node.</param>
        /// <returns>The opening map state.</returns>
        public static MapState At(string startNodeId) => new MapState
        {
            CurrentNodeId = startNodeId ?? string.Empty,
            Route = new[] { startNodeId ?? string.Empty },
        };

        /// <summary>Whether the run has stood on a node.</summary>
        /// <param name="nodeId">Node id.</param>
        /// <returns>Whether it is on the route.</returns>
        public bool Visited(string nodeId)
        {
            foreach (string id in Route)
            {
                if (string.Equals(id, nodeId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Steps to the next node, appending it to the route.</summary>
        /// <param name="nodeId">Node moved to.</param>
        /// <returns>The map state after the move.</returns>
        public MapState MoveTo(string nodeId)
        {
            if (nodeId is null)
            {
                throw new ArgumentNullException(nameof(nodeId));
            }

            var route = new List<string>(Route.Count + 1);
            route.AddRange(Route);
            route.Add(nodeId);

            return this with { CurrentNodeId = nodeId, Route = route };
        }

        /// <summary>
        /// A hash of the route taken, in order. The number a replay compares: identical seed plus
        /// identical command log must produce an identical route hash, coin flips included.
        /// </summary>
        /// <remarks>
        /// Its own hash rather than <see cref="GetHashCode"/> so that it is stable and means one
        /// thing — the sequence of nodes — while the state hash may grow other members later. Written
        /// out longhand (FNV-1a over the ordinal characters) rather than calling
        /// <c>string.GetHashCode</c>, whose value is randomised per process on .NET Core and would
        /// make the route hash differ between two runs of the same test.
        /// </remarks>
        /// <returns>The route hash.</returns>
        public int RouteHash()
        {
            unchecked
            {
                uint hash = 2166136261u;
                foreach (string id in Route)
                {
                    foreach (char c in id)
                    {
                        hash ^= c;
                        hash *= 16777619u;
                    }

                    // A separator, so ["ab","c"] and ["a","bc"] are different routes.
                    hash ^= 0x1Fu;
                    hash *= 16777619u;
                }

                return (int)hash;
            }
        }

        /// <inheritdoc/>
        public override string ToString() => string.Join(" > ", Route) + (Completed ? " (act cleared)" : string.Empty);

        /// <inheritdoc/>
        /// <param name="other">State to compare with.</param>
        /// <returns>Whether the two runs stand in the same place, having come the same way.</returns>
        public bool Equals(MapState? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (!string.Equals(CurrentNodeId, other.CurrentNodeId, StringComparison.Ordinal)
                || Completed != other.Completed
                || Route.Count != other.Route.Count)
            {
                return false;
            }

            for (int i = 0; i < Route.Count; i++)
            {
                if (!string.Equals(Route[i], other.Route[i], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = RouteHash();
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(CurrentNodeId ?? string.Empty);
                hash = (hash * 31) + (Completed ? 1 : 0);
                return hash;
            }
        }
    }
}
