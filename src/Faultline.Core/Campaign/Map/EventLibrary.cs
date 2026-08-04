using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// The events the game ships. One, for now: the Molting Pool.
    /// </summary>
    /// <remarks>
    /// MASTER_DESIGN §8.5 and §8.6 name ten between them. The other nine are not here, and are not
    /// stubbed here either — an id in a library with no handler behind it is a promise the map could
    /// route a run into. They arrive with the systems they price: the Old Current with charge
    /// conditions, the Tinkerer's Raft with mods, the Sunken Cache and the Nesting Thief with
    /// legendary consumables, the Toll Gate with column-skipping, the Peddler's Bargain and the
    /// Ferryman with curses and Pluck spending, the Duckling Lost with neutral units, the Marsh Light
    /// with free routing.
    /// </remarks>
    public static class EventLibrary
    {
        /// <summary>Id of the one event v1 ships.</summary>
        public const string MoltingPoolId = "molting-pool";

        private static readonly EventDefinition MoltingPoolEvent = new EventDefinition
        {
            Id = MoltingPoolId,
            Name = "The Molting Pool",
            Shape = EventShape.Offer,

            // §8.5's row, whole: pay 4 HP now, gain +2 max HP, blocked at lethal. The prices are the
            // doc's; the two lines of voice are this repo's and are flagged for the tone pass.
            Prompt =
                "Black water, and something under it that wants feathers. Wade in and it takes four "
                + "from you, and what grows back grows back thicker: two more than you had, for as "
                + "long as this run lasts. One of you. Your own choice, nobody else's.",
            WalkAwayLine = "The water settles. It was not asking twice.",
            HpCost = 4,
            MaxHpGain = 2,
        };

        /// <summary>
        /// The Molting Pool (MASTER_DESIGN §8.5): pay 4 HP now, gain +2 maximum for the run. Blocked
        /// at lethal, and only ever paid by a duck named out loud.
        /// </summary>
        public static EventDefinition MoltingPool => MoltingPoolEvent;

        /// <summary>Every event, in order.</summary>
        /// <returns>The events.</returns>
        public static IReadOnlyList<EventDefinition> All() => new[] { MoltingPoolEvent };

        /// <summary>Finds an event by id.</summary>
        /// <param name="id">Event id.</param>
        /// <returns>The event.</returns>
        /// <exception cref="ArgumentException">No event has that id.</exception>
        public static EventDefinition ById(string id)
        {
            foreach (var definition in All())
            {
                if (string.Equals(definition.Id, id, StringComparison.Ordinal))
                {
                    return definition;
                }
            }

            throw new ArgumentException("No event with id '" + id + "'.", nameof(id));
        }
    }
}
