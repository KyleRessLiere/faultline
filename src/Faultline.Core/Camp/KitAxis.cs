namespace Faultline.Core
{
    /// <summary>
    /// Which of a duck's two slot axes something sits on. <b>They are counted separately</b>: a duck
    /// has N ability slots <i>plus</i> its Pluck slots, and filling one has never any bearing on the
    /// other (D-230).
    /// </summary>
    /// <remarks>
    /// The axis is derived from the entry rather than stored beside it —
    /// <see cref="Kits.AxisOf(KitEntry)"/> — because a spender is a spender wherever it is written
    /// down, and a stored axis is a second opinion waiting to disagree.
    /// </remarks>
    public enum KitAxis
    {
        /// <summary>The ability slots: a basic attack and named actions.</summary>
        Ability = 0,

        /// <summary>The Pluck slots: what the duck's meter is spent on.</summary>
        Pluck = 1,
    }
}
