using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// The boards an event may field. "Event-fights are authored <c>.fight</c> files from the trials
    /// pool, never generated" (MASTER_DESIGN §8.5).
    /// </summary>
    /// <remarks>
    /// <para>
    /// A table in Core rather than a key in the <c>.fight</c> files. Fitness for an event is not a
    /// property of a board — the same trench is a fine guard fight and a hopeless escort — it is a
    /// property of the <em>pairing</em>, and pairings are what this list holds. Putting it in the
    /// files would also have meant editing sixty of them to record one judgement about eight.
    /// </para>
    /// <para>
    /// <b>Data only.</b> No event draws from this yet: §8.6's Nesting Thief and Duckling Lost are the
    /// two that will, and both need systems v1 does not have — legendary consumables for one, a
    /// neutral escortee for the other. The pool is tagged now so that when they land the content
    /// question is already answered and the tests already say which boards were meant.
    /// </para>
    /// <para>
    /// <c>hold-the-gate</c> is here because Act 1's graph does not field it: §8's ten-fight spine had
    /// it at slot 9 and the act map has six combat nodes, so it leaves the campaign and becomes the
    /// pool's best guard board. It stays in <see cref="CampaignLibrary.Faultline"/>, which still
    /// plays the linear ten.
    /// </para>
    /// </remarks>
    public static class EventFightPool
    {
        private static readonly IReadOnlyList<EventFightEntry> Pool = new[]
        {
            new EventFightEntry(
                "hold-the-gate",
                EventFightFitness.Guard,
                "A hold objective with a printed prize behind the line — the shape the Nesting Thief "
                + "and the Sunken Cache both want: the reward is visible and the fight is the price."),
            new EventFightEntry(
                "the-maw",
                EventFightFitness.EliteGuard,
                "The heaviest board outside the campaign. The Sunken Cache asks for an elite-grade "
                + "guard between the squad and a printed legendary; this is it."),
            new EventFightEntry(
                "tp-01-one-door",
                EventFightFitness.Escort,
                "One chokepoint and one way through it. An escortee has exactly one lane to be "
                + "shepherded down, which is what makes the Duckling Lost a puzzle rather than a "
                + "babysitting job."),
            new EventFightEntry(
                "hz-02-the-short-way",
                EventFightFitness.Escort,
                "A reach objective: the march across the board is already the fight. Adding someone "
                + "who has to survive the march changes what the route is worth, not what it is."),
            new EventFightEntry(
                "as-05-the-door",
                EventFightFitness.Guard,
                "Survive eight rounds. A guard fight where the prize is on the far side of a clock "
                + "rather than a body count."),
            new EventFightEntry(
                "cb-04-dead-weight",
                EventFightFitness.Guard,
                "Plain ground, ordinary bodies. The pool needs boards where the event is the "
                + "interesting part and the fight is honest work."),
            new EventFightEntry(
                "ec-08-triage",
                EventFightFitness.Guard,
                "Forces a choice about who is spent. Reads as a bargain even before an event frames "
                + "it as one."),
            new EventFightEntry(
                "as-02-both-sides-of-the-chasm",
                EventFightFitness.Escort,
                "A split board: the escortee is on one side of a problem and the squad on the other. "
                + "Escort-suitable, and the only pool board where the terrain is the antagonist."),
        };

        /// <summary>Every board tagged for event use, in authored order.</summary>
        /// <returns>The pool.</returns>
        public static IReadOnlyList<EventFightEntry> All() => Pool;

        /// <summary>Whether a board is in the pool.</summary>
        /// <param name="fightId">Fight identifier.</param>
        /// <returns>Whether an event may field it.</returns>
        public static bool Contains(string fightId) => Find(fightId) is not null;

        /// <summary>The pool entry for a board, or <c>null</c> when it is not in the pool.</summary>
        /// <param name="fightId">Fight identifier.</param>
        /// <returns>The entry, or null.</returns>
        public static EventFightEntry? Find(string fightId)
        {
            foreach (var entry in Pool)
            {
                if (string.Equals(entry.FightId, fightId, StringComparison.Ordinal))
                {
                    return entry;
                }
            }

            return null;
        }

        /// <summary>Every board in the pool judged fit for one kind of event.</summary>
        /// <param name="fitness">What the event needs the board to do.</param>
        /// <returns>The matching entries, in authored order.</returns>
        public static IReadOnlyList<EventFightEntry> ByFitness(EventFightFitness fitness)
        {
            var found = new List<EventFightEntry>();
            foreach (var entry in Pool)
            {
                if (entry.Fitness == fitness)
                {
                    found.Add(entry);
                }
            }

            return found;
        }
    }
}
