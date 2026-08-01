using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// The two event sinks a node handler writes into while it works.
    /// </summary>
    /// <remarks>
    /// Run events and fight events are kept apart rather than merged into one stream. A renderer
    /// animates them differently — a shove is a thing that moves on a board, a unit being lost for the
    /// run is a line on a roster — and a handler that has no fight, like <see cref="RestNodeHandler"/>,
    /// simply never touches the second list.
    /// </remarks>
    public sealed class RunContext
    {
        /// <summary>Creates a context over two fresh sinks.</summary>
        public RunContext()
            : this(new List<RunEvent>(), new List<GameEvent>())
        {
        }

        /// <summary>Creates a context over existing sinks.</summary>
        /// <param name="runEvents">Sink for run events.</param>
        /// <param name="fightEvents">Sink for combat events.</param>
        public RunContext(List<RunEvent> runEvents, List<GameEvent> fightEvents)
        {
            RunEvents = runEvents ?? throw new ArgumentNullException(nameof(runEvents));
            FightEvents = fightEvents ?? throw new ArgumentNullException(nameof(fightEvents));
        }

        /// <summary>What happened at the run level.</summary>
        public List<RunEvent> RunEvents { get; }

        /// <summary>What happened inside the fight, in order.</summary>
        public List<GameEvent> FightEvents { get; }
    }
}
