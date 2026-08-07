using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// The Rushmaster, the Warrens boss (MASTER_DESIGN §8.9): a boss whose crowd is both his armour and
/// the player's ammunition.
/// </summary>
/// <remarks>
/// Day Shift only. Night Shift and the Bellhand are deliberately unbuilt — §8.9's own reason is that
/// adding the escalations before the core is measured makes the numbers unreadable.
/// </remarks>
public class RushmasterTests
{
    // ---- E4: the shape of the fight ----------------------------------------------------------

    [Fact]
    public void TheHarnessedBlock_IsTheOneEightNinePrints()
    {
        var boss = UnitTemplate.For(UnitKind.Rushmaster);

        Assert.Equal(26, boss.MaxHp);
        Assert.Equal(1, boss.Move);
        Assert.Equal(AttackKind.Melee, boss.Attack);
        Assert.Equal(4, boss.Damage);
        Assert.Equal(1, boss.AttackPush);
        Assert.Equal(1, boss.PushResistance);
        Assert.Equal(1, boss.Footing);
        Assert.Equal(13, boss.EnrageAt);
    }

    [Fact]
    public void TheCutLooseBlock_IsMoveThreeAndAStampede()
    {
        var cutLoose = UnitTemplate.For(UnitKind.Rushmaster).Enraged;

        Assert.NotNull(cutLoose);
        Assert.Equal(3, cutLoose!.Move);
        Assert.Equal(2, cutLoose.BasicPush);

        // Everything else is unchanged: a phase swap is a stat block, not a pile of buffs (D-040).
        Assert.Equal(4, cutLoose.Damage);
        Assert.Equal(1, cutLoose.AttackPush);
        Assert.Equal(1, cutLoose.PushResistance);
    }

    [Fact]
    public void HeCarriesFootingOneAndNoShell_TheShellIsTheQuarryKings()
    {
        var definition = UnitDefinition.For(UnitKind.Rushmaster);

        Assert.Equal(1, definition.Stats.Footing);
        Assert.DoesNotContain(
            definition.Lifecycle.OnFightStart,
            e => e.CustomRule == UnitRule.QuarryKingShell);
        Assert.Contains(
            definition.Lifecycle.OnFightStart,
            e => e.CustomRule == UnitRule.StatBlockFooting);
    }

    [Fact]
    public void DisplacementAgainstHim_IsLegalOnceHisSingleRefusalIsSpent()
    {
        // Reached by play: the Vanguard shoves him twice. The first is refused by his one Footing;
        // the second moves him, because there is no shell behind it (MASTER_DESIGN §8.9, E4).
        var state = BoardBuilder.Rows(
                ".....",
                ".....",
                ".....",
                ".....",
                ".....")
            .PlayerA(UnitKind.Vanguard, 1, 2)
            .Enemy(UnitKind.Rushmaster, 2, 2)
            .Build();

        var boss = state.Units.Single(u => u.Kind == UnitKind.Rushmaster);
        Assert.Equal(1, boss.Footing);

        // Push resistance 1 shortens the Vanguard's Push 1 to nothing, so the shove that actually
        // MOVES him is the one that beats it. His refusals are not what stops the first tile — that
        // is the resistance on the block, and it is the whole of his anti-displacement.
        var preview = Displacement.PreviewAuto(
            state, boss.Id, new Coord(1, 2), DisplacementKind.Push, 2);

        Assert.Equal(1, preview.Resistance);
        Assert.Equal(1, preview.EffectiveDistance);
        Assert.Equal(new Coord(3, 2), preview.Destination);
    }

    // ---- E3: Cut Loose, on the shipped phase-swap mechanism -----------------------------------

    [Fact]
    public void CutLoose_TakesOverAfterTheTriggeringActionFullyResolves()
    {
        // 15 HP, one Archer shot for 4 -> 11, which is at or below 13. Reached by play: nothing
        // here sets Enraged, and no new timing mechanism was built to place the swap. It rides
        // D-040's OnHpThreshold, which Ai.ReplanInvalidated runs after Game.Resolve returns.
        var state = BoardBuilder.Rows(
                "......",
                "......",
                "......")
            .PlayerA(UnitKind.Archer, 1, 1)
            .Enemy(UnitKind.Rushmaster, 4, 1, hp: 15)
            .Build();

        var archer = state.Units.Single(u => u.Kind == UnitKind.Archer);
        var boss = state.Units.Single(u => u.Kind == UnitKind.Rushmaster);

        Assert.False(boss.Enraged);
        Assert.Equal(1, boss.Move);

        var after = Game.Apply(state, new AttackCommand(archer.Id, boss.Id)).NewState;
        var swapped = after.UnitById(boss.Id);

        Assert.True(swapped.Hp <= 13);
        Assert.True(swapped.Enraged);
        Assert.Equal(3, swapped.Move);
        Assert.Equal(2, swapped.Template.BasicPush);
    }

    [Fact]
    public void WhileHarnessed_HeHasNoStampedeAtAll()
    {
        // The branch is read off the stat block in force, so it is simply absent for the first half
        // of the fight rather than suppressed by a phase check.
        Assert.Equal(0, UnitTemplate.For(UnitKind.Rushmaster).BasicPush);
        Assert.False(Stampede.IsStampeder(
            Unit.FromTemplate(new UnitId(0), UnitKind.Rushmaster, Team.Enemy)));
    }

    [Fact]
    public void Stampede_ReachesHisOwnWorkers_AndTheContactStillCostsThem()
    {
        // "allies included, carrying the bloody-shoulder rider (2 contact + full board
        // consequences)". An ordinary jostle costs an ally nothing; this one does not.
        var state = CutLooseBoard();
        var boss = state.Units.Single(u => u.Kind == UnitKind.Rushmaster);
        var worker = state.Units.Single(u => u.Kind == UnitKind.Husk);

        Assert.True(Combat.CanPush(state, boss, worker));

        var result = Game.Apply(
            state, new AttackCommand(boss.Id, worker.Id, AttackMode.Push));

        Assert.Contains(result.Events.OfType<UnitStampeded>(), e => e.Ally && e.Damage == 2);
        Assert.True(result.NewState.UnitById(worker.Id).Hp < worker.Hp);
    }

    [Fact]
    public void AnOrdinaryShover_StillCannotAimAtItsOwnSide()
    {
        // The ally clause is one conditional on the Stampede, not a hole in the shove. A Stalker's
        // Push 1 is unchanged.
        var state = BoardBuilder.Rows("....", "....", "....")
            .Enemy(UnitKind.Stalker, 1, 1)
            .Enemy(UnitKind.Husk, 2, 1)
            .Build();

        var stalker = state.Units.Single(u => u.Kind == UnitKind.Stalker);
        var husk = state.Units.Single(u => u.Kind == UnitKind.Husk);

        Assert.False(Combat.CanPush(state, stalker, husk));
    }

    // ---- E2: Crew Cover ----------------------------------------------------------------------

    [Fact]
    public void CrewCover_PreviewShowsTheSwapTheInterceptorAndTheFinalCoordinates()
    {
        var state = CoverBoard();
        var vanguard = state.Units.Single(u => u.Kind == UnitKind.Vanguard);
        var boss = state.Units.Single(u => u.Kind == UnitKind.Rushmaster);
        var worker = state.Units.Single(u => u.Kind == UnitKind.Husk);

        var outlook = Abilities.Outlook(state, new AttackCommand(vanguard.Id, boss.Id));

        Assert.NotNull(outlook);
        Assert.True(outlook!.IsIntercepted);

        // The swap, the interceptor, and BOTH final coordinates — §8.9's whole interface clause.
        Assert.Equal(boss.Id, outlook.CrewCover!.BossId);
        Assert.Equal(worker.Id, outlook.CrewCover.InterceptorId);
        Assert.Equal(worker.Position, outlook.CrewCover.BossTo);
        Assert.Equal(boss.Position, outlook.CrewCover.InterceptorTo);

        // And the rest of the projection is already about the worker, because the blow really lands
        // on the worker. A preview still naming the boss would be the lie Stage A existed to kill.
        Assert.Equal(worker.Id, outlook.TargetId);
    }

    [Fact]
    public void CrewCover_SwapsThemAndTheBlowLandsOnTheWorker()
    {
        var state = CoverBoard();
        var vanguard = state.Units.Single(u => u.Kind == UnitKind.Vanguard);
        var boss = state.Units.Single(u => u.Kind == UnitKind.Rushmaster);
        var worker = state.Units.Single(u => u.Kind == UnitKind.Husk);

        var bossAt = boss.Position;
        var workerAt = worker.Position;

        var result = Game.Apply(state, new AttackCommand(vanguard.Id, boss.Id));
        var after = result.NewState;

        Assert.Equal(workerAt, after.UnitById(boss.Id).Position);
        Assert.Equal(bossAt, after.UnitById(worker.Id).Position);
        Assert.True(after.UnitById(worker.Id).Hp < worker.Hp);

        // The sword lands on the worker and on nobody else: the blow is aimed at the body now
        // standing in front of it.
        Assert.All(
            result.Events.OfType<UnitAttacked>(),
            e => Assert.Equal(worker.Id, e.TargetId));

        // And this is §8.9's whole design in one command — the swap saved him from the sword, and
        // then the shove that rode the sword put his own worker into him for a collision. Crew Cover
        // "does not stop impact": every point he loses here is the board's, not the blade's.
        Assert.All(
            result.Events.OfType<UnitDamaged>().Where(e => e.UnitId == boss.Id),
            e => Assert.Equal(DamageSource.Collision, e.Source));

        // Asserted on rendered output, not the flag.
        var line = result.Events
            .Select(e => CombatLog.Detail(e, state))
            .FirstOrDefault(t => t.Contains("swaps in for"));

        Assert.NotNull(line);
        Assert.Contains(bossAt.ToString(), line!);
        Assert.Contains(workerAt.ToString(), line);
    }

    [Fact]
    public void CrewCover_IsOncePerRound()
    {
        var state = CoverBoard();
        var vanguard = state.Units.Single(u => u.Kind == UnitKind.Vanguard);
        var boss = state.Units.Single(u => u.Kind == UnitKind.Rushmaster);

        var after = Game.Apply(state, new AttackCommand(vanguard.Id, boss.Id)).NewState;

        Assert.Equal(after.Round, after.UnitById(boss.Id).CrewCoverRound);
        Assert.Null(CrewCover.Interceptor(after, after.UnitById(boss.Id)));
        Assert.False(CrewCover.IsAvailable(after, after.UnitById(boss.Id)));
    }

    [Fact]
    public void CrewCover_DoesNotStopWhatTheBoardDoes()
    {
        // §8.9: "It does not stop impact, hazard, or area damage — the board still reaches him."
        // A body slammed into him collides for the full amount with his cover standing right there.
        var state = CoverBoard();
        var boss = state.Units.Single(u => u.Kind == UnitKind.Rushmaster);

        Assert.NotNull(CrewCover.Interceptor(state, boss));

        var events = new List<GameEvent>();
        var hurt = Combat.ApplyDamage(
            state, boss.Id, Displacement.CollisionDamage, DamageSource.Collision, events);

        Assert.Equal(boss.Hp - Displacement.CollisionDamage, hurt.UnitById(boss.Id).Hp);
        Assert.DoesNotContain(events, e => e is CrewCovered);
    }

    [Fact]
    public void CrewCover_WantsAStandingWorker_AndNothingElse()
    {
        var boss = Unit.FromTemplate(new UnitId(0), UnitKind.Rushmaster, Team.Enemy);

        Assert.True(CrewCover.Covers(boss));
        Assert.True(CrewCover.IsWorker(
            Unit.FromTemplate(new UnitId(1), UnitKind.Husk, Team.Enemy)));

        // A Lobber standing beside him is not his shift.
        Assert.False(CrewCover.IsWorker(
            Unit.FromTemplate(new UnitId(2), UnitKind.Lobber, Team.Enemy)));
    }

    // ---- E1: the Work Bells ------------------------------------------------------------------

    [Fact]
    public void ABellDestroyed_CancelsItsMouthsRemainingSpawns()
    {
        var mouth = new Coord(0, 1);
        var state = BellBoard(mouth);
        var bell = state.Structures.Single(s => s.IsPaired);

        Assert.Equal(2, Objectives.DueAt(state, mouth).Count);

        // One clean slam: §8.9 prices a structure collision at 6, and a Bell has 6 hit points.
        var events = new List<GameEvent>();
        var after = Objectives.Damage(
            state, bell.At, Displacement.StructureCollisionDamage, DamageSource.Collision, events);

        // StructureAt answers about standing masonry, so rubble reads as a clear tile.
        Assert.Null(after.StructureAt(bell.At));
        Assert.False(after.Structures.Single(s => s.At == bell.At).IsStanding);
        Assert.Empty(Objectives.DueAt(after, mouth));
        Assert.Contains(events.OfType<SpawnsCancelled>(), e => e.Mouth == mouth && e.Cancelled == 2);
    }

    [Fact]
    public void AnUnpairedMouthsArrivals_AreUntouchedByAnotherBellFalling()
    {
        var mouth = new Coord(0, 1);
        var other = new Coord(4, 1);
        var state = BellBoard(mouth);

        state = state with
        {
            Reinforcements = state.Reinforcements
                .Append(new PendingReinforcement(new UnitId(9), 5, other))
                .ToList(),
        };

        var bell = state.Structures.Single(s => s.IsPaired);
        var events = new List<GameEvent>();
        var after = Objectives.Damage(state, bell.At, 6, DamageSource.Collision, events);

        Assert.Single(Objectives.DueAt(after, other));
    }

    [Fact]
    public void ABellsStatus_CarriesItsMouthAndItsNextSpawn()
    {
        var mouth = new Coord(0, 1);
        var state = BellBoard(mouth);
        var status = StructureStatus.For(state, state.Structures.Single(s => s.IsPaired).At);

        Assert.NotNull(status);
        Assert.True(status!.IsPaired);
        Assert.Equal("Work Bell 6/6", status.Label);
        Assert.Equal(mouth, status.Mouth);
        Assert.Equal(UnitKind.Husk, status.NextSpawnKind);
        Assert.Equal(2, status.NextSpawnRound);
        Assert.Equal(2, status.DueAtMouth);

        // Rendered, not the fields: the objective panel draws this line.
        Assert.Equal("mouth 0,1 · next Husk r2 · 2 due", status.MouthLabel);
    }

    [Fact]
    public void ABell_IsAnObjectiveAndNotDebris_SoThePanelCarriesIt()
    {
        // The deliberate IsBlocker/Role call: a Bell is objective-linked, so it belongs in the
        // objective panel — but its role is Destroy, so nothing besieges it and no Guard shields it.
        var state = BellBoard(new Coord(0, 1));
        var bell = state.Structures.Single(s => s.IsPaired);

        Assert.False(bell.IsBlocker);
        Assert.False(bell.IsSiegeTarget);
        Assert.Contains(StructureStatus.ObjectivesOn(state), s => s.IsPaired);
    }

    // ---- fixtures ----------------------------------------------------------------------------

    private static GameState CoverBoard() =>
        BoardBuilder.Rows(
                "......",
                "......",
                "......")
            .PlayerA(UnitKind.Vanguard, 1, 1)
            .Enemy(UnitKind.Rushmaster, 2, 1)
            .Enemy(UnitKind.Husk, 3, 1)
            .Build();

    private static GameState CutLooseBoard()
    {
        var state = BoardBuilder.Rows(
                "......",
                "...#..",
                "......")
            .Enemy(UnitKind.Rushmaster, 1, 1, hp: 10)
            .Enemy(UnitKind.Husk, 2, 1)
            .Active(Team.Enemy)
            .Build();

        // Reached by play everywhere else; here the fixture starts him already under the threshold,
        // and the swap is applied by the shipped rule rather than by setting the flag.
        var events = new List<GameEvent>();
        return Ai.ReplanInvalidated(state, events);
    }

    private static GameState BellBoard(Coord mouth)
    {
        var state = BoardBuilder.Rows(
                "......",
                "......",
                "......")
            .Enemy(UnitKind.Rushmaster, 3, 1)
            .Wave(2, new EnemySpawn(UnitKind.Husk, mouth))
            .Wave(5, new EnemySpawn(UnitKind.Husk, mouth))
            .Build();

        var bell = new Structure
        {
            At = new Coord(1, 1),
            Hp = 6,
            MaxHp = 6,
            Role = ObjectiveKind.Destroy,
            Mouth = mouth,
        };

        var units = new List<Unit>(state.Units);
        var pending = new List<PendingReinforcement>();

        foreach (var wave in state.Fight.Waves)
        {
            foreach (var arrival in wave.Arrivals)
            {
                var id = new UnitId(units.Count);
                units.Add(Unit.FromTemplate(id, arrival.Kind, Team.Enemy));
                pending.Add(new PendingReinforcement(id, wave.Round, arrival.At));
            }
        }

        return state with
        {
            Units = units,
            Reinforcements = pending,
            Structures = new[] { bell },
        };
    }
}
