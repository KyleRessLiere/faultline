using System;

namespace Faultline.Core
{
    /// <summary>
    /// Deterministic xorshift32 generator. Pure integer math, identical on every runtime and
    /// architecture, which is what the replay test depends on.
    /// </summary>
    public sealed class SeededRng : IRng
    {
        private uint _state;

        /// <summary>Creates a generator from a raw state value.</summary>
        /// <param name="state">Seed or resumed state. Zero is remapped, since xorshift cannot leave it.</param>
        public SeededRng(int state)
        {
            _state = state == 0 ? 0x9E3779B9u : unchecked((uint)state);
        }

        /// <inheritdoc/>
        public int State => unchecked((int)_state);

        /// <inheritdoc/>
        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), maxExclusive, "Bound must be positive.");
            }

            // Rejection sampling keeps the distribution uniform without any float math.
            uint bound = unchecked((uint)maxExclusive);
            uint limit = uint.MaxValue - (uint.MaxValue % bound);
            uint draw;
            do
            {
                draw = NextRaw();
            }
            while (draw >= limit);

            return unchecked((int)(draw % bound));
        }

        private uint NextRaw()
        {
            unchecked
            {
                uint x = _state;
                x ^= x << 13;
                x ^= x >> 17;
                x ^= x << 5;
                _state = x;
                return x;
            }
        }
    }
}
