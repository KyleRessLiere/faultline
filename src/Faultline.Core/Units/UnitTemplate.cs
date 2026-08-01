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
    /// <param name="Footing">Footing tokens per fight.</param>
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
        int BasicPush = 0)
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
                // Brief §2: Player classes. Move 3 for all player units, 1 Footing per fight.
                // The Vanguard's basic shoves; the Threadcaster's may pull instead of hurting.
                new UnitTemplate(UnitKind.Vanguard, "Vanguard", 7, 3, AttackKind.Melee, 1, 1, 1, false, AttackPush: 1),
                new UnitTemplate(UnitKind.Archer, "Archer", 4, 3, AttackKind.Ranged, 3, 2, 1, true),
                new UnitTemplate(UnitKind.Threadcaster, "Threadcaster", 4, 3, AttackKind.Ranged, 3, 1, 1, false, BasicPull: 1),
                new UnitTemplate(UnitKind.Wardbearer, "Wardbearer", 6, 3, AttackKind.Melee, 1, 1, 1, false),

                // Brief §2: Enemies. Grappler and Stalker deal no damage at all — their whole action
                // is the displacement their priority list calls for (Pull 2 at range 3; Push 1 in melee).
                new UnitTemplate(UnitKind.Husk, "Husk", 2, 3, AttackKind.Melee, 1, 1, 1, false),
                new UnitTemplate(UnitKind.Lobber, "Lobber", 3, 2, AttackKind.Ranged, 3, 1, 1, false),
                new UnitTemplate(UnitKind.Anchor, "Anchor", 6, 1, AttackKind.Melee, 1, 2, 1, false),
                new UnitTemplate(UnitKind.Grappler, "Grappler", 5, 3, AttackKind.None, 3, 0, 1, false, BasicPull: 2),
                new UnitTemplate(UnitKind.Stalker, "Stalker", 4, 4, AttackKind.None, 1, 0, 1, false, BasicPush: 1),
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
