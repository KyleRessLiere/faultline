using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// A visible lane graph for one act (MASTER_DESIGN §8.5): columns of typed nodes, sparse doors
    /// between them, and the boss at the end of every lane. Authored data, not generated.
    /// </summary>
    /// <remarks>
    /// <para>
    /// v1 is hand-authored. The constraint generator and its proof log are acts-2-and-3 work, and a
    /// generator that produced Act 1 would have made the teaching act — the one board sequence the
    /// whole game is tuned against — a thing nobody could point at.
    /// </para>
    /// <para>
    /// Adjacency is the flat <see cref="Edges"/> list, walked on demand. Fifteen edges do not want an
    /// index, and a cached one would be mutable state on a record the run holds — the exact shape
    /// replay determinism forbids.
    /// </para>
    /// </remarks>
    public sealed record ActMap
    {
        /// <summary>Stable id.</summary>
        public string Id { get; init; } = string.Empty;

        /// <summary>Display name.</summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>Id of the node every run of this act opens on.</summary>
        public string StartNodeId { get; init; } = string.Empty;

        /// <summary>Every node, in authored order.</summary>
        public IReadOnlyList<MapNode> Nodes { get; init; } = Array.Empty<MapNode>();

        /// <summary>Every door, in the order they are offered.</summary>
        public IReadOnlyList<MapEdge> Edges { get; init; } = Array.Empty<MapEdge>();

        /// <summary>How many columns the map has.</summary>
        public int ColumnCount
        {
            get
            {
                int highest = -1;
                foreach (var node in Nodes)
                {
                    if (node.Column > highest)
                    {
                        highest = node.Column;
                    }
                }

                return highest + 1;
            }
        }

        /// <summary>Finds a node by id.</summary>
        /// <param name="id">Node id.</param>
        /// <returns>The node, or null when the map has none with that id.</returns>
        public MapNode? NodeAt(string id)
        {
            foreach (var node in Nodes)
            {
                if (string.Equals(node.Id, id, StringComparison.Ordinal))
                {
                    return node;
                }
            }

            return null;
        }

        /// <summary>The doors out of a node, in authored order.</summary>
        /// <param name="id">Node id.</param>
        /// <returns>Ids of the nodes it leads to. Empty for a terminal.</returns>
        public IReadOnlyList<string> Successors(string id)
        {
            var found = new List<string>();
            foreach (var edge in Edges)
            {
                if (string.Equals(edge.From, id, StringComparison.Ordinal))
                {
                    found.Add(edge.To);
                }
            }

            return found;
        }

        /// <summary>The doors into a node, in authored order.</summary>
        /// <param name="id">Node id.</param>
        /// <returns>Ids of the nodes that lead to it.</returns>
        public IReadOnlyList<string> Predecessors(string id)
        {
            var found = new List<string>();
            foreach (var edge in Edges)
            {
                if (string.Equals(edge.To, id, StringComparison.Ordinal))
                {
                    found.Add(edge.From);
                }
            }

            return found;
        }

        /// <summary>Every node in one column, in authored order.</summary>
        /// <param name="column">Zero-based column index.</param>
        /// <returns>The column's nodes.</returns>
        public IReadOnlyList<MapNode> ColumnAt(int column)
        {
            var found = new List<MapNode>();
            foreach (var node in Nodes)
            {
                if (node.Column == column)
                {
                    found.Add(node);
                }
            }

            return found;
        }

        /// <summary>Every node with no door out of it — where a run of this act can end.</summary>
        /// <returns>The terminal nodes, in authored order.</returns>
        public IReadOnlyList<MapNode> Terminals()
        {
            var found = new List<MapNode>();
            foreach (var node in Nodes)
            {
                if (Successors(node.Id).Count == 0)
                {
                    found.Add(node);
                }
            }

            return found;
        }

        /// <summary>Every <c>.fight</c> this map can field, in authored order and without duplicates.</summary>
        /// <returns>The fight ids.</returns>
        public IReadOnlyList<string> FightIds()
        {
            var ids = new List<string>();
            foreach (var node in Nodes)
            {
                if (node.IsCombat && node.FightId.Length > 0 && !ids.Contains(node.FightId))
                {
                    ids.Add(node.FightId);
                }
            }

            return ids;
        }

        /// <summary>Whether one node can be walked to from another by following doors.</summary>
        /// <param name="from">Node to start at.</param>
        /// <param name="to">Node to look for.</param>
        /// <returns>True when some route leads there. A node reaches itself only through a cycle.</returns>
        public bool Reaches(string from, string to)
        {
            var seen = new List<string>();
            var frontier = new Queue<string>();
            frontier.Enqueue(from);

            while (frontier.Count > 0)
            {
                string here = frontier.Dequeue();
                foreach (string next in Successors(here))
                {
                    if (string.Equals(next, to, StringComparison.Ordinal))
                    {
                        return true;
                    }

                    if (!seen.Contains(next))
                    {
                        seen.Add(next);
                        frontier.Enqueue(next);
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Whether a node is the act's pre-boss Still Pond — the floor §8.8 says every path reaches.
        /// </summary>
        /// <remarks>
        /// Derived, not authored (D-180, following D-162). A pond is the floor when every door out of
        /// it opens onto the boss, which is what "pre-boss" means on a graph whose edges step exactly
        /// one column. An authored flag would have been a second place for the same fact to live, and
        /// a map edited later could have moved the pond without moving the flag.
        /// </remarks>
        /// <param name="id">Node id.</param>
        /// <returns>True when it is a Rest node and every door out of it leads to the boss.</returns>
        public bool IsPreBossRest(string id)
        {
            if (NodeAt(id) is not { Type: MapNodeType.Rest })
            {
                return false;
            }

            var doors = Successors(id);
            if (doors.Count == 0)
            {
                return false;
            }

            foreach (string door in doors)
            {
                if (NodeAt(door) is not { Type: MapNodeType.Boss })
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Everything structurally wrong with the map, as sentences. Empty means the graph is sound.
        /// </summary>
        /// <remarks>
        /// Authored data gets a linter for the same reason a <c>.fight</c> file does: the failure mode
        /// of a hand-written graph is not a crash, it is a door that leads nowhere and a lane that
        /// quietly cannot reach the boss. The constraint generator will need exactly these checks as
        /// its acceptance test, so they are written against the authored map first.
        /// </remarks>
        /// <returns>One line per problem.</returns>
        public IReadOnlyList<string> Validate()
        {
            var issues = new List<string>();

            if (NodeAt(StartNodeId) is null)
            {
                issues.Add("The start node '" + StartNodeId + "' is not on the map.");
            }

            var ids = new List<string>();
            foreach (var node in Nodes)
            {
                if (node.Id.Length == 0)
                {
                    issues.Add("A node has no id.");
                }
                else if (ids.Contains(node.Id))
                {
                    issues.Add("Two nodes share the id '" + node.Id + "'.");
                }
                else
                {
                    ids.Add(node.Id);
                }

                if (node.IsCombat && node.FightId.Length == 0)
                {
                    issues.Add("Node '" + node.Id + "' is a " + node.Type + " and names no fight.");
                }

                if (node.Type == MapNodeType.Event && node.EventId.Length == 0)
                {
                    issues.Add("Node '" + node.Id + "' is an event and names no event.");
                }

                if (!string.Equals(node.Id, StartNodeId, StringComparison.Ordinal)
                    && Predecessors(node.Id).Count == 0)
                {
                    issues.Add("Node '" + node.Id + "' has no door into it.");
                }
            }

            foreach (var edge in Edges)
            {
                var from = NodeAt(edge.From);
                var to = NodeAt(edge.To);

                if (from is null)
                {
                    issues.Add("Edge " + edge + " leaves a node the map does not have.");
                    continue;
                }

                if (to is null)
                {
                    issues.Add("Edge " + edge + " leads to a node the map does not have.");
                    continue;
                }

                // Columns are the map's only ordering, and a door that does not step exactly one
                // column forward is either a cycle or a skipped column — both of which would make
                // "the next column" meaningless to a renderer and to the vote.
                if (to.Column != from.Column + 1)
                {
                    issues.Add(
                        "Edge " + edge + " runs from column " + from.Column + " to column "
                        + to.Column + "; doors step exactly one column forward.");
                }
            }

            var terminals = Terminals();
            if (terminals.Count != 1)
            {
                issues.Add("The map has " + terminals.Count + " terminal nodes; an act ends at one boss.");
            }
            else if (terminals[0].Type != MapNodeType.Boss)
            {
                issues.Add("The map ends on '" + terminals[0].Id + "', which is a " + terminals[0].Type + ".");
            }

            // The boss is found by type rather than by being the terminal, so that "a lane cannot
            // reach the boss" is reported for the lane that is broken and not swallowed by the
            // terminal count. A map with two dead ends should say which two.
            MapNode? boss = null;
            foreach (var node in Nodes)
            {
                if (node.Type == MapNodeType.Boss)
                {
                    if (boss is not null)
                    {
                        issues.Add("The map has more than one boss node.");
                        boss = null;
                        break;
                    }

                    boss = node;
                }
            }

            if (boss is null && !issues.Contains("The map has more than one boss node."))
            {
                issues.Add("The map has no boss node.");
            }

            foreach (var node in Nodes)
            {
                if (!string.Equals(node.Id, StartNodeId, StringComparison.Ordinal)
                    && !Reaches(StartNodeId, node.Id))
                {
                    issues.Add("Node '" + node.Id + "' cannot be reached from the start.");
                }

                if (boss is not null
                    && !string.Equals(node.Id, boss.Id, StringComparison.Ordinal)
                    && !Reaches(node.Id, boss.Id))
                {
                    issues.Add("Node '" + node.Id + "' is a dead end: it cannot reach the boss.");
                }
            }

            return issues;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Hand-written for the reason every equality in this codebase is: a record's generated
        /// members compare <see cref="Nodes"/> and <see cref="Edges"/> by reference.
        /// </remarks>
        /// <param name="other">Map to compare with.</param>
        /// <returns>Whether the two maps are the same graph.</returns>
        public bool Equals(ActMap? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (!string.Equals(Id, other.Id, StringComparison.Ordinal)
                || !string.Equals(Name, other.Name, StringComparison.Ordinal)
                || !string.Equals(StartNodeId, other.StartNodeId, StringComparison.Ordinal)
                || Nodes.Count != other.Nodes.Count
                || Edges.Count != other.Edges.Count)
            {
                return false;
            }

            for (int i = 0; i < Nodes.Count; i++)
            {
                if (!Nodes[i].Equals(other.Nodes[i]))
                {
                    return false;
                }
            }

            for (int i = 0; i < Edges.Count; i++)
            {
                if (!Edges[i].Equals(other.Edges[i]))
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
                int hash = StringComparer.Ordinal.GetHashCode(Id ?? string.Empty);
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(Name ?? string.Empty);
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(StartNodeId ?? string.Empty);
                foreach (var node in Nodes)
                {
                    hash = (hash * 31) + node.GetHashCode();
                }

                foreach (var edge in Edges)
                {
                    hash = (hash * 31) + edge.GetHashCode();
                }

                return hash;
            }
        }
    }
}
