using System;

namespace Faultline.Core
{
    /// <summary>
    /// The six offer-validity tags of MASTER_DESIGN §8.6. <b>Not a player resource</b> — nothing in a
    /// fight reads one. They exist so the camp director can ask whether a card connects to something
    /// the squad already owns.
    /// </summary>
    /// <remarks>
    /// A flags enum because §8.6 authors cards on two tags at once (<i>Crosscheck</i> is
    /// TRAFFIC/CONTROL, <i>Rattling Impact</i> is IMPACT/RELAY), and a card that could only carry one
    /// would force the director to pick which half of a card it is.
    /// </remarks>
    [Flags]
    public enum TechniqueTag
    {
        /// <summary>No tag. Only ever the empty accumulator.</summary>
        None = 0,

        /// <summary>Moves several bodies, or preserves lanes.</summary>
        Traffic = 1,

        /// <summary>Collisions continue, spread, or set up another.</summary>
        Impact = 2,

        /// <summary>Hands value to the other flock. The category the v1 pool lacked.</summary>
        Relay = 4,

        /// <summary>Changes where an action ENDS without adding range.</summary>
        Control = 8,

        /// <summary>Converts hostile pressure into position.</summary>
        Guard = 16,

        /// <summary>Turns a developed setup into tempo.</summary>
        Finish = 32,
    }
}
