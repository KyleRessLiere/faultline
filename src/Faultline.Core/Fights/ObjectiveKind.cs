namespace Faultline.Core
{
    /// <summary>
    /// What a fight asks of the players. <see cref="KillAll"/> is the default and the behaviour every
    /// fight had before objectives existed, so a file with no <c>objective:</c> key plays unchanged.
    /// </summary>
    public enum ObjectiveKind
    {
        /// <summary>Win when no enemy is left standing. The default.</summary>
        KillAll = 0,

        /// <summary>Win at the end of a named round if any player unit is still standing.</summary>
        Survive = 1,

        /// <summary>Win at the end of a named round if no enemy stands on the named tiles.</summary>
        Hold = 2,

        /// <summary>Win the moment a player unit stands on one of the named tiles.</summary>
        Reach = 3,

        /// <summary>A structure with hit points that enemies attack. Lose if it is destroyed.</summary>
        Protect = 4,

        /// <summary>A structure immune to attacks that only collision damage hurts. Win when it falls.</summary>
        Destroy = 5,
    }
}
