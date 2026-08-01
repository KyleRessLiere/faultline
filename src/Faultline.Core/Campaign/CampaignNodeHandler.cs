using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// What a kind of <see cref="CampaignNode"/> does. One handler per node type; the run engine owns
    /// the walk, and the handler owns the step.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the seam. Adding a node type — an event, a choice of upgrade, a shop — is three things:
    /// a <see cref="CampaignNode"/> record for its data, a <see cref="RunCommand"/> record for its
    /// input if it takes any, and a handler here. Nothing in
    /// <see cref="Campaign.ApplyRun(RunState, RunCommand)"/> changes, because the engine only ever
    /// asks the handler two questions: what happens when you are entered, and what is legal while you
    /// hold control.
    /// </para>
    /// <para>
    /// A handler must be deterministic and stateless. Every handler is a singleton shared by every
    /// run in the process, so anything it remembers between calls would leak across runs and break
    /// replay — state lives in <see cref="RunState"/> and nowhere else.
    /// </para>
    /// </remarks>
    public abstract class CampaignNodeHandler
    {
        /// <summary>The node record this handler is registered for.</summary>
        public abstract Type NodeType { get; }

        /// <summary>
        /// Resolves entering the node. A handler that finishes immediately — a rest — advances the
        /// run itself with <see cref="Campaign.Advance"/>. One that holds control — a fight — leaves
        /// the index where it is and sets a phase saying so.
        /// </summary>
        /// <param name="state">Run state at the moment of entry.</param>
        /// <param name="node">The node being entered.</param>
        /// <param name="context">Sinks for what happens.</param>
        /// <returns>The state after entry.</returns>
        public abstract RunState Enter(RunState state, CampaignNode node, RunContext context);

        /// <summary>
        /// Whether this node is still holding control and expecting commands. False for anything that
        /// resolves the moment it is entered.
        /// </summary>
        /// <param name="state">Current run state.</param>
        /// <returns>Whether the node wants more commands.</returns>
        public virtual bool HoldsControl(RunState state) => false;

        /// <summary>
        /// Applies a command while this node holds control. Only reached when
        /// <see cref="HoldsControl"/> is true, and only with a command this handler declared legal.
        /// </summary>
        /// <param name="state">Current run state.</param>
        /// <param name="node">The node holding control.</param>
        /// <param name="command">The command.</param>
        /// <param name="context">Sinks for what happens.</param>
        /// <returns>The state after the command.</returns>
        public virtual RunState Step(RunState state, CampaignNode node, RunCommand command, RunContext context) =>
            throw new InvalidOperationException(
                GetType().Name + " does not hold control, so it takes no commands.");

        /// <summary>
        /// Everything legal while this node holds control. Empty for a node that resolves on entry.
        /// </summary>
        /// <param name="state">Current run state.</param>
        /// <param name="node">The node holding control.</param>
        /// <returns>The legal commands.</returns>
        public virtual IReadOnlyList<RunCommand> LegalSteps(RunState state, CampaignNode node) =>
            Array.Empty<RunCommand>();
    }
}
