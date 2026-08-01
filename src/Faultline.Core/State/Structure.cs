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

        /// <summary>True while the structure still blocks its tile.</summary>
        public bool IsStanding => Hp > 0;

        /// <summary>
        /// True when an ordinary attack can hurt it. Brief §3, fight 4: a Destroy objective is
        /// "immune to attacks — only collision damage from a unit slammed into it hurts it".
        /// </summary>
        public bool IsAttackable => Role == ObjectiveKind.Protect;
    }
}
