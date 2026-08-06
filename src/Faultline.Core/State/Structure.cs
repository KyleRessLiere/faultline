namespace Faultline.Core
{
    /// <summary>
    /// An objective structure: a thing on the board with hit points that is not a
    /// <see cref="Unit"/> — the altar you defend or the pillar you bring down.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Deliberately not a unit with a new <see cref="Team"/>. A structure never activates, never
    /// moves, is never Staggered, cannot Cling, spends no Footing and has no archetype — so every
    /// rule that walks <see cref="GameState.Units"/> would need a "but not this one" clause, and the
    /// activation loop would have to skip it in four separate places. Separate state costs one extra
    /// occupancy check and keeps every unit rule honest (DECISIONS.md D-033).
    /// </para>
    /// <para>
    /// It occupies its tile: nothing walks onto it, and a unit displaced into it collides. That is
    /// the whole of its physics.
    /// </para>
    /// </remarks>
    public sealed record Structure
    {
        /// <summary>Tile the structure stands on.</summary>
        public Coord At { get; init; }

        /// <summary>Current hit points; zero or less means rubble.</summary>
        public int Hp { get; init; }

        /// <summary>Hit points it started the fight with.</summary>
        public int MaxHp { get; init; }

        /// <summary>Whether the fight wants this kept alive or brought down.</summary>
        public ObjectiveKind Role { get; init; } = ObjectiveKind.Protect;

        /// <summary>
        /// True for a <b>breakable blocker</b>: masonry that is in the way and nobody's objective.
        /// Bringing one down neither wins nor loses the fight — it opens the tile it stood on.
        /// </summary>
        /// <remarks>
        /// A blocker is the same physics as an objective structure and deliberately reuses it: it
        /// occupies its tile, it takes the flat chip from an attack and the full amount from a
        /// collision, and its rubble stops blocking. What it is <em>not</em> is a win condition, so
        /// <see cref="Objectives.AnyStructureStanding"/> ignores it and the enemy has no reason to
        /// besiege it. That separation is the whole of the flag (DECISIONS.md D-114).
        /// </remarks>
        public bool IsBlocker { get; init; }

        /// <summary>True while the structure still blocks its tile.</summary>
        public bool IsStanding => Hp > 0;

        /// <summary>
        /// True when enemies claw at it: the altar a Protect objective told the players to hold.
        /// </summary>
        /// <remarks>
        /// This is not an attackability rule. D-060 made every structure attackable — an attack takes
        /// <see cref="Objectives.AttackDamageToStructure"/> off any of them, whatever the weapon and
        /// whoever swung — and superseded the brief's "immune to attacks" clause along with the
        /// <c>IsAttackable</c> property that implemented it.
        /// What is left is whose objective the thing is: the enemy besieges what the players defend,
        /// and has no reason to help them bring down a structure they were sent to destroy.
        /// </remarks>
        public bool IsSiegeTarget => !IsBlocker && Role == ObjectiveKind.Protect;
    }
}
