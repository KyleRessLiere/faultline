using System;
using System.Collections.Generic;
using System.Linq;
using Faultline.Core;
using Faultline.Web.Shell;
using Faultline.Web.Shell.Playtest;

namespace Faultline.Web.Tests;

/// <summary>
/// Why an action is unavailable, on the button. The reported confusion this answers: an Archer with
/// 2 AP left, a Lobber standing on her toes and a Husk out of range, told only "2 AP left — move or
/// pick an action" while every action she had highlighted nothing when pressed.
/// </summary>
/// <remarks>
/// Every reason asserted here is <see cref="Targeting"/>'s; this file only checks that the words are
/// attached to the right one. A shell that decided legality for itself would be the second copy of a
/// rule the whole layout exists to prevent.
/// </remarks>
public sealed class ActionReasonUiTests
{
    // ---- (b) the dead zone, named -------------------------------------------------------------

    [Fact]
    public void TheDeadZoneReason_NamesTheRuleAndItsNumber()
    {
        var (state, archer) = DeadZone();
        var block = Targeting.BlockOn(state, archer, AttackMode.Damage);

        Assert.Equal(TargetingBlock.TooClose, block);

        string reason = ActionPoints.Reason(
            ActionPoints.Price(archer, Activation.ActionCost), block, archer.Template.MinRange);

        Assert.Contains("too close", reason);
        Assert.Contains("minimum range", reason);
        Assert.Contains(
            archer.Template.MinRange.ToString(System.Globalization.CultureInfo.InvariantCulture),
            reason);
    }

    [Fact]
    public void StaggerShotInTheDeadZone_CarriesTheSameReason()
    {
        var (state, archer) = DeadZone();
        var shot = AbilityDefinition.For(Ability.StaggerShot);

        string reason = ActionPoints.Reason(
            ActionPoints.Price(archer, AbilityDefinition.For(Ability.StaggerShot).Cost),
            Targeting.BlockOn(state, archer, shot),
            shot.MinRange);

        Assert.Contains("too close", reason);
        Assert.Contains("minimum range", reason);
    }

    /// <summary>
    /// The teaching moment's second half: on a ledge over the same adjacent enemy the reason has to
    /// go away, and the target has to be offered. A reason that only ever appears teaches the rule
    /// without its exception, which is the half that makes the ledge worth climbing.
    /// </summary>
    [Fact]
    public void OnHighGroundOverALowerAdjacentEnemy_TheReasonVanishesAndTheTargetIsOffered()
    {
        var session = Ledge(out var archer, out var lobber);
        var state = session.State;
        var held = state.Units.First(u => u.Id == archer.Id);

        var block = Targeting.BlockOn(state, held, AttackMode.Damage);
        Assert.Equal(TargetingBlock.None, block);
        Assert.Equal(
            string.Empty,
            ActionPoints.Reason(
                ActionPoints.Price(held, Activation.ActionCost), block, held.Template.MinRange));

        // Offered, not merely legal: the board has to hand the player a tile to click.
        session.SetMode(ActionMode.Attack);
        Assert.True(session.IsAvailable(ActionMode.Attack));
        Assert.Contains(lobber.Position, session.Targets.Keys);

        // And the ability follows the shot, because the exception is about the arc, not the button.
        Assert.Equal(
            TargetingBlock.None,
            Targeting.BlockOn(state, held, AbilityDefinition.For(Ability.StaggerShot)));
    }

    // ---- (a) the other reasons ----------------------------------------------------------------

    [Fact]
    public void AnUnaffordableAction_KeepsItsNumberAndSaysHowShortItIs()
    {
        var (state, archer) = InRange();
        var broke = archer with { MoveSpent = Activation.PlayerPool };
        var priced = ActionPoints.Price(broke, Activation.ActionCost);

        Assert.NotNull(priced);
        Assert.False(priced!.Affordable);
        Assert.Equal(Activation.ActionCost + " AP", priced.Chip);

        string reason = ActionPoints.Reason(
            priced, Targeting.BlockOn(state, broke, AttackMode.Damage), broke.Template.MinRange);

        Assert.Contains(
            priced.Shortfall.ToString(System.Globalization.CultureInfo.InvariantCulture), reason);
        Assert.Contains("short", reason);
    }

    [Fact]
    public void AnActionWithATarget_CarriesNoReasonAtAll()
    {
        var (state, archer) = InRange();

        Assert.Equal(
            string.Empty,
            ActionPoints.Reason(
                ActionPoints.Price(archer, Activation.ActionCost),
                Targeting.BlockOn(state, archer, AttackMode.Damage),
                archer.Template.MinRange));
    }

    [Fact]
    public void NothingInRange_ReadsAsNoTargetInRange()
    {
        Assert.Equal("no target in range", ActionPoints.BlockText(TargetingBlock.OutOfRange, 0));
        Assert.Equal(
            "already adjacent — nothing to pull", ActionPoints.BlockText(TargetingBlock.NoRoomToPull, 0));
        Assert.Equal(string.Empty, ActionPoints.BlockText(TargetingBlock.None, 2));
    }

    // ---- (c) the summary, and the way out of it -----------------------------------------------

    [Fact]
    public void TheSummarySaysNothingIsInRange_WhenNoActionIsLegalAtAll()
    {
        var (state, archer) = DeadZone();

        Assert.False(Targeting.HasAnyTarget(state, archer));

        string summary = ActionPoints.Summary(
            archer, Targeting.HasAnyTarget(state, archer), null);

        Assert.Contains("nothing in range", summary);
        Assert.Contains("move or pass", summary);
        Assert.Contains(ActionPoints.Remaining(archer).ToString(System.Globalization.CultureInfo.InvariantCulture), summary);
    }

    [Fact]
    public void TheSummaryKeepsItsOldSentence_WhenSomethingIsAimable()
    {
        var (state, archer) = InRange();

        Assert.Contains(
            "move or pick an action",
            ActionPoints.Summary(archer, Targeting.HasAnyTarget(state, archer), null));
    }

    [Fact]
    public void TheInverseHintAppears_WhenOneStepWouldOpenAShot()
    {
        var (state, archer) = DeadZone();

        int? opens = Targeting.MoveNeededToTarget(state, archer);
        Assert.Equal(Activation.StepCost, opens);

        string summary = ActionPoints.Summary(archer, Targeting.HasAnyTarget(state, archer), opens);

        Assert.Contains("nothing in range", summary);
        Assert.Contains(Activation.StepCost + " AP of movement opens a target.", summary);
    }

    [Fact]
    public void TheInverseHintStaysAway_WhenNoWalkWouldHelp()
    {
        var state = Fixture(12, 3, ("archer", 0, 0), ("husk", 11, 2));
        var archer = state.Units.First(u => u.Kind == UnitKind.Archer);

        Assert.Null(Targeting.MoveNeededToTarget(state, archer));

        string summary = ActionPoints.Summary(archer, false, null);

        Assert.Contains("nothing in range", summary);
        Assert.DoesNotContain("opens a target", summary);
    }

    // ---- fixtures ------------------------------------------------------------------------------

    /// <summary>The reported board: a Lobber on her toes, a Husk out of range, 2 AP left.</summary>
    private static (GameState State, Unit Archer) DeadZone()
    {
        var state = Fixture(8, 3, ("archer", 1, 1), ("lobber", 1, 0), ("husk", 7, 1));
        var archer = state.Units.First(u => u.Kind == UnitKind.Archer)
            with { MoveSpent = Activation.StepCost };

        return (state.WithUnit(archer), archer);
    }

    private static (GameState State, Unit Archer) InRange()
    {
        var state = Fixture(8, 3, ("archer", 1, 1), ("husk", 3, 1));
        return (state, state.Units.First(u => u.Kind == UnitKind.Archer));
    }

    /// <summary>An Archer on a ledge with the Lobber standing on the flat right beside it.</summary>
    private static GameSession Ledge(out Unit archer, out Unit lobber)
    {
        var rows = new List<string>
        {
            BoardLayout.HighGround + new string(BoardLayout.Open, 7),
            new string(BoardLayout.Open, 8),
            new string(BoardLayout.Open, 8),
        };

        var board = BoardLayout.Parse(rows);
        archer = Unit.FromTemplate(new UnitId(0), UnitKind.Archer, Team.PlayerA)
            with { Position = new Coord(0, 0), IsDeployed = true };
        lobber = Unit.FromTemplate(new UnitId(1), UnitKind.Lobber, Team.Enemy)
            with { Position = new Coord(1, 0), IsDeployed = true };

        var state = Battle(board, new List<Unit> { archer, lobber });
        var session = new GameSession();
        session.AdoptRunStep(
            new EndActivationCommand(new UnitId(0)),
            state,
            new StepResult(state, Array.Empty<GameEvent>(), Game.LegalCommands(state)));

        session.Select(archer.Id);
        return session;
    }

    private static GameState Fixture(int width, int height, params (string Kind, int X, int Y)[] units)
    {
        var rows = new List<string>(height);
        for (int y = 0; y < height; y++)
        {
            rows.Add(new string(BoardLayout.Open, width));
        }

        var board = BoardLayout.Parse(rows);
        var placed = new List<Unit>();

        foreach (var entry in units)
        {
            var kind = entry.Kind switch
            {
                "archer" => UnitKind.Archer,
                "lobber" => UnitKind.Lobber,
                _ => UnitKind.Husk,
            };

            placed.Add(Unit.FromTemplate(new UnitId(placed.Count), kind, kind == UnitKind.Archer ? Team.PlayerA : Team.Enemy)
                with { Position = new Coord(entry.X, entry.Y), IsDeployed = true });
        }

        return Battle(board, placed);
    }

    private static GameState Battle(Board board, IReadOnlyList<Unit> units) => new()
    {
        Seed = 1,
        RngState = 1,
        Fight = new FightDefinition { Number = 1, Name = "Reasons", Board = board },
        Board = board,
        Units = units,
        Round = 1,
        Phase = Phase.Battle,
        ActiveTeam = Team.PlayerA,
        NextPlayerTeam = Team.PlayerA,
        Outcome = FightOutcome.InProgress,
    };
}
