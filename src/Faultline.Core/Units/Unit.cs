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

        /// <summary>
        /// Movement points already spent this activation, across every segment walked so far (D-097).
        /// </summary>
        /// <remarks>
        /// Movement is not one decision any more. Each click while the move half is open walks one
        /// segment and adds its cost here; the next segment is routed from the new tile on what is
        /// left. Reset by <see cref="Faultline.Core.Game"/> when the activation ends.
        /// </remarks>
        public int MoveSpent { get; init; }

        /// <summary>
        /// True once the move half has been shut for this activation regardless of what is left in
        /// the budget — which is what taking an action does (D-097).
        /// </summary>
        public bool MoveClosed { get; init; }

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
        /// True while this unit is walking off a downing in the previous fight. It takes no activation
        /// slot in round 1 — the slot is omitted, not passed — and is cleared when round 2 begins.
        /// Everything else about it is a normal unit. See <see cref="Faultline.Core.Bedraggled"/>.
        /// </summary>
        public bool Bedraggled { get; init; }

        /// <summary>
        /// What the camps have given this duck, carried onto the board by the run (MASTER_DESIGN
        /// §8.5). Empty for enemies and for any fight played outside a run.
        /// </summary>
        /// <remarks>
        /// The rule sites read it directly — <see cref="Verve.CostOf(VerveSpend, Unit)"/>,
        /// <see cref="Movement.StepCost"/>, <see cref="Pits.CanFinish"/> and the rest — so a mod is
        /// one conditional at the rule it modifies rather than a parallel rulebook.
        /// </remarks>
        public DuckLoadout Loadout { get; init; } = DuckLoadout.Empty;

        /// <summary>
        /// Which <see cref="SecondWind"/> conditions have already paid out this <em>round</em>, as a
        /// bit per condition. Cleared when a round begins.
        /// </summary>
        /// <remarks>
        /// A bitmask rather than a flag per condition because "first time each round" is a property
        /// of a condition, not of the class — and a second latched condition must not have to find a
        /// second field.
        /// </remarks>
        public int SecondWindRoundUsed { get; init; }

        /// <summary>
        /// Which <see cref="SecondWind"/> conditions have already paid out this <em>fight</em>, as a
        /// bit per condition. Never cleared while the fight runs.
        /// </summary>
        public int SecondWindFightUsed { get; init; }

        /// <summary>
        /// True once the Guard Stance this unit is holding has actually absorbed something. Read when
        /// the stance expires, which is the only moment "unabsorbed" can be judged.
        /// </summary>
        public bool GuardAbsorbed { get; init; }

        /// <summary>
        /// Who last displaced this unit, and in which round — the two facts Chum the Water needs when
        /// somebody else lands the kill.
        /// </summary>
        /// <remarks>
        /// Recorded on the victim rather than as a list on the Fisher. A list on a unit would compare
        /// by reference under the record's generated equality and quietly break replay; two scalars
        /// on the thing that was moved say the same thing and cannot.
        /// </remarks>
        public UnitId? DisplacedBy { get; init; }

        /// <summary>The round <see cref="DisplacedBy"/> refers to. Zero when it has never been moved.</summary>
        public int DisplacedInRound { get; init; }

        /// <summary>
        /// The flock whose next displacement of this unit gains a tile. <c>null</c> when the unit
        /// carries no such mark.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Held on the victim rather than as a list on the marker, for the reason
        /// <see cref="DisplacedBy"/> gives: a list on a unit compares by reference under the record's
        /// generated equality and quietly breaks replay. The team rather than a bool, because the mark
        /// is spent by <em>the other flock</em> and by nobody else — a Vanguard who shoves his own
        /// Rattled enemy again must not consume it.
        /// </para>
        /// <para>
        /// <b>Two authors, one mark.</b> Rattling Impact writes it on a collision and a
        /// <see cref="Consumable.ChalkMark"/> writes it out of a pocket (MASTER_DESIGN §8.6). They
        /// say the identical sentence, so they are the identical field: a second, parallel mark
        /// would need its own composition rule at the request site and the two would drift (D-190).
        /// </para>
        /// </remarks>
        public Team? RattledFor { get; init; }

        /// <summary>
        /// Whether a Greased Feather has armed this unit's next displacement for a tile more
        /// (MASTER_DESIGN §8.6). Spent by the attempt, not by the result.
        /// </summary>
        /// <remarks>
        /// The mirror of <see cref="RattledFor"/> across the displacement: that one rides on the body
        /// being moved and this one on the body doing the moving, which is the difference between
        /// "the other flock's next displacement <em>of it</em>" and "<em>this duck's</em> next
        /// displacement". Shaped after <see cref="WreckingWeightArmed"/> and read at the same request
        /// site, so all three compose instead of layering (D-190).
        /// </remarks>
        public bool GreasedFeatherArmed { get; init; }

        /// <summary>
        /// Round in which this unit last applied Rattling Impact. Zero when it never has. The latch
        /// behind "the first enemy he collides each round".
        /// </summary>
        public int RattlingImpactRound { get; init; }

        /// <summary>
        /// Round in which this unit last fired Crossing Shot. Zero when it never has. The whole of the
        /// reaction's "once per round".
        /// </summary>
        public int CrossingShotRound { get; init; }

        /// <summary>
        /// Force banked by Stored Force, capped at <see cref="Techniques.StoredForceCap"/>. Spent as a
        /// push by a tip-tile Spear hit.
        /// </summary>
        public int StoredForce { get; init; }

        /// <summary>
        /// The enemy a Hand-Off has been granted against: this duck's next basic attack on it may take
        /// the granted push. <c>null</c> when no grant is outstanding.
        /// </summary>
        public UnitId? HandOffTarget { get; init; }

        /// <summary>
        /// A free step this duck has banked from Shelter Step — the tile the Wardbearer left.
        /// <c>null</c> when nothing is banked.
        /// </summary>
        /// <remarks>
        /// Banked rather than taken, because the tile belongs to the other player's duck and
        /// MASTER_DESIGN §8.5's bodily-consent rule means nothing moves another player's body without
        /// that owner saying so. The owner says so by issuing
        /// <see cref="TakeBankedStepCommand"/> — and never issuing it is a legal answer.
        /// </remarks>
        public Coord? BankedStepTo { get; init; }

        /// <summary>
        /// Tiles of free movement this duck is owed by a permanent legendary — Follow Through's move
        /// after a collision, Kestrel Step's move after a shot (MASTER_DESIGN §8.6).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The activation waits while this is above zero</b>, which is the rule-break the legendary
        /// tier is for: §3's AP turn ends an activation the moment its action lands, and these tiles
        /// arrive after it. Holding the activation open is the narrowest way to pay them — the duck is
        /// still the acting duck, its owner is still the acting player, and no off-turn timing has to
        /// be invented (§14 #13 leaves that unruled; D-202).
        /// </para>
        /// <para>
        /// Free of the AP purse: <see cref="Activation.Remaining"/> is untouched by spending one, so a
        /// duck that spent its whole pool still walks. Brambles still bite on arrival, exactly as they
        /// do for a banked Shelter Step.
        /// </para>
        /// </remarks>
        public int FreeSteps { get; init; }

        /// <summary>
        /// True once a legendary has already granted free steps this activation. The latch behind
        /// "after causing a collision" being one payout, not one per collision in a trample.
        /// </summary>
        public bool FreeStepsGranted { get; init; }

        /// <summary>
        /// True while Deep Roots is holding this Wardbearer's Guard Stance through an activation
        /// (MASTER_DESIGN §8.6). Set when the stance survives the start of an activation it would
        /// otherwise have dropped at, and cleared when that activation ends — which is what makes the
        /// card "persists through his NEXT activation" rather than "never drops".
        /// </summary>
        public bool GuardHeldByRoots { get; init; }

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

        /// <summary>
        /// Points still available this activation - action points for a player unit, movement
        /// points for an enemy. Zero once the activation is closed.
        /// </summary>
        public int MoveRemaining
        {
            get
            {
                if (MoveClosed)
                {
                    return 0;
                }

                int left = Activation.Pool(this) - MoveSpent;
                return left < 0 ? 0 : left;
            }
        }

        /// <summary>
        /// True when the move half is finished — the budget is gone, or an action closed it.
        /// </summary>
        /// <remarks>
        /// Derived rather than stored since D-097 made movement segmented: "has moved" used to be a
        /// latch a single walk set, and the question every caller was really asking is whether the
        /// unit can still walk. A unit two tiles into a three-point budget answers <c>false</c>, and
        /// so can still spend the rest of it — including to walk into reach of a rescue.
        /// </remarks>
        public bool HasMoved => MoveRemaining <= 0;

        /// <summary>
        /// True once this activation's movement has been started or shut — a tile walked, or an
        /// action that closed the move half.
        /// </summary>
        /// <remarks>
        /// Not the same question as <see cref="HasMoved"/>, and the difference is the whole reason
        /// this exists. <c>HasMoved</c> asks whether the unit can still walk; this asks whether it
        /// has spent anything yet. They agree for everything with legs and come apart for a Move-0
        /// archetype: the Warden's budget is empty before it does anything, so <c>HasMoved</c> is
        /// vacuously true for it from the first instant of every activation. A rule that costs the
        /// <em>whole</em> activation — the rescue slot — has to ask this one, or the archetype whose
        /// entire job is standing beside something can never take it.
        /// </remarks>
        public bool HasSpentMovement => MoveClosed || MoveSpent > 0;

        /// <summary>Display name.</summary>
        public string Name => Template.Name;

        /// <summary>True while the unit still has hit points and has not been voided.</summary>
        public bool IsAlive => Hp > 0 && !Voided;

        /// <summary>True when the unit is alive and standing on the board.</summary>
        public bool IsOnBoard => IsAlive && IsDeployed;

        /// <summary>Whether this duck's spender carries a mod.</summary>
        /// <param name="mod">Mod to look for.</param>
        /// <returns>Whether it is fitted.</returns>
        public bool Has(Mod mod) => Loadout.Has(mod);

        /// <summary>Whether this duck earns Pluck from an extra condition.</summary>
        /// <param name="wind">Condition to look for.</param>
        /// <returns>Whether it is held.</returns>
        public bool Has(SecondWind wind) => Loadout.Has(wind);

        /// <summary>Whether this duck carries a rule unlock.</summary>
        /// <param name="unlock">Unlock to look for.</param>
        /// <returns>Whether it is held.</returns>
        public bool Has(Unlock unlock) => Loadout.Has(unlock);

        /// <summary>
        /// Whether this duck's kit carries a technique modifier <em>and</em> is the class it belongs
        /// to. Checked together because every technique is class-bound, and a rule that trusted the
        /// loadout alone would be one draft edit from firing an Archer's card off a Wardbearer.
        /// </summary>
        /// <param name="technique">Technique to look for.</param>
        /// <returns>Whether this unit acts on it.</returns>
        public bool Has(TechniqueModifier technique) =>
            Kind == CampCatalogue.KindOf(technique) && Loadout.Has(technique);

        /// <summary>
        /// Whether this duck wears a permanent legendary, and is the class that can wear it. Class
        /// and loadout together for the reason <see cref="Has(TechniqueModifier)"/> checks both: a
        /// legendary is its class's epithet, and nothing else's.
        /// </summary>
        /// <param name="card">Legendary to look for.</param>
        /// <returns>Whether this unit acts on it.</returns>
        public bool Has(Legendary card) =>
            Kind == LegendaryCatalogue.KindOf(card) && Loadout.Has(card);

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
