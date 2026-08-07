using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>Which side drives a unit's decisions.</summary>
    public enum UnitControl
    {
        /// <summary>A player class: it has abilities and no priority list.</summary>
        Player = 0,

        /// <summary>An enemy archetype: it has a priority list and no abilities.</summary>
        Enemy = 1,
    }

    /// <summary>
    /// One archetype, whole: who drives it, what it is called, what it looks like, what its numbers
    /// are, what it can do, what happens to it at each lifecycle moment, and — for an enemy — which
    /// priority list plans for it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>It composes; it does not copy.</b> Statistics stay in <see cref="UnitTemplate"/>, abilities
    /// in <see cref="AbilityDefinition"/>, the priority list in <see cref="EnemyPlanDefinition"/> and
    /// the bestiary prose in <see cref="EnemyBehaviour"/>. This is the one place that knows all four
    /// exist for a given archetype, which is what lets a coverage test walk every unit and fail on the
    /// one whose registration is half-done (component review, "Validation requirements"). The review's
    /// blacklist names "multiple competing sources for names, rules text, and numbers"; every
    /// projection below is a read, never a second copy.
    /// </para>
    /// <para>
    /// <b>Presentation lives here.</b> <see cref="Name"/> goes through <see cref="Naming"/> so a
    /// display name and its identifier can differ (MASTER_DESIGN §15), and <see cref="Glyph"/> is
    /// authored for <em>every</em> archetype — a shell that keeps its own glyph table falls through to
    /// a placeholder the moment an archetype is added, which is exactly what happened to the Raider,
    /// the Quarry King and the Braced Husk.
    /// </para>
    /// </remarks>
    /// <param name="Kind">Which archetype.</param>
    /// <param name="Control">Player class or enemy archetype.</param>
    /// <param name="Glyph">
    /// Short stable board identifier, two characters. Never empty and never a placeholder — a
    /// coverage test says so.
    /// </param>
    public sealed record UnitDefinition(UnitKind Kind, UnitControl Control, string Glyph)
    {
        private static readonly Dictionary<UnitKind, UnitDefinition> ByKind = Build();

        private static readonly UnitKind[] Order = BuildOrder();

        /// <summary>What a player reads, through the naming layer.</summary>
        public string Name => Naming.Of(Kind);

        /// <summary>One line: what this archetype is for.</summary>
        public string Role => EnemyBehaviour.RoleFor(Kind);

        /// <summary>The stat block: hit points, movement, reach, damage, resistances, flags.</summary>
        public UnitTemplate Stats => UnitTemplate.For(Kind);

        /// <summary>The basic action on one line, e.g. "melee · 2 dmg".</summary>
        public string BasicAction => EnemyBehaviour.Describe(Stats);

        /// <summary>How it handles high ground in words, or <c>null</c> when it handles it the ordinary way.</summary>
        public string? Climb => EnemyBehaviour.DescribeClimb(Stats);

        /// <summary>Every ability it brings, in the order they should be offered. Empty for an enemy.</summary>
        public IReadOnlyList<AbilityDefinition> Abilities => AbilityDefinition.AllForKind(Kind);

        /// <summary>The priority list that plans for it, or <c>null</c> for a player class.</summary>
        public EnemyPlanDefinition? Plan => EnemyPlanDefinition.ForPlan(Stats.Plan);

        /// <summary>Its bestiary entry, or <c>null</c> for a player class.</summary>
        public EnemyBehaviour? Behaviour => EnemyBehaviour.ForKind(Kind);

        /// <summary>What happens to it at each lifecycle moment.</summary>
        public UnitLifecycle Lifecycle { get; init; } = UnitLifecycle.None;

        /// <summary>True when the priority list has no clause about player units at all (D-041).</summary>
        public bool IgnoresPlayerUnits => Plan?.IgnoresPlayerUnits == true;

        /// <summary>Every archetype, player classes first and then the bestiary's own order.</summary>
        public static IReadOnlyList<UnitKind> Kinds => Order;

        /// <summary>The definition for an archetype.</summary>
        /// <param name="kind">Archetype to look up.</param>
        /// <returns>Its definition.</returns>
        public static UnitDefinition For(UnitKind kind) =>
            ByKind.TryGetValue(kind, out var definition)
                ? definition
                : throw new ArgumentOutOfRangeException(nameof(kind), kind, "No definition for unit kind.");

        /// <summary>
        /// Every unit, in roster order. This is the registry coverage tests enumerate — they walk
        /// this rather than a hand-maintained list, so an archetype is covered the moment it is
        /// registered (component review, "Validation requirements").
        /// </summary>
        /// <returns>All definitions.</returns>
        public static IReadOnlyList<UnitDefinition> All()
        {
            var all = new List<UnitDefinition>(Order.Length);
            foreach (var kind in Order)
            {
                all.Add(ByKind[kind]);
            }

            return all;
        }

        private static UnitKind[] BuildOrder()
        {
            var order = new List<UnitKind>();
            foreach (var definition in AbilityDefinition.All())
            {
                if (!order.Contains(definition.Kind))
                {
                    order.Add(definition.Kind);
                }
            }

            foreach (var kind in EnemyBehaviour.EnemyKinds)
            {
                order.Add(kind);
            }

            return order.ToArray();
        }

        // Two characters per archetype, authored for all of them. Upper case for the heavies and the
        // player classes, lower for the rest; a variant borrows its archetype's letters and marks
        // itself, so a board reads as a family at a glance.
        private static string GlyphFor(UnitKind kind) => kind switch
        {
            UnitKind.Vanguard => "VG",
            UnitKind.Archer => "AR",
            UnitKind.Threadcaster => "TC",
            UnitKind.Wardbearer => "WB",
            UnitKind.Husk => "hu",
            UnitKind.Lobber => "lo",
            UnitKind.Anchor => "an",
            UnitKind.Grappler => "gr",
            UnitKind.Stalker => "st",
            UnitKind.Warden => "wd",
            UnitKind.Perch => "pe",
            UnitKind.Bulwark => "bw",
            UnitKind.Harrier => "ha",
            UnitKind.Runt => "ru",
            UnitKind.Colossus => "CO",
            UnitKind.LesserGrappler => "g-",
            UnitKind.BluntedStalker => "s-",
            UnitKind.HeavyHusk => "h+",
            UnitKind.MobileAnchor => "a+",
            UnitKind.BracedHusk => "h=",
            UnitKind.Raider => "ra",
            UnitKind.QuarryKing => "QK",
            UnitKind.Rushmaster => "RM",
            UnitKind.EscortDuckling => "du",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "No glyph for unit kind."),
        };

        // Authored lifecycle, for the one mechanism that is nobody else's. Everything else is derived
        // below from the stat block that already holds it, so there is one source for each.
        private static UnitLifecycleEffect[] AuthoredLifecycle(UnitKind kind, UnitTemplate template)
        {
            if (kind != UnitKind.QuarryKing)
            {
                return Array.Empty<UnitLifecycleEffect>();
            }

            return new[]
            {
                new UnitLifecycleEffect(
                    UnitLifecycleMoment.OnFightStart,
                    $"He starts the fight behind his shell: {template.Footing} refusals, the boss's "
                    + "whole anti-displacement budget and his own mechanism. It is not granted by an "
                    + "effect — the stat block carries it — and it does not negate: each refusal turns "
                    + "one displacement instance aside and is spent doing it, like everybody else's "
                    + "since D-143. A collision he suffers strips one, and so does ending a round "
                    + "beside a drain.")
                {
                    CustomRule = UnitRule.QuarryKingShell,
                },
            };
        }

        private static UnitLifecycle LifecycleFor(UnitKind kind, UnitControl control)
        {
            var template = UnitTemplate.For(kind);
            var entries = new List<UnitLifecycleEffect>(AuthoredLifecycle(kind, template));
            bool authoredStart = entries.Count > 0;

            if (template.Footing > 0)
            {
                if (!authoredStart)
                {
                    entries.Add(new UnitLifecycleEffect(
                        UnitLifecycleMoment.OnFightStart,
                        $"Starts the fight holding {template.Footing} Footing off its stat block — "
                        + "whole refusals, not a shortening. Player classes print zero and are granted "
                        + "theirs per scenario instead.")
                    {
                        CustomRule = UnitRule.StatBlockFooting,
                    });
                }

                entries.Add(new UnitLifecycleEffect(
                    UnitLifecycleMoment.OnRoundEnd,
                    "Ending a round orthogonally beside a drain strips one Footing, whether or not "
                    + "anybody touched it.")
                {
                    CustomRule = UnitRule.LipStrip,
                });
            }

            if (control == UnitControl.Enemy
                && template.Attack != AttackKind.None
                && template.Damage > 0)
            {
                entries.Add(new UnitLifecycleEffect(
                    UnitLifecycleMoment.OnActivationEnd,
                    $"Finishing its activation adjacent to a standing Protect structure claws it for "
                    + $"{template.Damage}, whoever else it swung at (D-034).")
                {
                    CustomRule = UnitRule.SiegeClaw,
                });
            }

            if (template.Enraged is not null)
            {
                entries.Add(new UnitLifecycleEffect(
                    UnitLifecycleMoment.OnHpThreshold,
                    $"At {template.EnrageAt} hit points or below the second stat block takes over "
                    + "whole — a substitution rather than a pile of exceptions — and the intent is "
                    + "re-declared on the spot so the telegraph never lies about which one you are "
                    + "facing (D-040).")
                {
                    CustomRule = UnitRule.PhaseSwap,
                    Threshold = template.EnrageAt,
                });
            }

            return entries.Count == 0
                ? UnitLifecycle.None
                : new UnitLifecycle { Entries = entries };
        }

        private static Dictionary<UnitKind, UnitDefinition> Build()
        {
            var players = new HashSet<UnitKind>();
            foreach (var definition in AbilityDefinition.All())
            {
                players.Add(definition.Kind);
            }

            var table = new Dictionary<UnitKind, UnitDefinition>();
            foreach (UnitKind kind in Enum.GetValues(typeof(UnitKind)))
            {
                var control = players.Contains(kind) ? UnitControl.Player : UnitControl.Enemy;
                table[kind] = new UnitDefinition(kind, control, GlyphFor(kind))
                {
                    Lifecycle = LifecycleFor(kind, control),
                };
            }

            return table;
        }
    }
}
