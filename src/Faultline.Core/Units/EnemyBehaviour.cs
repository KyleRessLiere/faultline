using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// One numbered step of an enemy's priority list, as the planner actually runs it.
    /// </summary>
    /// <param name="Order">1-based position in the list. Lower runs first.</param>
    /// <param name="Label">Short imperative headline, e.g. "Shoot from where it stands".</param>
    /// <param name="Detail">The full condition and consequence, in rules terms.</param>
    public sealed record BehaviourStep(int Order, string Label, string Detail);

    /// <summary>
    /// Everything a UI needs to explain an enemy: its role, the ordered priority list
    /// <see cref="Ai"/> runs, the quirks that surprise a player, and how to beat it. The same job
    /// <see cref="AbilityDescriptor"/> does for player abilities, so the shell never writes its own
    /// rules text.
    /// </summary>
    /// <remarks>
    /// Numbers are never retyped here: every figure in the prose is interpolated from
    /// <see cref="UnitTemplate"/> at construction, so a stat change moves the text with it. Adding a
    /// new enemy is adding one entry to <see cref="Build"/> — no UI file changes, and
    /// <c>EnemyBehaviourTests</c> fails until the entry exists.
    /// </remarks>
    /// <param name="Kind">Archetype this describes.</param>
    /// <param name="Role">One line: what this thing is for.</param>
    /// <param name="Summary">One line: what it does on the board.</param>
    /// <param name="Priorities">The priority list, in the order the planner evaluates it.</param>
    /// <param name="Quirks">Behaviour that is easy to get wrong, one line each.</param>
    /// <param name="Counterplay">How a player beats it, one line each.</param>
    public sealed record EnemyBehaviour(
        UnitKind Kind,
        string Role,
        string Summary,
        IReadOnlyList<BehaviourStep> Priorities,
        IReadOnlyList<string> Quirks,
        IReadOnlyList<string> Counterplay)
    {
        private static readonly UnitKind[] Order =
        {
            UnitKind.Husk, UnitKind.Lobber, UnitKind.Anchor, UnitKind.Grappler, UnitKind.Stalker,
        };

        private static readonly Dictionary<UnitKind, EnemyBehaviour> ByKind = Build();

        /// <summary>Every enemy archetype, in the order the bestiary lists them.</summary>
        public static IReadOnlyList<UnitKind> EnemyKinds => Order;

        /// <summary>The stat block this behaviour reads its numbers from.</summary>
        public UnitTemplate Template => UnitTemplate.For(Kind);

        /// <summary>Display name, from the stat block.</summary>
        public string Name => Template.Name;

        /// <summary>True when the archetype has a basic attack at all.</summary>
        public bool HasBasicAttack => Template.Attack != AttackKind.None;

        /// <summary>
        /// The basic action on one line, e.g. "melee · 2 dmg" or "no attack · range 3 · pull 2".
        /// </summary>
        public string AttackLine => Describe(Template);

        /// <summary>The behaviour for an archetype, or <c>null</c> when it is not an enemy.</summary>
        /// <param name="kind">Archetype to look up.</param>
        /// <returns>Its behaviour, or <c>null</c>.</returns>
        public static EnemyBehaviour? ForKind(UnitKind kind) =>
            ByKind.TryGetValue(kind, out var behaviour) ? behaviour : null;

        /// <summary>The behaviour for an archetype that must have one.</summary>
        /// <param name="kind">Enemy archetype to look up.</param>
        /// <returns>Its behaviour.</returns>
        public static EnemyBehaviour For(UnitKind kind) =>
            ForKind(kind) ?? throw new ArgumentOutOfRangeException(
                nameof(kind), kind, "No enemy behaviour for unit kind.");

        /// <summary>Every enemy behaviour, in bestiary order.</summary>
        /// <returns>All behaviours.</returns>
        public static IReadOnlyList<EnemyBehaviour> All()
        {
            var all = new List<EnemyBehaviour>(Order.Length);
            foreach (var kind in Order)
            {
                all.Add(ByKind[kind]);
            }

            return all;
        }

        /// <summary>
        /// The one-line role of any archetype, player class or enemy. Player classes carry their
        /// rules on <see cref="AbilityDescriptor"/>; this is the one word of framing they lack, and
        /// it belongs in Core with the rest of the words.
        /// </summary>
        /// <param name="kind">Archetype to describe.</param>
        /// <returns>Its role.</returns>
        public static string RoleFor(UnitKind kind)
        {
            var behaviour = ForKind(kind);
            if (behaviour is not null)
            {
                return behaviour.Role;
            }

            return kind switch
            {
                UnitKind.Vanguard => "front line — closes and shoves",
                UnitKind.Archer => "ranged damage, free climber",
                UnitKind.Threadcaster => "hook — pulls the enemy line apart",
                UnitKind.Wardbearer => "walking anchor for the units beside it",
                _ => "unclassified",
            };
        }

        /// <summary>The basic action of any archetype on one line.</summary>
        /// <param name="template">Stat block to describe.</param>
        /// <returns>Its basic action, e.g. "melee · 1 dmg · push 1".</returns>
        public static string Describe(UnitTemplate template)
        {
            if (template is null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            if (template.Attack == AttackKind.None)
            {
                if (template.CanPullWithBasic)
                {
                    return $"no attack · range {template.Range} · pull {template.BasicPull}";
                }

                return template.CanPushWithBasic
                    ? $"no attack · melee · push {template.BasicPush}"
                    : "none";
            }

            string reach = template.Attack == AttackKind.Melee ? "melee" : $"range {template.Range}";
            string text = $"{reach} · {template.Damage} dmg";

            if (template.AttackPush > 0)
            {
                text += $" · push {template.AttackPush}";
            }

            if (template.CanPullWithBasic)
            {
                text += $" · may pull {template.BasicPull} instead";
            }

            return text;
        }

        private static BehaviourStep[] Steps(params (string Label, string Detail)[] steps)
        {
            var list = new BehaviourStep[steps.Length];
            for (int i = 0; i < steps.Length; i++)
            {
                list[i] = new BehaviourStep(i + 1, steps[i].Label, steps[i].Detail);
            }

            return list;
        }

        private static Dictionary<UnitKind, EnemyBehaviour> Build()
        {
            var husk = UnitTemplate.For(UnitKind.Husk);
            var lobber = UnitTemplate.For(UnitKind.Lobber);
            var anchor = UnitTemplate.For(UnitKind.Anchor);
            var grappler = UnitTemplate.For(UnitKind.Grappler);
            var stalker = UnitTemplate.For(UnitKind.Stalker);
            var archer = UnitTemplate.For(UnitKind.Archer);
            var vanguard = UnitTemplate.For(UnitKind.Vanguard);

            var table = new Dictionary<UnitKind, EnemyBehaviour>(5);

            table[UnitKind.Husk] = new EnemyBehaviour(
                UnitKind.Husk,
                "chaff",
                $"Walks at you and hits you for {husk.Damage}. There is nothing else to it.",
                Steps(
                    ("Hit what is already in reach",
                     $"A player unit adjacent when the Husk activates is attacked for {husk.Damage} — "
                     + "without moving first. Priority 1 is literal: it does not reposition to a better tile."),
                    ("Otherwise close on the nearest",
                     $"Spends up to Move {husk.Move} walking toward the nearest player unit, and if the "
                     + "walk ends adjacent it attacks in the same activation (D-022). An activation is "
                     + "Move plus one Action, so chasing does not cost it the swing.")),
                new[]
                {
                    $"{husk.MaxHp} HP is the whole point: a Husk exists to eat one attack and be a body in the way.",
                    "\"Nearest\" ties break on lowest unit id, then row-major coordinate order — never randomly. "
                    + "Two runs of the same board make the same choice.",
                    "Routes prefer crossing fewer spike tiles, then the shorter walk. A Husk already standing "
                    + "where it wants to be does not shuffle.",
                },
                new[]
                {
                    $"{husk.MaxHp} HP dies to one Archer shot ({archer.Damage} damage), and to a Vanguard swing "
                    + $"({vanguard.Damage}) plus the push into anything solid.",
                    "It only ever attacks what is adjacent, so a step back is a whole round of its output denied — "
                    + "and it is the cheapest thing on the board to bait into a pit.",
                });

            table[UnitKind.Lobber] = new EnemyBehaviour(
                UnitKind.Lobber,
                "kiting artillery",
                $"Shoots you from {lobber.Range} tiles and runs the moment you close.",
                Steps(
                    ("Shoot from where it stands",
                     $"With no player unit adjacent and one within range {lobber.Range}, it shoots the nearest "
                     + $"for {lobber.Damage} and does not move. Fired from HighGround the shot is "
                     + $"{lobber.Damage + 1} instead — the +1 ranged bonus is not a player-only rule."),
                    ("Break contact before anything else",
                     "If any player unit is adjacent, it retreats to the reachable tile that maximises the "
                     + "distance to the nearest player unit, ties broken by the greater total distance to all "
                     + "of them. If that retreat actually breaks contact and something is still in range, it "
                     + "shoots after moving; if it cannot break contact, the whole activation is the retreat."),
                    ("Otherwise advance to a band, not to contact",
                     $"Walks toward the nearest player unit aiming for 2–{lobber.Range} tiles away — inside "
                     + $"range {lobber.Range}, outside melee (D-023) — preferring the further tile on a tie, "
                     + "then shoots if it arrives in range and out of contact.")),
                new[]
                {
                    $"{lobber.MaxHp} HP at Move {lobber.Move}, the second-slowest unit in the game. Its whole "
                    + "plan is to keep away from you and it is not fast enough to do it.",
                    "The adjacency check runs against every player unit, not just the one it declared against: "
                    + "a second unit stepping in also stops the shot.",
                    $"On HighGround it hits for {lobber.Damage + 1}, so a Lobber that climbs is a different "
                    + "threat from a Lobber on the flat.",
                },
                new[]
                {
                    $"Stand next to it and stay there. Adjacent, it never shoots — only runs — and at Move "
                    + $"{lobber.Move} it usually cannot get clear of a Move {archer.Move} player unit.",
                    "Two units pinning it from opposite sides remove it from the fight without killing it: "
                    + "every retreat tile it can reach is still in contact.",
                });

            table[UnitKind.Anchor] = new EnemyBehaviour(
                UnitKind.Anchor,
                "immovable screen",
                $"The hardest single hit on the enemy side, bolted to the floor at Move {anchor.Move}.",
                Steps(
                    ("Hit what is adjacent",
                     $"A player unit adjacent when it activates is attacked for {anchor.Damage} — the biggest "
                     + "basic attack in the game, player or enemy — and the Anchor does not move to do it."),
                    ("Otherwise take its one step",
                     $"Move {anchor.Move}: it closes exactly one tile toward the nearest player unit, and "
                     + "attacks if that single step lands it adjacent.")),
                new[]
                {
                    "It shrugs one tile off every Push (D-018). Push 1 moves it nowhere; Push 2 moves it 1; "
                    + "a Staggered Push 1 — normally an effective Push 2 — still only moves it 1.",
                    "Pull is unaffected. Reel and the Grappler's own pull drag an Anchor at full distance; the "
                    + "shrug is a Push rule, not a weight rule.",
                    $"{anchor.MaxHp} HP and Move {anchor.Move}: the most durable and the slowest thing on the board.",
                },
                new[]
                {
                    $"Walk around it. At Move {anchor.Move} it cannot chase anyone, so an Anchor away from what "
                    + "you care about is a statue with a scary damage number.",
                    "If it has to move, pull it — Reel ignores the shrug entirely, and dragging it out of the "
                    + "doorway costs one action instead of a whole fight.",
                });

            table[UnitKind.Grappler] = new EnemyBehaviour(
                UnitKind.Grappler,
                "abductor",
                $"Deals no damage at all. It drags you {grappler.BasicPull} tiles and lets the board do the rest.",
                Steps(
                    ("Yank someone out of position",
                     $"A player unit 2–{grappler.Range} tiles away is pulled {grappler.BasicPull} tiles toward "
                     + "the Grappler. It picks, in order: a unit standing on HighGround, then the Archer, then "
                     + "lowest unit id."),
                    ("Otherwise close to pulling range",
                     $"Spends up to Move {grappler.Move} walking toward the Archer — or the nearest player unit "
                     + $"when no Archer is on the board — aiming for the 2–{grappler.Range} band rather than "
                     + "contact (D-023), and pulls immediately if it arrives in range.")),
                new[]
                {
                    $"It has no attack at all ({grappler.Damage} damage). Every point of damage a Grappler causes "
                    + "is dealt by whatever the pull runs you into: a wall, another unit, spikes, a pit.",
                    $"A target already adjacent cannot be pulled — the next tile toward the Grappler is the "
                    + $"Grappler (D-020). What the code then does is not \"nothing\": it steps back to "
                    + $"{grappler.Range} tiles and yanks you {grappler.BasicPull} straight back in, because the "
                    + "band it advances to is 2–3. Standing in its face buys you a shuffle, not a stop.",
                    "The pull resolves every tile on the way, so it will drag a unit through spikes or over the "
                    + "lip of a pit rather than merely next to one.",
                    "It hunts the Archer by name. With an Archer on the board, a Grappler that cannot pull "
                    + "anything walks toward the Archer regardless of who is closer.",
                },
                new[]
                {
                    "Watch the tile behind you, not the Grappler. Its threat is entirely the geometry between "
                    + "you and it — pits, spikes and other units on that line.",
                    "Keep the Archer off HighGround while one is alive: HighGround outranks even the Archer "
                    + "preference, so climbing paints a target on whoever climbed.",
                    $"{grappler.MaxHp} HP and no attack means it is never urgent — kill the things that actually "
                    + "deal damage first, unless the pull line runs into a pit.",
                });

            table[UnitKind.Stalker] = new EnemyBehaviour(
                UnitKind.Stalker,
                "hazard flanker",
                $"Deals no damage at all. It walks around you at Move {stalker.Move} and shoves you into the terrain.",
                Steps(
                    ("Flank and shove into a hazard",
                     $"Looks for a player unit with a hazard directly on one side and the opposite tile "
                     + $"reachable this activation; moves onto that opposite tile and pushes {stalker.BasicPush} "
                     + "into the hazard. Hazards rank pit, then spikes, then wall or board edge (D-024) — it "
                     + "checks every target for a pit before it considers anyone's spikes."),
                    ("Otherwise stalk whoever is near terrain",
                     $"Walks toward the nearest player unit standing within 2 tiles of a hazard. Move "
                     + $"{stalker.Move} is the fastest in the game, so \"near a hazard\" is a wide net."),
                    ("Otherwise hold",
                     "With no player unit within 2 of any hazard it stands still and does nothing. A Stalker "
                     + "on genuinely open ground is a spectator.")),
                new[]
                {
                    $"No attack ({stalker.Damage} damage). Every wound it causes is the board's — a pit voids "
                    + "you outright, a wall or board edge is 2 and a Stagger, spikes are 3.",
                    "A hazard tile with a unit already standing on it does not count. That is an ordinary "
                    + "collision, not a hazard, so the Stalker looks elsewhere entirely.",
                    "Its step-2 search counts walls and the off-board edge as hazards, so on a walled board it "
                    + "nearly always has somewhere to be — even when no pit is in play.",
                    $"Wardbearer Hold does not stop it. Hold caps displacement at 1 tile, and the Stalker's "
                    + $"shove is exactly {stalker.BasicPush}.",
                    $"{stalker.MaxHp} HP, and it never trades: it has no attack to retaliate with, so the only "
                    + "cost of ignoring it is the tile you are standing on.",
                },
                new[]
                {
                    "Fight away from the walls. It needs a hazard on one side of you and a free tile on the "
                    + "other; deny either and its whole priority list falls through to \"hold\".",
                    "Occupy the flanking tile yourself. A body on the tile it wants to stand on is as good as "
                    + "killing it for that round.",
                    "A Footing token does not save you unless you spend it, and player units never spend one "
                    + "automatically (D-026) — so a unit holding an unused token can still be shoved into a pit.",
                });

            return table;
        }
    }
}
