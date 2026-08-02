namespace Faultline.Core
{
    /// <summary>The displacement verbs. Brief §2 "Displacement", plus the Fisher's throw.</summary>
    public enum DisplacementKind
    {
        /// <summary>Directly away from the source along the line.</summary>
        Push = 0,

        /// <summary>Directly toward the source along the line.</summary>
        Pull = 1,

        /// <summary>
        /// Picked up and put down. A lob: nothing between the thrower and the landing tile is
        /// consulted, and push resistance does not apply (D-091).
        /// </summary>
        Throw = 2,
    }
}
