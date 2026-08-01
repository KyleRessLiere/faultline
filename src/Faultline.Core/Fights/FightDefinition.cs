using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// Authored data for one fight in the run: terrain, where each side starts, and who the enemy is.
    /// Objectives (Protect / Destroy / Boss) arrive with M6; fight 1 is a plain Kill All.
    /// </summary>
    public sealed record FightDefinition
    {
        /// <summary>Stable slug, used to identify the fight in a command log and on disk.</summary>
        public string Id { get; init; } = string.Empty;

        /// <summary>One-based index into the run.</summary>
        public int Number { get; init; }

        /// <summary>Display name.</summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>One-line description, shown when picking a fight.</summary>
        public string Description { get; init; } = string.Empty;

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

        /// <summary>
        /// Footing tokens this scenario hands out, in the order the <c>footing:</c> key wrote them.
        /// Empty means nobody has any: Footing is scenario-granted, never automatic.
        /// </summary>
        public IReadOnlyList<FootingGrant> FootingGrants { get; init; } = new FootingGrant[0];

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

        /// <summary>Footing tokens a unit of this side and archetype starts the fight with.</summary>
        /// <remarks>
        /// A grant naming a unit kind beats a grant naming a side, because it is the more specific of
        /// the two; among grants of equal specificity the last one written wins, which is how the
        /// format treats a repeated key everywhere else. No grant at all means zero.
        /// </remarks>
        /// <param name="team">Unit's side.</param>
        /// <param name="kind">Unit's archetype.</param>
        /// <returns>Starting Footing tokens, zero when the scenario granted none.</returns>
        public int FootingFor(Team team, UnitKind kind)
        {
            int? byKind = null;
            int? bySide = null;

            foreach (var grant in FootingGrants)
            {
                if (grant.Kind.HasValue)
                {
                    if (grant.Kind.Value == kind)
                    {
                        byKind = grant.Count;
                    }
                }
                else if (grant.Side.HasValue && grant.Side.Value == team)
                {
                    bySide = grant.Count;
                }
            }

            return byKind ?? bySide ?? 0;
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
