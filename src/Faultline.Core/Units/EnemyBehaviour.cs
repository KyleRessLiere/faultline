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
            // The five the brief ships, then the behaviour variants, then the balance variants —
            // docs/ENEMY_ROSTER.md lists them in that order and the bestiary follows it.
            UnitKind.Husk, UnitKind.Lobber, UnitKind.Anchor, UnitKind.Grappler, UnitKind.Stalker,
            UnitKind.Warden, UnitKind.Perch, UnitKind.Bulwark, UnitKind.Harrier, UnitKind.Runt,
            UnitKind.Colossus,
            UnitKind.LesserGrappler, UnitKind.BluntedStalker, UnitKind.HeavyHusk, UnitKind.MobileAnchor,
            // The objective enemies, last: they are the two that need something on the board other
            // than a player unit to have anything to do (docs/CURATED_SET.md §5).
            UnitKind.Raider, UnitKind.QuarryKing,
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
                UnitKind.Wardbearer => "bodyguard — steps in front of the units beside it",
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

        /// <summary>
        /// How this archetype handles high ground, in words, or <c>null</c> when it handles it the
        /// ordinary way and there is nothing to say.
        /// </summary>
        /// <remarks>
        /// Here rather than in the renderer for the same reason <see cref="Describe(UnitTemplate)"/>
        /// is: it is a sentence about what a rule does, and a shell that writes its own would go on
        /// saying it after the rule changed. GAMEPLAY.md "Board and geometry" is the source.
        /// </remarks>
        /// <param name="template">Stat block to describe.</param>
        /// <returns>The sentence, or null.</returns>
        public static string? DescribeClimb(UnitTemplate template)
        {
            if (template is null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            return template.FreeClimb
                ? "free — climbing onto HighGround costs no extra movement"
                : null;
        }

        /// <summary>
        /// The clause every archetype's list now opens with: an enemy hauls its own out of a pit.
        /// </summary>
        /// <remarks>
        /// Added centrally rather than written into each entry, so an archetype cannot be authored
        /// without it — the slot lives in one place in <see cref="Ai"/> for the same reason.
        /// </remarks>
        private static IReadOnlyList<BehaviourStep> WithRescue(EnemyBehaviour behaviour)
        {
            var template = behaviour.Template;

            string outranked;
            if (template.Plan == EnemyPlan.Raider)
            {
                outranked = "Nothing outranks it here: this list has no clause about player units at "
                    + "all (D-045), so there is no killing blow for it to prefer, and a Raider beside "
                    + "a clinging ally genuinely stops walking at the shrine for a round.";
            }
            else if (template.Attack == AttackKind.None)
            {
                outranked = $"Nothing outranks it here: with no attack at all ({template.Damage} "
                    + "damage) it can never have a killing blow to prefer, so it rescues every time.";
            }
            else
            {
                outranked = $"It passes the rescue over only for a kill — an attack for "
                    + $"{template.Damage} that would put a player unit on 0 this activation, from "
                    + "where it stands or from a tile it can still walk to first.";
            }

            var steps = new List<BehaviourStep>(behaviour.Priorities.Count + 1)
            {
                new BehaviourStep(
                    1,
                    "Haul an ally off a pit lip",
                    "An adjacent ally clinging to a pit is pulled out. That spends the entire "
                    + "activation, move and action both, and places the ally on the first free tile "
                    + "beside the rescuer in the order up, right, down, left. Two clinging allies "
                    + "adjacent at once break the tie on lowest unit id. " + outranked),
            };

            foreach (var step in behaviour.Priorities)
            {
                steps.Add(new BehaviourStep(steps.Count + 1, step.Label, step.Detail));
            }

            return steps;
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
            var threadcaster = UnitTemplate.For(UnitKind.Threadcaster);
            var wardbearer = UnitTemplate.For(UnitKind.Wardbearer);

            var warden = UnitTemplate.For(UnitKind.Warden);
            var perch = UnitTemplate.For(UnitKind.Perch);
            var bulwark = UnitTemplate.For(UnitKind.Bulwark);
            var harrier = UnitTemplate.For(UnitKind.Harrier);
            var runt = UnitTemplate.For(UnitKind.Runt);
            var colossus = UnitTemplate.For(UnitKind.Colossus);
            var lesserGrappler = UnitTemplate.For(UnitKind.LesserGrappler);
            var bluntedStalker = UnitTemplate.For(UnitKind.BluntedStalker);
            var heavyHusk = UnitTemplate.For(UnitKind.HeavyHusk);
            var mobileAnchor = UnitTemplate.For(UnitKind.MobileAnchor);
            var raider = UnitTemplate.For(UnitKind.Raider);
            var king = UnitTemplate.For(UnitKind.QuarryKing);
            var kingEnraged = king.Enraged!;

            var table = new Dictionary<UnitKind, EnemyBehaviour>(Order.Length);

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
                    $"A hold aura does not stop it. The aura caps displacement at 1 tile, and the Stalker's "
                    + $"shove is exactly {stalker.BasicPush}. A {wardbearer.Name} in Guard Stance does stop "
                    + "it — the shove lands on the guard's tile instead, and its push resistance eats it.",
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

            // ---- behaviour variants (docs/ENEMY_ROSTER.md) ------------------------------------

            table[UnitKind.Warden] = new EnemyBehaviour(
                UnitKind.Warden,
                "the door",
                $"Move {warden.Move}. It never chases and never repositions; it hits whatever walks "
                + $"into reach for {warden.Damage} and is still standing there next round.",
                Steps(
                    ("Hit whatever is adjacent",
                     $"A player unit adjacent when the Warden activates is attacked for "
                     + $"{warden.Damage} — the hardest basic attack on the enemy side, level with the "
                     + "Anchor's. It does not step to a better tile first, because it has no step."),
                    ("Otherwise do nothing at all",
                     "With nothing in reach it holds. There is no closing branch in its priority list: "
                     + $"Move {warden.Move} leaves the planner no tile to walk to, and it never goes "
                     + "looking for one. Everything else on the board advances; the Warden does not.")),
                new[]
                {
                    $"{warden.MaxHp} HP behind Push resistance {warden.PushResistance}: "
                    + "Push 1 moves it nowhere, Push 2 moves it 1, and a Staggered Push 1 still only "
                    + "moves it 1 (D-018).",
                    $"Move {warden.Move} is not slow, it is fixed. A Warden placed in a gap is terrain "
                    + "with an attack, and the only thing that changes which tile it is on is you.",
                    "It spends only the action half of its activation, so a round in which nothing is "
                    + "adjacent to it costs it nothing and passes without an event.",
                },
                new[]
                {
                    $"Pull it. Push resistance only reads Pushes — the Threadcaster's Reel drags it "
                    + "the whole way out of the gap it is plugging and the door is open.",
                    $"Or pay the toll: {warden.MaxHp} HP is two Archer shots ({archer.Damage} each) "
                    + "plus a swing, and it cannot follow whoever slipped past while you did it.",
                });

            table[UnitKind.Perch] = new EnemyBehaviour(
                UnitKind.Perch,
                "artillery that takes the ledge",
                $"A Lobber that wants the high ground: it shoots for {perch.Damage} on the flat and "
                + $"{perch.Damage + 1} from HighGround, and once it is up there it does not come down.",
                Steps(
                    ("Shoot from where it stands",
                     $"With no player unit adjacent and one within range {perch.Range}, it shoots the "
                     + $"nearest without moving — {perch.Damage} normally, {perch.Damage + 1} if it is "
                     + "standing on HighGround. The +1 ranged bonus is not a player-only rule."),
                    ("Break contact before anything else",
                     "A player unit adjacent and the shot is off: it retreats to the reachable tile "
                     + "that maximises the distance to the nearest player unit, exactly as a Lobber "
                     + "does, and shoots afterwards only if the retreat broke contact."),
                    ("Otherwise climb",
                     $"Off HighGround with a ledge it can route to, it walks toward the nearest one — "
                     + $"real path distance, so a wall is a detour — and fires on arrival if that puts "
                     + $"someone in range {perch.Range}. Move {perch.Move} pays the +1 climb cost, so "
                     + "stepping onto an adjacent ledge is its entire move."),
                    ("Otherwise hold the ledge, or advance to the band",
                     $"Standing on HighGround with nothing in range it holds — it will not give the "
                     + $"tile up. Off HighGround with no ledge reachable it falls back to the Lobber's "
                     + $"2–{perch.Range} band (D-023).")),
                new[]
                {
                    $"{perch.MaxHp} HP. It is the flimsiest thing on the enemy side and it is standing "
                    + "in the most visible spot on the board.",
                    "It cannot be shoved *up* onto a ledge — the lip collides like a wall — but it can "
                    + "be shoved *off* one, for 1 fall damage, with the displacement continuing.",
                    "Elevation stops being free real estate. A Perch on the ridge is the Archer's tile, "
                    + "already occupied, and it hits as hard from up there as an Archer does from the flat.",
                },
                new[]
                {
                    "Take the ledge first. It is a race, not a fight — a Perch that never reaches "
                    + "HighGround is a worse Lobber.",
                    $"Stand next to it. Adjacent it never shoots, only runs, and at Move {perch.Move} "
                    + "carrying a climb cost it does not get far.",
                    "Shove it off the ledge and it does not climb back while anything is in range: it "
                    + "will stand on the flat and shoot for one less.",
                });

            table[UnitKind.Bulwark] = new EnemyBehaviour(
                UnitKind.Bulwark,
                "the hold aura, and the only thing that still carries one",
                $"Hits for {bulwark.Damage} and is not worth killing for that. It is worth killing "
                + "because every enemy standing next to it cannot be displaced more than 1 tile.",
                Steps(
                    ("Hit what is already in reach",
                     $"A player unit adjacent when it activates is attacked for {bulwark.Damage}, "
                     + "without repositioning first. Its priority list is the Husk's — it is the aura "
                     + "that is different, not the plan."),
                    ("Otherwise close on the nearest",
                     $"Spends up to Move {bulwark.Move} walking toward the nearest player unit, and "
                     + "attacks if the walk ends adjacent (D-022). Being slower than the things it "
                     + "protects, it tends to arrive as the anchor of a formation rather than ahead of one.")),
                new[]
                {
                    "Adjacent allies cap at 1 tile of displacement, and it does not protect itself "
                    + $"(D-019). The {wardbearer.Name} used to carry the identical rule and no longer "
                    + "does (D-058), so this is the only hold aura left in the game.",
                    "It reads as a direct answer to the best interaction in the game. A shove into "
                    + "another unit is 2 damage to both; next to a Bulwark, that shove travels 1 tile "
                    + "and often reaches nothing at all.",
                    "Hold caps distance, not damage. A push of exactly 1 into a body still collides and "
                    + "still deals 2 to both — the cap only bites on a shove that wanted to travel further.",
                    $"{bulwark.MaxHp} HP at Move {bulwark.Move}: tougher than the chaff it babysits and "
                    + "slower than all of it.",
                },
                new[]
                {
                    "Kill it first and the formation is a crowd again. Every enemy it was covering goes "
                    + "back to being shovable the moment it goes down.",
                    $"Or pull instead of pushing. Hold caps a Pull the same way, so the {threadcaster.Name}'s "
                    + "Reel is blunted too — but a Pull of 1 that drags someone off a ledge or over a "
                    + "pit lip still does the job.",
                    "Attack the units on the outside of its aura. Adjacency is four tiles, not a bubble.",
                });

            table[UnitKind.Harrier] = new EnemyBehaviour(
                UnitKind.Harrier,
                "separator",
                $"Deals no damage. It pushes {harrier.BasicPush} to peel a player unit away from the "
                + "rest of its side, and it works on a board with no terrain on it at all.",
                Steps(
                    ("Flank and shove someone away from their own side",
                     $"Looks for a player unit it can reach the far side of, and pushes "
                     + $"{harrier.BasicPush} so the target lands further from its nearest ally than it "
                     + "started. It scores every reachable flanking tile and takes the one that opens "
                     + "the biggest gap; a shove that would not actually move the target scores nothing "
                     + "and is never chosen. Ties break on lowest unit id, then direction order."),
                    ("Otherwise close on the nearest",
                     $"With no separating shove available it walks toward the nearest player unit at "
                     + $"Move {harrier.Move}, the joint-fastest speed in the game, and tries again next round.")),
                new[]
                {
                    "It never shoves into something solid. A wall, the board edge, a body or a ledge "
                    + "from below all leave the target on the tile it started on, which is worth zero "
                    + "to its score — that is the difference between a Harrier and a Stalker. It wants "
                    + "the tile you are not standing on, not the collision.",
                    $"No attack at all ({harrier.Damage} damage), and unlike the Stalker it does not "
                    + "want the board to deal it either. A Harrier can spend a whole fight without a "
                    + "point of damage on the log and still lose you the fight.",
                    $"{harrier.MaxHp} HP and Move {harrier.Move}: it is fast enough to pick which of "
                    + "your units is alone this round.",
                    "A lone player unit with no allies left is still shoved — with nobody to be "
                    + "separated from, any tile it did not choose counts as disruption.",
                },
                new[]
                {
                    "Stay in a block. Its whole score is the gap it can open, and two units on adjacent "
                    + "tiles leave it almost nothing to gain.",
                    "Fight with your back to a wall on purpose. It refuses shoves that do not move you, "
                    + "so terrain behind you is a shield rather than a hazard.",
                    $"{harrier.MaxHp} HP and no attack means it never trades — but it is also the enemy "
                    + "whose round you can waste simply by standing somewhere it cannot improve on.",
                });

            table[UnitKind.Runt] = new EnemyBehaviour(
                UnitKind.Runt,
                "swarm chaff",
                $"{runt.MaxHp} HP and Move {runt.Move}. It runs at you, hits for {runt.Damage}, and "
                + "dies to literally anything.",
                Steps(
                    ("Hit what is already in reach",
                     $"Adjacent player unit → attacked for {runt.Damage}, without moving first. The "
                     + "Husk's priority list unchanged; only the stat block is different."),
                    ("Otherwise close on the nearest",
                     $"Move {runt.Move} closes on the nearest player unit and attacks if the walk ends "
                     + "adjacent (D-022). It is as fast as the Stalker, so a Runt three tiles away is a "
                     + "Runt in your face.")),
                new[]
                {
                    $"One collision kills it outright: a shove into a wall, the board edge or another "
                    + "unit is 2 damage and it has {runt.MaxHp}. One spike tile is 3.",
                    "A shove into another Runt is a double kill, and it costs a basic attack rather "
                    + "than an ability.",
                    "It is the only unit that dies to a single point of fall damage off HighGround.",
                },
                new[]
                {
                    "Never spend an attack on one. Runts are what displacement is for — every point of "
                    + "damage the board deals to them is an action you did not spend.",
                    "Count the queue. Runts arrive in numbers and in lines, and a line is the shape "
                    + "collision damage punishes hardest.",
                    "The danger is arithmetic, not any individual one of them: four Runts adjacent is "
                    + $"{runt.Damage * 4} damage a round to whoever they surrounded.",
                });

            table[UnitKind.Colossus] = new EnemyBehaviour(
                UnitKind.Colossus,
                "immovable object",
                $"{colossus.MaxHp} HP, hits for {colossus.Damage}, and the board cannot be used against "
                + "it at all. Push does nothing to a Colossus.",
                Steps(
                    ("Hit what is adjacent",
                     $"A player unit adjacent when it activates is attacked for {colossus.Damage} — the "
                     + "largest single hit anything on the board has — and it does not move to do it."),
                    ("Otherwise take its one step",
                     $"Move {colossus.Move}: one tile toward the nearest player unit, then attack if "
                     + "that step lands it adjacent. It cannot chase and does not try to.")),
                new[]
                {
                    $"Push resistance {colossus.PushResistance} (D-018). Push 1 and Push 2 "
                    + "both move it nowhere, so the Vanguard's basic shove and Bull Rush are equally "
                    + "useless against it as openers.",
                    "Only a Stagger unlocks it: a Staggered Bull Rush is an effective Push 3, which "
                    + "resistance 2 turns into exactly 1 tile of travel.",
                    "Pull is untouched — resistance is a Push rule, not a weight rule. Reel drags a "
                    + $"Colossus the full distance, which makes the {threadcaster.Name} the answer to it.",
                    $"{colossus.MaxHp} HP is more than double anything else on the enemy side. It is a "
                    + "fight, not a puzzle.",
                },
                new[]
                {
                    $"Bring the {threadcaster.Name}. Pull is the only displacement that reads normally "
                    + "against it, so if the plan needs it moved, Reel is the plan.",
                    $"Or walk away. Move {colossus.Move} means a Colossus more than a tile from what you "
                    + "care about is scenery with a large damage number attached.",
                    $"If you must shove it, stagger it first — spike or collision damage from anything "
                    + "else, then Bull Rush.",
                });

            // ---- balance variants: the same priority list, different numbers ------------------

            table[UnitKind.LesserGrappler] = new EnemyBehaviour(
                UnitKind.LesserGrappler,
                "abductor, short-armed",
                $"A Grappler that has to be {lesserGrappler.Range} tiles away instead of "
                + $"{grappler.Range}. Same trap, half the board.",
                Steps(
                    ("Yank someone out of position",
                     $"A player unit exactly {lesserGrappler.Range} tiles away is pulled "
                     + $"{lesserGrappler.BasicPull} tiles toward it, preferring a unit on HighGround, "
                     + $"then the {archer.Name}, then lowest unit id. A unit already adjacent cannot be "
                     + "pulled (D-020)."),
                    ("Otherwise close to pulling range",
                     $"Move {lesserGrappler.Move} toward the {archer.Name}, or the nearest player unit "
                     + $"when none is on the board, aiming for the 2–{lesserGrappler.Range} band rather "
                     + "than contact, and pulls the moment it arrives.")),
                new[]
                {
                    $"No attack at all. Every point of damage it causes is dealt by whatever the pull "
                    + $"drags you into — and being pulled {lesserGrappler.BasicPull} tiles from "
                    + $"{lesserGrappler.Range} away lands you adjacent to it every time.",
                    $"Its band is 2–{lesserGrappler.Range}, which is one tile wide. It spends far more "
                    + "of the fight walking than a Grappler does.",
                    $"{lesserGrappler.MaxHp} HP: as durable as the full-range version, so the shorter "
                    + "reach is the whole discount.",
                },
                new[]
                {
                    $"Three tiles away is safe. That is the entire counterplay and it is worth knowing "
                    + $"— a {grappler.Name} at the same distance would already have you.",
                    "Its reach and its threat now overlap, so a unit it can pull is a unit that can hit "
                    + "it back next round.",
                });

            table[UnitKind.BluntedStalker] = new EnemyBehaviour(
                UnitKind.BluntedStalker,
                "hazard flanker, honest",
                $"Deals no damage and, unlike a {stalker.Name}, will not manufacture any: it pushes "
                + $"{bluntedStalker.BasicPush} into a pit or onto spikes and nothing else.",
                Steps(
                    ("Flank and shove into real terrain",
                     $"A player unit with a pit or a spike tile directly on one side and the opposite "
                     + $"tile reachable this activation → move there and push {bluntedStalker.BasicPush} "
                     + "into it. Pits still outrank spikes (D-024). The wall-and-board-edge tier of the "
                     + "ladder is switched off for it, so a player backed against the edge is not a target."),
                    ("Otherwise stalk whoever is near a hazard",
                     $"Walks toward the nearest player unit within 2 tiles of a pit or spikes — walls "
                     + $"do not count for this search either — at Move {bluntedStalker.Move}."),
                    ("Otherwise hold",
                     "With no pit and no spikes anywhere on the board its whole list falls through and "
                     + "it stands still for the entire fight.")),
                new[]
                {
                    "The tier it gave up is the one that is always available. The board edge exists on "
                    + "every map, so shoving into it was guaranteed damage every round on a unit "
                    + "documented as dealing none — removing it is the point of the variant.",
                    $"{bluntedStalker.MaxHp} HP and no attack. On a map with no pit and no spikes it "
                    + "contributes literally nothing, which is a deliberate scenario tool rather than "
                    + "an oversight.",
                    "A hazard tile with a unit standing on it still does not count — that is an "
                    + "ordinary collision, and collisions are exactly what this variant gave up.",
                    $"A hold aura still does not blunt it: the aura caps displacement above 1 tile and "
                    + $"its shove is exactly {bluntedStalker.BasicPush}.",
                },
                new[]
                {
                    "Fight near the walls on purpose. Against this one the board edge is safe ground, "
                    + "which inverts the habit the ordinary Stalker teaches.",
                    "Stay more than 2 tiles from every pit and spike tile and its entire priority list "
                    + "falls through to holding position.",
                });

            table[UnitKind.HeavyHusk] = new EnemyBehaviour(
                UnitKind.HeavyHusk,
                "chaff that survives the shove",
                $"A Husk with {heavyHusk.MaxHp} HP. It still walks at you and still hits for "
                + $"{heavyHusk.Damage}; it just does not die to the first collision.",
                Steps(
                    ("Hit what is already in reach",
                     $"Adjacent player unit → attacked for {heavyHusk.Damage} without moving. Priority "
                     + "1 is literal: it does not reposition to a better tile."),
                    ("Otherwise close on the nearest",
                     $"Move {heavyHusk.Move} toward the nearest player unit, attacking if the walk ends "
                     + "adjacent (D-022). Identical list to the Husk's — the only change is the hit points.")),
                new[]
                {
                    $"{heavyHusk.MaxHp} HP survives one collision (2 damage) and one Archer shot "
                    + $"({archer.Damage}). It does not survive both, and it does not survive spikes ("
                    + "3 damage) at all.",
                    "It breaks the double-kill: two Heavy Husks in a line are a shove for 2 apiece and "
                    + "two Staggered survivors, not two corpses.",
                    $"A {vanguard.Name} swing ({vanguard.Damage}) plus its push into something solid is "
                    + $"exactly {vanguard.Damage + 2} — still one short of killing it outright.",
                },
                new[]
                {
                    "Treat it as a body, not as chaff. Shoving it is still worth doing for the Stagger "
                    + "and the tile, but it will be standing afterwards.",
                    "The Stagger it takes from a collision makes the follow-up shove travel a tile "
                    + "further, so two shoves still reach a hazard the first one could not.",
                });

            table[UnitKind.MobileAnchor] = new EnemyBehaviour(
                UnitKind.MobileAnchor,
                "screen that can reach the fight",
                $"An Anchor at Move {mobileAnchor.Move}. Same {mobileAnchor.MaxHp} HP, same "
                + $"{mobileAnchor.Damage} damage, same shrug — it just arrives while the fight is still on.",
                Steps(
                    ("Hit what is adjacent",
                     $"A player unit adjacent when it activates is attacked for {mobileAnchor.Damage}, "
                     + "without moving first."),
                    ("Otherwise take its two steps",
                     $"Move {mobileAnchor.Move} toward the nearest player unit, attacking if the walk "
                     + $"ends adjacent. Double the Anchor's speed is the whole variant: at Move "
                     + $"{anchor.Move} the original could be ignored, and this one cannot.")),
                new[]
                {
                    $"It shrugs one tile off every Push exactly as an Anchor does (D-018): Push 1 moves "
                    + "it nowhere, Push 2 moves it 1, a Staggered Push 1 still moves it 1.",
                    "Pull is unaffected — the shrug is a Push rule, not a weight rule.",
                    $"{mobileAnchor.MaxHp} HP at Move {mobileAnchor.Move} is the most durable thing on "
                    + "the board that can also keep up with the players.",
                },
                new[]
                {
                    "Walking away no longer works. The original Anchor's counterplay was its speed; "
                    + "this one has to be pulled, killed or gone around.",
                    "Pull it out of the line rather than pushing it back into one — Reel ignores the "
                    + "shrug entirely.",
                });

            // ---- objective enemies (docs/CURATED_SET.md §5) ------------------------------------

            table[UnitKind.Raider] = new EnemyBehaviour(
                UnitKind.Raider,
                "siege chaff — it is not here for you",
                $"A Husk in every number that walks past you to the shrine and claws it for "
                + $"{raider.Damage} a round. It has no clause about player units in its list at all.",
                Steps(
                    ("Claw the structure it is standing next to",
                     $"Ends its activation adjacent to a Protect structure → the structure takes "
                     + $"{raider.Damage}. It does not step to a better tile first and it does not need "
                     + "to spend its action on it: standing there is the attack."),
                    ("Otherwise walk at the nearest structure",
                     $"Spends up to Move {raider.Move} closing on the nearest Protect structure by the "
                     + "same breadth-first path field every other archetype walks by (D-029) — a wall "
                     + "is a detour, a body is a toll of 2, and it claws in the same activation if the "
                     + "walk ends adjacent."),
                    ("Otherwise stand still",
                     "With no Protect structure standing anywhere — a shrine already in rubble, or a "
                     + "board that never had one — its list runs out and it holds. It re-checks every "
                     + "activation, so a structure appearing puts it back in motion.")),
                new[]
                {
                    "It never attacks a player unit. Not one it walks past, not one standing next to it, "
                    + "not one clinging to a pit lip beside it — the free finish every other armed enemy "
                    + "takes is a clause about player units, and this list has none (D-041).",
                    "It never defends itself either. Hitting a Raider costs you nothing in return, which "
                    + "is exactly why the clock and not the damage is the threat.",
                    $"{raider.MaxHp} HP: the same body a Husk has. One collision, one spike tile or one "
                    + "Archer shot removes it.",
                    "It targets a tile, not a unit, so its intent carries the structure's coordinate and "
                    + "no target id. Killing everything else on the board does not change its plan.",
                },
                new[]
                {
                    "Displace it. A thing that will not fight back is a thing you can spend the whole "
                    + "fight shoving off its lane — every tile it is pushed sideways is a round the "
                    + "shrine does not lose hit points. Shoving one of its escorts into a pit beside "
                    + "it costs it a whole activation of walking, for the same reason.",
                    "Count its walk, not its damage. Work out the round it arrives and make sure the tile "
                    + "it wants is occupied by then; a body on the ring is worth more than a kill.",
                    "Its escorts are the fight. The Raider cannot punish you for standing in the wrong "
                    + "place — whatever came with it can.",
                });

            table[UnitKind.QuarryKing] = new EnemyBehaviour(
                UnitKind.QuarryKing,
                "boss — the body the board cannot move",
                $"{king.MaxHp} HP at Move {king.Move}, hitting for {king.Damage} and shoving "
                + $"{king.AttackPush} with the same swing. Three Footing tokens that do not shorten a "
                + "displacement but cancel it, and a second stat block waiting at "
                + $"{king.EnrageAt} HP.",
                Steps(
                    ("Bull Rush, once the second block is in force",
                     $"At {king.EnrageAt} HP or below he charges up to Move {kingEnraged.Move} tiles in "
                     + $"a straight line, stops adjacent to the first player unit on it and pushes "
                     + $"{kingEnraged.BasicPush} — the player's own opener, aimed back. He takes it only "
                     + "when the shove beats the swing: into another body, or over the lip of a pit. On "
                     + "open ground he punches instead."),
                    ("Hit what is adjacent",
                     $"A player unit adjacent when he activates is attacked for {king.Damage}, the "
                     + $"hardest basic attack in the game, and shoved {king.AttackPush} by the same "
                     + "swing. He does not reposition first."),
                    ("Otherwise close on the nearest",
                     $"Move {king.Move} in his first phase — one tile a round, so the opening half of "
                     + "the fight happens at whatever range you choose. Attacks if the step lands him "
                     + "adjacent (D-022).")),
                new[]
                {
                    $"His {king.Footing} Footing tokens are not the ordinary one-tile shrug. While any "
                    + "remain, every Push and Pull against him resolves at distance 0 — Push 1, Push 2, "
                    + "Bull Rush and Reel all move him nowhere — and the token is not spent doing it (D-039).",
                    "A token is stripped by a collision he suffers, or by ending a round orthogonally "
                    + "next to a pit. He cannot be moved, so the way to strip him is to slam something "
                    + "else into him: a shoved Husk deals 2 to both and costs him a token.",
                    $"The swap at {king.EnrageAt} HP is a whole stat block, not a buff: Move {king.Move} "
                    + $"becomes Move {kingEnraged.Move} and the Bull Rush branch appears. He re-declares "
                    + "his intent on the spot, so the telegraph never lies about which King you are "
                    + "facing (D-040).",
                    "There is no area attack. Everything he does is single-target and fully telegraphed, "
                    + "which is what keeps him inside the intent system rather than beside it.",
                    "Voiding him is legal and is still the smart win. With the tokens gone he clings to a "
                    + "pit lip like anything else, and a finish costs a free action.",
                },
                new[]
                {
                    "Bring bodies to him. Every Husk you shove into the King is 2 damage to each of them "
                    + "and one token off the boss — the best-value interaction in the game, pointed at "
                    + $"the only {king.MaxHp} HP target in it.",
                    "Fight him on the rim. Ending a round beside a pit costs him a token whether or not "
                    + "you touched him, so the ground does a third of the stripping for free.",
                    $"Spend the Move {king.Move} phase. One tile a round is an enormous gift: strip tokens "
                    + $"and set the geometry before he drops to {king.EnrageAt} and starts charging.",
                    $"Then void him. Once the tokens are gone he is a {king.MaxHp} HP unit with no push "
                    + "resistance at all, and the pit does not care how many hit points he has left.",
                });

            foreach (var kind in Order)
            {
                table[kind] = table[kind] with { Priorities = WithRescue(table[kind]) };
            }

            return table;
        }
    }
}
