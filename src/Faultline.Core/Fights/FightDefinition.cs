using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// Authored data for one fight in the run: terrain, where each side starts, and who the enemy is.
    /// Objectives (Protect / Destroy / Boss) arrive with M6; fight 1 is a plain Kill All.
    /// </summary>
    public sealed record FightDefinition
    {
        /// <summary>One-based index into the five-fight run.</summary>
        public int Number { get; init; }

        /// <summary>Display name.</summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>Terrain.</summary>
        public Board Board { get; init; } = Board.Filled(1, 1);

        /// <summary>Tiles Player A may deploy onto.</summary>
        public IReadOnlyList<Coord> DeploymentZoneA { get; init; } = new Coord[0];

        /// <summary>Tiles Player B may deploy onto.</summary>
        public IReadOnlyList<Coord> DeploymentZoneB { get; init; } = new Coord[0];

        /// <summary>Player A's two units, in deployment order.</summary>
        public IReadOnlyList<UnitKind> RosterA { get; init; } = new UnitKind[0];

        /// <summary>Player B's two units, in deployment order.</summary>
        public IReadOnlyList<UnitKind> RosterB { get; init; } = new UnitKind[0];

        /// <summary>Enemies and their starting tiles.</summary>
        public IReadOnlyList<EnemySpawn> Enemies { get; init; } = new EnemySpawn[0];

        /// <summary>The 2x3 zone the collapse clock never cracks (M4).</summary>
        public IReadOnlyList<Coord> ProtectedZone { get; init; } = new Coord[0];

        /// <summary>The deployment zone belonging to a player team.</summary>
        /// <param name="team">Player team.</param>
        /// <returns>That team's legal deployment tiles, or an empty list for the enemy team.</returns>
        public IReadOnlyList<Coord> ZoneFor(Team team)
        {
            if (team == Team.PlayerA)
            {
                return DeploymentZoneA;
            }

            return team == Team.PlayerB ? DeploymentZoneB : new Coord[0];
        }

        /// <summary>The roster belonging to a player team.</summary>
        /// <param name="team">Player team.</param>
        /// <returns>That team's unit kinds, or an empty list for the enemy team.</returns>
        public IReadOnlyList<UnitKind> RosterFor(Team team)
        {
            if (team == Team.PlayerA)
            {
                return RosterA;
            }

            return team == Team.PlayerB ? RosterB : new UnitKind[0];
        }
    }
}
