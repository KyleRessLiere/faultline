using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// What one run command produced, mirroring <see cref="StepResult"/> one level up.
    /// </summary>
    /// <remarks>
    /// Run events and fight events arrive separately rather than interleaved. A renderer wants to
    /// animate a shove on a board and print "the Archer is gone for the run" in a roster panel, and
    /// those are different jobs on different surfaces; merging them would only mean sorting them apart
    /// again.
    /// </remarks>
    public sealed class RunStepResult
    {
        /// <summary>Creates a result.</summary>
        /// <param name="newState">State after the command.</param>
        /// <param name="events">What happened at the run level.</param>
        /// <param name="fightEvents">What happened inside the fight, in order.</param>
        /// <param name="legalNext">Commands legal from the new state.</param>
        /// <param name="finalBoard">
        /// The board this command finished on, when the run has already left it. Defaults to the new
        /// state's own fight.
        /// </param>
        public RunStepResult(
            RunState newState,
            IReadOnlyList<RunEvent> events,
            IReadOnlyList<GameEvent> fightEvents,
            IReadOnlyList<RunCommand> legalNext,
            GameState? finalBoard = null)
        {
            NewState = newState ?? throw new ArgumentNullException(nameof(newState));
            Events = events ?? throw new ArgumentNullException(nameof(events));
            FightEvents = fightEvents ?? throw new ArgumentNullException(nameof(fightEvents));
            LegalNext = legalNext ?? throw new ArgumentNullException(nameof(legalNext));
            FinalBoard = finalBoard ?? newState.Fight;
        }

        /// <summary>The run after the command.</summary>
        public RunState NewState { get; }

        /// <summary>What happened at the run level, in order.</summary>
        public IReadOnlyList<RunEvent> Events { get; }

        /// <summary>What happened inside the fight, in order.</summary>
        public IReadOnlyList<GameEvent> FightEvents { get; }

        /// <summary>Everything legal from <see cref="NewState"/>.</summary>
        public IReadOnlyList<RunCommand> LegalNext { get; }

        /// <summary>
        /// The board this command finished on, even when the run has already left it. Null for a
        /// command that touched no board at all, such as entering a rest.
        /// </summary>
        /// <remarks>
        /// The winning command is also the command that leaves the fight — the run advances and
        /// <see cref="RunState.Fight"/> is cleared (D-055) — so without this a renderer could never
        /// draw the blow that ended the fight. <see cref="NewState"/> is where the run *is*; this is
        /// where the step *happened*.
        /// </remarks>
        public GameState? FinalBoard { get; }

        /// <summary>The single run event of a type, or throws if there is not exactly one.</summary>
        /// <typeparam name="T">Event type.</typeparam>
        /// <returns>The event.</returns>
        public T Single<T>()
            where T : RunEvent
        {
            T? found = null;
            foreach (var e in Events)
            {
                if (e is T match)
                {
                    if (found is not null)
                    {
                        throw new InvalidOperationException("More than one " + typeof(T).Name + ".");
                    }

                    found = match;
                }
            }

            return found ?? throw new InvalidOperationException("No " + typeof(T).Name + ".");
        }

        /// <summary>Every run event of a type, in order.</summary>
        /// <typeparam name="T">Event type.</typeparam>
        /// <returns>The events.</returns>
        public IReadOnlyList<T> All<T>()
            where T : RunEvent
        {
            var found = new List<T>();
            foreach (var e in Events)
            {
                if (e is T match)
                {
                    found.Add(match);
                }
            }

            return found;
        }
    }
}
