namespace Faultline.Core
{
    /// <summary>
    /// The act's boss went down and the map is finished. Fired immediately before
    /// <see cref="RunWon"/>.
    /// </summary>
    /// <remarks>
    /// <paramref name="MoltAwarded"/> is false and <paramref name="Tally"/> says why in words. The
    /// Molt — the boss reward, full heal plus the guaranteed big pick (MASTER_DESIGN §8.5) — is not
    /// built, and an act-cleared screen that showed a reward summary here would be describing a
    /// payment that never happened. The honest v1 ending is the tally and the admission.
    /// </remarks>
    /// <param name="ActId">The map that was cleared.</param>
    /// <param name="BossFightId">The fight that ended it.</param>
    /// <param name="FightsWon">How many fights the run won.</param>
    /// <param name="NodesVisited">How many nodes it stood on, the start included.</param>
    /// <param name="RouteHash">The route it took, as a number — see <see cref="MapState.RouteHash"/>.</param>
    /// <param name="MoltAwarded">Always false in v1.</param>
    /// <param name="Tally">One line, naming what v1 does not yet pay.</param>
    public sealed record ActCleared(
        string ActId,
        string BossFightId,
        int FightsWon,
        int NodesVisited,
        int RouteHash,
        bool MoltAwarded,
        string Tally) : RunEvent
    {
        /// <summary>The v1 tally line. Names the gap rather than dressing it.</summary>
        public const string PlaceholderTally =
            "Act cleared. The Molt is not built: no reward is granted here yet (MASTER_DESIGN §8.5).";
    }
}
