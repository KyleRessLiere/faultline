namespace Faultline.Core
{
    /// <summary>Every unit archetype in the MVP. Brief §2 "Player classes" and "Enemies".</summary>
    public enum UnitKind
    {
        /// <summary>Player class: HP 7, melee, pushes.</summary>
        Vanguard = 0,

        /// <summary>Player class: HP 4, range 3.</summary>
        Archer = 1,

        /// <summary>Player class: HP 4, range 3, pulls.</summary>
        Threadcaster = 2,

        /// <summary>Player class: HP 6, melee, anchors nearby allies.</summary>
        Wardbearer = 3,

        /// <summary>Enemy: HP 2, chaff.</summary>
        Husk = 4,

        /// <summary>Enemy: HP 3, ranged skirmisher.</summary>
        Lobber = 5,

        /// <summary>Enemy: HP 6, immovable bruiser.</summary>
        Anchor = 6,

        /// <summary>Enemy: HP 5, puller.</summary>
        Grappler = 7,

        /// <summary>Enemy: HP 4, hazard-flanker.</summary>
        Stalker = 8,

        // Variants (docs/ENEMY_ROSTER.md). Appended, never renumbered: a fight's unit ids and the
        // command logs that replay them are keyed off this enum's order.

        /// <summary>Enemy: HP 6, Move 0. The door that stays shut.</summary>
        Warden = 9,

        /// <summary>Enemy: HP 3, ranged skirmisher that takes the high ground.</summary>
        Perch = 10,

        /// <summary>Enemy: HP 5, the enemy Wardbearer — anchors the allies beside it.</summary>
        Bulwark = 11,

        /// <summary>Enemy: HP 4, shoves players apart rather than into terrain.</summary>
        Harrier = 12,

        /// <summary>Enemy: HP 1, swarm chaff.</summary>
        Runt = 13,

        /// <summary>Enemy: HP 10, ignores Push entirely.</summary>
        Colossus = 14,

        /// <summary>Enemy: Grappler with pull range 2.</summary>
        LesserGrappler = 15,

        /// <summary>Enemy: Stalker that will not shove into a wall or the board edge.</summary>
        BluntedStalker = 16,

        /// <summary>Enemy: Husk with HP 3 — survives one collision.</summary>
        HeavyHusk = 17,

        /// <summary>Enemy: Anchor with Move 2.</summary>
        MobileAnchor = 18,

        /// <summary>Enemy: HP 2, walks at the Protect structure and never at you.</summary>
        Raider = 19,

        /// <summary>Enemy: HP 14 boss. Three Footing and a second stat block at 14 HP.</summary>
        QuarryKing = 20,

        /// <summary>
        /// Enemy: a Husk that holds Footing 2 — the reserved stacked-Footing fixture (D-144). It
        /// exists so the instance model's stack rules have something to be asserted against, and it
        /// is fielded by no fight; putting it on a board is a design decision, not a test's.
        /// </summary>
        BracedHusk = 21,

        /// <summary>
        /// Neutral: an escort duckling. No attack, no shove — it puts distance between itself and
        /// whatever is hostile to it and otherwise stands still. It exists to prove that a genuinely
        /// new priority list costs one registration in <see cref="EnemyPlanDefinition"/> and one
        /// planner in <see cref="Ai"/>, and it is <b>fielded by no <c>.fight</c> file and named by no
        /// campaign or acquisition pool</b>: putting a neutral on a board is a design decision, not a
        /// test's.
        /// </summary>
        EscortDuckling = 22,

        /// <summary>
        /// Enemy: HP 26 boss — the Warrens' traffic foreman. Move 1 in harness behind a crowd that is
        /// his armour, Move 3 and a Stampede once the harness breaks at 13 HP (MASTER_DESIGN §8.9).
        /// </summary>
        Rushmaster = 23,

        /// <summary>
        /// Enemy: HP 8, Move 2, no attack — the barrel clock (MASTER_DESIGN §6). He never fights;
        /// killing him stops the clock, not the barrels already on the board.
        /// </summary>
        Cooper = 24,

        /// <summary>
        /// An OBJECT, not a fighter: 4 HP, immobile of its own accord, shovable by anything, and it
        /// <b>pops</b> on collision or death (MASTER_DESIGN §6).
        /// </summary>
        /// <remarks>
        /// <b>Stored as a unit, and that is an implementation ruling rather than a reclassification.</b>
        /// The doc calls it debris-class, and debris is a <see cref="Structure"/> — but the
        /// displacement pipeline takes a <see cref="Unit"/> as its target at every level, and nothing
        /// in the game has ever moved a structure. A barrel that had to be shoved as a structure would
        /// need a second resolution path threaded through the whole pipeline, which is exactly what
        /// §6 forbids: "the displacement pipeline resolves the roll and the pipeline never checks who
        /// pushed". As a unit it rides the existing path with no change to it at all. See D-273.
        /// </remarks>
        Barrel = 25,
    }

    /// <summary>
    /// Which priority list in <see cref="Ai"/> plans for an archetype.
    /// </summary>
    /// <remarks>
    /// A stat-block variant is not a new behaviour: a Heavy Husk and a Husk both run
    /// <see cref="Melee"/>, so the two can never drift apart. The planner switches on this rather
    /// than on <see cref="UnitKind"/> for exactly that reason (docs/ENEMY_ROSTER.md).
    /// </remarks>
    /// <seealso cref="EnemyPlanDefinition">
    /// Where each member is registered, together with the method that executes it and the description
    /// of its branches. A member here without a registration there cannot run at all.
    /// </seealso>
    public enum EnemyPlan
    {
        /// <summary>Not an enemy; the archetype is player-controlled and has no priority list.</summary>
        None = 0,

        /// <summary>Adjacent → attack, else close on the nearest. Husk, Anchor and their variants.</summary>
        Melee = 1,

        /// <summary>Shoot from a band, retreat when contacted.</summary>
        Lobber = 2,

        /// <summary>Pull from range, preferring HighGround then the Archer.</summary>
        Grappler = 3,

        /// <summary>Flank and shove into terrain, ranked by hazard.</summary>
        Stalker = 4,

        /// <summary>Attack anything adjacent, and never move.</summary>
        Warden = 5,

        /// <summary>Take the nearest HighGround and shoot from it.</summary>
        Perch = 6,

        /// <summary>Flank and shove players away from their own allies.</summary>
        Harrier = 7,

        /// <summary>
        /// Claw the Protect structure, else walk to it. The only list with no clause about player
        /// units in it at all.
        /// </summary>
        Raider = 8,

        /// <summary>
        /// The melee list plus a Bull Rush branch, which exists only while the stat block in force
        /// carries a standalone shove.
        /// </summary>
        QuarryKing = 9,

        /// <summary>
        /// Break away from the nearest hostile, else hold. The escort duckling's list, and the only
        /// one that never acts on anybody.
        /// </summary>
        Escort = 10,

        /// <summary>
        /// The Rushmaster's published list (MASTER_DESIGN §8.9): a Stampede branch that reads the
        /// stat block in force, then lethal melee, then an adjacent attack, then a walk — Bell-ward
        /// while the harness holds, and at the largest cluster once it breaks.
        /// </summary>
        Rushmaster = 11,

        /// <summary>
        /// The Cooper's published list (MASTER_DESIGN §6): adjacent to a barrel → shove it down the
        /// lane holding the most players; else walk toward the nearest barrel; else place one.
        /// </summary>
        Cooper = 12,

        /// <summary>
        /// No clauses at all. An object's list: it never moves of its own accord and never acts. The
        /// barrel's, and the only plan whose whole content is that there is nothing to plan.
        /// </summary>
        Inert = 13,
    }

    /// <summary>How a unit's basic attack reaches its target.</summary>
    public enum AttackKind
    {
        /// <summary>No basic attack; the archetype acts only through its ability.</summary>
        None = 0,

        /// <summary>Range 1 only.</summary>
        Melee = 1,

        /// <summary>Ranged; gains <see cref="Combat.HighGroundBonus"/> damage from HighGround.</summary>
        Ranged = 2,
    }
}
