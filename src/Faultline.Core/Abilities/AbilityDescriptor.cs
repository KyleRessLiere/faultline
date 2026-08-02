using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// Everything a UI needs to present an ability: what it is called, what it reads as, how far it
    /// reaches and what it does. Kept in Core so the shell never writes its own rules text.
    /// </summary>
    /// <param name="Ability">Which ability.</param>
    /// <param name="Kind">Archetype that has it.</param>
    /// <param name="Name">Display name.</param>
    /// <param name="Summary">One-line rules text.</param>
    /// <param name="Targeting">What the player must pick.</param>
    /// <param name="Range">
    /// Reach in orthogonal steps; for Bull Rush, how far it charges; for a Line ability, how many
    /// tiles ahead the line covers.
    /// </param>
    /// <param name="Damage">Direct damage, before any collision or hazard the displacement causes.</param>
    /// <param name="Push">Push distance applied to the target, zero when it does not push.</param>
    /// <param name="PullsToAdjacent">True when it pulls the target all the way in.</param>
    public sealed record AbilityDescriptor(
        Ability Ability,
        UnitKind Kind,
        string Name,
        string Summary,
        AbilityTargeting Targeting,
        int Range,
        int Damage,
        int Push,
        bool PullsToAdjacent)
    {
        private static readonly AbilityDescriptor[] Empty = new AbilityDescriptor[0];

        private static readonly int[] NoTileDamage = new int[0];

        private static readonly Dictionary<UnitKind, AbilityDescriptor[]> ByKind = Build();

        private static readonly UnitKind[] PlayerOrder =
        {
            UnitKind.Vanguard, UnitKind.Archer, UnitKind.Threadcaster, UnitKind.Wardbearer,
        };

        /// <summary>
        /// Damage per tile of a <see cref="AbilityTargeting.Line"/>, nearest tile first: index 0 is
        /// the adjacent tile, index 1 the tile beyond it. Every value is authored explicitly — there
        /// is no falloff rule derived from <see cref="Damage"/>, because a line that deals 2 then 1
        /// is a pair of numbers and not a formula. Empty for every ability that is not a Line.
        /// </summary>
        public IReadOnlyList<int> TileDamage { get; init; } = NoTileDamage;

        /// <summary>Short effect line, e.g. "1 dmg · push 1".</summary>
        public string Effect
        {
            get
            {
                var parts = new List<string>();

                if (Targeting == AbilityTargeting.Line)
                {
                    parts.Add("line " + Range);
                }

                if (Targeting == AbilityTargeting.Self)
                {
                    parts.Add("stance");
                }

                if (TileDamage.Count > 0)
                {
                    parts.Add(string.Join("/", TileDamage) + " dmg");
                }

                if (Damage > 0)
                {
                    parts.Add(Damage + " dmg");
                }

                if (Push > 0)
                {
                    parts.Add("push " + Push);
                }

                if (PullsToAdjacent)
                {
                    parts.Add("pull to adjacent");
                }

                if (Targeting == AbilityTargeting.Passive)
                {
                    parts.Add("passive");
                }

                return parts.Count == 0 ? "—" : string.Join(" · ", parts);
            }
        }

        /// <summary>
        /// What this ability deals to one tile of its line, counted from the tile directly ahead of
        /// the user. Zero for any tile the line does not damage.
        /// </summary>
        /// <param name="index">Tile index along the line, 0 for the adjacent tile.</param>
        /// <returns>Hit points this ability delivers to that tile.</returns>
        public int DamageOnTile(int index) =>
            index >= 0 && index < TileDamage.Count ? TileDamage[index] : 0;

        /// <summary>
        /// The archetype's first ability, or <c>null</c> for enemies. An archetype may bring more than
        /// one — see <see cref="AllForKind"/>; this is the one a shell reaches for when it wants a
        /// single headline.
        /// </summary>
        /// <param name="kind">Archetype to look up.</param>
        /// <returns>Its first ability descriptor, or <c>null</c>.</returns>
        public static AbilityDescriptor? ForKind(UnitKind kind) =>
            ByKind.TryGetValue(kind, out var descriptors) && descriptors.Length > 0 ? descriptors[0] : null;

        /// <summary>Every ability an archetype brings, in the order it should be offered.</summary>
        /// <param name="kind">Archetype to look up.</param>
        /// <returns>Its abilities; empty for enemies.</returns>
        public static IReadOnlyList<AbilityDescriptor> AllForKind(UnitKind kind) =>
            ByKind.TryGetValue(kind, out var descriptors) ? descriptors : Empty;

        /// <summary>Looks up a descriptor by ability.</summary>
        /// <param name="ability">Ability to look up.</param>
        /// <returns>Its descriptor.</returns>
        public static AbilityDescriptor For(Ability ability)
        {
            foreach (var kind in PlayerOrder)
            {
                foreach (var descriptor in ByKind[kind])
                {
                    if (descriptor.Ability == ability)
                    {
                        return descriptor;
                    }
                }
            }

            throw new ArgumentOutOfRangeException(nameof(ability), ability, "No descriptor for ability.");
        }

        /// <summary>Every ability, in archetype order.</summary>
        /// <returns>All descriptors.</returns>
        public static IReadOnlyList<AbilityDescriptor> All()
        {
            var all = new List<AbilityDescriptor>();
            foreach (var kind in PlayerOrder)
            {
                all.AddRange(ByKind[kind]);
            }

            return all;
        }

        private static Dictionary<UnitKind, AbilityDescriptor[]> Build() =>
            new Dictionary<UnitKind, AbilityDescriptor[]>
            {
                [UnitKind.Vanguard] = new[]
                {
                    new AbilityDescriptor(
                        Ability.BullRush,
                        UnitKind.Vanguard,
                        "Bull Rush",
                        "Charge up to 3 tiles in a straight line. The first enemy you reach is pushed 2 and you stop adjacent to it.",
                        AbilityTargeting.Direction,
                        3, 0, 2, false),
                },

                [UnitKind.Archer] = new[]
                {
                    new AbilityDescriptor(
                        Ability.StaggerShot,
                        UnitKind.Archer,
                        "Stagger Shot",
                        "Range 3. Deals 1 damage and pushes the target 1 tile directly away from you.",
                        AbilityTargeting.Enemy,
                        3, 1, 1, false),
                },

                [UnitKind.Threadcaster] = new[]
                {
                    new AbilityDescriptor(
                        Ability.Reel,
                        UnitKind.Threadcaster,
                        "Reel",
                        "Range 3. Pulls one enemy all the way in until it is adjacent to you, resolving every tile on the way.",
                        AbilityTargeting.Enemy,
                        3, 0, 0, true),
                },

                [UnitKind.Wardbearer] = new[]
                {
                    new AbilityDescriptor(
                        Ability.SpearThrust,
                        UnitKind.Wardbearer,
                        "Spear Thrust",
                        "The two tiles directly ahead. An enemy in the adjacent tile takes 2 damage, "
                        + "one in the tile beyond it takes 1. Nothing is displaced.",
                        AbilityTargeting.Line,
                        2, 0, 0, false)
                    {
                        TileDamage = new[] { 2, 1 },
                    },

                    new AbilityDescriptor(
                        Ability.GuardStance,
                        UnitKind.Wardbearer,
                        "Guard Stance",
                        "Costs the action only. Until your next activation, damage and displacement aimed at "
                        + "adjacent allies land on you instead — same damage, same direction, from your tile — "
                        + "and attack damage you take is halved, rounded up, minimum 1. Impact damage is never reduced.",
                        AbilityTargeting.Self,
                        1, 0, 0, false),
                },
            };
    }
}
