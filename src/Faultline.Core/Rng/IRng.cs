namespace Faultline.Core
{
    /// <summary>
    /// The only source of randomness in Core. Brief §1: seeded from <see cref="GameState.Seed"/>,
    /// integer-only, no <c>System.Random</c> and no wall-clock. Its <see cref="State"/> is written
    /// back into <see cref="GameState"/> so replays reproduce every draw exactly.
    /// </summary>
    public interface IRng
    {
        /// <summary>Current internal state, suitable for persisting into <see cref="GameState"/>.</summary>
        int State { get; }

        /// <summary>Draws a value in <c>[0, maxExclusive)</c>.</summary>
        /// <param name="maxExclusive">Exclusive upper bound; must be positive.</param>
        /// <returns>The drawn value.</returns>
        int Next(int maxExclusive);
    }
}
