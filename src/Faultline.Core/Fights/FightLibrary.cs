using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// The authored fights of the run. Fights 2-5 land with M6; M1 ships fight 1 only.
    /// </summary>
    public static class FightLibrary
    {
        /// <summary>
        /// Fight 1 — Kill All against Husks and a Lobber.
        /// </summary>
        /// <remarks>
        /// Layout obeys Brief §2 "Board": walls and pits sit on the outer two rings, the centre 3x3
        /// (x,y in 2..4) is clear at start, and the three spike tiles sit one ring in from the edge.
        /// See DECISIONS.md D-005 for why the spikes are not on the ring the brief calls "middle".
        /// Players take opposite corners; the four enemies spawn on the two opposite short edges.
        /// </remarks>
        /// <returns>The fight definition.</returns>
        public static FightDefinition Fight1()
        {
            var board = BoardLayout.Parse(new[]
            {
                //   x=0123456
                /* y=0 */ "#..O...",
                /* y=1 */ ".H.^...",
                /* y=2 */ "O.....#",
                /* y=3 */ ".^...^.",
                /* y=4 */ "#.....O",
                /* y=5 */ ".....H.",
                /* y=6 */ "...O..#",
            });

            return new FightDefinition
            {
                Number = 1,
                Name = "Kill All",
                Board = board,
                RosterA = new[] { UnitKind.Vanguard, UnitKind.Archer },
                RosterB = new[] { UnitKind.Threadcaster, UnitKind.Wardbearer },

                // Opposite corners: A bottom-left, B top-right.
                DeploymentZoneA = new[]
                {
                    new Coord(0, 5), new Coord(1, 5), new Coord(0, 6), new Coord(1, 6),
                },
                DeploymentZoneB = new[]
                {
                    new Coord(5, 0), new Coord(6, 0), new Coord(5, 1), new Coord(6, 1),
                },

                Enemies = new[]
                {
                    new EnemySpawn(UnitKind.Husk, new Coord(2, 0)),
                    new EnemySpawn(UnitKind.Lobber, new Coord(4, 0)),
                    new EnemySpawn(UnitKind.Husk, new Coord(2, 6)),
                    new EnemySpawn(UnitKind.Husk, new Coord(4, 6)),
                },

                ProtectedZone = new Coord[0],
            };
        }

        /// <summary>Every fight currently authored, in run order.</summary>
        /// <returns>The fights of the run.</returns>
        public static IReadOnlyList<FightDefinition> All() => new[] { Fight1() };
    }
}
