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

        /// <summary>
        /// Remaining Footing tokens for this fight (M2). Zero unless the scenario granted some through
        /// the <c>footing:</c> key — see <see cref="FightDefinition.FootingFor"/>.
        /// </summary>
        public int Footing { get; init; }

        /// <summary>Staggered until end of round; the next displacement against it gains +1 (M2).</summary>
        public bool Staggered { get; init; }

        /// <summary>
        /// True while this unit is holding Guard Stance: damage and displacement aimed at adjacent
        /// allies land on it instead, and attack damage it takes is halved, rounded up, minimum 1.
        /// Set by <see cref="Ability.GuardStance"/> and cleared at the start of the unit's next
        /// activation — not at end of round, which is the whole point of it (D-058).
        /// </summary>
        public bool Guarding { get; init; }

        /// <summary>
        /// Verve banked by this unit, capped at <see cref="Faultline.Core.Verve.Cap"/>. Earned on its
        /// own class's condition and spent only by itself — see <see cref="Faultline.Core.Verve"/>.
        /// Carries across fights on the <see cref="RunUnit"/> and is never reset by anything but
        /// spending it.
        /// </summary>
        public int Verve { get; init; }

        /// <summary>
        /// True once this unit has spent Verve during the current activation. One spend per
        /// activation, and spending costs neither half of it.
        /// </summary>
        public bool HasSpentVerve { get; init; }

        /// <summary>
        /// True while Wrecking Weight is armed: the next Push this unit causes gains a tile and deals
        /// 1 damage on contact. Consumed by that push, and dropped at the end of the activation
        /// whether it was used or not.
        /// </summary>
        public bool WreckingWeightArmed { get; init; }

        /// <summary>
        /// Attack actions still owed beyond the one the activation comes with, from Double Nock.
        /// Each attack spends one instead of ending the action half.
        /// </summary>
        public int ExtraAttacks { get; init; }

        /// <summary>
        /// The enemy a Reel has just dragged into contact, and only until this unit does anything
        /// else. Slingshot's window: while this is set, the pair may exchange tiles.
        /// </summary>
        public UnitId? SlingshotTarget { get; init; }

        /// <summary>True while clinging to the lip of a pit.</summary>
        public bool Clinging { get; init; }

        /// <summary>Round the unit went into the pit, so end-of-round resolution knows how long it has hung on.</summary>
        public int ClingingSinceRound { get; init; }

        /// <summary>Permanently removed from the run — died in a pit (M2).</summary>
        public bool Voided { get; init; }

        /// <summary>
        /// True once a two-phase archetype has swapped to its second stat block. Set the moment the
        /// unit drops to its template's <see cref="UnitTemplate.EnrageAt"/> and never cleared; only
        /// the Quarry King has a second block to swap to (D-040).
        /// </summary>
        public bool Enraged { get; init; }

        /// <summary>
        /// Stat block for this unit right now. A two-phase archetype reads its second block once
        /// <see cref="Enraged"/> is set, so every rule that asks a unit for its numbers — movement,
        /// damage, push resistance, the planner's dispatch — sees the swap at the same instant.
        /// </summary>
        public UnitTemplate Template
        {
            get
            {
                var template = UnitTemplate.For(Kind);
                return Enraged && template.Enraged is not null ? template.Enraged : template;
            }
        }

        /// <summary>Movement points per activation, read from the live stat block.</summary>
        public int Move => Template.Move;

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
                Footing = template.Footing,
                Position = default,
                IsDeployed = false,
            };
        }
    }
}
