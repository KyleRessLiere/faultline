using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// The boss objective and the rout: "defeat or sweep him; the workers flee when he falls"
/// (MASTER_DESIGN §8.9), and §8's "Boss down → Rest (full heal) → the Molt", which leaves no room
/// for a mop-up phase between them.
/// </summary>
/// <remarks>
/// <para>
/// Two rules that had to land together. A boss board's objective is <b>not</b> Kill All, and until
/// D-223 <c>Objectives.Check</c> won on <c>!AnyEnemyLeft</c> under every objective — so a boss board
/// would have resolved correctly by accident, and the accident would have gone on covering for a
/// missing win condition. The tests below pin both halves: the boss objective wins on the body, and
/// clearing a board no longer wins the two objectives that are not about clearing it.
/// </para>
/// <para>
/// Everything here is reached by playing. The only arrangements are the fixture's own — starting hit
/// points and where the bodies stand — which is what a board file does too.
/// </para>
/// </remarks>
public class BossRoutTests
{
    // ---- the boss objective declares its own win ----------------------------------------------

    [Fact]
    public void BossObjective_TheCrowdComingDownIsNotProgress()
    {
        // The whole reason a boss board is not Kill All: putting his crew down, all of it, moves the
        // fight not one inch closer to being over.
        var state = BoardBuilder.Rows(
                "......",
                "......",
                "......")
            .PlayerA(UnitKind.Archer, 0, 1)
            .Enemy(UnitKind.Rushmaster, 5, 1)
            .Enemy(UnitKind.Husk, 3, 1, hp: 2)
            .Objective(ObjectiveKind.Boss)
            .Build();

        var archer = state.Find(UnitKind.Archer);
        var worker = state.Find(UnitKind.Husk);

        var after = state.Then(new AttackCommand(archer.Id, worker.Id));

        Assert.False(after.Get(worker.Id).IsAlive);
        Assert.False(Objectives.BossHasFallen(after));
        Assert.Equal(FightOutcome.InProgress, after.Outcome);
    }

    [Fact]
    public void BossFalling_EndsTheFight_AndTheRoutIsWhatEndsIt()
    {
        var state = BossBoard(bossHp: 3);
        var archer = state.Find(UnitKind.Archer);
        var boss = state.Find(UnitKind.Rushmaster);

        var result = state.Step(new AttackCommand(archer.Id, boss.Id));

        Assert.False(result.NewState.Get(boss.Id).IsAlive);
        Assert.Equal(FightOutcome.Won, result.NewState.Outcome);

        // The order is the ruling: the crowd breaks, and only then is the fight declared over.
        var order = result.Events.ToList();
        int routed = order.FindIndex(e => e is WorkersRouted);
        int won = order.FindIndex(e => e is FightWon);

        Assert.True(routed >= 0, "the rout must be announced");
        Assert.True(won > routed, "the win must resolve after the rout it caused");
    }

    [Fact]
    public void TheRout_RemovesTheStandingWorkersAndCancelsEveryMouth()
    {
        var state = BossBoard(bossHp: 3);
        var archer = state.Find(UnitKind.Archer);
        var boss = state.Find(UnitKind.Rushmaster);
        var standing = state.Units.Where(u => u.Kind == UnitKind.Husk && u.IsOnBoard).ToList();

        Assert.Equal(2, standing.Count);
        Assert.Single(Objectives.Schedule(state));

        var result = state.Step(new AttackCommand(archer.Id, boss.Id));
        var announcement = result.Single<WorkersRouted>();

        Assert.Equal(boss.Id, announcement.BossId);
        Assert.Equal(2, announcement.Fled);
        Assert.Equal(1, announcement.Cancelled);

        Assert.All(standing, w => Assert.False(result.NewState.Get(w.Id).IsOnBoard));
        Assert.Empty(Objectives.Schedule(result.NewState));

        // One body, one departure — so a renderer can animate them leaving rather than find them
        // missing on the next frame.
        Assert.Equal(
            standing.Select(w => w.Id).OrderBy(id => id.Value),
            result.All<UnitFled>().Select(e => e.UnitId).OrderBy(id => id.Value));
    }

    [Fact]
    public void TheRout_ResolvesTheWinImmediately_NotAtRoundEnd()
    {
        // "The turn limit should be pricing the boss fight; if it's also pricing cleanup, 6-8 rounds
        // isn't a target, it's a sum of two different fights."
        var state = BossBoard(bossHp: 3);
        var archer = state.Find(UnitKind.Archer);
        var boss = state.Find(UnitKind.Rushmaster);

        var result = state.Step(new AttackCommand(archer.Id, boss.Id));

        Assert.Equal(FightOutcome.Won, result.NewState.Outcome);
        Assert.Equal(state.Round, result.NewState.Round);
        Assert.False(result.Has<RoundEnded>());
    }

    [Fact]
    public void TheRout_IsARenderedBeat_NotASilentDespawn()
    {
        // Pillar 3: the crowd's disappearance is the fiction paying off the mechanic, so it has a
        // line of its own naming who fell, how many ran, and what will now never turn up.
        var state = BossBoard(bossHp: 3);
        var archer = state.Find(UnitKind.Archer);
        var boss = state.Find(UnitKind.Rushmaster);

        var result = state.Step(new AttackCommand(archer.Id, boss.Id));

        string rout = result.Events
            .Select(e => CombatLog.Detail(e, state))
            .Single(t => t.Contains("the crew breaks"));

        Assert.Contains("2 worker(s) scatter", rout);
        Assert.Contains("1 arrival(s) cancelled", rout);

        // And each body says so on its way out, in words that do not claim a kill.
        var fled = result.All<UnitFled>().Select(e => CombatLog.Detail(e, state)).ToList();

        Assert.Equal(2, fled.Count);
        Assert.All(fled, line => Assert.Contains("no one killed it", line));
    }

    [Fact]
    public void TheObjectivePanel_MeasuresTheBossAndNotTheCrowd()
    {
        var status = ObjectiveStatus.For(BossBoard(bossHp: 26));

        Assert.Equal(ObjectiveKind.Boss, status.Kind);
        Assert.Contains("Rushmaster", status.Goal);
        Assert.Contains("scatters", status.Goal);
        Assert.Equal("Rushmaster 26/26", status.Label);
        Assert.Equal(0, status.Progress);
        Assert.Equal(UnitTemplate.For(UnitKind.Rushmaster).MaxHp, status.Target);
    }

    // ---- a duck hanging when he falls goes home ----------------------------------------------

    [Fact]
    public void ADuckClingingWhenTheBossFalls_GoesHome()
    {
        // Reached by play: the boss's own swing shoves the Vanguard over the lip, and the Archer
        // finishes him in the same round. Cling resolves at round end and the fight ended before one
        // arrived, so nobody is swept — and swept is permanent and out of the gene pool, which is
        // why ending combat on a technicality is the wrong way to lose a duck (§3, D-222).
        var state = BoardBuilder.Rows(
                ".....",
                "O....",
                ".....")
            .PlayerA(UnitKind.Vanguard, 1, 1)
            .PlayerA(UnitKind.Archer, 4, 2)
            .Enemy(UnitKind.Rushmaster, 2, 1, hp: 3)
            .Objective(ObjectiveKind.Boss)
            .Active(Team.Enemy)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        var archer = state.Find(UnitKind.Archer);
        var boss = state.Find(UnitKind.Rushmaster);

        var hanging = state.Then(new AttackCommand(boss.Id, vanguard.Id));

        Assert.True(hanging.Get(vanguard.Id).Clinging);
        Assert.Equal(FightOutcome.InProgress, hanging.Outcome);

        var result = hanging.Step(new AttackCommand(archer.Id, boss.Id));
        var survivor = result.NewState.Get(vanguard.Id);

        Assert.Equal(FightOutcome.Won, result.NewState.Outcome);
        Assert.True(survivor.IsAlive);
        Assert.False(survivor.Voided);
        Assert.True(survivor.Clinging, "the grip is simply never resolved — no round end arrives");
        Assert.DoesNotContain(result.Events, e => e is Voided);
    }

    // ---- fleeing is not dying -----------------------------------------------------------------

    [Fact]
    public void AFleeingWorker_PaysNoDeathIncome_NotEvenChumTheWater()
    {
        // Chum the Water is the most visible condition that pays on a death, and the fight ending is
        // not a death: nothing was earned. If that reads badly in play it is a tuning note, not a bug.
        var state = BoardBuilder.Open(6, 3)
            .PlayerA(UnitKind.Threadcaster, 0, 1)
            .PlayerA(UnitKind.Archer, 0, 2)
            .Enemy(UnitKind.Rushmaster, 3, 2, hp: 3)
            .Enemy(UnitKind.Husk, 3, 1)
            .Objective(ObjectiveKind.Boss)
            .Build();

        var caster = state.Find(UnitKind.Threadcaster);
        var archer = state.Find(UnitKind.Archer);
        var boss = state.Find(UnitKind.Rushmaster);
        var worker = state.Find(UnitKind.Husk);

        state = state.WithWind(caster.Id, SecondWind.ChumTheWater);

        var dragged = state.Step(new AttackCommand(caster.Id, worker.Id, AttackMode.Pull));
        Assert.False(dragged.Has<VerveCharged>());

        var beforeTheFall = dragged.NewState.PassCurrent().NewState;

        // The condition is armed at the moment the boss falls: she moved this worker, this round.
        Assert.Equal(caster.Id, beforeTheFall.Get(worker.Id).DisplacedBy);
        Assert.Equal(beforeTheFall.Round, beforeTheFall.Get(worker.Id).DisplacedInRound);

        var result = beforeTheFall.Step(new AttackCommand(archer.Id, boss.Id));

        // It left the board — so the absence of income is about the rout, not about it never
        // having been there.
        Assert.Contains(result.All<UnitFled>(), e => e.UnitId == worker.Id);

        // And nothing died but the boss, so nothing paid.
        Assert.Equal(new[] { boss.Id }, result.All<UnitDowned>().Select(e => e.UnitId).ToArray());
        Assert.DoesNotContain(result.All<VerveCharged>(), c => c.Source == VerveSource.Chum);
        Assert.Equal(0, result.NewState.Get(caster.Id).Verve);
    }

    // ---- 0b: the universal kill-all clause is no longer universal -----------------------------

    [Fact]
    public void Destroy_ClearingTheBoardNoLongerWins_TheStructureStillHasToComeDown()
    {
        // §7: Destroy is "objective only; turn-limit expiry is a loss". D-032/D-034 made the
        // cleared-board win universal so a Destroy fight that killed its own ammunition could not
        // deadlock; §7 wants exactly that deadlock, resolved as a loss on the clock (D-223).
        var at = new Coord(4, 0);
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 2)
            .Objective(ObjectiveKind.Destroy, hp: 8, tiles: at)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        var husk = state.Find(UnitKind.Husk);

        var cleared = state.Then(new AttackCommand(vanguard.Id, husk.Id));

        Assert.False(cleared.Get(husk.Id).IsAlive);
        Assert.Equal(FightOutcome.InProgress, cleared.Outcome);
        Assert.True(cleared.StructureAt(at)!.IsStanding);

        // The one thing that ends it still ends it.
        var events = new System.Collections.Generic.List<GameEvent>();
        var down = Objectives.Damage(cleared, at, 8, DamageSource.Collision, events);

        Assert.Equal(FightOutcome.Won, Objectives.Check(down, false, events).Outcome);
    }

    [Fact]
    public void Protect_StillWinsOnAClearedBoard_BecauseItHasNoOtherWinCondition()
    {
        // The clause is not deleted, it is scoped. A Protect board's only win is the board emptying;
        // so are Survive's and Hold's before their deadlines, and Reach would deadlock without it.
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 2)
            .Objective(ObjectiveKind.Protect, hp: 12, tiles: new Coord(4, 0))
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        var husk = state.Find(UnitKind.Husk);

        Assert.Equal(FightOutcome.Won, state.Then(new AttackCommand(vanguard.Id, husk.Id)).Outcome);

        Assert.True(Objectives.ClearedBoardWins(ObjectiveKind.KillAll));
        Assert.True(Objectives.ClearedBoardWins(ObjectiveKind.Protect));
        Assert.True(Objectives.ClearedBoardWins(ObjectiveKind.Survive));
        Assert.True(Objectives.ClearedBoardWins(ObjectiveKind.Hold));
        Assert.True(Objectives.ClearedBoardWins(ObjectiveKind.Reach));
        Assert.False(Objectives.ClearedBoardWins(ObjectiveKind.Destroy));
        Assert.False(Objectives.ClearedBoardWins(ObjectiveKind.Boss));
    }

    [Fact]
    public void ABossObjective_IsAuthorableAndNamesNoTiles()
    {
        Assert.True(Objective.TryParseKind("boss", out var kind));
        Assert.Equal(ObjectiveKind.Boss, kind);
        Assert.Equal("boss", new Objective { Kind = ObjectiveKind.Boss }.ToValueText());
        Assert.False(new Objective { Kind = ObjectiveKind.Boss }.HasStructure);
        Assert.Equal(0, new Objective { Kind = ObjectiveKind.Boss }.Deadline);
    }

    [Fact]
    public void ABoardThatFieldsNoBoss_NeverWinsItsBossObjective()
    {
        // A boss objective written onto a board with nobody to kill runs out its clock rather than
        // winning on round one against an absence — the loud failure, not the silent one.
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 2)
            .Objective(ObjectiveKind.Boss)
            .Build();

        Assert.False(Objectives.BossHasFallen(state));

        var cleared = state.Then(
            new AttackCommand(state.Find(UnitKind.Vanguard).Id, state.Find(UnitKind.Husk).Id));

        Assert.Equal(FightOutcome.InProgress, cleared.Outcome);
    }

    // ---- fixture -------------------------------------------------------------------------------

    /// <summary>
    /// A boss board in miniature: him, two of his crew standing well clear of the shot so Crew Cover
    /// has nobody to swap in, and one more worker still to be rung for.
    /// </summary>
    /// <remarks>
    /// The wave is turned into a pending arrival the way <c>RushmasterTests.BellBoard</c> does it —
    /// <see cref="BoardBuilder"/> carries the schedule on the fight and <see cref="Game.Start"/> is
    /// what lands it, and these tests start mid-battle.
    /// </remarks>
    private static GameState BossBoard(int bossHp)
    {
        var state = BoardBuilder.Rows(
                "......",
                "......",
                "......")
            .PlayerA(UnitKind.Archer, 0, 1)
            .Enemy(UnitKind.Rushmaster, 3, 1, hp: bossHp)
            .Enemy(UnitKind.Husk, 5, 0)
            .Enemy(UnitKind.Husk, 5, 2)
            .Objective(ObjectiveKind.Boss)
            .Wave(3, new EnemySpawn(UnitKind.Husk, new Coord(5, 1)))
            .Build();

        var units = new System.Collections.Generic.List<Unit>(state.Units);
        var pending = new System.Collections.Generic.List<PendingReinforcement>();

        foreach (var wave in state.Fight.Waves)
        {
            foreach (var arrival in wave.Arrivals)
            {
                var id = new UnitId(units.Count);
                units.Add(Unit.FromTemplate(id, arrival.Kind, Team.Enemy));
                pending.Add(new PendingReinforcement(id, wave.Round, arrival.At));
            }
        }

        return state with { Units = units, Reinforcements = pending };
    }
}
