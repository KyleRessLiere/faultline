using System;
using System.Globalization;

namespace Faultline.Core
{
    /// <summary>
    /// One token of the <c>footing:</c> key: N Footing tokens handed to a whole side, or to every unit
    /// of one <see cref="UnitKind"/>.
    /// </summary>
    /// <remarks>
    /// Footing is not automatic — every archetype starts a fight on zero (<see cref="UnitTemplate"/>) —
    /// so a scenario that wants a unit to shrug off a shove has to say so. A grant names either a side
    /// (<c>a</c>, <c>b</c>, <c>enemy</c>) or a unit kind (<c>Anchor</c>), never both.
    /// </remarks>
    public sealed record FootingGrant
    {
        private FootingGrant()
        {
        }

        /// <summary>The side this grant covers, or <c>null</c> when it names a unit kind instead.</summary>
        public Team? Side { get; init; }

        /// <summary>The unit kind this grant covers, or <c>null</c> when it names a side instead.</summary>
        public UnitKind? Kind { get; init; }

        /// <summary>Tokens granted. Never negative.</summary>
        public int Count { get; init; }

        /// <summary>The grant exactly as it is written in a <c>.fight</c> file, e.g. <c>a=1</c>.</summary>
        public string Token =>
            (Kind.HasValue ? Kind.Value.ToString() : SideToken(Side ?? Team.PlayerA))
            + "="
            + Count.ToString(CultureInfo.InvariantCulture);

        /// <summary>Grants tokens to every unit on one side.</summary>
        /// <param name="side">Side to grant to.</param>
        /// <param name="count">Tokens per unit.</param>
        /// <returns>The grant.</returns>
        public static FootingGrant ForSide(Team side, int count) =>
            new FootingGrant { Side = side, Count = count };

        /// <summary>Grants tokens to every unit of one archetype, whichever side it is on.</summary>
        /// <param name="kind">Archetype to grant to.</param>
        /// <param name="count">Tokens per unit.</param>
        /// <returns>The grant.</returns>
        public static FootingGrant ForKind(UnitKind kind, int count) =>
            new FootingGrant { Kind = kind, Count = count };

        /// <summary>The word a side is written with in a <c>.fight</c> file.</summary>
        /// <param name="side">Side to name.</param>
        /// <returns><c>a</c>, <c>b</c> or <c>enemy</c>.</returns>
        public static string SideToken(Team side)
        {
            switch (side)
            {
                case Team.PlayerA: return "a";
                case Team.PlayerB: return "b";
                case Team.Enemy: return "enemy";
                default: throw new ArgumentOutOfRangeException(nameof(side), side, "No footing token for this side.");
            }
        }

        /// <summary>Reads the side a <c>footing:</c> token names.</summary>
        /// <param name="text">Target text, case-insensitive.</param>
        /// <param name="side">The side named.</param>
        /// <returns>Whether the text names a side.</returns>
        public static bool TryParseSide(string text, out Team side)
        {
            if (string.Equals(text, "a", StringComparison.OrdinalIgnoreCase))
            {
                side = Team.PlayerA;
                return true;
            }

            if (string.Equals(text, "b", StringComparison.OrdinalIgnoreCase))
            {
                side = Team.PlayerB;
                return true;
            }

            if (string.Equals(text, "enemy", StringComparison.OrdinalIgnoreCase))
            {
                side = Team.Enemy;
                return true;
            }

            side = Team.PlayerA;
            return false;
        }

        /// <summary>True when this grant applies to a unit of the given side and archetype.</summary>
        /// <param name="team">Unit's side.</param>
        /// <param name="kind">Unit's archetype.</param>
        /// <returns>Whether the grant covers it.</returns>
        public bool Covers(Team team, UnitKind kind) =>
            Kind.HasValue ? Kind.Value == kind : Side.HasValue && Side.Value == team;

        /// <inheritdoc/>
        public override string ToString() => Token;
    }
}
