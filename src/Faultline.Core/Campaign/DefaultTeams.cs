using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// Who fields whom when nobody has said otherwise: <b>Player A takes the Vanguard and the
    /// Fisher, Player B the Wardbearer and the Archer</b> (D-092).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The suggested loadout for the dock draft, and what a campaign run uses. A free draft overrides
    /// it entirely — this is a default, not a rule about who may hold what.
    /// </para>
    /// <para>
    /// <b>Resolved at run start rather than per board.</b> Every campaign fight rosters the same four
    /// classes and only disagrees about which side holds which, so the split is a property of the
    /// squad rather than of the board. Reading it off ten <c>.fight</c> files meant ten places to
    /// change and ten chances to disagree; the run now re-splits whatever a campaign board fields.
    /// Boards outside the campaign keep their authored rosters, because a trial that hands one player
    /// three units is making a point with that.
    /// </para>
    /// </remarks>
    public static class DefaultTeams
    {
        /// <summary>Player A's suggested pair: the shove and the throw.</summary>
        public static readonly IReadOnlyList<UnitKind> A = new[]
        {
            UnitKind.Vanguard,
            UnitKind.Threadcaster,
        };

        /// <summary>Player B's suggested pair: the wall and the bow.</summary>
        public static readonly IReadOnlyList<UnitKind> B = new[]
        {
            UnitKind.Wardbearer,
            UnitKind.Archer,
        };

        /// <summary>Which side would field this class by default.</summary>
        /// <param name="kind">Archetype to place.</param>
        /// <returns>The side, or <c>null</c> for a class neither default names.</returns>
        public static Team? SideFor(UnitKind kind)
        {
            foreach (var candidate in A)
            {
                if (candidate == kind)
                {
                    return Team.PlayerA;
                }
            }

            foreach (var candidate in B)
            {
                if (candidate == kind)
                {
                    return Team.PlayerB;
                }
            }

            return null;
        }

        /// <summary>
        /// Re-splits the classes a board fields across the two players by the default loadout,
        /// keeping the board's own composition and count.
        /// </summary>
        /// <remarks>
        /// Composition is the board designer's decision and side is not. A class neither default
        /// names — nothing today, but the enum is open — falls to whichever side is currently
        /// smaller, so a board is never handed to one player entire.
        /// </remarks>
        /// <param name="fielded">Every class the board rosters, both sides together.</param>
        /// <param name="sideA">Player A's roster.</param>
        /// <param name="sideB">Player B's roster.</param>
        public static void Split(
            IEnumerable<UnitKind> fielded,
            out IReadOnlyList<UnitKind> sideA,
            out IReadOnlyList<UnitKind> sideB)
        {
            var a = new List<UnitKind>();
            var b = new List<UnitKind>();

            if (fielded is not null)
            {
                foreach (var kind in fielded)
                {
                    switch (SideFor(kind))
                    {
                        case Team.PlayerA:
                            a.Add(kind);
                            break;
                        case Team.PlayerB:
                            b.Add(kind);
                            break;
                        default:
                            (a.Count <= b.Count ? a : b).Add(kind);
                            break;
                    }
                }
            }

            sideA = a;
            sideB = b;
        }
    }
}
