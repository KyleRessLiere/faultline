using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// One ability, whole: what it is called, what it reads as, what the player must pick, what it
    /// costs, and what it does once picked. Kept in Core so the shell never writes its own rules text
    /// and never maintains its own gameplay lists.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Targeting is separated from resolution</b> (component review, "Player abilities"). Before
    /// this, the targeting shape silently chose the resolver: every <c>Self</c> ability was Guard
    /// Stance, every <c>Line</c> was Spear Thrust, every <c>Enemy</c> was the one damage-push-pull
    /// sequence. Now <see cref="Targeting"/> answers only "what does the player select"; what happens
    /// afterwards is <see cref="Effects"/>, or a named <see cref="CustomRule"/> for the handful of
    /// abilities that are algorithms rather than lists.
    /// </para>
    /// <para>
    /// <b>The effect list is the single source of the numbers.</b> <see cref="Damage"/>,
    /// <see cref="Push"/> and <see cref="PullsToAdjacent"/> are projections of it, not separately
    /// authored fields — the review's blacklist names "multiple competing sources for names, rules
    /// text, and numbers" as a way this refactor could fail. Bull Rush keeps a custom rule and still
    /// authors its shove as a <see cref="PushEffect"/>, so its custom handler reads the same 2 that
    /// the preview and the UI read.
    /// </para>
    /// <para>
    /// <b>Cost has no default.</b> It is a required positional parameter precisely so that an ability
    /// added without one fails to compile rather than silently inheriting
    /// <see cref="Activation.ActionCost"/> — which is what the old <c>Activation.CostOf</c> switch
    /// did, and is the failure mode the review calls out by name.
    /// </para>
    /// </remarks>
    /// <param name="Ability">Which ability.</param>
    /// <param name="Kind">Archetype that has it.</param>
    /// <param name="Name">Display name.</param>
    /// <param name="Summary">One-line rules text.</param>
    /// <param name="Targeting">What the player must pick — and only that.</param>
    /// <param name="Cost">
    /// Action points it takes out of the pool. Explicit for every ability; there is no default.
    /// </param>
    /// <param name="Range">
    /// Reach in orthogonal steps; for Bull Rush, how far it charges; for a Line ability, how many
    /// tiles ahead the line covers.
    /// </param>
    /// <param name="MinRange">
    /// Closest tile it can be aimed at. Zero for everything without a stated minimum; the Archer's
    /// two abilities share her bow's, because a rule the basic shot obeys and the ability does not
    /// is not a rule (D-099).
    /// </param>
    public sealed record AbilityDefinition(
        Ability Ability,
        UnitKind Kind,
        string Name,
        string Summary,
        AbilityTargeting Targeting,
        int Cost,
        int Range,
        int MinRange = 0)
    {
        private static readonly AbilityDefinition[] Empty = new AbilityDefinition[0];

        private static readonly int[] NoTileDamage = new int[0];

        private static readonly Dictionary<UnitKind, AbilityDefinition[]> ByKind = Build();

        private static readonly UnitKind[] PlayerOrder =
        {
            UnitKind.Vanguard, UnitKind.Archer, UnitKind.Threadcaster, UnitKind.Wardbearer,
        };

        /// <summary>
        /// What the ability does once its target is chosen, in the order it does it. Empty for an
        /// ability that is wholly a <see cref="CustomRule"/>.
        /// </summary>
        public IReadOnlyList<AbilityEffect> Effects { get; init; } = NoEffects.List;

        /// <summary>
        /// The named algorithm that resolves this ability, or <see cref="AbilityRule.None"/> when
        /// <see cref="Effects"/> is the whole of it.
        /// </summary>
        public AbilityRule CustomRule { get; init; } = AbilityRule.None;

        /// <summary>
        /// Damage per tile of a <see cref="AbilityTargeting.Line"/>, nearest tile first: index 0 is
        /// the adjacent tile, index 1 the tile beyond it. Every value is authored explicitly — there
        /// is no falloff rule derived from <see cref="Damage"/>, because a line that deals 2 then 4
        /// is a pair of numbers and not a formula. Empty for every ability that is not a Line.
        /// </summary>
        public IReadOnlyList<int> TileDamage { get; init; } = NoTileDamage;

        /// <summary>
        /// Direct damage to the chosen target, before any collision or hazard the displacement
        /// causes. Read off <see cref="Effects"/>; zero when nothing in the list deals damage.
        /// </summary>
        public int Damage
        {
            get
            {
                foreach (var effect in Effects)
                {
                    if (effect is DamageEffect damage && effect.Subject == EffectSubject.Target)
                    {
                        return damage.Amount;
                    }
                }

                return 0;
            }
        }

        /// <summary>
        /// Push distance applied to the target, zero when it does not push. Read off
        /// <see cref="Effects"/>.
        /// </summary>
        public int Push
        {
            get
            {
                foreach (var effect in Effects)
                {
                    if (effect is PushEffect push && effect.Subject == EffectSubject.Target)
                    {
                        return push.Distance;
                    }
                }

                return 0;
            }
        }

        /// <summary>True when it hauls the target all the way in. Read off <see cref="Effects"/>.</summary>
        public bool PullsToAdjacent
        {
            get
            {
                foreach (var effect in Effects)
                {
                    if (effect is PullEffect pull && pull.ToAdjacent)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

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
        /// <returns>Its first ability definition, or <c>null</c>.</returns>
        public static AbilityDefinition? ForKind(UnitKind kind) =>
            ByKind.TryGetValue(kind, out var definitions) && definitions.Length > 0 ? definitions[0] : null;

        /// <summary>Every ability an archetype brings, in the order it should be offered.</summary>
        /// <param name="kind">Archetype to look up.</param>
        /// <returns>Its abilities; empty for enemies.</returns>
        public static IReadOnlyList<AbilityDefinition> AllForKind(UnitKind kind) =>
            ByKind.TryGetValue(kind, out var definitions) ? definitions : Empty;

        /// <summary>Looks up a definition by ability.</summary>
        /// <param name="ability">Ability to look up.</param>
        /// <returns>Its definition.</returns>
        public static AbilityDefinition For(Ability ability)
        {
            foreach (var kind in PlayerOrder)
            {
                foreach (var definition in ByKind[kind])
                {
                    if (definition.Ability == ability)
                    {
                        return definition;
                    }
                }
            }

            throw new ArgumentOutOfRangeException(nameof(ability), ability, "No definition for ability.");
        }

        /// <summary>
        /// Every ability, in archetype order. This is the registry coverage tests enumerate — they
        /// walk this rather than a hand-maintained list, so a new ability is covered the moment it is
        /// registered (component review, "Validation requirements").
        /// </summary>
        /// <returns>All definitions.</returns>
        public static IReadOnlyList<AbilityDefinition> All()
        {
            var all = new List<AbilityDefinition>();
            foreach (var kind in PlayerOrder)
            {
                all.AddRange(ByKind[kind]);
            }

            return all;
        }

        private static Dictionary<UnitKind, AbilityDefinition[]> Build() =>
            new Dictionary<UnitKind, AbilityDefinition[]>
            {
                [UnitKind.Vanguard] = new[]
                {
                    // Custom: the charge is path traversal, terrain, self-damage, stopping rules and
                    // contact timing — an algorithm, not a list (review, "Custom abilities remain
                    // valid"). Its shove is still authored as a standard effect, and the custom
                    // handler applies that list to whatever it makes contact with.
                    new AbilityDefinition(
                        Ability.BullRush,
                        UnitKind.Vanguard,
                        "Bull Rush",
                        "Charge up to 3 tiles in a straight line. The first enemy you reach is pushed 2 and you stop adjacent to it.",
                        AbilityTargeting.Direction,
                        Cost: Activation.BullRushCost,
                        Range: 3)
                    {
                        CustomRule = AbilityRule.Charge,
                        Effects = new AbilityEffect[] { new PushEffect(2) },
                    },
                },

                [UnitKind.Archer] = new[]
                {
                    new AbilityDefinition(
                        Ability.StaggerShot,
                        UnitKind.Archer,
                        "Stagger Shot",
                        "Range 3. Deals 2 damage and pushes the target 1 tile directly away from you.",
                        AbilityTargeting.Enemy,
                        Cost: Activation.ActionCost,
                        Range: 3,
                        MinRange: 2)
                    {
                        Effects = new AbilityEffect[]
                        {
                            new DamageEffect(2),
                            new PushEffect(1),
                        },
                    },
                },

                [UnitKind.Threadcaster] = new[]
                {
                    new AbilityDefinition(
                        Ability.Reel,
                        UnitKind.Threadcaster,
                        "Reel",
                        "Range 4. Pulls one enemy all the way in until it is adjacent to you, resolving every tile on the way. "
                        + "Nothing between you and it is consulted — the line flies over rock and body alike.",
                        AbilityTargeting.Enemy,
                        Cost: Activation.ReelCost,
                        Range: 4)
                    {
                        // D-139 extended push resistance to Pull, and "pull all the way to adjacent"
                        // cannot survive being shortened — an Anchor would arrive one tile out and the
                        // ability would no longer do what it says. The carve-out is an explicit flag
                        // on the effect rather than a special case in a resolver.
                        Effects = new AbilityEffect[]
                        {
                            new PullEffect(0) { ToAdjacent = true, BypassResistance = true },
                        },
                    },
                },

                [UnitKind.Wardbearer] = new[]
                {
                    // Custom: a Line is a shape with per-tile damage, and the tiles are not a target
                    // the effect vocabulary can name yet. Damage only — it displaces nothing (D-068).
                    new AbilityDefinition(
                        Ability.SpearThrust,
                        UnitKind.Wardbearer,
                        "Spear Thrust",
                        "The two tiles directly ahead. An enemy in the adjacent tile takes 2 damage; "
                        + "one in the tile beyond takes 4 — the tip is the sweet spot. Nothing is displaced.",
                        AbilityTargeting.Line,
                        Cost: Activation.ActionCost,
                        Range: 2)
                    {
                        CustomRule = AbilityRule.Line,
                        TileDamage = new[] { 2, 4 },
                    },

                    // Custom on purpose (review, implementation order step 5). The stance is not only
                    // a flag: it arms interception, halves attack damage, and its unabsorbed expiry is
                    // a question about this stance in particular.
                    new AbilityDefinition(
                        Ability.GuardStance,
                        UnitKind.Wardbearer,
                        "Guard Stance",
                        "Costs the action only. Until your next activation, damage and displacement aimed at "
                        + "adjacent allies land on you instead — same damage, same direction, from your tile — "
                        + "and attack damage you take is halved, rounded up, minimum 1. Impact damage is never reduced.",
                        AbilityTargeting.Self,
                        Cost: Activation.ActionCost,
                        Range: 1)
                    {
                        CustomRule = AbilityRule.GuardStance,
                    },
                },
            };
    }
}
