using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// The node-type to handler table. Fixed at type-load and never added to at run time.
    /// </summary>
    /// <remarks>
    /// A registry that could be written to would be a way for one run to change how another one
    /// resolves, which is exactly the kind of hidden state replay determinism forbids. Registering a
    /// new node type is editing this table, in a commit, with its tests.
    /// </remarks>
    public static class CampaignNodeHandlers
    {
        private static readonly IReadOnlyDictionary<Type, CampaignNodeHandler> Table =
            new Dictionary<Type, CampaignNodeHandler>
            {
                [typeof(FightNode)] = new FightNodeHandler(),
                [typeof(RestNode)] = new RestNodeHandler(),
            };

        /// <summary>The handler for a node.</summary>
        /// <param name="node">Node to resolve.</param>
        /// <returns>Its handler.</returns>
        /// <exception cref="ArgumentNullException">The node was null.</exception>
        /// <exception cref="NotSupportedException">No handler is registered for that node type.</exception>
        public static CampaignNodeHandler For(CampaignNode node)
        {
            if (node is null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (Table.TryGetValue(node.GetType(), out var handler))
            {
                return handler;
            }

            throw new NotSupportedException(
                "No handler registered for node type " + node.GetType().Name
                + ". Add one to CampaignNodeHandlers.");
        }

        /// <summary>Whether a node type has a handler.</summary>
        /// <param name="nodeType">Node record type.</param>
        /// <returns>Whether it is registered.</returns>
        public static bool IsRegistered(Type nodeType) => nodeType is not null && Table.ContainsKey(nodeType);

        /// <summary>How many node types the game knows about.</summary>
        public static int Count => Table.Count;
    }
}
