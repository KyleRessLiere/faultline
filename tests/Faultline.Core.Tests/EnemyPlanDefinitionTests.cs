using System;
using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// Component review, step 8. The enemy priority list used to exist twice — as executable C# in
/// <see cref="Ai"/> and as prose in <see cref="EnemyBehaviour"/> — with nothing but review keeping
/// them in step. <see cref="EnemyPlanDefinition"/> joins them, and these are the tests that make the
/// join load-bearing rather than decorative.
/// </summary>
/// <remarks>
/// Every one of them <b>enumerates the registry</b>. There is no hand-maintained
/// <c>[InlineData]</c> roster of plans anywhere in this file, because an index that has to be
/// remembered is an index that drifts from the thing it indexes (component review, "Validation
/// requirements").
/// </remarks>
public class EnemyPlanDefinitionTests
{
    private static readonly EnemyPlan[] AllPlans = (EnemyPlan[])Enum.GetValues(typeof(EnemyPlan));

    // ---- coverage: a plan cannot ship unregistered ----------------------------------------------

    [Fact]
    public void EveryEnemyPlan_ExceptNone_HasExactlyOneRegistration()
    {
        var registered = EnemyPlanDefinition.All();

        foreach (var plan in AllPlans)
        {
            if (plan == EnemyPlan.None)
            {
                Assert.Null(EnemyPlanDefinition.ForPlan(plan));
                continue;
            }

            var definition = EnemyPlanDefinition.ForPlan(plan);
            Assert.True(definition is not null, plan + " names no registration in EnemyPlanDefinition.");
            Assert.Equal(plan, definition!.Plan);
        }

        Assert.Equal(AllPlans.Length - 1, registered.Count);
        Assert.Equal(registered.Count, registered.Select(d => d.Plan).Distinct().Count());
        Assert.Equal(EnemyPlanDefinition.Plans, registered.Select(d => d.Plan).ToList());
    }

    [Fact]
    public void ForPlan_OnNone_IsNotAMissingRegistration_AndTheStrictOverloadThrows()
    {
        // Every player class carries EnemyPlan.None. Asking for its planner is a mistake; having no
        // registration for it is not.
        Assert.Null(EnemyPlanDefinition.ForPlan(EnemyPlan.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => EnemyPlanDefinition.For(EnemyPlan.None));
    }

    [Fact]
    public void EveryRegisteredPlan_IsNamedByAtLeastOneArchetype()
    {
        var named = new HashSet<EnemyPlan>();
        foreach (var definition in UnitDefinition.All())
        {
            named.Add(definition.Stats.Plan);
        }

        foreach (var plan in EnemyPlanDefinition.All())
        {
            Assert.True(
                named.Contains(plan.Plan),
                plan.Plan + " is registered but no stat block names it — an orphan priority list.");
        }
    }

    [Fact]
    public void EveryEnemyArchetype_NamesARegisteredPlan_AndEveryPlayerClassNamesNone()
    {
        foreach (var definition in UnitDefinition.All())
        {
            if (definition.Control == UnitControl.Enemy)
            {
                Assert.NotEqual(EnemyPlan.None, definition.Stats.Plan);
                Assert.NotNull(definition.Plan);
            }
            else
            {
                Assert.Equal(EnemyPlan.None, definition.Stats.Plan);
                Assert.Null(definition.Plan);
            }
        }
    }

    // ---- shape: a registration is actually usable ------------------------------------------------

    [Fact]
    public void EveryPlan_HasAPlannerNameSummaryAndAPriorityList()
    {
        foreach (var plan in EnemyPlanDefinition.All())
        {
            Assert.True(plan.Planner is not null, plan.Plan + " has no planner.");
            Assert.False(string.IsNullOrWhiteSpace(plan.Name), plan.Plan + " has no name.");
            Assert.False(string.IsNullOrWhiteSpace(plan.Summary), plan.Plan + " has no summary.");

            Assert.True(
                plan.Priorities.Count >= 2,
                $"{plan.Plan} registers {plan.Priorities.Count} branch(es); every planner in Ai.cs "
                + "has at least an act branch and a fallback.");

            Assert.NotEmpty(plan.Quirks);
        }
    }

    [Fact]
    public void PriorityBranchesAreNumberedFromOneWithoutGaps()
    {
        foreach (var plan in EnemyPlanDefinition.All())
        {
            for (int i = 0; i < plan.Priorities.Count; i++)
            {
                var step = plan.Priorities[i];
                Assert.Equal(i + 1, step.Order);
                Assert.False(string.IsNullOrWhiteSpace(step.Label), $"{plan.Plan} branch {i + 1} has no label.");
                Assert.False(string.IsNullOrWhiteSpace(step.Detail), $"{plan.Plan} branch {i + 1} has no detail.");
            }
        }
    }

    /// <summary>
    /// The same guard <c>EnemyBehaviourTests</c> keeps on the bestiary: a fragment that lost its
    /// <c>$</c> prefix compiles perfectly and ships a literal brace to whoever reads it.
    /// </summary>
    [Fact]
    public void NoPlanString_ShipsAnUninterpolatedPlaceholder()
    {
        foreach (var plan in EnemyPlanDefinition.All())
        {
            foreach (var fragment in Fragments(plan))
            {
                Assert.False(
                    fragment.IndexOf('{') >= 0 || fragment.IndexOf('}') >= 0,
                    $"{plan.Plan} ships a brace: a concatenated fragment is missing its $ prefix:\n{fragment}");
            }
        }
    }

    // ---- the join: prose and planner cannot drift apart ------------------------------------------

    /// <summary>
    /// The whole point of step 8, and the reason this registry can become the decision trace's data
    /// source. Each archetype writes its branches in its own words with its own numbers, but the
    /// <em>count</em> is the plan's, plus the one rescue slot <see cref="Ai"/> puts on the front of
    /// every list. Grow a planner a branch and every archetype running it fails here until its
    /// bestiary entry grows one too.
    /// </summary>
    [Fact]
    public void EveryArchetypesDocumentedList_HasOneStepPerRegisteredBranchPlusTheRescueSlot()
    {
        int checks = 0;

        foreach (var definition in UnitDefinition.All())
        {
            if (definition.Control != UnitControl.Enemy)
            {
                continue;
            }

            var plan = definition.Plan!;
            var behaviour = definition.Behaviour!;
            checks++;

            Assert.True(
                behaviour.Priorities.Count == plan.Priorities.Count + 1,
                $"{definition.Kind} documents {behaviour.Priorities.Count} priority steps but "
                + $"{plan.Plan} registers {plan.Priorities.Count} branches (+1 for the shared rescue "
                + "slot). The bestiary and the planner have drifted apart.");
        }

        Assert.Equal(EnemyBehaviour.EnemyKinds.Count, checks);
    }

    /// <summary>
    /// <see cref="EnemyPlanParameters.PrefersLethalAttack"/> would be a second source for something
    /// the stat block already decides, so it is pinned to it: a plan claims the kill clause exactly
    /// when its archetypes can actually have one.
    /// </summary>
    [Fact]
    public void PlanParameters_AgreeWithTheStatBlocksThatRunThem()
    {
        foreach (var definition in UnitDefinition.All())
        {
            if (definition.Control != UnitControl.Enemy)
            {
                continue;
            }

            var plan = definition.Plan!;
            var stats = definition.Stats;

            bool canKill = stats.Attack != AttackKind.None
                && stats.Damage > 0
                && !plan.IgnoresPlayerUnits;

            Assert.True(
                plan.Parameters.PrefersLethalAttack == canKill,
                $"{plan.Plan} claims PrefersLethalAttack {plan.Parameters.PrefersLethalAttack} but "
                + $"{definition.Kind} runs it with attack {stats.Attack} for {stats.Damage}.");

            Assert.True(
                plan.Parameters.PreferredRangeLow >= 1,
                plan.Plan + " wants to fight from inside its own tile.");
        }
    }

    /// <summary>
    /// One list has no clause about player units in it at all, and exactly one (D-041, D-045). The
    /// flag is the single source: <see cref="Ai"/> reads it to decline the free finish and to plan
    /// before the candidate search, and <see cref="EnemyBehaviour"/> reads it to word the rescue slot.
    /// </summary>
    [Fact]
    public void OnlyTheRaidersList_IgnoresPlayerUnits()
    {
        foreach (var plan in EnemyPlanDefinition.All())
        {
            Assert.Equal(plan.Plan == EnemyPlan.Raider, plan.IgnoresPlayerUnits);
        }
    }

    [Fact]
    public void TheRescueSlotIsWorded_FromTheRegistrationRatherThanFromTheArchetype()
    {
        foreach (var definition in UnitDefinition.All())
        {
            if (definition.Control != UnitControl.Enemy)
            {
                continue;
            }

            string opener = definition.Behaviour!.Priorities[0].Detail;
            bool canOutrankIt = definition.Stats.Attack != AttackKind.None
                && !definition.Plan!.IgnoresPlayerUnits;

            Assert.Contains(canOutrankIt ? "only for a kill" : "Nothing outranks it", opener);
        }
    }

    // ---- executable: a registration that cannot run is not a registration ------------------------

    /// <summary>
    /// The planner the registry hands out is invoked directly, on a board with a player unit in the
    /// open in front of the enemy. A plan that returns nothing, throws, or plans for somebody else is
    /// a registration pointing at the wrong method.
    /// </summary>
    [Fact]
    public void EveryRegisteredPlan_IsExecutableThroughItsOwnPlannerReference()
    {
        var covered = new HashSet<EnemyPlan>();

        foreach (var definition in UnitDefinition.All())
        {
            if (definition.Control != UnitControl.Enemy)
            {
                continue;
            }

            var plan = definition.Plan!;
            var state = BoardFor(definition);
            var enemy = state.Find(definition.Kind);
            var hostiles = Hostiles(state);

            var intent = plan.Planner(state, enemy, hostiles, hostiles);

            Assert.Equal(enemy.Id, intent.UnitId);
            Assert.Equal(definition.Kind, intent.Kind);
            covered.Add(plan.Plan);
        }

        Assert.Equal(EnemyPlanDefinition.All().Count, covered.Count);
    }

    /// <summary>
    /// The planner is a pure function of state (Brief §1): no <c>IRng</c>, no wall clock, no
    /// unordered iteration. Two runs over the same board therefore declare the identical intent.
    /// </summary>
    [Fact]
    public void EveryRegisteredPlan_PlansTheSameThingTwiceFromTheSameState()
    {
        foreach (var definition in UnitDefinition.All())
        {
            if (definition.Control != UnitControl.Enemy)
            {
                continue;
            }

            var state = BoardFor(definition);
            var enemy = state.Find(definition.Kind);

            Assert.Equal(Ai.Declare(state, enemy), Ai.Declare(state, enemy));
        }
    }

    /// <summary>
    /// <see cref="Ai"/> looks the plan up rather than switching on it, so the intent a planner
    /// produces when it is called directly and the intent the dispatcher produces are the same
    /// object graph. That equality is the join: one registration, one implementation, no second copy
    /// of the dispatch table.
    /// </summary>
    [Fact]
    public void AiDispatchesThroughTheRegistry_NotThroughASecondTable()
    {
        foreach (var definition in UnitDefinition.All())
        {
            if (definition.Control != UnitControl.Enemy)
            {
                continue;
            }

            var plan = definition.Plan!;
            var state = BoardFor(definition);
            var enemy = state.Find(definition.Kind);

            var direct = plan.Planner(
                state,
                enemy,
                plan.IgnoresPlayerUnits ? Array.Empty<Unit>() : Hostiles(state),
                plan.IgnoresPlayerUnits ? Array.Empty<Unit>() : Hostiles(state));

            Assert.Equal(direct, Ai.Declare(state, enemy));
        }
    }

    // ---- the new plan: one registration, one planner, and no board -------------------------------

    [Fact]
    public void TheEscortPlan_IsRegisteredWithItsOwnPlannerAndItsOwnBranches()
    {
        var plan = EnemyPlanDefinition.For(EnemyPlan.Escort);

        Assert.Equal("survive and flee", plan.Name);
        Assert.Equal(2, plan.Priorities.Count);
        Assert.False(plan.IgnoresPlayerUnits);
        Assert.False(plan.Parameters.PrefersLethalAttack);
        Assert.Equal(EnemyTargetSelection.None, plan.Parameters.TargetSelection);

        // The one archetype that names it, and nothing else does.
        var users = UnitDefinition.All().Where(d => d.Stats.Plan == EnemyPlan.Escort).ToList();
        Assert.Equal(new[] { UnitKind.EscortDuckling }, users.Select(d => d.Kind).ToArray());
    }

    [Fact]
    public void TheEscortDuckling_PutsDistanceBetweenItselfAndTheNearestHostile()
    {
        // Duckling at the left edge, a Vanguard four tiles away. Every reachable tile is scored by
        // distance to the nearest hostile, so it walks to the far corner rather than standing still.
        var state = BoardBuilder.Rows(".......")
            .Enemy(UnitKind.EscortDuckling, 3, 0)
            .PlayerA(UnitKind.Vanguard, 6, 0)
            .Active(Team.Enemy)
            .Build();

        var duckling = state.Find(UnitKind.EscortDuckling);
        var intent = Ai.Declare(state, duckling);

        Assert.Equal(IntentAction.Retreat, intent.Action);
        Assert.Equal(new Coord(0, 0), intent.MoveTo);
        Assert.Equal(0, intent.Damage);
        Assert.Null(intent.Displacement);
    }

    [Fact]
    public void TheEscortDuckling_HoldsWhenNoReachableTileIsBetter()
    {
        // Backed into the corner with the hostile on the other side of a wall it cannot improve on:
        // every reachable tile is at least as close to the Vanguard as the one it is standing on.
        var state = BoardBuilder.Rows("..")
            .Enemy(UnitKind.EscortDuckling, 0, 0)
            .PlayerA(UnitKind.Vanguard, 1, 0)
            .Active(Team.Enemy)
            .Build();

        var duckling = state.Find(UnitKind.EscortDuckling);

        Assert.Equal(IntentAction.Hold, Ai.Declare(state, duckling).Action);
        Assert.Equal(
            new EndActivationCommand(duckling.Id),
            Ai.Plan(state, duckling));
    }

    [Fact]
    public void TheEscortDuckling_NeverAttacksAndNeverShoves()
    {
        var stats = UnitTemplate.For(UnitKind.EscortDuckling);

        Assert.Equal(AttackKind.None, stats.Attack);
        Assert.Equal(0, stats.Damage);
        Assert.Equal(0, stats.BasicPush);
        Assert.Equal(0, stats.BasicPull);
        Assert.Equal(0, stats.AttackPush);
        Assert.Equal(0, stats.HazardRanks);
    }

    /// <summary>
    /// It is a proof of the architecture, not an addition to the roster. Fielded by no battle, named
    /// by no campaign squad — the same discipline the Braced Husk fixture is held to (D-144).
    /// </summary>
    [Fact]
    public void TheEscortDuckling_IsFieldedByNoFightAndNamedByNoCampaignSquad()
    {
        foreach (var parsed in FightLibrary.LoadAll())
        {
            var fight = parsed.Fight;
            if (fight is null)
            {
                continue;
            }

            Assert.DoesNotContain(fight.Enemies, e => e.Kind == UnitKind.EscortDuckling);
            Assert.DoesNotContain(fight.RosterA, k => k == UnitKind.EscortDuckling);
            Assert.DoesNotContain(fight.RosterB, k => k == UnitKind.EscortDuckling);
        }

        foreach (var campaign in CampaignLibrary.All())
        {
            Assert.DoesNotContain(campaign.Squad, k => k == UnitKind.EscortDuckling);
        }
    }

    // ---- helpers ---------------------------------------------------------------------------------

    // The board EnemyBehaviourTests uses for the same job: a player unit in the open, spikes for the
    // hazard flankers, a structure for the list that has no clause about player units, and a Move 0
    // archetype started already adjacent because it is not a chaser.
    private static GameState BoardFor(UnitDefinition definition)
    {
        int from = definition.Stats.Move == 0 ? 4 : 1;

        var builder = BoardBuilder.Rows(".....^.")
            .PlayerA(UnitKind.Vanguard, 5, 0)
            .Enemy(definition.Kind, from, 0)
            .Active(Team.Enemy);

        if (definition.IgnoresPlayerUnits)
        {
            builder = builder.Objective(ObjectiveKind.Protect, 0, 6, new Coord(6, 0));
        }

        return builder.Build();
    }

    private static IReadOnlyList<Unit> Hostiles(GameState state)
    {
        var list = new List<Unit>();
        foreach (var unit in state.Units)
        {
            if (unit.Team != Team.Enemy && unit.IsOnBoard && !unit.Clinging)
            {
                list.Add(unit);
            }
        }

        return list;
    }

    private static IEnumerable<string> Fragments(EnemyPlanDefinition plan)
    {
        yield return plan.Name;
        yield return plan.Summary;

        foreach (var step in plan.Priorities)
        {
            yield return step.Label;
            yield return step.Detail;
        }

        foreach (var quirk in plan.Quirks)
        {
            yield return quirk;
        }
    }
}
