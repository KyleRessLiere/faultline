namespace Faultline.Core
{
    /// <summary>
    /// One door: an edge from a node in one column to a node in the next.
    /// </summary>
    /// <remarks>
    /// The map's adjacency is a flat list of these rather than a per-node array of children, because
    /// the graph is sparse — Act 1 is thirteen nodes and fifteen edges — and because a flat list is
    /// the form the authored data reads best in: one line per door, in the order the doors are
    /// offered. <see cref="ActMap.Successors"/> is the only thing that walks it.
    /// </remarks>
    /// <param name="From">Id of the node the door leads out of.</param>
    /// <param name="To">Id of the node it leads to.</param>
    public readonly record struct MapEdge(string From, string To)
    {
        /// <inheritdoc/>
        public override string ToString() => From + " -> " + To;
    }
}
