using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// The published activation order (D-103). Intents say what each enemy will do; this says when.
/// </summary>
public class TurnOrderTests
{
    // ---- enemies are named, in the order the rules would take them --------------------------

    [Fact]
    public void Upcoming_NamesEnemies_InUnitsOrder()
    {
        var state = Board();

        var enemies = TurnOrder.Upcoming(state)
            .Where(e => e.Kind == ActivationKind.Enemy && e.Round == state.Round)
            .Select(e => e.UnitId!.Value)
            .ToList();

        var expected = state.Units
            .Where(u => u.Team == Team.Enemy && u.IsOnBoard && !u.Clinging)
            .Select(u => u.Id)
            .ToList();

        Assert.Equal(expected, enemies);
    }

    // The enemy queue is the rules' own answer, so it must survive the units list being reordered
    // only in the way the rules would — first pending in Units order, no sort, no tiebreak.
    [Fact]
    public void Upcoming_TheFirstEnemy_IsTheOneTheRulesWouldActivate()
    {
        var state = EnemyTurn(Board());

        var first = TurnOrder.Upcoming(state).First(e => e.Kind == ActivationKind.Enemy);
        var command = Game.NextEnemyCommand(state);

        Assert.NotNull(command);
        Assert.Equal(first.UnitId!.Value, UnitOf(command!));
    }

    // ---- a player place is a slot, not a guess ----------------------------------------------

    [Fact]
    public void Upcoming_APlayerEntry_CarriesBothCandidatesAndNamesNobody()
    {
        var state = Board();

        var slot = TurnOrder.Upcoming(state).First(e => e.Kind == ActivationKind.PlayerSlot);

        Assert.Equal(Team.PlayerA, slot.Team);
        Assert.Null(slot.UnitId);
        Assert.False(slot.IsNamed);
        Assert.Equal(2, slot.Candidates.Count);
    }

    [Fact]
    public void Upcoming_APlayerSlot_CollapsesWhenOnlyOneCandidateIsLeft()
    {
        var state = Board();
        var vanguard = state.Find(UnitKind.Vanguard).Id;

        // One of Player A's two has already gone, so the choice is no longer a choice.
        var after = state.WithUnit(state.Get(vanguard) with { HasActivated = true });

        var slot = TurnOrder.Upcoming(after).First(
            e => e.Kind == ActivationKind.PlayerSlot && e.Team == Team.PlayerA);

        Assert.True(slot.IsNamed);
        Assert.Equal(after.Find(UnitKind.Threadcaster).Id, slot.UnitId);
        Assert.Single(slot.Candidates);
    }

    [Fact]
    public void Upcoming_APlayerSlot_CollapsesToTheUnitThePlayerCommittedTo()
    {
        var state = Board();
        var fisher = state.Find(UnitKind.Threadcaster).Id;

        var committed = state with { ActiveUnitId = fisher };
        var slot = TurnOrder.Upcoming(committed).First();

        Assert.True(slot.IsCurrent);
        Assert.Equal(ActivationKind.PlayerSlot, slot.Kind);
        Assert.Equal(fisher, slot.UnitId);
    }

    // ---- deaths ------------------------------------------------------------------------------

    [Fact]
    public void Upcoming_AUnitThatDiesMidRound_DropsOutAndReshufflesNothingElse()
    {
        var state = EnemyTurn(Board());
        var before = TurnOrder.Upcoming(state);
        var husk = state.Units.First(u => u.Kind == UnitKind.Husk).Id;

        var after = TurnOrder.Upcoming(state.WithUnit(state.Get(husk) with { Hp = 0, IsDeployed = false }));

        Assert.Contains(before, e => e.UnitId == husk);
        Assert.DoesNotContain(after, e => e.UnitId == husk);

        // Everything else in this round keeps its relative order. The claim is deliberately about
        // the current round: across the seam the order legitimately *grows*, because a side with one
        // fewer body reaches further into the next round before the horizon closes.
        Assert.Equal(
            before.Where(e => e.Round == state.Round && e.UnitId != husk).Select(Shape).ToList(),
            after.Where(e => e.Round == state.Round).Select(Shape).ToList());
    }

    // ---- the round seam ----------------------------------------------------------------------

    [Fact]
    public void Upcoming_ReachesIntoTheNextRound()
    {
        var state = Board();
        var order = TurnOrder.Upcoming(state);

        Assert.Contains(order, e => e.Round == state.Round);
        Assert.Contains(order, e => e.Round == state.Round + 1);
        Assert.DoesNotContain(order, e => e.Round > state.Round + 1);
    }

    // The whole reason the horizon crosses the seam: an enemy that acts last and first swings twice
    // with nothing of yours in between, and a player has to be able to see that coming.
    [Fact]
    public void Upcoming_ShowsABackToBackDoubleActivationAcrossTheSeam()
    {
        var state = Board();

        // Everybody has gone except one enemy, so it closes this round and opens the next.
        var spent = state;
        foreach (var unit in state.Units)
        {
            if (unit.Team != Team.Enemy || unit.Kind != UnitKind.Husk)
            {
                spent = spent.WithUnit(spent.Get(unit.Id) with { HasActivated = true });
            }
        }

        spent = EnemyTurn(spent);
        var order = TurnOrder.Upcoming(spent);
        var husk = spent.Units.First(u => u.Kind == UnitKind.Husk).Id;

        int last = order.ToList().FindIndex(e => e.Round == spent.Round && e.UnitId == husk);
        Assert.True(last >= 0, "the last enemy of this round is not in the order");

        // The very next entry is the new round, and the same unit is in its opening.
        Assert.Equal(spent.Round + 1, order[last + 1].Round);
        Assert.Contains(order.Where(e => e.Round == spent.Round + 1), e => e.UnitId == husk);
    }

    // ---- clinging is shown, and changes nothing ----------------------------------------------

    [Fact]
    public void Upcoming_ClingingUnit_AppearsSkippedWithoutTakingASlot()
    {
        var state = Clinging(out var archer);
        var order = TurnOrder.Upcoming(state);

        var skipped = order.Single(e => e.Kind == ActivationKind.Skipped);
        Assert.Equal(archer, skipped.UnitId);
        Assert.Equal(Team.PlayerB, skipped.Team);

        // It is shown, and it is not a candidate for anybody's slot.
        Assert.DoesNotContain(order.Where(e => e.Kind == ActivationKind.PlayerSlot),
            e => e.Candidates.Contains(archer));
    }

    // The load-bearing assertion of the whole ruling: showing the skip must not change the game.
    // A clinging unit that became a real slot would insert a dead player activation and hand the
    // enemy an extra interleave, which D-103 rejects outright. So the clinging unit is never handed
    // a slot, its side keeps activating through whoever is left, and the round still ends.
    [Fact]
    public void AClingingUnit_TakesNoSlot_AndTheAlternationRunsOnWithoutIt()
    {
        var state = Clinging(out var archer);

        var walked = state;
        var actors = new List<UnitId>();
        var slots = new List<(Team Team, int Round)>();

        for (int i = 0; i < 12 && walked.Outcome == FightOutcome.InProgress; i++)
        {
            var actor = Game.LegalCommands(walked)
                .Select(UnitOf)
                .FirstOrDefault(id => id != UnitId.None);

            if (actor == UnitId.None)
            {
                break;
            }

            slots.Add((walked.ActiveTeam, walked.Round));
            actors.Add(actor);
            walked = Game.Apply(walked, new EndActivationCommand(actor)).NewState;
        }

        // Never handed a slot, and never offered a command.
        Assert.DoesNotContain(archer, actors);

        // Its side still activates — through the unit that is not in the hole.
        Assert.Contains(slots, s => s.Team == Team.PlayerB);
        Assert.Contains(walked.Units, u => u.Team == Team.PlayerB && u.Id != archer);

        // And the rounds still turn over rather than stalling on a dead slot.
        Assert.True(walked.Round > state.Round);
    }

    // The strip is a query and nothing else: asking for it must not touch the board.
    [Fact]
    public void AskingForTheOrder_ChangesNothingAboutWhatHappensNext()
    {
        var state = Clinging(out _);
        var actor = Game.LegalCommands(state).Select(UnitOf).First(id => id != UnitId.None);

        var untouched = Game.Apply(state, new EndActivationCommand(actor)).NewState;

        // Same command, on a board somebody has asked the order of several times over.
        for (int i = 0; i < 5; i++)
        {
            TurnOrder.Upcoming(state);
        }

        var asked = Game.Apply(state, new EndActivationCommand(actor)).NewState;

        Assert.Equal(untouched, asked);
        Assert.Equal(untouched.GetHashCode(), asked.GetHashCode());
    }

    // ---- nothing to publish -------------------------------------------------------------------

    [Fact]
    public void Upcoming_IsEmptyDuringDeployment()
    {
        var start = Game.Start(FightLibrary.Fight1(), seed: 1).NewState;

        Assert.Equal(Phase.Deployment, start.Phase);
        Assert.Empty(TurnOrder.Upcoming(start));
    }

    [Fact]
    public void Upcoming_IsEmptyOnceTheFightHasResolved()
    {
        var state = Board() with { Outcome = FightOutcome.Won };

        Assert.Empty(TurnOrder.Upcoming(state));
    }

    // ---- reinforcements are flagged, never ordered ---------------------------------------------

    // MASTER_DESIGN §14 #8 is undecided, so the query says the round is not the whole story and
    // invents nothing about where an arrival would go.
    [Fact]
    public void Upcoming_MarksAPeekedRoundThatAWaveLandsIn_AndOrdersNoArrivals()
    {
        var state = Board();
        var pending = Unit.FromTemplate(new UnitId(90), UnitKind.Husk, Team.Enemy);

        var waved = state with
        {
            Units = state.Units.Concat(new[] { pending }).ToList(),
            Reinforcements = new[] { new PendingReinforcement(pending.Id, state.Round + 1, new Coord(0, 0)) },
        };

        var order = TurnOrder.Upcoming(waved);

        Assert.All(order.Where(e => e.Round == waved.Round), e => Assert.False(e.ReinforcementsDue));
        Assert.Contains(order.Where(e => e.Round == waved.Round + 1), e => e.ReinforcementsDue);
        Assert.DoesNotContain(order, e => e.UnitId == pending.Id);
    }

    // ---- fixtures -------------------------------------------------------------------------------

    private static object Shape(ActivationEntry e) =>
        new { e.Round, e.Kind, e.Team, e.UnitId, Candidates = string.Join(",", e.Candidates) };

    private static UnitId UnitOf(Command command) => command switch
    {
        MoveCommand c => c.UnitId,
        AttackCommand c => c.UnitId,
        AbilityCommand c => c.UnitId,
        RescueCommand c => c.UnitId,
        FinishClingingCommand c => c.UnitId,
        SpendVerveCommand c => c.UnitId,
        EndActivationCommand c => c.UnitId,
        DeployCommand c => c.UnitId,
        _ => UnitId.None,
    };

    // Two units a side and two enemies, so every alternation branch has something to choose from.
    private static GameState Board() =>
        BoardBuilder.Open(8, 4)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .PlayerA(UnitKind.Threadcaster, 0, 1)
            .PlayerB(UnitKind.Wardbearer, 1, 0)
            .PlayerB(UnitKind.Archer, 1, 1)
            .Enemy(UnitKind.Husk, 7, 0)
            .Enemy(UnitKind.Anchor, 7, 1)
            .Build();

    private static GameState Clinging(out UnitId archer)
    {
        var state = BoardBuilder.Rows(
                "........",
                ".......O",
                "........",
                "........")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .PlayerA(UnitKind.Threadcaster, 0, 1)
            .PlayerB(UnitKind.Wardbearer, 1, 0)
            .PlayerB(UnitKind.Archer, 7, 1)
            .Enemy(UnitKind.Husk, 5, 0)
            .Build();

        archer = state.Find(UnitKind.Archer).Id;
        return state.WithUnit(state.Get(archer) with { Clinging = true });
    }

    private static GameState EnemyTurn(GameState state)
    {
        foreach (var unit in state.Units.ToList())
        {
            if (unit.Team != Team.Enemy)
            {
                state = state.WithUnit(state.Get(unit.Id) with { HasActivated = true });
            }
        }

        return state with { ActiveTeam = Team.Enemy, NextPlayerTeam = Team.PlayerA, ActiveUnitId = null };
    }
}
