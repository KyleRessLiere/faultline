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
    /// <param name="Range">Reach in orthogonal steps; for Bull Rush, how far it charges.</param>
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
        private static readonly Dictionary<UnitKind, AbilityDescriptor> ByKind = Build();

        /// <summary>Short effect line, e.g. "1 dmg · push 1".</summary>
        public string Effect
        {
            get
            {
                var parts = new List<string>();
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

        /// <summary>The ability belonging to an archetype, or <c>null</c> for enemies.</summary>
        /// <param name="kind">Archetype to look up.</param>
        /// <returns>Its ability descriptor, or <c>null</c>.</returns>
        public static AbilityDescriptor? ForKind(UnitKind kind) =>
            ByKind.TryGetValue(kind, out var descriptor) ? descriptor : null;

        /// <summary>Looks up a descriptor by ability.</summary>
        /// <param name="ability">Ability to look up.</param>
        /// <returns>Its descriptor.</returns>
        public static AbilityDescriptor For(Ability ability)
        {
            foreach (var descriptor in ByKind.Values)
            {
                if (descriptor.Ability == ability)
                {
                    return descriptor;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(ability), ability, "No descriptor for ability.");
        }

        /// <summary>Every ability, in archetype order.</summary>
        /// <returns>All descriptors.</returns>
        public static IReadOnlyList<AbilityDescriptor> All()
        {
            var all = new List<AbilityDescriptor>(ByKind.Count);
            foreach (var kind in new[] { UnitKind.Vanguard, UnitKind.Archer, UnitKind.Threadcaster, UnitKind.Wardbearer })
            {
                all.Add(ByKind[kind]);
            }

            return all;
        }

        private static Dictionary<UnitKind, AbilityDescriptor> Build() =>
            new Dictionary<UnitKind, AbilityDescriptor>
            {
                [UnitKind.Vanguard] = new AbilityDescriptor(
                    Ability.BullRush,
                    UnitKind.Vanguard,
                    "Bull Rush",
                    "Charge up to 3 tiles in a straight line. The first enemy you reach is pushed 2 and you stop adjacent to it.",
                    AbilityTargeting.Direction,
                    3, 0, 2, false),

                [UnitKind.Archer] = new AbilityDescriptor(
                    Ability.StaggerShot,
                    UnitKind.Archer,
                    "Stagger Shot",
                    "Range 3. Deals 1 damage and pushes the target 1 tile directly away from you.",
                    AbilityTargeting.Enemy,
                    3, 1, 1, false),

                [UnitKind.Threadcaster] = new AbilityDescriptor(
                    Ability.Reel,
                    UnitKind.Threadcaster,
                    "Reel",
                    "Range 3. Pulls one enemy all the way in until it is adjacent to you, resolving every tile on the way.",
                    AbilityTargeting.Enemy,
                    3, 0, 0, true),

                [UnitKind.Wardbearer] = new AbilityDescriptor(
                    Ability.Hold,
                    UnitKind.Wardbearer,
                    "Hold",
                    "Always on. Allies standing next to you cannot be displaced more than 1 tile.",
                    AbilityTargeting.Passive,
                    1, 0, 0, false),
            };
    }
}
