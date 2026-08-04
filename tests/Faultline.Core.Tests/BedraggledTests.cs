using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// The downed return (MASTER_DESIGN §3, locked 2026-08-02(d)). A duck that hit zero comes back on a
/// quarter of its ceiling and misses the first activation its side would have spent on it — and is
/// an entirely ordinary unit in every other respect while it waits.
/// </summary>
public class BedraggledTests
{
    // --- The formula --------------------------------------------------------------------------

    [Theory]
    [InlineData(14, 4)]   // Vanguard and Wardbearer, doubled scale
    [InlineData(8, 2)]    // Archer and Fisher
    [InlineData(2, 1)]    // the floor: a quarter of 2 is not zero
    [InlineData(1, 1)]
    [InlineData(4, 1)]
    [InlineData(5, 2)]
    [InlineData(16, 4)]
    [InlineData(17, 5)]
    [InlineData(20, 5)]
    public void ReturningHp_IsAQuarterOfTheCeilingRoundedUpNeverBelowOne(int maxHp, int expected)
    {
        Assert.Equal(expected, Bedraggled.ReturningHp(maxHp));
    }

    [Fact]
    public void ReturningHp_IsAFormula_SoARaisedCeilingRaisesTheReturn()
    {
        // Camp offers raise max HP (MASTER_DESIGN §8.5). A lookup keyed on archetype would hand an
        // upgraded duck the base class's return, and the upgrade would silently not apply at the one
        // moment it matters most.
        int baseline = UnitTemplate.For(UnitKind.Vanguard).MaxHp;

        Assert.Equal(14, baseline);
        Assert.Equal(4, Bedraggled.ReturningHp(baseline));
        Assert.Equal(5, Bedraggled.ReturningHp(baseline + 4));
        Assert.Equal(6, Bedraggled.ReturningHp(baseline + 8));
    }

    [Fact]
    public void ReturningHp_NeverExceedsTheCeilingItIsAQuarterOf()
    {
        for (int max = 1; max <= 64; max++)
        {
            int back = Bedraggled.ReturningHp(max);
            Assert.True(back >= 1, max + " returned " + back);
            Assert.True(back <= max, max + " returned " + back);
            Assert.True(back * 4 >= max, "a quarter of " + max + " rounded down to " + back);
            Assert.True(max <= Bedraggled.Divisor || (back - 1) * 4 < max, max + " rounded up too far");
        }
    }

    // --- The scheduler ------------------------------------------------------------------------

    [Fact]
    public void Bedraggled_TakesNoActivationSlot_AndIsNeverOfferedACommand()
    {
        var state = TwoOnOneSide(bedraggledFirst: true);
        var recovering = state.Units[0];

        Assert.True(recovering.Bedraggled);
        Assert.False(Game.CanActivate(recovering));

        // Not "it passes": the pool it would have activated out of never opens for it at all, so it
        // is not even offered the bare pass every other unit always has.
        TestPlay.AssertNotLegal(state, new EndActivationCommand(recovering.Id));
        Assert.DoesNotContain(
            Game.LegalCommands(state),
            c => c is MoveCommand m && m.UnitId.Equals(recovering.Id));
    }

    [Fact]
    public void Bedraggled_TheSlotIsOmittedNotPassed_SoTheSideHasOneFewerActivationInRoundOne()
    {
        var withRecovery = ActivationsInRoundOne(TwoOnOneSide(bedraggledFirst: true));
        var withoutRecovery = ActivationsInRoundOne(TwoOnOneSide(bedraggledFirst: false));

        Assert.Equal(withoutRecovery[Team.PlayerA] - 1, withRecovery[Team.PlayerA]);
        Assert.Equal(1, withRecovery[Team.PlayerA]);

        // The enemy's slots are the enemy's business, and none of them moved.
        Assert.Equal(withoutRecovery[Team.Enemy], withRecovery[Team.Enemy]);
    }

    [Fact]
    public void BothBedraggled_OnOneSide_GivesThatSideNoActivationsInRoundOne()
    {
        var state = BoardBuilder.Open(9, 9)
            .PlayerA(UnitKind.Vanguard, 0, 0, bedraggled: true)
            .PlayerA(UnitKind.Archer, 1, 0, bedraggled: true)
            .PlayerB(UnitKind.Wardbearer, 0, 8)
            .Enemy(UnitKind.Husk, 8, 0)
            .Enemy(UnitKind.Husk, 8, 8)
            .Active(Team.PlayerA)
            .Build();

        var taken = ActivationsInRoundOne(state);

        Assert.False(taken.ContainsKey(Team.PlayerA));
        Assert.Equal(1, taken[Team.PlayerB]);
        Assert.Equal(2, taken[Team.Enemy]);
    }

    [Fact]
    public void Bedraggled_AlternationCompactsAroundTheMissingSlot()
    {
        // Player A's Vanguard is recovering, so A's only body is the Archer. The order that comes out
        // is the ordinary alternation over the sides that still have somebody — the same compaction a
        // clinging unit already produces, not a hole with a turn in it.
        var state = BoardBuilder.Open(9, 9)
            .PlayerA(UnitKind.Vanguard, 0, 0, bedraggled: true)
            .PlayerA(UnitKind.Archer, 1, 0)
            .PlayerB(UnitKind.Wardbearer, 0, 8)
            .PlayerB(UnitKind.Threadcaster, 1, 8)
            .Enemy(UnitKind.Husk, 8, 0)
            .Enemy(UnitKind.Husk, 8, 8)
            .Active(Team.PlayerA)
            .Build();

        Assert.Equal(
            new[] { Team.PlayerA, Team.Enemy, Team.PlayerB, Team.Enemy, Team.PlayerB },
            OrderInRoundOne(state));
    }

    [Fact]
    public void Bedraggled_EnemySlotsAreUntouched()
    {
        var state = TwoOnOneSide(bedraggledFirst: true);
        var enemies = state.Units.Where(u => u.Team == Team.Enemy).ToList();

        foreach (var enemy in enemies)
        {
            Assert.False(enemy.Bedraggled);
            Assert.True(Game.CanActivate(enemy));
        }

        Assert.Equal(enemies.Count, ActivationsInRoundOne(state)[Team.Enemy]);
    }

    // --- The strip ----------------------------------------------------------------------------

    [Fact]
    public void TheStrip_ShowsTheMissingSlotAsAGapRatherThanSilence()
    {
        var state = TwoOnOneSide(bedraggledFirst: true);
        var recovering = state.Units[0];

        var gap = TurnOrder.Upcoming(state)
            .Single(e => e.Kind == ActivationKind.Skipped && e.Round == state.Round);

        Assert.Equal(recovering.Id, gap.UnitId);
        Assert.Equal(ActivationSkip.Bedraggled, gap.Skip);
        Assert.Equal(Team.PlayerA, gap.Team);
    }

    [Fact]
    public void TheStrip_ShowsBothGapsWhenASideHasNoSlotsAtAllToHangThemBeside()
    {
        var state = BoardBuilder.Open(9, 9)
            .PlayerA(UnitKind.Vanguard, 0, 0, bedraggled: true)
            .PlayerA(UnitKind.Archer, 1, 0, bedraggled: true)
            .PlayerB(UnitKind.Wardbearer, 0, 8)
            .Enemy(UnitKind.Husk, 8, 0)
            .Active(Team.PlayerA)
            .Build();

        var gaps = TurnOrder.Upcoming(state)
            .Where(e => e.Kind == ActivationKind.Skipped && e.Round == state.Round)
            .ToList();

        Assert.Equal(2, gaps.Count);
        Assert.All(gaps, g => Assert.Equal(ActivationSkip.Bedraggled, g.Skip));
    }

    [Fact]
    public void TheStrip_DistinguishesRecoveringFromClinging()
    {
        var state = BoardBuilder.Rows(
                ".........",
                ".O.......",
                ".........",
                ".........",
                ".........")
            .PlayerA(UnitKind.Vanguard, 0, 0, bedraggled: true)
            .PlayerA(UnitKind.Archer, 3, 0)
            .PlayerB(UnitKind.Wardbearer, 0, 4)
            .Enemy(UnitKind.Husk, 8, 0)
            .Active(Team.PlayerA)
            .Build();

        state = state.WithUnit(state.Find(UnitKind.Archer) with
        {
            Position = new Coord(1, 1),
            Clinging = true,
            ClingingSinceRound = state.Round,
        });

        var skips = TurnOrder.Upcoming(state)
            .Where(e => e.Kind == ActivationKind.Skipped && e.Round == state.Round)
            .ToDictionary(e => e.UnitId!.Value, e => e.Skip);

        Assert.Equal(ActivationSkip.Bedraggled, skips[state.Find(UnitKind.Vanguard).Id]);
        Assert.Equal(ActivationSkip.Clinging, skips[state.Find(UnitKind.Archer).Id]);
    }

    [Fact]
    public void TheStrip_PeeksTheSlotThatComesBackNextRound()
    {
        // The peek clears the state the way BeginRound does, so the round-2 opening shows the duck's
        // slot returning. A peek that hid it would be advertising a shortage that is about to end.
        var state = TwoOnOneSide(bedraggledFirst: true);

        var next = TurnOrder.Upcoming(state).Where(e => e.Round == state.Round + 1).ToList();

        Assert.Contains(next, e => e.Kind == ActivationKind.PlayerSlot && e.Team == Team.PlayerA);
        Assert.DoesNotContain(next, e => e.Kind == ActivationKind.Skipped);
    }

    // --- When it clears -----------------------------------------------------------------------

    [Fact]
    public void Bedraggled_ClearsWhenRoundTwoBegins_AndTheDuckActivatesNormally()
    {
        var state = TwoOnOneSide(bedraggledFirst: true);
        var id = state.Units[0].Id;

        state = PlayOutRoundOne(state);

        Assert.Equal(2, state.Round);
        Assert.False(state.Get(id).Bedraggled);
        Assert.True(Game.CanActivate(state.Get(id)));
    }

    [Fact]
    public void Bedraggled_CostsExactlyOneActivation_NotOnePerRound()
    {
        var state = TwoOnOneSide(bedraggledFirst: true);
        var id = state.Units[0].Id;

        var roundOne = ActivationsInRoundOne(state);
        state = PlayOutRoundOne(state);
        var roundTwo = ActivationsInRoundOne(state);

        Assert.Equal(1, roundOne[Team.PlayerA]);
        Assert.Equal(2, roundTwo[Team.PlayerA]);
        Assert.False(state.Get(id).Bedraggled);
    }

    // --- Full physics while it waits ------------------------------------------------------------

    [Fact]
    public void Bedraggled_IsDamageableLikeAnyOtherUnit()
    {
        var state = BoardBuilder.Open(5, 5)
            .Enemy(UnitKind.Husk, 1, 1)
            .PlayerA(UnitKind.Vanguard, 2, 1, hp: 4, bedraggled: true)
            .Active(Team.Enemy)
            .Build();

        var husk = state.Units[0];
        var duck = state.Units[1];

        var result = state.Step(new AttackCommand(husk.Id, duck.Id));

        Assert.True(result.Has<UnitDamaged>());
        Assert.True(result.NewState.Get(duck.Id).Hp < 4);
    }

    [Fact]
    public void Bedraggled_IsDisplaceableLikeAnyOtherUnit()
    {
        var state = BoardBuilder.Open(5, 5)
            .PlayerA(UnitKind.Vanguard, 1, 1)
            .PlayerB(UnitKind.Archer, 2, 1, bedraggled: true)
            .Enemy(UnitKind.Husk, 4, 4)
            .Build();

        var duck = state.Units[1];
        var events = new List<GameEvent>();
        var after = Displacement.Resolve(
            state, duck.Id, new Coord(1, 1), DisplacementKind.Push, 1, false, events);

        Assert.Single(events.OfType<UnitPushed>());
        Assert.Equal(new Coord(3, 1), after.Get(duck.Id).Position);
    }

    [Fact]
    public void Bedraggled_SweptIntoADrain_IsVoidedForTheRun()
    {
        // The two states are unrelated and the permanent one still wins. A duck on its comeback round
        // shoved into a drain is out of the run and out of the gene pool, exactly like anyone else.
        var state = BoardBuilder.Rows(
                ".....",
                ".....",
                "..O..",
                ".....",
                ".....")
            .PlayerA(UnitKind.Vanguard, 4, 0)
            .PlayerB(UnitKind.Archer, 1, 2, bedraggled: true)
            .Enemy(UnitKind.Husk, 4, 4)
            .Build();

        var duck = state.Units[1];
        var events = new List<GameEvent>();
        var next = Displacement.Resolve(
            state, duck.Id, new Coord(0, 2), DisplacementKind.Push, 1, false, events);

        Assert.True(next.Get(duck.Id).Clinging);

        // Both rounds out, and nothing rescues it: it loses its grip at the end of the round after
        // the one it fell in (D-016).
        var swept = PlayOutRounds(next, 2);

        Assert.True(swept.Get(duck.Id).Voided);
        Assert.False(swept.Get(duck.Id).IsAlive);
    }

    [Fact]
    public void Bedraggled_IsARescuerAndARescueeLikeAnyOtherUnit()
    {
        var state = BoardBuilder.Rows(
                ".....",
                ".O...",
                ".....",
                ".....",
                ".....")
            .PlayerA(UnitKind.Vanguard, 2, 1)
            .PlayerA(UnitKind.Archer, 1, 2, bedraggled: true)
            .Enemy(UnitKind.Husk, 4, 4)
            .Active(Team.PlayerA)
            .Build();

        var clinging = state.Units[0];
        var recovering = state.Units[1];

        state = state.WithUnit(clinging with
        {
            Position = new Coord(1, 1),
            Clinging = true,
            ClingingSinceRound = state.Round,
        });

        // It is a legal target for a rescue — being recovering does not make a duck unsavable.
        Assert.True(Pits.IsEligibleRescuer(state.Get(recovering.Id), state.Get(clinging.Id)));
    }

    [Fact]
    public void Bedraggled_KeepsItsMeterAndSpendsItTheRoundItComesBack()
    {
        var state = BoardBuilder.Open(5, 5)
            .PlayerA(UnitKind.Vanguard, 1, 1, bedraggled: true)
            .PlayerB(UnitKind.Archer, 0, 4)
            .Enemy(UnitKind.Husk, 4, 4)
            .Active(Team.PlayerA)
            .Build();

        var id = state.Units[0].Id;
        state = state.WithUnit(state.Get(id) with { Verve = Verve.CostOf(VerveSpend.WreckingWeight) });

        // Held, but unspendable — because there is no activation to spend it in.
        TestPlay.AssertNotLegal(state, new SpendVerveCommand(id, VerveSpend.WreckingWeight));

        state = PlayOutRoundOne(state);

        Assert.Equal(Verve.CostOf(VerveSpend.WreckingWeight), state.Get(id).Verve);
        TestPlay.AssertLegal(state, new SpendVerveCommand(id, VerveSpend.WreckingWeight));
    }

    // --- No AI preference -----------------------------------------------------------------------

    [Fact]
    public void ThePlanner_TreatsARecoveringDuckExactlyLikeAnyOther()
    {
        // Two identical targets at identical distance. Flipping one to Bedraggled must not move the
        // planner's hand by so much as a tiebreak.
        var board = BoardBuilder.Open(5, 5)
            .Enemy(UnitKind.Husk, 2, 2)
            .PlayerA(UnitKind.Vanguard, 0, 2)
            .PlayerB(UnitKind.Vanguard, 4, 2)
            .Active(Team.Enemy);

        var plain = board.Build().WithIntents();
        var husk = plain.Units[0];
        var plan = Ai.Plan(plain, plain.Get(husk.Id));

        foreach (int index in new[] { 1, 2 })
        {
            var marked = plain.WithUnit(plain.Units[index] with { Bedraggled = true }).WithIntents();
            Assert.Equal(plan, Ai.Plan(marked, marked.Get(husk.Id)));
        }
    }

    [Fact]
    public void ThePlanner_TreatsARecoveringDuckExactlyLikeAnyOther_AcrossAWholeFight()
    {
        var fight = FightLibrary.ById("first-contact");

        var plain = Game.Start(fight, seed: 4242).NewState;
        var marked = Game.Start(
            fight,
            seed: 4242,
            new SquadLoadout { BedraggledA = new[] { true, true }, BedraggledB = new[] { true, true } })
            .NewState;

        // Same board, same enemies, same seed; the only difference is a flag the planner may not see.
        // Every enemy's declared intent must therefore be identical.
        var plainIntents = Ai.DeclareAll(Deploy(plain), new List<GameEvent>()).Intents;
        var markedIntents = Ai.DeclareAll(Deploy(marked), new List<GameEvent>()).Intents;

        Assert.Equal(plainIntents, markedIntents);
    }

    [Fact]
    public void NoPlannerSourceFileMentionsTheState()
    {
        // The behavioural tests above prove the shipped planner ignores it. This is what stops the
        // next named preference clause — "finish the wounded one" — from being written at all. The
        // lethal-attack clause finding a low-HP target on its own is allowed; keying on the state is
        // not (MASTER_DESIGN §3).
        var rules = Path.Combine(RepoRoot(), "src", "Faultline.Core", "Rules");

        foreach (var file in new[] { "Ai.cs", "Threat.cs", "EnemyIntent.cs", "IntentAction.cs" })
        {
            var source = File.ReadAllText(Path.Combine(rules, file));
            Assert.DoesNotContain("Bedraggled", source, StringComparison.Ordinal);
        }
    }

    // --- The run seam ---------------------------------------------------------------------------

    [Fact]
    public void Run_DownInOneFight_ReturnsBedraggledInTheNextAndNormalInTheOneAfter()
    {
        var run = RunFixture.StartedInFirstFight(out var vanguard);
        run = RunFixture.Deploy(run);
        run = RunFixture.HurtTo(run, vanguard, 0);
        run = RunFixture.WinTheFight(run);

        // Fight N+1: back on a quarter, and holding no slot in round 1.
        run = RunFixture.Enter(run);
        var returning = RunFixture.OnBoard(run, vanguard);
        Assert.True(returning.Bedraggled);
        Assert.Equal(Bedraggled.ReturningHp(returning.MaxHp), returning.Hp);

        run = RunFixture.Deploy(run);
        Assert.False(Game.CanActivate(RunFixture.OnBoard(run, vanguard)));

        // Fight N+2: not downed again, so an ordinary wounded duck with a slot of its own.
        run = RunFixture.HurtTo(run, vanguard, 3);
        run = RunFixture.WinTheFight(run);
        run = RunFixture.Enter(run);

        var later = RunFixture.OnBoard(run, vanguard);
        Assert.False(later.Bedraggled);
        Assert.Equal(3, later.Hp);
    }

    [Fact]
    public void Run_DownedAgainWhileBedraggled_ReturnsBedraggledAgainAndNoWorse()
    {
        var run = RunFixture.StartedInFirstFight(out var vanguard);
        run = RunFixture.Deploy(run);
        run = RunFixture.HurtTo(run, vanguard, 0);
        run = RunFixture.WinTheFight(run);

        run = RunFixture.Enter(run);
        run = RunFixture.Deploy(run);
        int firstReturn = RunFixture.OnBoard(run, vanguard).Hp;

        run = RunFixture.HurtTo(run, vanguard, 0);
        run = RunFixture.WinTheFight(run);

        run = RunFixture.Enter(run);
        var second = RunFixture.OnBoard(run, vanguard);

        // The penalty does not compound. Same quarter, same one skipped activation.
        Assert.Equal(firstReturn, second.Hp);
        Assert.True(second.Bedraggled);
    }

    [Fact]
    public void Run_ARestClearsTheDownedMark_SoNothingComesBackBedraggledFromOne()
    {
        // D-053 stands: a rest is the clean return, and this ruling only changes what happens when
        // there is no rest between the downing and the next fight.
        var run = RunFixture.PlayForwardToRest(RunFixture.Start());
        var member = run.Squad[0];

        run = run with
        {
            Squad = run.Squad
                .Select(u => u.Id.Equals(member.Id) ? u with { Hp = 0, Status = RunUnitStatus.Downed } : u)
                .ToList(),
        };

        run = RunFixture.Enter(run);

        Assert.Equal(RunUnitStatus.Ready, run.FindUnit(member.Id)!.Status);
        Assert.False(run.FindUnit(member.Id)!.ReturnsBedraggled);
    }

    [Fact]
    public void Run_ReplaysAcrossTheSeamWithARecoveringDuckOnTheBoard()
    {
        var (played, log) = RunFixture.PlayWholeRun(RunFixture.Seed);
        var replayed = Campaign.Replay(CampaignLibrary.Faultline, RunFixture.Seed, log);

        Assert.Equal(played, replayed);
        Assert.Equal(played.GetHashCode(), replayed.GetHashCode());
    }

    [Fact]
    public void Bedraggled_IsPartOfTheStateHash()
    {
        // A field the hash cannot see is a field a replay can silently disagree about.
        var plain = TwoOnOneSide(bedraggledFirst: false);
        var marked = TwoOnOneSide(bedraggledFirst: true);

        Assert.NotEqual(plain.GetHashCode(), marked.GetHashCode());
        Assert.NotEqual(plain, marked);
    }

    // --- Fixtures -------------------------------------------------------------------------------

    /// <summary>
    /// Player A fields two, one of them optionally recovering; player B fields one and the enemy two.
    /// Everybody is far enough apart that nothing resolves on its own.
    /// </summary>
    private static GameState TwoOnOneSide(bool bedraggledFirst) =>
        BoardBuilder.Open(9, 9)
            .PlayerA(UnitKind.Vanguard, 0, 0, bedraggled: bedraggledFirst)
            .PlayerA(UnitKind.Archer, 1, 0)
            .PlayerB(UnitKind.Wardbearer, 0, 8)
            .Enemy(UnitKind.Husk, 8, 0)
            .Enemy(UnitKind.Husk, 8, 8)
            .Active(Team.PlayerA)
            .Build();

    /// <summary>How many activations each side actually spends before the round turns over.</summary>
    private static Dictionary<Team, int> ActivationsInRoundOne(GameState state)
    {
        var taken = new Dictionary<Team, int>();
        foreach (var team in Order(state, out _))
        {
            taken.TryGetValue(team, out int count);
            taken[team] = count + 1;
        }

        return taken;
    }

    private static IReadOnlyList<Team> OrderInRoundOne(GameState state) => Order(state, out _);

    /// <summary>Passes every activation of the round, recording whose slot each one was.</summary>
    private static IReadOnlyList<Team> Order(GameState state, out GameState after)
    {
        var order = new List<Team>();
        int round = state.Round;

        while (state.Round == round && state.Outcome == FightOutcome.InProgress)
        {
            var unit = state.Units.FirstOrDefault(u =>
                u.Team == state.ActiveTeam && Game.CanActivate(u) && !u.HasActivated);

            if (unit is null)
            {
                break;
            }

            order.Add(state.ActiveTeam);
            state = state.Then(new EndActivationCommand(unit.Id));
        }

        after = state;
        return order;
    }

    private static GameState PlayOutRoundOne(GameState state) => PlayOutRounds(state, 1);

    private static GameState PlayOutRounds(GameState state, int rounds)
    {
        for (int i = 0; i < rounds && state.Outcome == FightOutcome.InProgress; i++)
        {
            Order(state, out state);
        }

        return state;
    }

    /// <summary>Plays deployment out with the first legal command each time.</summary>
    private static GameState Deploy(GameState state)
    {
        for (int i = 0; i < 40 && state.Phase == Phase.Deployment; i++)
        {
            var legal = Game.LegalCommands(state);
            if (legal.Count == 0)
            {
                break;
            }

            state = state.Then(legal[0]);
        }

        return state;
    }

    private static string RepoRoot([CallerFilePath] string here = "")
    {
        var dir = Directory.GetParent(here);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "FIGHT_FORMAT.md")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return dir!.FullName;
    }
}
