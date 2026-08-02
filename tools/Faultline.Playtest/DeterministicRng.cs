namespace Faultline.Playtest;

/// <summary>
/// xorshift32, seeded per run. The harness's own source, kept out of Core.
/// </summary>
/// <remarks>
/// A random policy still has to be reproducible or the report it produces cannot be argued with:
/// the same policy at the same seed must play the same run twice. Nothing here touches the game's
/// own randomness, which lives in <see cref="Faultline.Core.GameState.RngState"/> and is driven by
/// the fight seed.
/// </remarks>
public sealed class DeterministicRng
{
    private uint _state;

    /// <summary>Creates a source. A zero seed is nudged, since xorshift is stuck at zero.</summary>
    /// <param name="seed">Starting state.</param>
    public DeterministicRng(int seed) => _state = seed == 0 ? 0x9E3779B9u : unchecked((uint)seed);

    /// <summary>Next value below an exclusive bound.</summary>
    /// <param name="exclusiveBound">Upper bound, at least 1.</param>
    /// <returns>A value in [0, bound).</returns>
    public int Next(int exclusiveBound)
    {
        if (exclusiveBound <= 1)
        {
            return 0;
        }

        _state ^= _state << 13;
        _state ^= _state >> 17;
        _state ^= _state << 5;
        return (int)(_state % (uint)exclusiveBound);
    }
}
