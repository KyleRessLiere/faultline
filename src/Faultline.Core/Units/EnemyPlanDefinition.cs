using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// The signature every enemy planner has. A plain delegate rather than an interface or a
    /// reflected lookup: the registry hands out the method group itself, so "the plan that is
    /// documented" and "the plan that runs" are the same object (component review, "Risks of
    /// over-engineering" — no reflection-based handler discovery).
    /// </summary>
    /// <param name="state">Current state.</param>
    /// <param name="enemy">Enemy being planned for.</param>
    /// <param name="hostiles">Every reachable hostile unit, unfiltered.</param>
    /// <param name="choices">
    /// The hostiles this plan may name, which is <paramref name="hostiles"/> narrowed to the target
    /// already locked by a declared intent, when there is one.
    /// </param>
    /// <returns>The intent the plan declares.</returns>
    public delegate EnemyIntent EnemyPlanner(
        GameState state, Unit enemy, IReadOnlyList<Unit> hostiles, IReadOnlyList<Unit> choices);

    /// <summary>How a plan picks which hostile it acts on. Descriptive; the planner is the authority.</summary>
    public enum EnemyTargetSelection
    {
        /// <summary>The nearest hostile, ties on lowest unit id then row-major order.</summary>
        Nearest = 0,

        /// <summary>A unit on HighGround first, then the Archer, then lowest unit id.</summary>
        HighGroundThenArcher = 1,

        /// <summary>The nearest hostile standing within two tiles of a hazard.</summary>
        NearestBesideHazard = 2,

        /// <summary>Whichever hostile a shove can separate furthest from its own side.</summary>
        MostSeparable = 3,

        /// <summary>The nearest standing Protect structure. Not a unit at all.</summary>
        NearestStructure = 4,

        /// <summary>Nobody: the plan is about getting away from all of them.</summary>
        None = 5,
    }

    /// <summary>
    /// The handful of knobs a plan exposes. Deliberately tiny (component review, "Recommended enemy
    /// plan definition"), and deliberately free of anything the stat block already holds: the upper
    /// end of a ranged plan's band is <see cref="UnitTemplate.Range"/> and is never restated here,
    /// because two sources for one number is the way this refactor fails.
    /// </summary>
    public sealed record EnemyPlanParameters
    {
        /// <summary>The shared default: melee, nearest target, no retreat.</summary>
        public static readonly EnemyPlanParameters Default = new EnemyPlanParameters();

        /// <summary>
        /// Closest tile the plan is willing to fight from. The ranged plans want 2 — inside their
        /// range, outside melee (D-023) — and <see cref="Ai"/> reads this rather than a literal.
        /// </summary>
        public int PreferredRangeLow { get; init; } = 1;

        /// <summary>True when the plan breaks contact before doing anything else.</summary>
        public bool RetreatWhenAdjacent { get; init; }

        /// <summary>
        /// True when a killing blow on a player unit outranks the rescue slot. False for every plan
        /// whose archetypes deal no damage, and for a plan that never attacks a player unit.
        /// </summary>
        public bool PrefersLethalAttack { get; init; } = true;

        /// <summary>How the plan picks whom to act on.</summary>
        public EnemyTargetSelection TargetSelection { get; init; } = EnemyTargetSelection.Nearest;
    }

    /// <summary>One branch of a plan's priority list, in the order the planner evaluates it.</summary>
    /// <param name="Order">1-based position. Lower runs first.</param>
    /// <param name="Label">Short imperative headline for the branch.</param>
    /// <param name="Detail">What the branch tests for and what it does, in rules terms.</param>
    public sealed record PlanPriority(int Order, string Label, string Detail);

    /// <summary>
    /// One enemy priority list, whole: its identity, the method that executes it, the branches it
    /// evaluates in order, the quirks every archetype running it inherits, and the few parameters it
    /// exposes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why this exists.</b> The priority list used to exist twice — as executable C# in
    /// <see cref="Ai"/> and as prose in <see cref="EnemyBehaviour"/> — with nothing but review
    /// keeping them in step (component review, "Enemies and behavior"). They are joined here:
    /// <see cref="Ai"/> dispatches by looking this registry up, so a plan with no registration cannot
    /// run at all, and <see cref="EnemyBehaviour"/> is pinned to the same registration by
    /// <c>EnemyPlanDefinitionTests</c>, which asserts that every archetype's documented list has
    /// exactly one step per branch registered here plus the shared rescue slot. Growing a planner a
    /// branch without growing its description now fails the build's tests rather than shipping a
    /// bestiary that lies.
    /// </para>
    /// <para>
    /// <b>This registry is intended to become the AI decision-trace's data source.</b> When a trace
    /// exists it will report "branch 2 of melee-chaser fired" by naming the <see cref="PlanPriority"/>
    /// the planner took, which is only meaningful while the numbering here and the branches in
    /// <see cref="Ai"/> are the same list. That is the reason the prose and the planner must not be
    /// allowed to drift apart again: a trace built on a stale description is worse than no trace.
    /// </para>
    /// <para>
    /// <b>The AI is not declarative and is not going to be.</b> Stalker hazard evaluation, Grappler
    /// target priority, Perch high-ground selection and Quarry King phase behaviour stay as tested C#
    /// algorithms with deterministic tie-breaking (component review, same section). What is data here
    /// is the <em>registration</em> and the <em>description</em>, never the geometry.
    /// </para>
    /// </remarks>
    /// <param name="Plan">Which priority list this is.</param>
    /// <param name="Name">Display name, e.g. "melee chaser".</param>
    /// <param name="Summary">One line: what the list is for.</param>
    /// <param name="Planner">
    /// The method in <see cref="Ai"/> that executes it. Positional and without a default on purpose:
    /// a plan registered without a planner fails to compile rather than silently holding position.
    /// </param>
    public sealed record EnemyPlanDefinition(
        EnemyPlan Plan,
        string Name,
        string Summary,
        EnemyPlanner Planner)
    {
        private static readonly string[] NoQuirks = new string[0];

        private static readonly EnemyPlan[] Order =
        {
            EnemyPlan.Melee, EnemyPlan.Lobber, EnemyPlan.Grappler, EnemyPlan.Stalker,
            EnemyPlan.Warden, EnemyPlan.Perch, EnemyPlan.Harrier, EnemyPlan.Raider,
            EnemyPlan.QuarryKing, EnemyPlan.Escort, EnemyPlan.Rushmaster,
            EnemyPlan.Cooper, EnemyPlan.Inert,
        };

        private static readonly Dictionary<EnemyPlan, EnemyPlanDefinition> ByPlan = Build();

        /// <summary>The branches the planner evaluates, in order.</summary>
        public IReadOnlyList<PlanPriority> Priorities { get; init; } = Array.Empty<PlanPriority>();

        /// <summary>Behaviour every archetype running this list inherits, one line each.</summary>
        public IReadOnlyList<string> Quirks { get; init; } = NoQuirks;

        /// <summary>The knobs this plan exposes.</summary>
        public EnemyPlanParameters Parameters { get; init; } = EnemyPlanParameters.Default;

        /// <summary>
        /// True when the list contains no clause about player units at all. Only the Raider's does
        /// (D-041, D-045): it is planned before anybody asks who the player units are, it takes no
        /// free finish on a clinging player, and its rescue slot is never outranked by a kill. A flag
        /// here rather than an archetype check in three files, which is what it used to be.
        /// </summary>
        public bool IgnoresPlayerUnits { get; init; }

        /// <summary>
        /// True when this list belongs to an act's boss — the body an <see cref="ObjectiveKind.Boss"/>
        /// fight is about.
        /// </summary>
        /// <remarks>
        /// Registered here rather than asked of an archetype, for D-221's reason: every boss clause in
        /// this codebase is keyed off the priority list the stat block names, so a rebalanced variant
        /// that reuses a boss's list is a boss and cannot drift into not being one. It is what
        /// <see cref="Objectives.Check"/> asks when it decides a boss fight is over (D-222).
        /// </remarks>
        public bool IsBoss { get; init; }

        /// <summary>Whether a unit running this plan is an act's boss.</summary>
        /// <param name="template">Stat block to ask about.</param>
        /// <returns>Whether its plan is a boss's.</returns>
        public static bool IsBossPlan(UnitTemplate? template) =>
            template is not null && ForPlan(template.Plan) is { IsBoss: true };

        /// <summary>Every registered plan, in roster order.</summary>
        public static IReadOnlyList<EnemyPlan> Plans => Order;

        /// <summary>
        /// The definition for a plan, or <c>null</c> for <see cref="EnemyPlan.None"/> — which is what
        /// a player class carries, and is not a missing registration.
        /// </summary>
        /// <param name="plan">Plan to look up.</param>
        /// <returns>Its definition, or <c>null</c>.</returns>
        public static EnemyPlanDefinition? ForPlan(EnemyPlan plan) =>
            ByPlan.TryGetValue(plan, out var definition) ? definition : null;

        /// <summary>The definition for a plan that must have one.</summary>
        /// <param name="plan">Plan to look up.</param>
        /// <returns>Its definition.</returns>
        public static EnemyPlanDefinition For(EnemyPlan plan) =>
            ForPlan(plan) ?? throw new ArgumentOutOfRangeException(
                nameof(plan), plan, "No definition for enemy plan.");

        /// <summary>
        /// Every plan, in roster order. This is the registry coverage tests enumerate — they walk
        /// this rather than a hand-maintained list, so a plan is covered the moment it is registered
        /// (component review, "Validation requirements").
        /// </summary>
        /// <returns>All definitions.</returns>
        public static IReadOnlyList<EnemyPlanDefinition> All()
        {
            var all = new List<EnemyPlanDefinition>(Order.Length);
            foreach (var plan in Order)
            {
                all.Add(ByPlan[plan]);
            }

            return all;
        }

        private static PlanPriority[] Steps(params (string Label, string Detail)[] steps)
        {
            var list = new PlanPriority[steps.Length];
            for (int i = 0; i < steps.Length; i++)
            {
                list[i] = new PlanPriority(i + 1, steps[i].Label, steps[i].Detail);
            }

            return list;
        }

        private static Dictionary<EnemyPlan, EnemyPlanDefinition> Build()
        {
            var table = new Dictionary<EnemyPlan, EnemyPlanDefinition>(Order.Length);

            table[EnemyPlan.Melee] = new EnemyPlanDefinition(
                EnemyPlan.Melee,
                "melee chaser",
                "Hit what is already in reach, otherwise walk at the nearest player unit.",
                Ai.PlanMelee)
            {
                Priorities = Steps(
                    ("Attack an adjacent player unit",
                     "A player unit adjacent when the archetype activates is attacked without "
                     + "repositioning first. Priority 1 is literal: the planner does not look for a "
                     + "better tile before swinging."),
                    ("Otherwise close on the nearest",
                     "Spends its movement walking the breadth-first path field toward the nearest "
                     + "player unit, and attacks in the same activation if the walk ends adjacent "
                     + "(D-022).")),
                Quirks = new[]
                {
                    "Ties break on lowest unit id, then row-major coordinate order — never randomly.",
                    "Routes prefer crossing fewer spike tiles, then the shorter walk, so an archetype "
                    + "already standing where it wants to be does not shuffle.",
                },
            };

            table[EnemyPlan.Lobber] = new EnemyPlanDefinition(
                EnemyPlan.Lobber,
                "ranged kiter",
                "Shoot from a band, and break contact before doing anything else.",
                Ai.PlanLobber)
            {
                Priorities = Steps(
                    ("Shoot from where it stands",
                     "With no player unit adjacent and one inside the stat block's range, it shoots "
                     + "the nearest without moving. A shot fired from HighGround carries the ranged "
                     + "bonus, which is not a player-only rule."),
                    ("Break contact before anything else",
                     "Any player unit adjacent and the shot is off: it retreats to the reachable tile "
                     + "that maximises the distance to the nearest player unit, ties broken by the "
                     + "greater total distance to all of them, and shoots afterwards only if the "
                     + "retreat actually broke contact."),
                    ("Otherwise advance to the band, not to contact",
                     "Walks toward the nearest player unit aiming for the band rather than for "
                     + "adjacency (D-023), then shoots if it arrives in range and out of contact.")),
                Quirks = new[]
                {
                    "The adjacency check runs against every player unit, not only the one it declared "
                    + "against: a second unit stepping in also stops the shot.",
                },
                Parameters = new EnemyPlanParameters
                {
                    PreferredRangeLow = 2,
                    RetreatWhenAdjacent = true,
                },
            };

            table[EnemyPlan.Grappler] = new EnemyPlanDefinition(
                EnemyPlan.Grappler,
                "abductor",
                "Haul a player unit out of position, otherwise close to hauling range.",
                Ai.PlanGrappler)
            {
                Priorities = Steps(
                    ("Yank someone out of position",
                     "A player unit inside the band is pulled toward the archetype. It picks, in "
                     + "order: a unit standing on HighGround, then the Archer, then lowest unit id. A "
                     + "target already adjacent cannot be pulled (D-020)."),
                    ("Otherwise close to pulling range",
                     "Walks toward the Archer — or the nearest player unit when no Archer is on the "
                     + "board — aiming for the band rather than for contact (D-023), and pulls "
                     + "immediately if it arrives in range.")),
                Quirks = new[]
                {
                    "The pull resolves every tile on the way, so it drags a unit through spikes or "
                    + "over the lip of a pit rather than merely next to one.",
                    "It hunts the Archer by name: with an Archer on the board, an archetype that "
                    + "cannot pull anything walks toward her regardless of who is closer.",
                },
                Parameters = new EnemyPlanParameters
                {
                    PreferredRangeLow = 2,
                    PrefersLethalAttack = false,
                    TargetSelection = EnemyTargetSelection.HighGroundThenArcher,
                },
            };

            table[EnemyPlan.Stalker] = new EnemyPlanDefinition(
                EnemyPlan.Stalker,
                "hazard flanker",
                "Shove a player unit into the terrain, otherwise stalk whoever is standing near some.",
                Ai.PlanStalker)
            {
                Priorities = Steps(
                    ("Flank and shove into a hazard",
                     "Looks for a player unit with a hazard directly on one side and the opposite tile "
                     + "reachable this activation; moves onto that opposite tile and shoves into the "
                     + "hazard. Hazards rank pit, then spikes, then wall or board edge (D-024), and "
                     + "how far down that ladder the archetype will go is the stat block's hazard "
                     + "ranks."),
                    ("Otherwise stalk whoever is near terrain",
                     "Walks toward the nearest player unit standing within 2 tiles of a hazard it "
                     + "counts."),
                    ("Otherwise hold",
                     "With no player unit within 2 of any hazard it stands still and does nothing.")),
                Quirks = new[]
                {
                    "A hazard tile with a unit already standing on it does not count. That is an "
                    + "ordinary collision, not a hazard, so the planner looks elsewhere entirely.",
                },
                Parameters = new EnemyPlanParameters
                {
                    PrefersLethalAttack = false,
                    TargetSelection = EnemyTargetSelection.NearestBesideHazard,
                },
            };

            table[EnemyPlan.Warden] = new EnemyPlanDefinition(
                EnemyPlan.Warden,
                "the door",
                "Attack anything adjacent, and never move.",
                Ai.PlanWarden)
            {
                Priorities = Steps(
                    ("Attack an adjacent player unit",
                     "A player unit adjacent when it activates is attacked. It does not step to a "
                     + "better tile first, because it has no step."),
                    ("Otherwise do nothing at all",
                     "There is deliberately no closing branch: the archetype holds, and everything "
                     + "else on the board advances.")),
                Quirks = new[]
                {
                    "It spends only the action half of its activation, so a round in which nothing is "
                    + "adjacent to it costs it nothing and passes without an event.",
                },
            };

            table[EnemyPlan.Perch] = new EnemyPlanDefinition(
                EnemyPlan.Perch,
                "ledge-taking artillery",
                "The kiter's list with a climb in it, and a ledge it will not give up.",
                Ai.PlanPerch)
            {
                Priorities = Steps(
                    ("Shoot from where it stands",
                     "With no player unit adjacent and one inside range, it shoots the nearest without "
                     + "moving — harder from HighGround, because the ranged bonus is not a player-only "
                     + "rule."),
                    ("Break contact before anything else",
                     "A player unit adjacent and the shot is off: it retreats exactly as the kiter "
                     + "does, and shoots afterwards only if the retreat broke contact."),
                    ("Otherwise climb",
                     "Off HighGround with a ledge it can route to, it walks toward the nearest one by "
                     + "real path distance and fires on arrival if that puts someone in range."),
                    ("Otherwise hold the ledge, or advance to the band",
                     "Standing on HighGround with nothing in range it holds — it will not give the "
                     + "tile up. Off HighGround with no ledge reachable it falls back to the kiter's "
                     + "band (D-023).")),
                Quirks = new[]
                {
                    "It cannot be shoved up onto a ledge — the lip collides like a wall — but it can "
                    + "be shoved off one, with the displacement continuing.",
                },
                Parameters = new EnemyPlanParameters
                {
                    PreferredRangeLow = 2,
                    RetreatWhenAdjacent = true,
                },
            };

            table[EnemyPlan.Harrier] = new EnemyPlanDefinition(
                EnemyPlan.Harrier,
                "separator",
                "Shove a player unit away from its own side, otherwise close on the nearest.",
                Ai.PlanHarrier)
            {
                Priorities = Steps(
                    ("Flank and shove someone away from their own side",
                     "Scores every reachable flanking tile by how much further from its nearest ally "
                     + "the target would land, and takes the one that opens the biggest gap. A shove "
                     + "that would not actually move the target scores nothing and is never chosen. "
                     + "Ties break on lowest unit id, then direction order."),
                    ("Otherwise close on the nearest",
                     "With no separating shove available it walks toward the nearest player unit and "
                     + "tries again next round.")),
                Quirks = new[]
                {
                    "It never shoves into something solid: a wall, the board edge, a body or a ledge "
                    + "from below all leave the target where it started, which is worth zero to its "
                    + "score. It wants the tile you are not standing on, not the collision.",
                    "A lone player unit with no allies left is still shoved — with nobody to be "
                    + "separated from, any tile it did not choose counts as disruption.",
                },
                Parameters = new EnemyPlanParameters
                {
                    PrefersLethalAttack = false,
                    TargetSelection = EnemyTargetSelection.MostSeparable,
                },
            };

            table[EnemyPlan.Cooper] = new EnemyPlanDefinition(
                EnemyPlan.Cooper,
                "the barrel clock",
                "Shove a barrel down the fullest lane, else walk at one, else set one down.",
                Ai.PlanCooper)
            {
                // His list is about barrels and has no clause about player units at all, exactly like
                // the Raider's — so it is planned before anybody asks who the players are (D-041).
                IgnoresPlayerUnits = true,
                Parameters = new EnemyPlanParameters { PrefersLethalAttack = false },
                Priorities = Steps(
                    ("Shove the barrel it is standing next to",
                     "Picks the lane holding the most player units and rolls the barrel down it; a "
                     + "tie goes to the lowest unit id. The shove is the ordinary displacement — the "
                     + "pipeline never checks who pushed."),
                    ("Otherwise walk at the nearest barrel",
                     "The same breadth-first path field every archetype walks by (D-029), grown from "
                     + "the barrels. Two tiles a turn, so a barrel two lanes away costs him a round."),
                    ("Otherwise set a barrel down",
                     "On an adjacent open tile, shovable from his next activation — so a Cooper with "
                     + "nothing to roll spends a turn making something to roll.")),
                Quirks = new[]
                {
                    "He never attacks, never defends himself, and never takes the free finish on a "
                    + "clinging player — those are clauses about player units and this list has none, "
                    + "exactly like the Raider's (D-041, D-045).",
                    "Killing him stops the CLOCK, not the barrels: every barrel already on the board "
                    + "stays exactly where it is and still pops when something shoves it.",
                },
            };

            table[EnemyPlan.Inert] = new EnemyPlanDefinition(
                EnemyPlan.Inert,
                "an object",
                "Nothing. It never moves of its own accord and never acts on anybody.",
                Ai.PlanInert)
            {
                IgnoresPlayerUnits = true,
                Parameters = new EnemyPlanParameters { PrefersLethalAttack = false },
                Priorities = Steps(
                    ("Be shoved",
                     "Everything a barrel does to the board it does because somebody moved it. The "
                     + "displacement pipeline resolves the roll and never checks who pushed, so this "
                     + "branch belongs to whoever shoved it rather than to the barrel."),
                    ("Otherwise stand still",
                     "It has no activation worth spending and never takes one. An object's list is "
                     + "the shortest in the game and this is the whole of it.")),
                Quirks = new[]
                {
                    "It is on the enemy side so that shoves and collisions treat it like any other "
                    + "body, but clearing the board does not mean destroying it: a kill-all is won "
                    + "with barrels still standing (MASTER_DESIGN §6).",
                },
            };

            table[EnemyPlan.Raider] = new EnemyPlanDefinition(
                EnemyPlan.Raider,
                "siege chaff",
                "Claw the structure, else walk to it. The only list with no clause about player units.",
                Ai.PlanRaider)
            {
                Priorities = Steps(
                    ("Claw the structure it is standing next to",
                     "Ends its activation adjacent to a standing Protect structure and the structure "
                     + "takes its attack damage. It does not step to a better tile first."),
                    ("Otherwise walk at the nearest structure",
                     "Closes on the nearest Protect structure by the same breadth-first path field "
                     + "every other archetype walks by (D-029), and claws in the same activation if "
                     + "the walk ends adjacent."),
                    ("Otherwise stand still",
                     "With no Protect structure standing anywhere its list runs out and it holds. It "
                     + "re-checks every activation, so a structure appearing puts it back in motion.")),
                Quirks = new[]
                {
                    "It never attacks a player unit, never defends itself, and never takes the free "
                    + "finish on a clinging one — the free finish is a clause about player units, and "
                    + "this list has none (D-041, D-045).",
                    "It targets a tile, not a unit, so its intent carries the structure's coordinate "
                    + "and no target id.",
                },
                Parameters = new EnemyPlanParameters
                {
                    PrefersLethalAttack = false,
                    TargetSelection = EnemyTargetSelection.NearestStructure,
                },
                IgnoresPlayerUnits = true,
            };

            table[EnemyPlan.QuarryKing] = new EnemyPlanDefinition(
                EnemyPlan.QuarryKing,
                "boss — charge or punch",
                "The melee list with a charge branch in front of it, present only while the stat block "
                + "in force carries a standalone shove.",
                Ai.PlanQuarryKing)
            {
                IsBoss = true,
                Priorities = Steps(
                    ("Charge, once a stat block with a standalone shove is in force",
                     "Runs up to its movement in a straight line, stops adjacent to the first player "
                     + "unit on it and shoves. Taken only when the shove beats the swing it replaces — "
                     + "into another body, or over the lip of a pit — so on open ground it punches. "
                     + "Which branches exist is read off the stat block rather than off the archetype, "
                     + "so a phase swap adds the branch without there being two lists to keep in step "
                     + "(D-040)."),
                    ("Attack an adjacent player unit",
                     "A player unit adjacent when it activates is attacked, and shoved by the same "
                     + "swing when the stat block's attack carries a push. It does not reposition "
                     + "first."),
                    ("Otherwise close on the nearest",
                     "Walks toward the nearest player unit and attacks if the step lands it adjacent "
                     + "(D-022).")),
                Quirks = new[]
                {
                    "Everything it does is single-target and fully telegraphed, which is what keeps a "
                    + "boss inside the intent system rather than beside it.",
                    "The charge scores a shove by what it deals to both bodies, with a large bonus for "
                    + "the two endings a swing cannot produce: a drain lip, and a kill.",
                },
            };

            // The escort duckling. A genuinely new plan, added to prove the architecture's claim that
            // one costs one registration and one planner (component review, "Acceptance test for the
            // architecture itself"). It is fielded by no .fight file and belongs to no acquisition or
            // campaign pool — putting a neutral on a board is a design decision, not a test's.
            table[EnemyPlan.Escort] = new EnemyPlanDefinition(
                EnemyPlan.Escort,
                "survive and flee",
                "Not here to fight. It puts distance between itself and the nearest hostile, and holds "
                + "when it cannot improve on the tile it is standing on.",
                Ai.PlanEscort)
            {
                Priorities = Steps(
                    ("Break away from the nearest hostile",
                     "Retreats to the reachable tile that maximises the distance to the nearest "
                     + "hostile, ties broken by the greater total distance to all of them — the same "
                     + "scoring the kiter's retreat uses, run every activation rather than only when "
                     + "something is adjacent."),
                    ("Otherwise hold",
                     "With no reachable tile better than the one it is on, it stands still. Standing "
                     + "still wins every tie on cost, so it does not shuffle.")),
                Quirks = new[]
                {
                    "It has no attack and no shove: it never deals a point of damage, and every hazard "
                    + "it walks past is a hazard it can be herded into.",
                    "It flees hostiles, not danger. Nothing in the list reads the terrain, so a "
                    + "retreat that maximises distance can end on spikes or beside a drain.",
                },
                Parameters = new EnemyPlanParameters
                {
                    PrefersLethalAttack = false,
                    TargetSelection = EnemyTargetSelection.None,
                },
            };

            // MASTER_DESIGN §8.9, the Warrens boss. Four branches, published exactly as §8.9 prints
            // them, and ONE list across both phases: which branches are live is read off the stat
            // block in force, so the harness breaking adds the Stampede and turns the walk around
            // without there being a second list to keep in step (D-040's read, D-220's list).
            table[EnemyPlan.Rushmaster] = new EnemyPlanDefinition(
                EnemyPlan.Rushmaster,
                "boss — the crowd is the weapon",
                "Walks his own Bells while the harness holds. Once it breaks he runs at the thickest "
                + "part of the crowd and puts whatever is in front of him through whatever is behind "
                + "it — his own workers included.",
                Ai.PlanRushmaster)
            {
                IsBoss = true,
                Priorities = Steps(
                    ("Stampede, once the harness is off",
                     "Runs up to its movement in a straight line, stops adjacent to the first body on "
                     + "it — ally or not — and shoves it 2, with 2 contact damage on the way in. "
                     + "Taken only for one of four endings, ranked in this order: a drain entry, a "
                     + "collision with a unit, a collision with a Bell, a collision with debris. A run "
                     + "that produces none of them is not on the list and the next branch runs "
                     + "instead. The branch exists only while the stat block in force carries a "
                     + "standalone shove, which the harnessed block does not."),
                    ("Finish an adjacent body",
                     "An adjacent unit its own attack would take to zero is attacked before any "
                     + "other, counting whoever would actually take the blow rather than who was "
                     + "aimed at."),
                    ("Attack an adjacent body",
                     "Otherwise the first adjacent unit is attacked, and shoved by the same swing. It "
                     + "does not reposition first."),
                    ("Walk — Bell-ward, or at the crowd",
                     "While the harness holds he walks toward the nearest standing Bell and never "
                     + "swings at it. Once it breaks he walks toward the largest cluster instead — "
                     + "the body with the most company within 2 tiles, ties broken by proximity and "
                     + "then by lowest id — and attacks if the step lands him adjacent (D-022). With "
                     + "every Bell down and the harness still on, he closes on the nearest.")),
                Quirks = new[]
                {
                    "The Stampede reaches his own side. Everything else in the game that shoves an "
                    + "ally does it for nothing; this one carries the reserved bloody shoulder, so a "
                    + "worker he runs through takes the contact damage a player would.",
                    "He is telegraphed like everybody else. The run, the body it stops on, the shove "
                    + "and the whole collision chain are on the intent before he takes it.",
                },
            };

            return table;
        }
    }
}
