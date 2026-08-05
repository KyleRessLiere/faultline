using System;
using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// Component review, steps 9 and 10. <see cref="UnitDefinition"/> is the one place that knows an
/// archetype's statistics, lifecycle, plan reference and presentation all exist; these are the tests
/// that make a half-finished registration fail rather than ship.
/// </summary>
/// <remarks>
/// Every one of them <b>enumerates the registry</b> — <c>foreach</c> over
/// <see cref="UnitDefinition.All()"/>, never a hand-maintained <c>[InlineData]</c> roster. The
/// review's "Validation requirements" list is the checklist: a complete definition exists, it is
/// classified player-controlled or enemy-controlled, its presentation name exists, its glyph is not
/// a placeholder, its statistics are valid, and every enemy has a registered and executable plan.
/// </remarks>
public class UnitDefinitionTests
{
    private static readonly UnitKind[] AllKinds = (UnitKind[])Enum.GetValues(typeof(UnitKind));

    // ---- a complete definition exists, for every archetype --------------------------------------

    [Fact]
    public void EveryUnitKind_HasExactlyOneDefinition()
    {
        var all = UnitDefinition.All();

        foreach (var kind in AllKinds)
        {
            var definition = UnitDefinition.For(kind);
            Assert.Equal(kind, definition.Kind);
        }

        Assert.Equal(AllKinds.Length, all.Count);
        Assert.Equal(all.Count, all.Select(d => d.Kind).Distinct().Count());
        Assert.Equal(UnitDefinition.Kinds, all.Select(d => d.Kind).ToList());
    }

    [Fact]
    public void For_OnAKindThatIsNotOne_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => UnitDefinition.For((UnitKind)999));
    }

    // ---- classified, one way or the other -------------------------------------------------------

    [Fact]
    public void EveryUnit_IsPlayerControlledOrEnemyControlled_AndNeverBoth()
    {
        int players = 0;
        int enemies = 0;

        foreach (var definition in UnitDefinition.All())
        {
            if (definition.Control == UnitControl.Player)
            {
                players++;
                Assert.NotEmpty(definition.Abilities);
                Assert.Null(definition.Plan);
                Assert.Null(definition.Behaviour);
                Assert.Equal(EnemyPlan.None, definition.Stats.Plan);
            }
            else
            {
                enemies++;
                Assert.Empty(definition.Abilities);
                Assert.NotNull(definition.Plan);
                Assert.NotNull(definition.Behaviour);
                Assert.NotEqual(EnemyPlan.None, definition.Stats.Plan);
            }
        }

        Assert.Equal(AllKinds.Length, players + enemies);
        Assert.Equal(EnemyBehaviour.EnemyKinds.Count, enemies);
        Assert.Equal(AbilityDefinition.All().Select(a => a.Kind).Distinct().Count(), players);
    }

    // ---- presentation ----------------------------------------------------------------------------

    [Fact]
    public void EveryUnit_HasAPresentationNameThroughTheNamingLayer()
    {
        foreach (var definition in UnitDefinition.All())
        {
            Assert.False(string.IsNullOrWhiteSpace(definition.Name), definition.Kind + " has no name.");
            Assert.Equal(Naming.Of(definition.Kind), definition.Name);
            Assert.False(string.IsNullOrWhiteSpace(definition.Role), definition.Kind + " has no role.");
            Assert.NotEqual("unclassified", definition.Role);
        }

        // The name is decoupled from the identifier on purpose (MASTER_DESIGN §15), and this is the
        // archetype that proves the layer is real rather than a pass-through.
        Assert.Equal("Fisher", UnitDefinition.For(UnitKind.Threadcaster).Name);
    }

    /// <summary>
    /// The failure this exists for: a shell that keeps its own glyph table falls through to a
    /// placeholder the moment an archetype is added, silently, and three of them are on the board
    /// today reading as "??". A glyph in Core is authored for every kind or the build's tests fail.
    /// </summary>
    [Fact]
    public void EveryUnit_HasARealGlyph_NotAPlaceholder()
    {
        var seen = new Dictionary<string, UnitKind>();

        foreach (var definition in UnitDefinition.All())
        {
            string glyph = definition.Glyph;

            Assert.False(string.IsNullOrWhiteSpace(glyph), definition.Kind + " has no glyph.");
            Assert.Equal(2, glyph.Length);
            Assert.DoesNotContain("?", glyph);
            Assert.False(
                seen.TryGetValue(glyph, out var other),
                $"{definition.Kind} and {other} both draw as \"{glyph}\".");

            seen[glyph] = definition.Kind;
        }
    }

    [Fact]
    public void EveryUnit_DescribesItsBasicActionFromItsOwnStatBlock()
    {
        foreach (var definition in UnitDefinition.All())
        {
            Assert.Same(UnitTemplate.For(definition.Kind), definition.Stats);
            Assert.Equal(EnemyBehaviour.Describe(definition.Stats), definition.BasicAction);
            Assert.Equal(EnemyBehaviour.DescribeClimb(definition.Stats), definition.Climb);
        }
    }

    // ---- statistics are valid --------------------------------------------------------------------

    [Fact]
    public void EveryUnitsStatisticsAreValid()
    {
        foreach (var definition in UnitDefinition.All())
        {
            var stats = definition.Stats;
            string who = definition.Kind.ToString();

            Assert.True(stats.MaxHp > 0, who + " has no hit points.");
            Assert.True(stats.Move >= 0, who + " has negative movement.");
            Assert.True(stats.Range >= 0, who + " has negative range.");
            Assert.True(stats.Damage >= 0, who + " deals negative damage.");
            Assert.True(stats.Footing >= 0, who + " holds negative Footing.");
            Assert.True(stats.PushResistance >= 0, who + " has negative push resistance.");
            Assert.True(stats.MinRange >= 0, who + " has a negative minimum range.");
            Assert.True(stats.BasicPull >= 0 && stats.BasicPush >= 0 && stats.AttackPush >= 0, who);
            Assert.True(stats.HazardRanks >= 0, who + " hunts a negative number of hazard tiers.");
            Assert.False(string.IsNullOrWhiteSpace(stats.RawName), who + " has no table label.");

            // An archetype either has an attack that does something, or has none and says so.
            Assert.Equal(stats.Attack != AttackKind.None, stats.Damage > 0);

            if (stats.Attack == AttackKind.Ranged)
            {
                Assert.True(stats.Range >= 1, who + " shoots at range 0.");
                Assert.True(stats.MinRange < stats.Range, who + " cannot reach its own minimum range.");
            }

            if (stats.Attack == AttackKind.None)
            {
                Assert.Equal(0, stats.AttackPush);
            }

            // A second stat block only makes sense with a threshold inside the first one's health.
            if (stats.Enraged is not null)
            {
                Assert.True(stats.EnrageAt > 0 && stats.EnrageAt < stats.MaxHp, who);
                Assert.Equal(definition.Kind, stats.Enraged.Kind);
                Assert.Equal(stats.Plan, stats.Enraged.Plan);
            }
            else
            {
                Assert.Equal(0, stats.EnrageAt);
            }
        }
    }

    // ---- every enemy has a registered and executable plan ----------------------------------------

    [Fact]
    public void EveryEnemy_HasARegisteredPlanThatActuallyRuns()
    {
        int checked_ = 0;

        foreach (var definition in UnitDefinition.All())
        {
            if (definition.Control != UnitControl.Enemy)
            {
                continue;
            }

            var plan = definition.Plan!;
            Assert.Equal(definition.Stats.Plan, plan.Plan);
            Assert.True(plan.Planner is not null, definition.Kind + " names a plan with no planner.");

            int from = definition.Stats.Move == 0 ? 4 : 1;
            var builder = BoardBuilder.Rows(".....^.")
                .PlayerA(UnitKind.Vanguard, 5, 0)
                .Enemy(definition.Kind, from, 0)
                .Active(Team.Enemy);

            if (definition.IgnoresPlayerUnits)
            {
                builder = builder.Objective(ObjectiveKind.Protect, 0, 6, new Coord(6, 0));
            }

            var state = builder.Build();
            var enemy = state.Find(definition.Kind);

            // A plan that holds with a player unit in the open in front of it is a plan the
            // dispatcher never found — the same teeth EnemyBehaviourTests keeps on the bestiary.
            Assert.NotEqual(IntentAction.Hold, Ai.Declare(state, enemy).Action);
            checked_++;
        }

        Assert.Equal(EnemyBehaviour.EnemyKinds.Count, checked_);
    }

    // ---- lifecycle -------------------------------------------------------------------------------

    [Fact]
    public void EveryLifecycleEntry_SaysWhatItDoesAndHasSomethingToDo()
    {
        foreach (var definition in UnitDefinition.All())
        {
            foreach (var entry in definition.Lifecycle.Entries)
            {
                Assert.False(
                    string.IsNullOrWhiteSpace(entry.Detail),
                    $"{definition.Kind} has a {entry.Moment} entry with no detail.");
                Assert.DoesNotContain("{", entry.Detail);
                Assert.DoesNotContain("}", entry.Detail);

                Assert.True(
                    entry.CustomRule != UnitRule.None || entry.Effects.Count > 0,
                    $"{definition.Kind}'s {entry.Moment} entry does nothing at all.");

                if (entry.Moment == UnitLifecycleMoment.OnHpThreshold)
                {
                    Assert.Equal(definition.Stats.EnrageAt, entry.Threshold);
                    Assert.True(entry.Threshold > 0);
                }
                else
                {
                    Assert.Equal(0, entry.Threshold);
                }
            }

            // The accessors are filters over one list, so nothing can be registered at a moment the
            // definition does not expose.
            int byMoment = definition.Lifecycle.OnFightStart.Count
                + definition.Lifecycle.OnDeploy.Count
                + definition.Lifecycle.OnActivationEnd.Count
                + definition.Lifecycle.OnRoundEnd.Count
                + definition.Lifecycle.OnHpThreshold.Count;

            Assert.Equal(definition.Lifecycle.Entries.Count, byMoment);
        }
    }

    /// <summary>
    /// The moments are the five that have a consumer in the rules today. The component review's own
    /// sketch also lists <c>OnActivationStart</c> and <c>OnRoundStart</c>; nothing happens at either,
    /// and the same review's blacklist names anticipating undesigned mechanics as a way this fails.
    /// </summary>
    [Fact]
    public void TheLifecycleMomentsAreTheFiveWithConsumers_AndNoSpeculativeOnes()
    {
        Assert.Equal(
            new[]
            {
                UnitLifecycleMoment.OnFightStart,
                UnitLifecycleMoment.OnDeploy,
                UnitLifecycleMoment.OnActivationEnd,
                UnitLifecycleMoment.OnRoundEnd,
                UnitLifecycleMoment.OnHpThreshold,
            },
            (UnitLifecycleMoment[])Enum.GetValues(typeof(UnitLifecycleMoment)));
    }

    /// <summary>
    /// The correction the component review got wrong. Its worked example reads "Quarry King —
    /// OnFightStart: Grant 3 negating Footing"; both halves are false. The shell is the boss's own
    /// mechanism carried by his stat block, so granting it as an effect would hand him a second
    /// helping on top of what <see cref="Unit"/> already copies off the template, and D-143 retired
    /// the negating token — his refusals are spent one per displacement instance like everybody
    /// else's.
    /// </summary>
    [Fact]
    public void TheQuarryKingsShell_IsItsOwnMechanismAndIsNeverGrantedAsAFootingEffect()
    {
        var king = UnitDefinition.For(UnitKind.QuarryKing);
        var opening = king.Lifecycle.OnFightStart;

        var shell = Assert.Single(opening);
        Assert.Equal(UnitRule.QuarryKingShell, shell.CustomRule);
        Assert.Empty(shell.Effects);
        Assert.Contains("own mechanism", shell.Detail);
        Assert.Contains("not granted by an effect", shell.Detail);

        // The number is the stat block's, quoted rather than retyped.
        Assert.Contains(king.Stats.Footing.ToString(), shell.Detail);
        Assert.Equal(3, king.Stats.Footing);
    }

    [Fact]
    public void NoUnitAnywhere_GrantsItselfFootingAtFightStart()
    {
        foreach (var definition in UnitDefinition.All())
        {
            foreach (var entry in definition.Lifecycle.Entries)
            {
                Assert.DoesNotContain(entry.Effects, e => e is FootingEffect);
            }
        }
    }

    [Fact]
    public void TheStatBlockIsTheOnlySourceOfWhoStartsWithFooting()
    {
        foreach (var definition in UnitDefinition.All())
        {
            bool declaresFooting = definition.Lifecycle.OnFightStart.Any(
                e => e.CustomRule == UnitRule.StatBlockFooting
                    || e.CustomRule == UnitRule.QuarryKingShell);

            Assert.Equal(definition.Stats.Footing > 0, declaresFooting);

            // And anything holding Footing is stripped by a drain lip at round end — universal, not
            // an archetype privilege (D-144).
            Assert.Equal(
                definition.Stats.Footing > 0,
                definition.Lifecycle.OnRoundEnd.Any(e => e.CustomRule == UnitRule.LipStrip));
        }
    }

    [Fact]
    public void OnlyATwoPhaseArchetype_RegistersAThresholdSwap()
    {
        foreach (var definition in UnitDefinition.All())
        {
            Assert.Equal(
                definition.Stats.Enraged is not null,
                definition.Lifecycle.OnHpThreshold.Any(e => e.CustomRule == UnitRule.PhaseSwap));
        }

        Assert.Equal(
            new[] { UnitKind.QuarryKing },
            UnitDefinition.All()
                .Where(d => d.Lifecycle.OnHpThreshold.Count > 0)
                .Select(d => d.Kind)
                .ToArray());
    }

    [Fact]
    public void EveryArmedEnemy_ClawsAStructureItFinishesItsActivationBeside()
    {
        foreach (var definition in UnitDefinition.All())
        {
            bool claws = definition.Control == UnitControl.Enemy
                && definition.Stats.Attack != AttackKind.None
                && definition.Stats.Damage > 0;

            Assert.Equal(
                claws,
                definition.Lifecycle.OnActivationEnd.Any(e => e.CustomRule == UnitRule.SiegeClaw));
        }
    }

    [Fact]
    public void NothingRegistersAnythingAtDeployYet()
    {
        // Honest emptiness. The moment exists because deployment does; a unit gets an entry when a
        // rule needs one, and never before.
        foreach (var definition in UnitDefinition.All())
        {
            Assert.Empty(definition.Lifecycle.OnDeploy);
        }
    }

    // ---- the stat-only variant, as pure data -----------------------------------------------------

    /// <summary>
    /// The review's own acceptance item: "a stat-only enemy variant can be added by selecting an
    /// existing enemy plan". The Heavy Husk is that variant — it shares the Husk's plan
    /// <em>object</em>, not a copy of it, carries no lifecycle of its own, and differs in nothing but
    /// hit points.
    /// </summary>
    [Fact]
    public void TheHeavyHusk_IsAHuskWithDifferentNumbersAndNothingElse()
    {
        var husk = UnitDefinition.For(UnitKind.Husk);
        var heavy = UnitDefinition.For(UnitKind.HeavyHusk);

        Assert.Same(husk.Plan, heavy.Plan);
        Assert.DoesNotContain(heavy.Lifecycle.Entries, e => e.CustomRule == UnitRule.QuarryKingShell);
        Assert.Equal(husk.Control, heavy.Control);

        // The only differences are numbers on the row.
        Assert.NotEqual(husk.Stats.MaxHp, heavy.Stats.MaxHp);
        Assert.Equal(husk.Stats.Move, heavy.Stats.Move);
        Assert.Equal(husk.Stats.Damage, heavy.Stats.Damage);
        Assert.Equal(husk.Stats.Attack, heavy.Stats.Attack);
        Assert.Equal(husk.Stats.Footing, heavy.Stats.Footing);
    }

    /// <summary>
    /// The Heavy Husk and the Braced Husk are two different answers to two different questions, and
    /// keeping them apart is deliberate (D-144). The Heavy Husk is a <em>fielded</em> hit-point
    /// variant — <c>nv-05-numbers.fight</c> spawns it as the control that survives the collision
    /// killing the Runts around it — and it holds no Footing. The Braced Husk is the reserved
    /// stacked-Footing fixture, identical to a Husk in every number but Footing, and fielded by
    /// nothing. Giving the Heavy Husk Footing 2, as MASTER_DESIGN Design Log (u) words it, would have
    /// changed what a shipped board fields from inside a rules change: the D-092 trap.
    /// </summary>
    [Fact]
    public void TheHeavyHuskAndTheBracedHusk_AreDifferentVariantsOfDifferentThings()
    {
        var husk = UnitDefinition.For(UnitKind.Husk).Stats;
        var heavy = UnitDefinition.For(UnitKind.HeavyHusk).Stats;
        var braced = UnitDefinition.For(UnitKind.BracedHusk).Stats;

        // Heavy: the hit-point variant. More HP than a Husk, no Footing at all.
        Assert.True(heavy.MaxHp > husk.MaxHp);
        Assert.Equal(0, heavy.Footing);

        // Braced: the Footing variant. A Husk's hit points exactly, and a stack on top.
        Assert.Equal(husk.MaxHp, braced.MaxHp);
        Assert.Equal(2, braced.Footing);

        // Same plan for all three: the priority list is shared, never copied.
        Assert.Equal(husk.Plan, heavy.Plan);
        Assert.Equal(husk.Plan, braced.Plan);
    }

    [Fact]
    public void TheBracedHuskIsFieldedByNothing_AndTheHeavyHuskIsFieldedBySomething()
    {
        bool heavyIsFielded = false;

        foreach (var parsed in FightLibrary.LoadAll())
        {
            var fight = parsed.Fight;
            if (fight is null)
            {
                continue;
            }

            Assert.DoesNotContain(fight.Enemies, e => e.Kind == UnitKind.BracedHusk);

            if (fight.Enemies.Any(e => e.Kind == UnitKind.HeavyHusk))
            {
                heavyIsFielded = true;
            }
        }

        Assert.True(
            heavyIsFielded,
            "the Heavy Husk is fielded by no battle, which changes the reasoning behind D-144.");
    }

    // ---- the new archetype, as data ---------------------------------------------------------------

    [Fact]
    public void TheEscortDuckling_IsANeutralAddedByNamingAPlan_WithNoLifecycleOfItsOwn()
    {
        var duckling = UnitDefinition.For(UnitKind.EscortDuckling);

        Assert.Equal(UnitControl.Enemy, duckling.Control);
        Assert.Equal(EnemyPlan.Escort, duckling.Stats.Plan);
        Assert.NotNull(duckling.Plan);
        Assert.NotNull(duckling.Behaviour);
        Assert.Empty(duckling.Abilities);

        // Nothing hangs off it: no Footing, no phase, and no claw, because it has no attack.
        Assert.Empty(duckling.Lifecycle.Entries);
        Assert.Equal("none", duckling.BasicAction);
        Assert.Null(duckling.Climb);
    }
}
