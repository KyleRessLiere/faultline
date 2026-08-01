namespace Faultline.Core
{
    /// <summary>Allegiance. The two player teams are allies; both oppose <see cref="Enemy"/>.</summary>
    public enum Team
    {
        /// <summary>First hotseat player.</summary>
        PlayerA = 0,

        /// <summary>Second hotseat player.</summary>
        PlayerB = 1,

        /// <summary>AI side.</summary>
        Enemy = 2,
    }

    /// <summary>Helpers over <see cref="Team"/>.</summary>
    public static class Teams
    {
        /// <summary>True for <see cref="Team.PlayerA"/> and <see cref="Team.PlayerB"/>.</summary>
        /// <param name="team">Team to test.</param>
        /// <returns>Whether this is a player-controlled team.</returns>
        public static bool IsPlayer(this Team team) => team == Team.PlayerA || team == Team.PlayerB;

        /// <summary>True when the two teams are on opposite sides of the fight.</summary>
        /// <param name="a">First team.</param>
        /// <param name="b">Second team.</param>
        /// <returns>Whether they are hostile to one another.</returns>
        public static bool IsHostileTo(this Team a, Team b) => a.IsPlayer() != b.IsPlayer();

        /// <summary>The other player team.</summary>
        /// <param name="team">A player team.</param>
        /// <returns>The opposite player team, or the input when it is not a player team.</returns>
        public static Team OtherPlayer(this Team team)
        {
            if (team == Team.PlayerA)
            {
                return Team.PlayerB;
            }

            return team == Team.PlayerB ? Team.PlayerA : team;
        }
    }
}
