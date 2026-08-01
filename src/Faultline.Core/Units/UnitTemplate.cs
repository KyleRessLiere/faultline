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
    public sealed record UnitTemplate(
        UnitKind Kind,
        string Name,
        int MaxHp,
        int Move,
        AttackKind Attack,
        int Range,
        int Damage,
        int Footing,
        bool FreeClimb)
    {
        private static readonly Dictionary<UnitKind, UnitTemplate> Table = Build();

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
                new UnitTemplate(UnitKind.Vanguard, "Vanguard", 7, 3, AttackKind.Melee, 1, 1, 1, false),
                new UnitTemplate(UnitKind.Archer, "Archer", 4, 3, AttackKind.Ranged, 3, 2, 1, true),
                new UnitTemplate(UnitKind.Threadcaster, "Threadcaster", 4, 3, AttackKind.Ranged, 3, 1, 1, false),
                new UnitTemplate(UnitKind.Wardbearer, "Wardbearer", 6, 3, AttackKind.Melee, 1, 1, 1, false),

                // Brief §2: Enemies. Grappler and Stalker have no basic attack — they act via displacement (M3).
                new UnitTemplate(UnitKind.Husk, "Husk", 2, 3, AttackKind.Melee, 1, 1, 1, false),
                new UnitTemplate(UnitKind.Lobber, "Lobber", 3, 2, AttackKind.Ranged, 3, 1, 1, false),
                new UnitTemplate(UnitKind.Anchor, "Anchor", 6, 1, AttackKind.Melee, 1, 2, 1, false),
                new UnitTemplate(UnitKind.Grappler, "Grappler", 5, 3, AttackKind.None, 0, 0, 1, false),
                new UnitTemplate(UnitKind.Stalker, "Stalker", 4, 4, AttackKind.None, 0, 0, 1, false),
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
