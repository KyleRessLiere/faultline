using System;
using System.Collections.Generic;
using System.Linq;
using Faultline.Core;
using Faultline.Web.Shell;
using Faultline.Web.Shell.Playtest;

namespace Faultline.Web.Tests;

/// <summary>
/// D-083: while somebody is over the edge, the shell says so, says by when, and says who could get
/// there — including by naming what is stopping the ones who cannot.
/// </summary>
public sealed class RescueSurfacingTests
{
    // ---- the deadline is named, not implied -------------------------------------------------

    [Fact]
    public void AClingingAlly_ProducesALineNamingTheRound()
    {
        var state = Hanging(round: 2, since: 2, out var archer);

        var lines = PlaytestText.ClingingLines(state);

        var line = Assert.Single(lines);
        Assert.Contains(state.UnitById(archer).Name, line);
        Assert.Contains("round 3", line);
    }

    [Fact]
    public void OnTheLastRound_TheLineSaysThisRound()
    {
        var state = Hanging(round: 3, since: 2, out _);

        Assert.Contains("this round", Assert.Single(PlaytestText.ClingingLines(state)));
    }

    [Fact]
    public void NobodyHanging_NoLines()
    {
        var state = Board(out _, out _);

        Assert.Empty(PlaytestText.ClingingLines(state));
    }

    [Fact]
    public void AClingingEnemy_DoesNotRaiseTheAllyBanner()
    {
        // The banner is about your people. An enemy on a ledge is an opportunity, not an emergency.
        var state = Board(out _, out _);
        var husk = state.Units.First(u => u.Team == Team.Enemy);

        var hanging = state.WithUnit(husk with
        {
            Clinging = true,
            Position = new Coord(1, 1),
            ClingingSinceRound = state.Round,
        });

        Assert.Empty(PlaytestText.ClingingLines(hanging));
    }

    // ---- disabled buttons say why -----------------------------------------------------------

    [Fact]
    public void AnOutOfReachRescuer_IsToldHowMuchMoreMoveItNeeds()
    {
        var state = Hanging(round: 2, since: 2, out var archer);
        var vanguard = state.Units.First(u => u.Kind == UnitKind.Vanguard);
        var far = state.WithUnit(vanguard with { Position = new Coord(4, 1) });

        string reason = PlaytestText.RescueBlockedReason(
            far, far.UnitById(vanguard.Id), far.UnitById(archer));

        Assert.Equal("needs 2 more move", reason);
    }

    [Fact]
    public void AnAdjacentRescuer_HasNoReasonAgainstIt()
    {
        var state = Hanging(round: 2, since: 2, out var archer);
        var vanguard = state.Units.First(u => u.Kind == UnitKind.Vanguard);

        Assert.Equal(
            string.Empty,
            PlaytestText.RescueBlockedReason(state, state.UnitById(vanguard.Id), state.UnitById(archer)));
    }

    [Fact]
    public void ARescuerThatHasAlreadyActed_SaysSo()
    {
        var state = Hanging(round: 2, since: 2, out var archer);
        var vanguard = state.Units.First(u => u.Kind == UnitKind.Vanguard);
        var spent = state.WithUnit(vanguard with { HasActed = true });

        Assert.Equal(
            "action already spent",
            PlaytestText.RescueBlockedReason(spent, spent.UnitById(vanguard.Id), spent.UnitById(archer)));
    }

    [Fact]
    public void ARescuerThatCannotGetThereAtAll_SaysThatInstead()
    {
        var state = Hanging(round: 2, since: 2, out var archer);
        var vanguard = state.Units.First(u => u.Kind == UnitKind.Vanguard);
        var miles = state.WithUnit(vanguard with { Position = new Coord(8, 3) });

        Assert.Equal(
            "cannot reach this activation",
            PlaytestText.RescueBlockedReason(miles, miles.UnitById(vanguard.Id), miles.UnitById(archer)));
    }

    // ---- reach is Core's answer -------------------------------------------------------------

    [Fact]
    public void ReachIsAskedOfCore_NotGuessedFromDistance()
    {
        var state = Hanging(round: 2, since: 2, out var archer);
        var vanguard = state.Units.First(u => u.Kind == UnitKind.Vanguard);
        var stepAway = state.WithUnit(vanguard with { Position = new Coord(3, 1) });

        Assert.True(Pits.CanReachToRescue(
            stepAway, stepAway.UnitById(vanguard.Id), stepAway.UnitById(archer)));

        // Walled in: adjacent by distance is not adjacent by pathfinding.
        var boxed = Walled(out var boxedRescuer, out var boxedClinging);
        Assert.Null(Pits.MoveNeededToReach(
            boxed, boxed.UnitById(boxedRescuer), boxed.UnitById(boxedClinging)));
    }

    // ---- boards -----------------------------------------------------------------------------

    private static GameState Board(out UnitId vanguard, out UnitId archer)
    {
        var rows = new List<string> { ".........", ".O.......", ".........", "........." };
        var board = BoardLayout.Parse(rows);

        var units = new List<Unit>
        {
            Unit.FromTemplate(new UnitId(0), UnitKind.Vanguard, Team.PlayerA) with
            {
                Position = new Coord(2, 1), IsDeployed = true,
            },
            Unit.FromTemplate(new UnitId(1), UnitKind.Archer, Team.PlayerA) with
            {
                Position = new Coord(6, 3), IsDeployed = true,
            },
            Unit.FromTemplate(new UnitId(2), UnitKind.Husk, Team.Enemy) with
            {
                Position = new Coord(8, 0), IsDeployed = true,
            },
        };

        vanguard = new UnitId(0);
        archer = new UnitId(1);

        return new GameState
        {
            Seed = 1,
            RngState = 1,
            Fight = new FightDefinition { Number = 1, Name = "Ledge", Board = board },
            Board = board,
            Units = units,
            Round = 1,
            Phase = Phase.Battle,
            ActiveTeam = Team.PlayerA,
            NextPlayerTeam = Team.PlayerA,
            Outcome = FightOutcome.InProgress,
        };
    }

    private static GameState Hanging(int round, int since, out UnitId archer)
    {
        var state = Board(out _, out archer);
        var id = archer;

        return (state with { Round = round }).WithUnit(state.UnitById(id) with
        {
            Position = new Coord(1, 1),
            Clinging = true,
            ClingingSinceRound = since,
        });
    }

    private static GameState Walled(out UnitId rescuer, out UnitId clinging)
    {
        var rows = new List<string> { "..#....", ".O#....", "..#...." };
        var board = BoardLayout.Parse(rows);

        var units = new List<Unit>
        {
            Unit.FromTemplate(new UnitId(0), UnitKind.Vanguard, Team.PlayerA) with
            {
                Position = new Coord(5, 1), IsDeployed = true,
            },
            Unit.FromTemplate(new UnitId(1), UnitKind.Archer, Team.PlayerA) with
            {
                Position = new Coord(1, 1),
                IsDeployed = true,
                Clinging = true,
                ClingingSinceRound = 1,
            },
        };

        rescuer = new UnitId(0);
        clinging = new UnitId(1);

        return new GameState
        {
            Seed = 1,
            RngState = 1,
            Fight = new FightDefinition { Number = 1, Name = "Walled", Board = board },
            Board = board,
            Units = units,
            Round = 1,
            Phase = Phase.Battle,
            ActiveTeam = Team.PlayerA,
            NextPlayerTeam = Team.PlayerA,
            Outcome = FightOutcome.InProgress,
        };
    }
}

/// <summary>
/// §3.5: the objective panel shows the goal, live progress and the loss condition, and every figure
/// in it comes from Core rather than being recomputed beside the board.
/// </summary>
public sealed class ObjectivePanelTests
{
    [Fact]
    public void KillAll_CountsEnemiesDown()
    {
        var state = Game.Start(FightLibrary.Fight1(), seed: 0).NewState;

        var status = ObjectiveStatus.For(state);

        Assert.Equal(ObjectiveKind.KillAll, status.Kind);
        Assert.True(status.HasBar);
        Assert.Equal(0, status.Progress);
        Assert.Equal(state.Units.Count(u => u.Team == Team.Enemy), status.Target);
        Assert.Contains("Enemies 0/", status.Label);
    }

    [Fact]
    public void TheBarMovesAsTheFightDoes()
    {
        var state = Game.Start(FightLibrary.Fight1(), seed: 0).NewState;
        var enemy = state.Units.First(u => u.Team == Team.Enemy);

        var after = state.WithUnit(enemy with { Hp = 0 });

        Assert.Equal(0, ObjectiveStatus.For(state).Progress);
        Assert.Equal(1, ObjectiveStatus.For(after).Progress);
        Assert.True(ObjectiveStatus.For(after).Fraction > 0);
    }

    [Fact]
    public void EveryObjectiveKindSaysWhatToDoAndWhatLosesIt()
    {
        // Equal billing is the ruling: a player who knows only how to win is playing half the fight.
        foreach (var fight in FightLibrary.All())
        {
            var status = ObjectiveStatus.For(Game.Start(fight, seed: 0).NewState);

            Assert.False(string.IsNullOrWhiteSpace(status.Goal), fight.Id + " has no goal text");
            Assert.False(string.IsNullOrWhiteSpace(status.Loss), fight.Id + " has no loss text");
        }
    }

    [Fact]
    public void AStructureObjective_ReadsItsHitPointsOffTheStructures()
    {
        var fight = FightLibrary.All().First(f => f.Objective?.Kind == ObjectiveKind.Protect);
        var state = Game.Start(fight, seed: 0).NewState;

        var status = ObjectiveStatus.For(state);

        Assert.Equal(state.Structures.Sum(s => s.Hp), status.Progress);
        Assert.Equal(state.Structures.Sum(s => s.MaxHp), status.Target);
    }

    [Fact]
    public void ADamagedProtectStructure_TurnsUrgentAtHalf()
    {
        var fight = FightLibrary.All().First(f => f.Objective?.Kind == ObjectiveKind.Protect);
        var state = Game.Start(fight, seed: 0).NewState;

        Assert.False(ObjectiveStatus.For(state).Urgent);

        var structure = state.Structures[0];
        var hurt = state with
        {
            Structures = new[] { structure with { Hp = 1 } },
        };

        Assert.True(ObjectiveStatus.For(hurt).Urgent);
    }

    [Fact]
    public void ATurnLimit_ShowsAClockAndGoesUrgentNearTheEnd()
    {
        var fight = FightLibrary.All().First(f => f.TurnLimit > 0);
        var state = Game.Start(fight, seed: 0).NewState;

        Assert.Contains("Turn ", ObjectiveStatus.For(state).Clock);

        var late = state with { Round = fight.TurnLimit };
        Assert.True(ObjectiveStatus.For(late).Urgent);
    }

    [Fact]
    public void AFightWithNoLimit_ShowsNoClock()
    {
        var fight = FightLibrary.All().First(f => f.TurnLimit == 0);

        Assert.Equal(string.Empty, ObjectiveStatus.For(Game.Start(fight, seed: 0).NewState).Clock);
    }

    [Fact]
    public void TheFractionIsClampedBothWays()
    {
        var status = new ObjectiveStatus(
            ObjectiveKind.KillAll, "g", "l", 99, 3, "x", string.Empty, false, Array.Empty<Coord>());

        Assert.Equal(1, status.Fraction);
    }
}

/// <summary>
/// Cast aims twice, so the shell arms rather than fires (D-091): pick the enemy, then the tile.
/// </summary>
public sealed class CastAimingTests
{
    private static GameSession Fisher(out UnitId fisher, out UnitId husk)
    {
        var rows = new List<string> { ".........", ".O.^.....", "........." };
        var board = BoardLayout.Parse(rows);

        var units = new List<Unit>
        {
            Unit.FromTemplate(new UnitId(0), UnitKind.Threadcaster, Team.PlayerA) with
            {
                Position = new Coord(2, 1), IsDeployed = true, Verve = Verve.Cap,
            },
            Unit.FromTemplate(new UnitId(1), UnitKind.Husk, Team.Enemy) with
            {
                Position = new Coord(2, 0), IsDeployed = true,
            },
            Unit.FromTemplate(new UnitId(2), UnitKind.Husk, Team.Enemy) with
            {
                Position = new Coord(8, 2), IsDeployed = true,
            },
        };

        fisher = new UnitId(0);
        husk = new UnitId(1);

        var state = new GameState
        {
            Seed = 1,
            RngState = 1,
            Fight = new FightDefinition { Number = 1, Name = "Cast", Board = board },
            Board = board,
            Units = units,
            Round = 1,
            Phase = Phase.Battle,
            ActiveTeam = Team.PlayerA,
            NextPlayerTeam = Team.PlayerA,
            Outcome = FightOutcome.InProgress,
        };

        var session = new GameSession();
        session.AdoptRunStep(
            new EndActivationCommand(new UnitId(0)),
            state,
            new StepResult(state, Array.Empty<GameEvent>(), Game.LegalCommands(state)));

        session.Select(fisher);
        return session;
    }

    [Fact]
    public void NothingIsHighlightedUntilCastIsArmed()
    {
        var session = Fisher(out _, out _);

        Assert.False(session.AimingCast);
        Assert.Empty(session.CastGrabTiles);
        Assert.Empty(session.CastLandings);
    }

    [Fact]
    public void ArmingShowsTheGrabTargets_AndNoLandingsYet()
    {
        var session = Fisher(out var fisher, out var husk);
        session.ToggleCast();

        Assert.True(session.AimingCast);
        Assert.Contains(session.State.UnitById(husk).Position, session.CastGrabTiles);
        Assert.Empty(session.CastLandings);

        // The far Husk is out of grab range, so it is not offered.
        Assert.DoesNotContain(new Coord(8, 2), session.CastGrabTiles);
    }

    [Fact]
    public void PickingTheTarget_SwapsToTheLandingTiles()
    {
        var session = Fisher(out var fisher, out var husk);
        session.ToggleCast();
        session.AimCastAt(husk);

        Assert.Empty(session.CastGrabTiles);
        Assert.NotEmpty(session.CastLandings);

        // Her four orthogonal tiles, minus the one the Husk is standing on being fine to reuse.
        Assert.All(
            session.CastLandings.Keys,
            tile => Assert.Equal(1, session.State.UnitById(fisher).Position.DistanceTo(tile)));
    }

    [Fact]
    public void EveryLandingSaysWhatItDoes()
    {
        var session = Fisher(out _, out var husk);
        session.ToggleCast();
        session.AimCastAt(husk);

        Assert.Equal("drain", PlaytestText.CastOutcome(session.State, new Coord(1, 1)));
        Assert.Equal(
            "spikes " + Displacement.SpikeDamage,
            PlaytestText.CastOutcome(session.State, new Coord(3, 1)));
        Assert.Equal("open", PlaytestText.CastOutcome(session.State, new Coord(2, 2)));
    }

    [Fact]
    public void EveryLandingKnowsWhichSideOfHerItIs()
    {
        // The cone: choosing a landing is choosing a side, so each tile carries the direction it
        // lies in from the Fisher (D-093).
        var session = Fisher(out var fisher, out var husk);
        session.ToggleCast();
        session.AimCastAt(husk);

        var her = session.State.UnitById(fisher).Position;

        foreach (var landing in session.CastLandings.Keys)
        {
            var side = session.CastLandingSide(landing);

            Assert.NotNull(side);
            Assert.Equal(landing, her.Step(side!.Value));
        }

        // All four sides are distinct, which is what makes a cone rather than a smear.
        var sides = session.CastLandings.Keys.Select(session.CastLandingSide).ToList();
        Assert.Equal(sides.Count, sides.Distinct().Count());
    }

    [Fact]
    public void ATileThatIsNotOneOfHers_HasNoSide()
    {
        var session = Fisher(out _, out var husk);
        session.ToggleCast();
        session.AimCastAt(husk);

        Assert.Null(session.CastLandingSide(new Coord(8, 0)));
    }

    [Fact]
    public void SubmittingALanding_ThrowsAndPutsTheAimingAway()
    {
        var session = Fisher(out _, out var husk);
        session.ToggleCast();
        session.AimCastAt(husk);

        session.Submit(session.CastLandings[new Coord(1, 1)]);

        Assert.True(session.State.UnitById(husk).Clinging);
        Assert.False(session.AimingCast);
        Assert.Null(session.CastTarget);
    }

    [Fact]
    public void DisarmingPutsItAwayWithoutThrowing()
    {
        var session = Fisher(out _, out var husk);
        var before = session.State.UnitById(husk).Position;

        session.ToggleCast();
        session.AimCastAt(husk);
        session.ToggleCast();

        Assert.False(session.AimingCast);
        Assert.Null(session.CastTarget);
        Assert.Equal(before, session.State.UnitById(husk).Position);
    }
}
