using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// What the enemy side can reach on the first round, before the players have had a turn.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The design law this exists for is <b>agency before injury</b> (D-080): a player should never
    /// lose hit points to a decision they were not allowed to make. Deployment is the one moment in a
    /// fight where the player commits without information, so the information is given — the board
    /// shades every tile the enemy could put damage on before the first activation.
    /// </para>
    /// <para>
    /// <b>Deliberately an over-approximation.</b> It asks what an enemy *could* do, not what its
    /// priority list *will* do, and it computes reach with the board empty of player units — bodies
    /// only ever block, so a real deployment can shrink this set but never grow it. A threat overlay
    /// that under-reported would be worse than none at all: it would be trusted.
    /// </para>
    /// <para>
    /// <b>There is no line of sight in this game.</b> <see cref="Combat.RangeTiles"/> is pure step
    /// distance, so a wall stops an archer walking but never stops it shooting. A ranged enemy's
    /// threat is therefore a diamond of radius (how far it can walk + its range), and the only way to
    /// shrink one is to box in where it can stand.
    /// </para>
    /// </remarks>
    public static class Threat
    {
        /// <summary>
        /// Every tile this unit could land an attack on within one activation: anywhere it can reach,
        /// plus everything in range from each of those tiles.
        /// </summary>
        /// <param name="state">Board to measure against.</param>
        /// <param name="unit">Unit whose threat to compute.</param>
        /// <returns>The threatened tiles, empty for a unit with no attack or off the board.</returns>
        public static IReadOnlyCollection<Coord> ForUnit(GameState state, Unit unit)
        {
            var tiles = new HashSet<Coord>();
            if (state is null || unit is null || !unit.IsOnBoard)
            {
                return tiles;
            }

            // Something that shoulders through bodies threatens tiles even with no reach at all, so
            // the no-attack shortcut has to let a trampler past it.
            if (unit.Template.BasicReach <= 0 && !unit.Template.Tramples)
            {
                return tiles;
            }

            var stands = new List<Coord> { unit.Position };
            foreach (var reached in Movement.Reachable(state, unit).Keys)
            {
                stands.Add(reached);
            }

            foreach (var stand in stands)
            {
                foreach (var tile in Combat.RangeTiles(state, unit with { Position = stand }))
                {
                    tiles.Add(tile);
                }
            }

            // Standing in a trampler's way costs a hit point and a tile of position, so a lane is a
            // threatened tile in exactly the sense this overlay means (D-100).
            foreach (var lane in Trample.Lanes(state, unit, stands))
            {
                tiles.Add(lane);
            }

            return tiles;
        }

        /// <summary>
        /// Every tile any living enemy could reach with its basic action, whether or not that action
        /// deals damage. What the in-fight threat overlay paints.
        /// </summary>
        /// <param name="state">Board to measure against.</param>
        /// <returns>The threatened tiles.</returns>
        public static IReadOnlyCollection<Coord> All(GameState state)
        {
            var tiles = new HashSet<Coord>();
            if (state is null)
            {
                return tiles;
            }

            foreach (var unit in state.Units)
            {
                if (unit.Team != Team.Enemy || !unit.IsOnBoard)
                {
                    continue;
                }

                foreach (var tile in ForUnit(state, unit))
                {
                    tiles.Add(tile);
                }
            }

            return tiles;
        }

        /// <summary>
        /// Every tile the enemy side could deal damage on before the players activate.
        /// </summary>
        /// <remarks>
        /// Only enemies that actually deal damage count. The Grappler, Stalker and Harrier hit for
        /// nothing — their whole action is a displacement — so they threaten <em>position</em> rather
        /// than hit points, and <see cref="DisplacementRound1"/> reports them separately. The law is
        /// worded as damage, so this is what the validation reads (D-080).
        /// </remarks>
        /// <param name="state">Board to measure against, normally straight out of <see cref="Game.Start(FightDefinition, int)"/>.</param>
        /// <returns>The threatened tiles.</returns>
        public static IReadOnlyCollection<Coord> DamageRound1(GameState state) =>
            Union(state, damage: true);

        /// <summary>
        /// Every tile the enemy side could <em>displace</em> a unit from before the players activate,
        /// counting only the enemies that deal no damage at all.
        /// </summary>
        /// <remarks>
        /// Not part of the law, and reported so that it can be seen anyway: a Stalker that shoves a
        /// player into a pit on round 1 has taken the whole unit without dealing a point of damage,
        /// which is worse than anything <see cref="DamageRound1"/> covers.
        /// </remarks>
        /// <param name="state">Board to measure against.</param>
        /// <returns>The threatened tiles.</returns>
        public static IReadOnlyCollection<Coord> DisplacementRound1(GameState state) =>
            Union(state, damage: false);

        /// <summary>
        /// The tiles of a side's deployment zone that no enemy can damage on round 1.
        /// </summary>
        /// <param name="state">Board to measure against.</param>
        /// <param name="team">Which side's zone.</param>
        /// <returns>The safe tiles, in the zone's own order.</returns>
        public static IReadOnlyList<Coord> SafeDeploymentTiles(GameState state, Team team)
        {
            var safe = new List<Coord>();
            if (state is null)
            {
                return safe;
            }

            var threatened = new HashSet<Coord>(DamageRound1(state));
            // §3's spots belong to neither side, so "which tiles could this team stand on safely" is
            // asked of the whole published list. On an unmigrated board Spots is the union of the two
            // zones, which widens this from one side's corner to the board's real answer.
            foreach (var tile in state.Fight.Spots)
            {
                if (!threatened.Contains(tile))
                {
                    safe.Add(tile);
                }
            }

            return safe;
        }

        /// <summary>
        /// Whether both sides could deploy every unit they field onto a tile nothing can damage on
        /// round 1.
        /// </summary>
        /// <remarks>
        /// The whole search collapses to counting. Threat is a property of a tile rather than of who
        /// is standing on it, and player units cannot be told apart by it, so "is there a safe
        /// arrangement" is exactly "are there at least as many safe tiles as units to place". A unit
        /// on a safe tile that then passes its activation takes nothing, so a safe deployment is
        /// sufficient on its own — no ordering has to be searched.
        /// </remarks>
        /// <param name="fight">Fight to check.</param>
        /// <returns>Whether a damage-free round 1 is available to the player.</returns>
        public static bool HasSafeDeployment(FightDefinition fight) =>
            fight is not null && UnsafeSides(fight).Count == 0;

        /// <summary>
        /// The sides that cannot field their roster on safe tiles, with how short each one is.
        /// </summary>
        /// <param name="fight">Fight to check.</param>
        /// <returns>One entry per failing side; empty when the fight obeys the law.</returns>
        public static IReadOnlyList<UnsafeSide> UnsafeSides(FightDefinition fight)
        {
            var failures = new List<UnsafeSide>();
            if (fight is null)
            {
                return failures;
            }

            var state = Game.Start(fight, seed: 0).NewState;

            foreach (var team in new[] { Team.PlayerA, Team.PlayerB })
            {
                int needed = team == Team.PlayerA ? fight.RosterA.Count : fight.RosterB.Count;
                if (needed == 0)
                {
                    continue;
                }

                int safe = SafeDeploymentTiles(state, team).Count;
                if (safe < needed)
                {
                    failures.Add(new UnsafeSide(team, needed, safe, state.Fight.Spots.Count));
                }
            }

            return failures;
        }

        private static IReadOnlyCollection<Coord> Union(GameState state, bool damage)
        {
            var tiles = new HashSet<Coord>();
            if (state is null)
            {
                return tiles;
            }

            foreach (var unit in state.Units)
            {
                if (unit.Team != Team.Enemy || !unit.IsOnBoard)
                {
                    continue;
                }

                bool hurts = unit.Template.Damage > 0;
                if (hurts != damage)
                {
                    continue;
                }

                foreach (var tile in ForUnit(state, unit))
                {
                    tiles.Add(tile);
                }
            }

            return tiles;
        }
    }

    /// <summary>A side that cannot deploy out of harm's way on round 1.</summary>
    /// <param name="Team">The side.</param>
    /// <param name="Needed">Units it fields.</param>
    /// <param name="Safe">Zone tiles nothing can damage on round 1.</param>
    /// <param name="ZoneSize">Zone tiles in total.</param>
    public sealed record UnsafeSide(Team Team, int Needed, int Safe, int ZoneSize)
    {
        /// <inheritdoc/>
        public override string ToString() =>
            Team + " fields " + Needed + " but has " + Safe + " safe tile(s) of " + ZoneSize;
    }
}
