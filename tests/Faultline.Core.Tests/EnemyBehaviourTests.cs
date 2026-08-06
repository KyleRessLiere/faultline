using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// M3. <see cref="EnemyBehaviour"/> is the bestiary's only source of rules text, so these tests
/// exist to make a newly added enemy fail loudly rather than ship undocumented: every enemy kind
/// must have a behaviour, every behaviour's numbers must come from <see cref="UnitTemplate"/>, and
/// every kind the bestiary lists must be a kind <see cref="Ai"/> can actually plan for.
/// </summary>
public class EnemyBehaviourTests
{
    private static readonly UnitKind[] AllKinds =
        (UnitKind[])Enum.GetValues(typeof(UnitKind));

    private static IReadOnlyList<UnitKind> PlayerKinds =>
        AbilityDefinition.All().Select(a => a.Kind).ToList();

    // ---- coverage: a new enemy cannot ship undocumented ---------------------------------------

    [Fact]
    public void EveryUnitKind_IsEitherAPlayerClassWithAnAbility_OrAnEnemyWithABehaviour()
    {
        var players = new HashSet<UnitKind>(PlayerKinds);
        var enemies = new HashSet<UnitKind>(EnemyBehaviour.EnemyKinds);

        foreach (var kind in AllKinds)
        {
            bool isPlayer = players.Contains(kind);
            bool isEnemy = enemies.Contains(kind);

            Assert.True(
                isPlayer ^ isEnemy,
                $"{kind} is neither a documented player class nor a documented enemy, or is both. "
                + "Add an AbilityDefinition or an EnemyBehaviour entry.");
        }

        Assert.Equal(AllKinds.Length, players.Count + enemies.Count);
    }

    [Fact]
    public void EveryEnemyKind_HasABehaviour()
    {
        foreach (var kind in EnemyBehaviour.EnemyKinds)
        {
            var behaviour = EnemyBehaviour.ForKind(kind);
            Assert.NotNull(behaviour);
            Assert.Equal(kind, behaviour!.Kind);
        }

        Assert.Equal(EnemyBehaviour.EnemyKinds.Count, EnemyBehaviour.All().Count);
    }

    [Fact]
    public void NoPlayerClass_HasAnEnemyBehaviour()
    {
        foreach (var kind in PlayerKinds)
        {
            Assert.Null(EnemyBehaviour.ForKind(kind));
        }
    }

    [Fact]
    public void All_ListsEveryEnemyInTheDeclaredOrder()
    {
        Assert.Equal(EnemyBehaviour.EnemyKinds, EnemyBehaviour.All().Select(b => b.Kind).ToList());
    }

    [Fact]
    public void ForKind_OnAnUndocumentedKind_ThrowsFromTheStrictOverload()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => EnemyBehaviour.For(UnitKind.Archer));
    }

    // ---- shape: the descriptor is actually usable ---------------------------------------------

    [Fact]
    public void EveryBehaviour_HasRoleSummaryPrioritiesQuirksAndCounterplay()
    {
        foreach (var behaviour in EnemyBehaviour.All())
        {
            Assert.False(string.IsNullOrWhiteSpace(behaviour.Role), $"{behaviour.Kind} has no role.");
            Assert.False(string.IsNullOrWhiteSpace(behaviour.Summary), $"{behaviour.Kind} has no summary.");

            Assert.True(
                behaviour.Priorities.Count >= 2,
                $"{behaviour.Kind} has {behaviour.Priorities.Count} priority step(s); every archetype in "
                + "Ai.cs has at least an act branch and a move branch.");

            Assert.NotEmpty(behaviour.Quirks);
            Assert.NotEmpty(behaviour.Counterplay);

            foreach (var quirk in behaviour.Quirks)
            {
                Assert.False(string.IsNullOrWhiteSpace(quirk));
            }

            foreach (var line in behaviour.Counterplay)
            {
                Assert.False(string.IsNullOrWhiteSpace(line));
            }
        }
    }

    [Fact]
    public void PriorityStepsAreNumberedFromOneWithoutGaps()
    {
        foreach (var behaviour in EnemyBehaviour.All())
        {
            for (int i = 0; i < behaviour.Priorities.Count; i++)
            {
                var step = behaviour.Priorities[i];
                Assert.Equal(i + 1, step.Order);
                Assert.False(string.IsNullOrWhiteSpace(step.Label), $"{behaviour.Kind} step {i + 1} has no label.");
                Assert.False(string.IsNullOrWhiteSpace(step.Detail), $"{behaviour.Kind} step {i + 1} has no detail.");
            }
        }
    }

    /// <summary>
    /// The rescue slot is one clause in <see cref="Ai.Compute"/> that every archetype runs, so it is
    /// step 1 of every documented list — including the Raider's, whose list has no clause about
    /// player units but plenty about its own side.
    /// </summary>
    [Fact]
    public void EveryPriorityList_OpensWithTheRescueSlot()
    {
        foreach (var behaviour in EnemyBehaviour.All())
        {
            var first = behaviour.Priorities[0];

            Assert.Equal(1, first.Order);
            Assert.Contains("clinging", first.Detail);
            Assert.Contains("entire activation", first.Detail);
            Assert.Contains("lowest unit id", first.Detail);

            // Below a lethal attack on a player unit — and the archetypes for which that clause is
            // vacuous say so: the ones that deal no damage, and the Raider, which never attacks a
            // player unit at all (D-045).
            bool canOutrankIt = behaviour.HasBasicAttack
                && behaviour.Template.Plan != EnemyPlan.Raider;

            Assert.Contains(canOutrankIt ? "only for a kill" : "Nothing outranks it", first.Detail);
        }
    }

    // ---- the numbers cannot drift from UnitTemplate --------------------------------------------

    [Fact]
    public void BehaviourReadsItsStatsFromTheTemplate()
    {
        foreach (var behaviour in EnemyBehaviour.All())
        {
            var template = UnitTemplate.For(behaviour.Kind);

            Assert.Same(template, behaviour.Template);
            Assert.Equal(template.Name, behaviour.Name);
            Assert.Equal(template.Attack != AttackKind.None, behaviour.HasBasicAttack);
            Assert.Equal(EnemyBehaviour.Describe(template), behaviour.AttackLine);
        }
    }

    /// <summary>
    /// The prose interpolates its figures from <see cref="UnitTemplate"/> rather than retyping
    /// them. This asserts the result: change a stat and the text has to move with it, or this fails.
    /// The attack figures are covered by <see cref="EnemyBehaviour.AttackLine"/>, which is derived;
    /// HP and Move are only ever stated in prose, so they are checked as whole phrases.
    /// </summary>
    [Fact]
    public void BehaviourTextQuotesTheTemplatesOwnNumbers()
    {
        foreach (var behaviour in EnemyBehaviour.All())
        {
            var template = UnitTemplate.For(behaviour.Kind);
            string text = Text(behaviour);

            Assert.Contains($"Move {template.Move}", text);
            Assert.Contains($"{template.MaxHp} HP", text);

            if (template.Attack == AttackKind.Ranged)
            {
                Assert.Contains($"range {template.Range}", text);
                Assert.Contains($"for {template.Damage}", text);
            }

            if (template.Attack == AttackKind.Melee)
            {
                Assert.Contains($"for {template.Damage}", text);
            }

            if (template.CanPullWithBasic)
            {
                Assert.Contains($"pulled {template.BasicPull} tiles", text);
            }

            if (template.CanPushWithBasic)
            {
                Assert.Contains($"pushes {template.BasicPush}", text);
            }
        }
    }

    // ---- the prose cannot drift from the rule constants either ---------------------------------

    /// <summary>
    /// The bug this exists for: a concatenated fragment that lost its <c>$</c> prefix compiles
    /// perfectly and ships the literal text <c>{runt.MaxHp}</c> to the player. Nothing in the
    /// language catches it, and nothing in a shape test catches it either, because the string is
    /// non-empty and reads almost right. A brace is never wanted in bestiary prose, so its presence
    /// is the whole signal.
    /// </summary>
    [Fact]
    public void NoBehaviourString_ShipsAnUninterpolatedPlaceholder()
    {
        foreach (var behaviour in EnemyBehaviour.All())
        {
            foreach (var fragment in Fragments(behaviour))
            {
                Assert.False(
                    fragment.IndexOf('{') >= 0 || fragment.IndexOf('}') >= 0,
                    $"{behaviour.Kind} ships a brace to the player — a concatenated fragment is "
                    + $"missing its $ prefix:\n{fragment}");
            }
        }
    }

    /// <summary>
    /// Every rule number the bestiary quotes, checked against the constant that actually holds it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The x2 rescale produced four rounds of stragglers because display strings were hand-written
    /// per file: the code moved, the prose did not, and nothing failed. The structural fix is
    /// interpolation — <c>EnemyBehaviour</c> now reads <see cref="Displacement.CollisionDamage"/> and
    /// friends rather than retyping them — and this is the guard that makes the next relapse loud.
    /// </para>
    /// <para>
    /// It reads the finished strings, so it does not care <em>how</em> the number got there. A phrase
    /// re-typed as a literal that happens to be right today fails here the moment the constant moves,
    /// which is the case the four previous rounds were made of. Each row is a phrase shape the
    /// bestiary actually uses; adding prose in a new shape means adding a row.
    /// </para>
    /// </remarks>
    [Fact]
    public void EveryRuleNumberInTheProse_MatchesTheConstantThatHoldsIt()
    {
        (string Pattern, int Expected, string Source)[] rules =
        {
            (@"(\d+)\s+fall damage", Displacement.FallDamage, "Displacement.FallDamage"),
            (@"spike tile is (\d+)", Displacement.SpikeDamage, "Displacement.SpikeDamage"),
            (@"spikes are (\d+)", Displacement.SpikeDamage, "Displacement.SpikeDamage"),
            (@"spikes \((\d+) damage\)", Displacement.SpikeDamage, "Displacement.SpikeDamage"),
            (@"collision \((\d+) damage\)", Displacement.CollisionDamage, "Displacement.CollisionDamage"),
            (@"board edge is (\d+)", Displacement.CollisionDamage, "Displacement.CollisionDamage"),
            (@"another unit is (\d+) damage", Displacement.CollisionDamage, "Displacement.CollisionDamage"),
            (@"deals (\d+) to both", Displacement.CollisionDamage, "Displacement.CollisionDamage"),
            (@"(\d+) damage to both", Displacement.CollisionDamage, "Displacement.CollisionDamage"),
            (@"(\d+) damage to each", Displacement.CollisionDamage, "Displacement.CollisionDamage"),
            (@"shove for (\d+) apiece", Displacement.CollisionDamage, "Displacement.CollisionDamage"),
            (@"\+(\d+) ranged", Combat.HighGroundBonus, "Combat.HighGroundBonus"),
            (@"toll of (\d+)", PathField.OccupiedPenalty, "PathField.OccupiedPenalty"),
        };

        int checks = 0;

        foreach (var behaviour in EnemyBehaviour.All())
        {
            string text = Text(behaviour);

            foreach (var rule in rules)
            {
                foreach (Match match in Regex.Matches(text, rule.Pattern))
                {
                    checks++;
                    Assert.True(
                        int.Parse(match.Groups[1].Value) == rule.Expected,
                        $"{behaviour.Kind} quotes \"{match.Value}\" but {rule.Source} is "
                        + $"{rule.Expected}. Interpolate the constant instead of retyping it.");
                }
            }
        }

        // A silent zero would mean the phrases were reworded out from under the guard rather than
        // corrected, which is the failure this test is least able to see for itself.
        Assert.True(checks >= rules.Length, $"only {checks} rule numbers were found in the bestiary.");
    }

    /// <summary>
    /// The two claims the rescale made outright false, pinned to the arithmetic they describe rather
    /// than to the sentence that was written once. A number can be corrected and still leave the
    /// sentence around it lying, which is what "still one short of killing it outright" did: the
    /// figure was stale and the conclusion drawn from it had inverted.
    /// </summary>
    [Theory]
    [InlineData(UnitKind.HeavyHusk)]
    [InlineData(UnitKind.Runt)]
    public void TheLethalityClaims_AgreeWithTheArithmeticTheyDescribe(UnitKind kind)
    {
        var template = UnitTemplate.For(kind);
        string text = Text(EnemyBehaviour.For(kind));

        if (kind == UnitKind.HeavyHusk)
        {
            int opener = UnitTemplate.For(UnitKind.Vanguard).Damage + Displacement.CollisionDamage;

            Assert.Contains($"exactly {opener}", text);
            Assert.Equal(
                opener >= template.MaxHp,
                Regex.IsMatch(text, @"kills? it outright"));
            Assert.DoesNotContain("short of killing", text);
            return;
        }

        Assert.Equal(
            Displacement.FallDamage >= template.MaxHp,
            text.Contains("the drop alone kills"));
        Assert.DoesNotContain("single point of fall damage", text);
    }

    /// <summary>
    /// The Warden's "pay the toll" line quotes a shot count, which is a consequence of two stats and
    /// was wrong at both scales. It must be the smallest number of shots that actually gets there.
    /// </summary>
    [Fact]
    public void TheWardensTollLine_QuotesTheShotCountItActuallyTakes()
    {
        var warden = UnitTemplate.For(UnitKind.Warden);
        var match = Regex.Match(
            Text(EnemyBehaviour.For(UnitKind.Warden)), @"(\d+) Archer shots \((\d+) each\)");

        Assert.True(match.Success, "the Warden no longer states what it costs to shoot down.");

        int shots = int.Parse(match.Groups[1].Value);
        int each = int.Parse(match.Groups[2].Value);

        Assert.Equal(UnitTemplate.For(UnitKind.Archer).Damage, each);
        Assert.True(shots * each >= warden.MaxHp, $"{shots} shots of {each} do not fell {warden.MaxHp} HP.");
        Assert.True((shots - 1) * each < warden.MaxHp, $"{shots} shots of {each} is one more than it takes.");
    }

    [Fact]
    public void ArchetypesWithNoAttack_SayThatTheyDealNoDamage()
    {
        foreach (var behaviour in EnemyBehaviour.All())
        {
            if (behaviour.HasBasicAttack)
            {
                continue;
            }

            Assert.Equal(0, behaviour.Template.Damage);
            Assert.Contains("no attack", Text(behaviour), StringComparison.OrdinalIgnoreCase);
        }
    }

    [Theory]
    [InlineData(UnitKind.Husk, "melee · 2 dmg")]
    [InlineData(UnitKind.Lobber, "range 3 · 2 dmg")]
    [InlineData(UnitKind.Anchor, "melee · 4 dmg")]
    [InlineData(UnitKind.Grappler, "no attack · range 3 · pull 2")]
    [InlineData(UnitKind.Stalker, "no attack · melee · push 1")]
    public void AttackLine_ReadsAsTheStatTableDoes(UnitKind kind, string expected)
    {
        Assert.Equal(expected, EnemyBehaviour.For(kind).AttackLine);
    }

    [Fact]
    public void Describe_CoversThePlayerClassesToo()
    {
        Assert.Equal("melee · 2 dmg · push 1", EnemyBehaviour.Describe(UnitTemplate.For(UnitKind.Vanguard)));
        Assert.Equal("range 3 · 4 dmg", EnemyBehaviour.Describe(UnitTemplate.For(UnitKind.Archer)));
        Assert.Equal(
            "range 3 · 2 dmg · may pull 1 instead",
            EnemyBehaviour.Describe(UnitTemplate.For(UnitKind.Threadcaster)));
        Assert.Equal("melee · 2 dmg", EnemyBehaviour.Describe(UnitTemplate.For(UnitKind.Wardbearer)));
    }

    [Fact]
    public void RoleFor_AnswersForEveryKind()
    {
        foreach (var kind in AllKinds)
        {
            var role = EnemyBehaviour.RoleFor(kind);
            Assert.False(string.IsNullOrWhiteSpace(role), $"{kind} has no role.");
            Assert.NotEqual("unclassified", role);
        }
    }

    // ---- the bestiary cannot list an enemy the planner ignores ---------------------------------

    /// <summary>
    /// <see cref="Ai"/> dispatches on the plan named by the stat block and falls through to Hold. A
    /// new enemy with a behaviour but no planner branch would document a unit that stands still
    /// forever, so every documented enemy is put on a board with a player unit in front of it and
    /// must plan something.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A Move 0 archetype is started already adjacent. It is not a chaser and has no closing branch
    /// to assert — asking it to reach a target four tiles away would be asserting that Move 0 does
    /// not work. The teeth are unchanged: a static enemy with no planner branch still falls through
    /// to Hold with a player unit touching it, and still fails here.
    /// </para>
    /// <para>
    /// An archetype whose priority list has no clause about player units gets the thing its list is
    /// actually about put in front of it instead — for the Raider, a Protect structure (D-041). The
    /// player unit stays on the board either way, so "it ignores you and plans anyway" is what is
    /// being asserted, not "it was given something easier".
    /// </para>
    /// </remarks>
    [Fact]
    public void EveryDocumentedEnemy_HasAPlannerBranch()
    {
        foreach (var behaviour in EnemyBehaviour.All())
        {
            int from = behaviour.Template.Move == 0 ? 4 : 1;

            // Spikes at (5,0) give the Stalker a hazard to shove into; everyone else ignores them.
            var builder = BoardBuilder.Rows(".....^.")
                .PlayerA(UnitKind.Vanguard, 5, 0)
                .Enemy(behaviour.Kind, from, 0)
                .Active(Team.Enemy);

            if (behaviour.Template.Plan == EnemyPlan.Raider)
            {
                builder = builder.Objective(ObjectiveKind.Protect, 0, 6, new Coord(6, 0));
            }

            var state = builder.Build();

            var enemy = state.Find(behaviour.Kind);
            var intent = Ai.Declare(state, enemy);

            Assert.True(
                intent.Action != IntentAction.Hold,
                $"{behaviour.Kind} is documented in the bestiary but Ai.Compute has no branch for it — "
                + "it holds position with a player unit in the open in front of it.");

            Assert.NotEqual(new EndActivationCommand(enemy.Id), Ai.Plan(state, enemy));
        }
    }

    private static string Text(EnemyBehaviour behaviour)
    {
        var parts = new List<string> { behaviour.Role, behaviour.Summary, behaviour.AttackLine };
        foreach (var step in behaviour.Priorities)
        {
            parts.Add(step.Label);
            parts.Add(step.Detail);
        }

        parts.AddRange(behaviour.Quirks);
        parts.AddRange(behaviour.Counterplay);
        return string.Join(" ", parts);
    }

    /// <summary>Every string a UI would render for this enemy, one at a time.</summary>
    private static IEnumerable<string> Fragments(EnemyBehaviour behaviour)
    {
        yield return behaviour.Role;
        yield return behaviour.Summary;
        yield return behaviour.AttackLine;

        foreach (var step in behaviour.Priorities)
        {
            yield return step.Label;
            yield return step.Detail;
        }

        foreach (var quirk in behaviour.Quirks)
        {
            yield return quirk;
        }

        foreach (var line in behaviour.Counterplay)
        {
            yield return line;
        }
    }

    [Fact]
    public void DescribeClimb_SaysNothingForAnybody_BecauseNobodyPaysToClimb()
    {
        // The surcharge is gone on both sides (MASTER_DESIGN §3, locked u), so there is no perk left
        // to print. Kept as a test rather than deleted: the sentence lives in Core precisely so it
        // cannot go on being displayed after the rule behind it changes.
        foreach (var kind in System.Enum.GetValues(typeof(UnitKind)).Cast<UnitKind>())
        {
            Assert.Null(EnemyBehaviour.DescribeClimb(UnitTemplate.For(kind)));
        }
    }

}
