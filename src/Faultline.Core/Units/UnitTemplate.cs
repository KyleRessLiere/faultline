using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>Static stat block for a <see cref="UnitKind"/>. Brief §2 stat tables.</summary>
    /// <param name="Kind">Archetype this describes.</param>
    /// <param name="RawName">The table's own label; <see cref="Name"/> is what a player reads.</param>
    /// <param name="MaxHp">Starting and maximum hit points.</param>
    /// <param name="Move">Movement points per activation.</param>
    /// <param name="Attack">How the basic attack reaches.</param>
    /// <param name="Range">Basic attack range in orthogonal steps.</param>
    /// <param name="Damage">Basic attack damage before HighGround bonuses.</param>
    /// <param name="Footing">
    /// Footing the archetype starts a fight with, counted in whole <em>refusals</em> rather than in
    /// tiles (MASTER_DESIGN §3, Design Log (t)). Zero for the player classes, who are granted it per
    /// scenario by the <c>footing:</c> key in a <c>.fight</c> file. On an enemy this is the
    /// anti-displacement stat and a bestiary lever: regulars carry 1, and a stack of 2 or more is how
    /// a stat block says "this one will cost you properly to fish".
    /// </param>
    /// <param name="AttackPush">Push distance the basic attack applies on top of its damage.</param>
    /// <param name="BasicPull">
    /// Pull distance the basic action may apply instead of dealing damage, within <paramref name="Range"/>.
    /// Zero when the archetype cannot pull.
    /// </param>
    /// <param name="BasicPush">
    /// Push distance the basic action applies on its own, for archetypes whose entire action is a
    /// shove. Zero when the archetype has no standalone shove.
    /// </param>
    /// <param name="Plan">
    /// Which priority list in <see cref="Ai"/> plans for this archetype. Stat-block variants share a
    /// plan with the archetype they vary, so the two lists cannot drift apart.
    /// </param>
    /// <param name="PushResistance">
    /// Tiles subtracted from every displacement against this unit, before the Bulwark cap (D-018,
    /// D-139). One for the Anchor, two for the Colossus and the Wardbearer. Resistance SHORTENS and
    /// Footing REFUSES: the two never share arithmetic.
    /// </param>
    /// <param name="HoldAura">
    /// True when adjacent allies cannot be displaced more than 1 tile. Only the enemy Bulwark carries
    /// it: the Wardbearer's copy was deleted with the rest of its old kit (D-058), but the mechanic
    /// stays a flag on the stat block rather than an archetype check (D-031).
    /// </param>
    /// <param name="Tramples">
    /// Whether it shoulders through a body in its way rather than stopping or going round it
    /// (D-100). Only the Husk does.
    /// </param>
    /// <param name="MinRange">
    /// Closest tile the basic attack can be aimed at. Zero for everything without a stated minimum
    /// — only the Archer has one, and hers is what makes closing on her an answer (D-099).
    /// </param>
    /// <param name="HazardRanks">
    /// How many tiers of the pit → spikes → wall/edge ladder a flanking shover will aim for: 3 takes
    /// all three, 2 stops at spikes, 0 means the archetype does not hunt hazards at all (D-024).
    /// </param>
    /// <param name="Enraged">
    /// The second stat block this archetype swaps to at <paramref name="EnrageAt"/> hit points, or
    /// <c>null</c> for the single-phase majority. It is a whole template, so a phase change is a
    /// substitution rather than a pile of exceptions (D-040).
    /// </param>
    /// <param name="EnrageAt">
    /// Hit points at or below which <paramref name="Enraged"/> takes over. Meaningless when there is
    /// no second stat block.
    /// </param>
    /// <param name="SweetSpot">
    /// The one distance at which the basic attack deals <paramref name="Damage"/> rather than
    /// <paramref name="OffSpotDamage"/>. Zero for every profile whose damage does not depend on
    /// distance, which is everything but the Archer (MASTER_DESIGN §4, locked af).
    /// </param>
    /// <param name="OffSpotDamage">
    /// What the basic attack deals anywhere in its band that is not the <paramref name="SweetSpot"/>.
    /// Meaningless when there is no sweet spot.
    /// </param>
    /// <param name="IsObject">
    /// True for a thing on the board that is not a fighter — the barrel (MASTER_DESIGN §6). It stands
    /// on the enemy side so that collisions and shoves treat it like any other body, but <b>clearing
    /// the board does not mean destroying it</b>: a kill-all is won with barrels still standing, and a
    /// Cooper placing more of them can never make a fight unwinnable.
    /// </param>
    public sealed record UnitTemplate(
        UnitKind Kind,
        string RawName,
        int MaxHp,
        int Move,
        AttackKind Attack,
        int Range,
        int Damage,
        int Footing,
        int AttackPush = 0,
        int BasicPull = 0,
        int BasicPush = 0,
        EnemyPlan Plan = EnemyPlan.None,
        int PushResistance = 0,
        bool HoldAura = false,
        int HazardRanks = 0,
        int MinRange = 0,
        bool Tramples = false,
        UnitTemplate? Enraged = null,
        int EnrageAt = 0,
        int SweetSpot = 0,
        int OffSpotDamage = 0,
        bool IsObject = false)
    {
        private static readonly Dictionary<UnitKind, UnitTemplate> Table = Build();

        /// <summary>
        /// What this archetype is called on screen, through the naming layer so that a display name
        /// and its identifier can differ without anything having two names (MASTER_DESIGN §15).
        /// <see cref="RawName"/> is the label the table carries; this is what a player reads.
        /// </summary>
        public string Name => Naming.Of(Kind);

        /// <summary>
        /// True when this unit cannot shoot at what is standing on top of it — a bow needs room
        /// (D-099). Zero for everything without a stated minimum, which is everything but the Archer.
        /// </summary>
        public bool HasMinRange => MinRange > 1;

        /// <summary>True when the basic attack's damage depends on how far away the target is.</summary>
        public bool HasSweetSpot => SweetSpot > 0;

        /// <summary>
        /// What the basic attack deals at a given distance, before the HighGround bonus.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>MASTER_DESIGN §4, the Archer's sweet spot (locked af):</b> range 2–4, <b>4 at range 3</b>
        /// and <b>2 at ranges 2 and 4</b> — the Wardbearer's spear tip as a gun, so her damage is
        /// something she earns with feet rather than something her profile hands her.
        /// </para>
        /// <para>
        /// One expression, and it is the <em>only</em> place the falloff exists: <see cref="Combat"/>
        /// asks it for both the preview and the resolution, so the number on the hover and the number
        /// that lands cannot disagree. Distance is whatever the caller already measured — this
        /// deliberately does no measuring of its own, because a second distance metric for the band
        /// would be a second geometry for the same bow.
        /// </para>
        /// <para>
        /// A profile with no sweet spot answers <see cref="Damage"/> at every distance, which is every
        /// profile but the Archer's.
        /// </para>
        /// </remarks>
        /// <param name="distance">How far the target is, in the game's own distance.</param>
        /// <returns>Damage before elevation.</returns>
        public int DamageAtRange(int distance) =>
            HasSweetSpot && distance != SweetSpot ? OffSpotDamage : Damage;

        /// <summary>True when the basic action may pull instead of dealing damage.</summary>
        public bool CanPullWithBasic => BasicPull > 0;

        /// <summary>True when the basic action is a standalone shove rather than an attack.</summary>
        public bool CanPushWithBasic => BasicPush > 0;

        /// <summary>
        /// How far the unit's basic action reaches: the attack's range, or the reach of its
        /// displacement action when it has no attack.
        /// </summary>
        public int BasicReach
        {
            get
            {
                int reach = Attack == AttackKind.Melee ? 1 : (Attack == AttackKind.Ranged ? Range : 0);
                if (BasicPull > 0 && Range > reach)
                {
                    reach = Range;
                }

                if (BasicPush > 0)
                {
                    int pushReach = Range < 1 ? 1 : Range;
                    if (pushReach > reach)
                    {
                        reach = pushReach;
                    }
                }

                return reach;
            }
        }

        /// <summary>Looks up the stat block for an archetype.</summary>
        /// <param name="kind">Archetype to look up.</param>
        /// <returns>Its template.</returns>
        public static UnitTemplate For(UnitKind kind)
        {
            if (!Table.TryGetValue(kind, out var template))
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "No template for unit kind.");
            }

            return template;
        }

        private static Dictionary<UnitKind, UnitTemplate> Build()
        {
            // docs/archive/CURATED_SET.md §5B: the Quarry King's second stat block. Move 1 becomes Move 3 and
            // the priority list gains a Bull Rush, which the planner reads off the standalone shove
            // rather than off the archetype — the enraged block is the one that carries a BasicPush.
            var quarryKingEnraged = new UnitTemplate(
                UnitKind.QuarryKing, "Quarry King", 28, 3, AttackKind.Melee, 1, 6, 3,
                AttackPush: 1, BasicPush: 2, Plan: EnemyPlan.QuarryKing);

            // MASTER_DESIGN §8.9: the Rushmaster's second stat block, in force at 13 HP or below.
            // The harness breaks — Move 1 becomes Move 3 — and the standalone shove that appears is
            // Stampede. Same read as the Quarry King's: which branches his list has is a property of
            // the block in force, so a phase swap adds the branch and there are never two lists.
            // Footing is deliberately the same number: a phase swap is a stat block, and §8.9 prints
            // "remaining" because what is left is the LIVE counter on the unit, which nothing here
            // refills — Unit.Footing is set once at FromTemplate and only ever spent.
            var rushmasterCutLoose = new UnitTemplate(
                UnitKind.Rushmaster, "Rushmaster", 26, 3, AttackKind.Melee, 1, 4, 1,
                AttackPush: 1, BasicPush: 2, Plan: EnemyPlan.Rushmaster, PushResistance: 1);

            var all = new[]
            {
                // Brief §2: Player classes. Move 3 for all player units. Footing is 0 for everyone —
                // a blanket token on every unit shortens every shove by a tile and makes "resist a
                // push" the default, which blunts the board. Scenarios grant it instead, through the
                // 'footing:' key in the .fight file.
                // The Vanguard's basic shoves; the Threadcaster's may pull instead of hurting.
                new UnitTemplate(UnitKind.Vanguard, "Vanguard", 14, 3, AttackKind.Melee, 1, 2, 0, AttackPush: 1),
                // The sweet spot (MASTER_DESIGN §4, locked af): the band is 2–4 and the 4 damage lives
                // at exactly 3. Minimum range is untouched — the dead zone is a different rule and
                // keeps its high-ground exception.
                new UnitTemplate(
                    UnitKind.Archer, "Archer", 8, 3, AttackKind.Ranged, 4, 4, 0,
                    MinRange: 2, SweetSpot: 3, OffSpotDamage: 2),
                new UnitTemplate(UnitKind.Threadcaster, "Threadcaster", 8, 3, AttackKind.Ranged, 3, 2, 0, BasicPull: 1),
                // D-058: the Wardbearer's hold aura is gone and it is heavier instead — 7 HP behind
                // push resistance 2, the Colossus's number on a player class. The aura mechanic
                // itself survives on the enemy Bulwark below.
                new UnitTemplate(UnitKind.Wardbearer, "Wardbearer", 14, 3, AttackKind.Melee, 1, 2, 0, PushResistance: 2),

                // Brief §2: Enemies. Grappler and Stalker deal no damage at all — their whole action
                // is the displacement their priority list calls for (Pull 2 at range 3; Push 1 in melee).
                new UnitTemplate(UnitKind.Husk, "Husk", 4, 3, AttackKind.Melee, 1, 2, 0, Plan: EnemyPlan.Melee, Tramples: true),
                new UnitTemplate(UnitKind.Lobber, "Lobber", 6, 2, AttackKind.Ranged, 3, 2, 0, Plan: EnemyPlan.Lobber),
                new UnitTemplate(UnitKind.Anchor, "Anchor", 12, 1, AttackKind.Melee, 1, 4, 0, Plan: EnemyPlan.Melee, PushResistance: 1),
                new UnitTemplate(UnitKind.Grappler, "Grappler", 10, 3, AttackKind.None, 3, 0, 0, BasicPull: 2, Plan: EnemyPlan.Grappler),
                new UnitTemplate(UnitKind.Stalker, "Stalker", 8, 4, AttackKind.None, 1, 0, 0, BasicPush: 1, Plan: EnemyPlan.Stalker, HazardRanks: 3),

                // docs/ENEMY_ROSTER.md: behaviour variants. Each one names a plan the shipped five do
                // not have; everything else about it is a number on this row.
                new UnitTemplate(UnitKind.Warden, "Warden", 12, 0, AttackKind.Melee, 1, 4, 2, Plan: EnemyPlan.Warden),
                new UnitTemplate(UnitKind.Perch, "Perch", 6, 2, AttackKind.Ranged, 3, 2, 0, Plan: EnemyPlan.Perch),
                new UnitTemplate(UnitKind.Bulwark, "Bulwark", 10, 2, AttackKind.Melee, 1, 2, 0, Plan: EnemyPlan.Melee, HoldAura: true),
                new UnitTemplate(UnitKind.Harrier, "Harrier", 8, 4, AttackKind.None, 1, 0, 0, BasicPush: 1, Plan: EnemyPlan.Harrier),
                new UnitTemplate(UnitKind.Runt, "Runt", 2, 4, AttackKind.Melee, 1, 2, 0, Plan: EnemyPlan.Melee),
                new UnitTemplate(UnitKind.Colossus, "Colossus", 20, 1, AttackKind.Melee, 1, 6, 0, Plan: EnemyPlan.Melee, PushResistance: 2),

                // docs/ENEMY_ROSTER.md: balance variants. Same plan as the archetype they vary, so the
                // priority list is shared rather than copied — only the numbers differ.
                new UnitTemplate(UnitKind.LesserGrappler, "Lesser Grappler", 10, 3, AttackKind.None, 2, 0, 0, BasicPull: 2, Plan: EnemyPlan.Grappler),
                new UnitTemplate(UnitKind.BluntedStalker, "Blunted Stalker", 8, 4, AttackKind.None, 1, 0, 0, BasicPush: 1, Plan: EnemyPlan.Stalker, HazardRanks: 2),
                new UnitTemplate(UnitKind.HeavyHusk, "Heavy Husk", 6, 3, AttackKind.Melee, 1, 2, 0, Plan: EnemyPlan.Melee),

                // The reserved Footing-2 fixture. A Husk in every other number, fielded by nothing:
                // it exists so the stacked-Footing rules have something to be asserted against
                // without a shipped board being re-tiered behind a rules change (the D-092 trap).
                new UnitTemplate(UnitKind.BracedHusk, "Braced Husk", 4, 3, AttackKind.Melee, 1, 2, 2, Plan: EnemyPlan.Melee),
                new UnitTemplate(UnitKind.MobileAnchor, "Mobile Anchor", 12, 2, AttackKind.Melee, 1, 4, 0, Plan: EnemyPlan.Melee, PushResistance: 1),

                // The neutral. A stat block with nothing on it — no attack, no shove, no resistance —
                // whose whole identity is the plan it names. Fielded by nothing: it is the proof that
                // a new priority list costs one registration and one planner, not a proof that the
                // roster needed another body (component review, "Acceptance test for the architecture
                // itself").
                new UnitTemplate(UnitKind.EscortDuckling, "Escort Duckling", 4, 4, AttackKind.None, 0, 0, 0, Plan: EnemyPlan.Escort),

                // docs/archive/CURATED_SET.md §5A/§5B: the objective enemies. The Raider is a Husk in every
                // number and differs only in the list it runs. The Quarry King's three carry the
                // boss's whole anti-displacement budget: three refusals, spent one per instance like
                // everybody else's since D-143 retired the negating token.
                new UnitTemplate(UnitKind.Raider, "Raider", 4, 3, AttackKind.Melee, 1, 2, 0, Plan: EnemyPlan.Raider),

                // MASTER_DESIGN §6. The Cooper is a CLOCK, not a fighter: no attack at all, and two
                // tiles of walk to reach the next barrel. Killing him stops the clock and leaves every
                // barrel already on the board exactly where it is.
                new UnitTemplate(UnitKind.Cooper, "Cooper", 8, 2, AttackKind.None, 0, 0, 0, Plan: EnemyPlan.Cooper),

                // The barrel. Move 0 and no attack — it does nothing of its own accord, which is what
                // an object is; everything it does to the board it does because somebody shoved it.
                new UnitTemplate(
                    UnitKind.Barrel, "Barrel", 4, 0, AttackKind.None, 0, 0, 0,
                    Plan: EnemyPlan.Inert, IsObject: true),
                new UnitTemplate(
                    UnitKind.QuarryKing, "Quarry King", 28, 1, AttackKind.Melee, 1, 6, 3,
                    AttackPush: 1, Plan: EnemyPlan.QuarryKing,
                    Enraged: quarryKingEnraged, EnrageAt: 14),

                // MASTER_DESIGN §8.9: the Warrens boss. Footing ONE and no shell — the shell is the
                // Quarry King's and is reserved for the Locks. Displacement against the Rushmaster is
                // always legal after his single refusal, which is what makes his own crowd ammunition
                // instead of armour once you have spent it (D-217).
                new UnitTemplate(
                    UnitKind.Rushmaster, "Rushmaster", 26, 1, AttackKind.Melee, 1, 4, 1,
                    AttackPush: 1, Plan: EnemyPlan.Rushmaster, PushResistance: 1,
                    Enraged: rushmasterCutLoose, EnrageAt: 13),
            };

            var table = new Dictionary<UnitKind, UnitTemplate>(all.Length);
            foreach (var template in all)
            {
                table[template.Kind] = template;
            }

            return table;
        }
    }
}
