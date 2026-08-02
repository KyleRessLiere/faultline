using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// D-081: a clinging unit nothing can still save is swept the moment it becomes hopeless, rather
/// than holding the fight open until the end of a round it was always going to lose.
/// </summary>
public class DoomedClingTests
{
    // ---- the enemy side --------------------------------------------------------------------

    [Fact]
    public void TheLastEnemyShovedIntoAPit_IsSweptOnTheSpot()
    {
        var state = OneEnemyBesideAPit(out var vanguard, out var husk);

        var result = state.Step(new AttackCommand(vanguard, husk));

        Assert.True(result.Has<Clinging>());
        Assert.True(result.Has<Voided>());
        Assert.True(result.NewState.Get(husk).Voided);
        Assert.Equal(FightOutcome.Won, result.NewState.Outcome);
    }

    [Fact]
    public void AClingingEnemyWithAnAllyStillStanding_HangsOn()
    {
        // The other Husk could walk over and haul it out (D-072). Nothing is hopeless yet.
        // The Vanguard shoves west with his basic attack; the pit is the tile behind the Husk.
        var state = BoardBuilder.Rows("O.....")
            .PlayerA(UnitKind.Vanguard, 2, 0)
            .Enemy(UnitKind.Husk, 1, 0, footing: 0, hp: 6)
            .Enemy(UnitKind.Husk, 5, 0)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard).Id;
        var near = state.Units.Single(u => u.Position == new Coord(1, 0)).Id;

        var result = state.Step(new AttackCommand(vanguard, near));

        Assert.True(result.Has<Clinging>());
        Assert.False(result.Has<Voided>());
        Assert.True(result.NewState.Get(near).Clinging);
        Assert.Equal(FightOutcome.InProgress, result.NewState.Outcome);
    }

    [Fact]
    public void AClingingEnemyWithAWaveStillDue_HangsOn()
    {
        // A reinforcement that has not landed is a rescuer that has not arrived. The fight stays
        // open for it.
        var fight = FightLibrary.All().First(f => f.Waves.Count > 0);
        var state = Game.Start(fight, seed: 0).NewState;

        Assert.NotEmpty(state.Reinforcements);

        var enemy = state.Units.First(u => u.Team == Team.Enemy && u.IsOnBoard);

        // Everything of that side hanging on a ledge, and a wave still to come.
        var hanging = state;
        foreach (var unit in state.Units.Where(u => u.Team == Team.Enemy && u.IsOnBoard))
        {
            hanging = hanging.WithUnit(hanging.Get(unit.Id) with { Clinging = true });
        }

        var events = new List<GameEvent>();
        var after = Pits.ResolveDoomed(hanging, events);

        Assert.Empty(events);
        Assert.True(after.Get(enemy.Id).Clinging);
        Assert.False(after.Get(enemy.Id).Voided);
    }

    [Fact]
    public void WithTheWaveGone_TheSameBoardSweeps()
    {
        // The only difference from the test above is the pending schedule, which is the whole
        // condition.
        var fight = FightLibrary.All().First(f => f.Waves.Count > 0);
        var state = Game.Start(fight, seed: 0).NewState;

        var hanging = state with { Reinforcements = System.Array.Empty<PendingReinforcement>() };
        foreach (var unit in state.Units.Where(u => u.Team == Team.Enemy && u.IsOnBoard))
        {
            hanging = hanging.WithUnit(hanging.Get(unit.Id) with { Clinging = true });
        }

        var events = new List<GameEvent>();
        var after = Pits.ResolveDoomed(hanging, events);

        Assert.NotEmpty(events.OfType<Voided>());
        Assert.All(
            after.Units.Where(u => u.Team == Team.Enemy && u.Kind != UnitKind.Husk || u.Clinging),
            u => Assert.False(u.Clinging));
    }

    // ---- the player side, symmetrically -----------------------------------------------------

    [Fact]
    public void APlayerSideThatIsNothingButHands_IsSweptAndTheFightIsLost()
    {
        var state = BoardBuilder.Rows(".O....")
            .PlayerA(UnitKind.Vanguard, 2, 0)
            .Enemy(UnitKind.Stalker, 5, 0)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard).Id;

        var hanging = state.WithUnit(state.Get(vanguard) with
        {
            Clinging = true,
            Position = new Coord(1, 0),
            ClingingSinceRound = state.Round,
        });

        var events = new List<GameEvent>();
        var after = Objectives.Check(Pits.ResolveDoomed(hanging, events), false, events);

        Assert.True(after.Get(vanguard).Voided);
        Assert.Equal(FightOutcome.Lost, after.Outcome);
    }

    [Fact]
    public void APlayerClingingWithAnAllyStillUp_HangsOn()
    {
        // The ally is the rescuer. It does not have to be able to reach this round — only to exist.
        var state = BoardBuilder.Rows(".O....")
            .PlayerA(UnitKind.Vanguard, 2, 0)
            .PlayerB(UnitKind.Archer, 5, 0)
            .Enemy(UnitKind.Husk, 4, 1)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard).Id;

        var hanging = state.WithUnit(state.Get(vanguard) with
        {
            Clinging = true,
            Position = new Coord(1, 0),
            ClingingSinceRound = state.Round,
        });

        var events = new List<GameEvent>();
        var after = Pits.ResolveDoomed(hanging, events);

        Assert.Empty(events);
        Assert.True(after.Get(vanguard).Clinging);
    }

    // ---- charging happens on the way in, not on the way out ---------------------------------

    [Fact]
    public void TheThreadcasterIsChargedBeforeTheSweep_NotByIt()
    {
        // Pluck is earned at hazard entry. The sweep that follows is a consequence, and a consequence
        // must not pay a second time — nor must it be the thing that pays at all, or a Threadcaster
        // would earn nothing from a pit until the round ended.
        var state = BoardBuilder.Rows(".O...")
            .PlayerA(UnitKind.Threadcaster, 0, 0)
            .Enemy(UnitKind.Husk, 3, 0, footing: 0)
            .Build();

        var caster = state.Find(UnitKind.Threadcaster).Id;
        var husk = state.Find(UnitKind.Husk).Id;

        var result = state.Step(new AbilityCommand(caster, Ability.Reel, husk));

        Assert.True(result.Has<Clinging>());
        Assert.True(result.Has<Voided>());

        var charged = result.Single<VerveCharged>();
        Assert.Equal(caster, charged.UnitId);
        Assert.Equal(VerveSource.Hazard, charged.Source);
        Assert.Equal(1, result.NewState.Get(caster).Verve);

        // Ordering, not just presence: the charge belongs to the entry it was earned by.
        var order = result.Events.ToList();
        Assert.True(
            order.FindIndex(e => e is Clinging) < order.FindIndex(e => e is VerveCharged),
            "the charge must follow the Clinging it was earned by");
    }

    // ---- an auto-sweep is not a different kind of death --------------------------------------

    [Fact]
    public void AnAutoSweepAndAnEndOfRoundSweep_ProduceTheSameEventChain()
    {
        // Same board, same clinger, two routes to the same end: one hopeless on the spot, one that
        // simply ran out of round. A renderer and a log reader should not be able to tell them apart.
        var natural = Hanging(out var naturalId);
        var auto = Hanging(out var autoId);

        var naturalEvents = new List<GameEvent>();
        Pits.ResolveEndOfRound(natural with { Round = natural.Round + 1 }, naturalEvents);

        var autoEvents = new List<GameEvent>();
        Pits.ResolveDoomed(auto, autoEvents);

        Assert.Equal(Describe(naturalEvents, naturalId), Describe(autoEvents, autoId));
    }

    /// <summary>
    /// A board whose only enemy is hanging: doomed under D-081, and a round overdue under D-016.
    /// </summary>
    private static GameState Hanging(out UnitId clinger)
    {
        var state = BoardBuilder.Rows(".O....")
            .PlayerA(UnitKind.Vanguard, 3, 0)
            .Enemy(UnitKind.Husk, 4, 0)
            .Build();

        clinger = state.Find(UnitKind.Husk).Id;
        var id = clinger;

        return state.WithUnit(state.Get(id) with
        {
            Clinging = true,
            Position = new Coord(1, 0),
            ClingingSinceRound = state.Round,
        });
    }

    /// <summary>
    /// The event chain as anything downstream sees it: types, payloads and order, with the unit id
    /// normalised out so two boards can be compared.
    /// </summary>
    private static string Describe(IReadOnlyList<GameEvent> events, UnitId clinger) =>
        string.Join(
            " | ",
            events.Select(e => e switch
            {
                Voided v => $"Voided(unit={(v.UnitId == clinger ? "clinger" : v.UnitId.ToString())},"
                    + $"team={v.Team},at={v.At},reason={v.Reason})",
                _ => e.GetType().Name,
            }));

    private static GameState OneEnemyBesideAPit(out UnitId vanguard, out UnitId husk)
    {
        var state = BoardBuilder.Rows(".O....")
            .PlayerA(UnitKind.Vanguard, 3, 0)
            .PlayerB(UnitKind.Archer, 3, 1)
            .Enemy(UnitKind.Husk, 2, 0, footing: 0)
            .Build();

        vanguard = state.Find(UnitKind.Vanguard).Id;
        husk = state.Find(UnitKind.Husk).Id;
        return state;
    }
}
