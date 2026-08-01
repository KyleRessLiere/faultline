using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>Static stat block for a <see cref="UnitKind"/>. Brief §2 stat tables.</summary>
    /// <param name="Kind">Archetype this describes.</param>
    /// <param name="Name">Display name.</param>
    /// <param name="MaxHp">Starting and maximum hit points.</param>
    /// <param name="Move">Movement points per activation.</param>
    /// <param name="Attack">How the basic attack reaches.</param>
    /// <param name="Range">Basic attack range in orthogonal steps.</param>
    /// <param name="Damage">Basic attack damage before HighGround bonuses.</param>
    /// <param name="Footing">
    /// Footing tokens the archetype starts a fight with. Zero for every archetype: Footing is granted
    /// per scenario by the <c>footing:</c> key in a <c>.fight</c> file, never automatically.
    /// </param>
    /// <param name="FreeClimb">True when entering HighGround costs no extra movement.</param>
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
    /// Tiles subtracted from every Push against this unit, before Hold and Footing (D-018). One for
    /// the Anchor, two for the Colossus. Pull is never reduced.
    /// </param>
    /// <param name="HoldAura">
    /// True when adjacent allies cannot be displaced more than 1 tile. The Wardbearer's Hold and the
    /// Bulwark's are the same rule, read off this flag (D-019).
    /// </param>
    /// <param name="HazardRanks">
    /// How many tiers of the pit → spikes → wall/edge ladder a flanking shover will aim for: 3 takes
    /// all three, 2 stops at spikes, 0 means the archetype does not hunt hazards at all (D-024).
    /// </param>
    public sealed record UnitTemplate(
        UnitKind Kind,
        string Name,
        int MaxHp,
        int Move,
        AttackKind Attack,
        int Range,
        int Damage,
        int Footing,
        bool FreeClimb,
        int AttackPush = 0,
        int BasicPull = 0,
        int BasicPush = 0,
        EnemyPlan Plan = EnemyPlan.None,
        int PushResistance = 0,
        bool HoldAura = false,
        int HazardRanks = 0)
    {
        private static readonly Dictionary<UnitKind, UnitTemplate> Table = Build();

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
            var all = new[]
            {
                // Brief §2: Player classes. Move 3 for all player units. Footing is 0 for everyone —
                // a blanket token on every unit shortens every shove by a tile and makes "resist a
                // push" the default, which blunts the board. Scenarios grant it instead, through the
                // 'footing:' key in the .fight file.
                // The Vanguard's basic shoves; the Threadcaster's may pull instead of hurting.
                new UnitTemplate(UnitKind.Vanguard, "Vanguard", 7, 3, AttackKind.Melee, 1, 1, 0, false, AttackPush: 1),
                new UnitTemplate(UnitKind.Archer, "Archer", 4, 3, AttackKind.Ranged, 3, 2, 0, true),
                new UnitTemplate(UnitKind.Threadcaster, "Threadcaster", 4, 3, AttackKind.Ranged, 3, 1, 0, false, BasicPull: 1),
                new UnitTemplate(UnitKind.Wardbearer, "Wardbearer", 6, 3, AttackKind.Melee, 1, 1, 0, false, HoldAura: true),

                // Brief §2: Enemies. Grappler and Stalker deal no damage at all — their whole action
                // is the displacement their priority list calls for (Pull 2 at range 3; Push 1 in melee).
                new UnitTemplate(UnitKind.Husk, "Husk", 2, 3, AttackKind.Melee, 1, 1, 0, false, Plan: EnemyPlan.Melee),
                new UnitTemplate(UnitKind.Lobber, "Lobber", 3, 2, AttackKind.Ranged, 3, 1, 0, false, Plan: EnemyPlan.Lobber),
                new UnitTemplate(UnitKind.Anchor, "Anchor", 6, 1, AttackKind.Melee, 1, 2, 0, false, Plan: EnemyPlan.Melee, PushResistance: 1),
                new UnitTemplate(UnitKind.Grappler, "Grappler", 5, 3, AttackKind.None, 3, 0, 0, false, BasicPull: 2, Plan: EnemyPlan.Grappler),
                new UnitTemplate(UnitKind.Stalker, "Stalker", 4, 4, AttackKind.None, 1, 0, 0, false, BasicPush: 1, Plan: EnemyPlan.Stalker, HazardRanks: 3),

                // docs/ENEMY_ROSTER.md: behaviour variants. Each one names a plan the shipped five do
                // not have; everything else about it is a number on this row.
                new UnitTemplate(UnitKind.Warden, "Warden", 6, 0, AttackKind.Melee, 1, 2, 0, false, Plan: EnemyPlan.Warden, PushResistance: 1),
                new UnitTemplate(UnitKind.Perch, "Perch", 3, 2, AttackKind.Ranged, 3, 1, 0, false, Plan: EnemyPlan.Perch),
                new UnitTemplate(UnitKind.Bulwark, "Bulwark", 5, 2, AttackKind.Melee, 1, 1, 0, false, Plan: EnemyPlan.Melee, HoldAura: true),
                new UnitTemplate(UnitKind.Harrier, "Harrier", 4, 4, AttackKind.None, 1, 0, 0, false, BasicPush: 1, Plan: EnemyPlan.Harrier),
                new UnitTemplate(UnitKind.Runt, "Runt", 1, 4, AttackKind.Melee, 1, 1, 0, false, Plan: EnemyPlan.Melee),
                new UnitTemplate(UnitKind.Colossus, "Colossus", 10, 1, AttackKind.Melee, 1, 3, 0, false, Plan: EnemyPlan.Melee, PushResistance: 2),

                // docs/ENEMY_ROSTER.md: balance variants. Same plan as the archetype they vary, so the
                // priority list is shared rather than copied — only the numbers differ.
                new UnitTemplate(UnitKind.LesserGrappler, "Lesser Grappler", 5, 3, AttackKind.None, 2, 0, 0, false, BasicPull: 2, Plan: EnemyPlan.Grappler),
                new UnitTemplate(UnitKind.BluntedStalker, "Blunted Stalker", 4, 4, AttackKind.None, 1, 0, 0, false, BasicPush: 1, Plan: EnemyPlan.Stalker, HazardRanks: 2),
                new UnitTemplate(UnitKind.HeavyHusk, "Heavy Husk", 3, 3, AttackKind.Melee, 1, 1, 0, false, Plan: EnemyPlan.Melee),
                new UnitTemplate(UnitKind.MobileAnchor, "Mobile Anchor", 6, 2, AttackKind.Melee, 1, 2, 0, false, Plan: EnemyPlan.Melee, PushResistance: 1),
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
