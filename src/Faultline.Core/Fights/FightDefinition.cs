using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// Authored data for one fight in the run: terrain, where each side starts, who the enemy is,
    /// what winning means, and when the rest of the enemy shows up.
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

        /// <summary>
        /// The design notes: why this battle exists and what it is asking the player to work out.
        /// One entry per <c>design:</c> line, in file order, each read as its own paragraph.
        /// </summary>
        /// <remarks>
        /// Separate from <see cref="Description"/>, which is the one sentence a picker shows. These are
        /// the longer "here is the idea" notes, and they are a repeatable key because the format has no
        /// line continuation — a paragraph is consecutive <c>design:</c> lines, the same way a fight's
        /// enemies are consecutive <c>spawn</c> lines.
        /// </remarks>
        public IReadOnlyList<string> DesignNotes { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Why this battle was retired, or <c>null</c> while it is active. Set by the
        /// <c>retired:</c> key, whose value is the reason and is required — a battle cannot be
        /// retired without saying why (docs/RETIRING_BATTLES.md).
        /// </summary>
        /// <remarks>
        /// Retired is not deleted. The file stays embedded and still has to parse, so a retired
        /// battle cannot quietly rot; it simply drops out of <see cref="FightLibrary.All"/> and
        /// turns up in <see cref="FightLibrary.Retired"/> with this reason attached.
        /// </remarks>
        public string? RetiredReason { get; init; }

        /// <summary>True when a <c>retired:</c> key took this battle out of the playable set.</summary>
        public bool IsRetired => RetiredReason is not null;

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
        /// What winning means. Defaults to <see cref="Objective.KillAll"/>, so a file with no
        /// <c>objective:</c> key plays exactly as it did before objectives existed.
        /// </summary>
        public Objective Objective { get; init; } = Objective.KillAll;

        /// <summary>
        /// Round cap from the <c>turn-limit:</c> key; zero means the fight runs until someone wins.
        /// Reaching it is a loss unless the objective wins on expiry — which is the whole point of
        /// <see cref="ObjectiveKind.Survive"/>.
        /// </summary>
        public int TurnLimit { get; init; }

        /// <summary>Enemies that arrive mid-fight, sorted by round then by the order the file wrote them.</summary>
        public IReadOnlyList<ReinforcementWave> Waves { get; init; } = new ReinforcementWave[0];

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

        /// <summary>
        /// The last round this fight can reach: whichever of the objective's own deadline and the
        /// turn limit comes first, or zero when neither is set and the fight runs until someone wins.
        /// </summary>
        /// <returns>The final round, or zero for no clock.</returns>
        public int LastRound()
        {
            int deadline = Objective.Deadline;
            if (deadline <= 0)
            {
                return TurnLimit;
            }

            return TurnLimit > 0 && TurnLimit < deadline ? TurnLimit : deadline;
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
