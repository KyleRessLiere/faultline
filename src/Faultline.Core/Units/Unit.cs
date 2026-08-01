namespace Faultline.Core
{
    /// <summary>
    /// A single combatant. Immutable: every rule returns a new unit via <c>with</c>.
    /// </summary>
    public sealed record Unit
    {
        /// <summary>Stable identifier.</summary>
        public UnitId Id { get; init; }

        /// <summary>Archetype.</summary>
        public UnitKind Kind { get; init; }

        /// <summary>Allegiance.</summary>
        public Team Team { get; init; }

        /// <summary>Current hit points; zero or less means downed.</summary>
        public int Hp { get; init; }

        /// <summary>Hit point ceiling, raised only by between-fight upgrades (M6).</summary>
        public int MaxHp { get; init; }

        /// <summary>Movement points per activation.</summary>
        public int Move { get; init; }

        /// <summary>Board position. Meaningless until <see cref="IsDeployed"/> is true.</summary>
        public Coord Position { get; init; }

        /// <summary>True once the unit has been placed on the board during deployment.</summary>
        public bool IsDeployed { get; init; }

        /// <summary>True once this unit has taken its activation this round.</summary>
        public bool HasActivated { get; init; }

        /// <summary>True once this unit has moved during the current activation.</summary>
        public bool HasMoved { get; init; }

        /// <summary>True once this unit has taken its action during the current activation.</summary>
        public bool HasActed { get; init; }

        /// <summary>Remaining Footing tokens for this fight (M2).</summary>
        public int Footing { get; init; }

        /// <summary>Staggered until end of round; the next displacement against it gains +1 (M2).</summary>
        public bool Staggered { get; init; }

        /// <summary>True while clinging to the lip of a pit.</summary>
        public bool Clinging { get; init; }

        /// <summary>Round the unit went into the pit, so end-of-round resolution knows how long it has hung on.</summary>
        public int ClingingSinceRound { get; init; }

        /// <summary>Permanently removed from the run — died in a pit (M2).</summary>
        public bool Voided { get; init; }

        /// <summary>Stat block for this unit's archetype.</summary>
        public UnitTemplate Template => UnitTemplate.For(Kind);

        /// <summary>Display name.</summary>
        public string Name => Template.Name;

        /// <summary>True while the unit still has hit points and has not been voided.</summary>
        public bool IsAlive => Hp > 0 && !Voided;

        /// <summary>True when the unit is alive and standing on the board.</summary>
        public bool IsOnBoard => IsAlive && IsDeployed;

        /// <summary>Creates a unit at full health from its archetype template.</summary>
        /// <param name="id">Stable identifier.</param>
        /// <param name="kind">Archetype.</param>
        /// <param name="team">Allegiance.</param>
        /// <returns>An undeployed, full-health unit.</returns>
        public static Unit FromTemplate(UnitId id, UnitKind kind, Team team)
        {
            var template = UnitTemplate.For(kind);
            return new Unit
            {
                Id = id,
                Kind = kind,
                Team = team,
                Hp = template.MaxHp,
                MaxHp = template.MaxHp,
                Move = template.Move,
                Footing = template.Footing,
                Position = default,
                IsDeployed = false,
            };
        }
    }
}
