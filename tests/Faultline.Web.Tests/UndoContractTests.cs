using System;
using System.Collections.Generic;
using System.Linq;
using Faultline.Core;
using Faultline.Web.Shell;
using Faultline.Web.Shell.Playtest;

namespace Faultline.Web.Tests;

/// <summary>
/// The undo contract: <b>one press takes back one command, and only while it is still the acting
/// player's to take back.</b>
/// </summary>
/// <remarks>
/// <para>
/// Undo is shell policy over a Core-owned log, not a rule. Core decides what is legal and what a
/// command does; this decides which of the commands already played the shell is still willing to
/// drop off the end before replaying. Nothing here changes a rule, and the replay itself is Core's
/// determinism guarantee — seed plus command log reproduces the state exactly.
/// </para>
/// <para>
/// The boundaries are refusals with words. A button that quietly does nothing teaches a player that
/// undo is unreliable, which is worse than a button that says why it will not fire — the same
/// reason-beside-the-refusal shape <see cref="Targeting.BlockOn"/> and
/// <see cref="ActionPoints.Reason"/> already carry.
/// </para>
/// </remarks>
public sealed class UndoContractTests
{
    private const string Board = "hz-10-bone-yard";

    private static GameSession Fresh()
    {
        var session = new GameSession();
        session.StartFight(FightLibrary.ById(Board), GameSession.DefaultSeed);
        return session;
    }

    private static GameSession Deployed()
    {
        var session = Fresh();
        session.SettleDraftOrder();

        while (session.Legal.OfType<DeployCommand>().FirstOrDefault() is { } deploy)
        {
            session.Submit(deploy);
        }

        return session;
    }

    // One tile at a time, so a test that means "a segment" cannot accidentally be handed a walk.
    private static IReadOnlyList<MoveCommand> Segments(GameSession session) =>
        session.Legal.OfType<MoveCommand>().Where(m => m.Path.Count == 1).ToList();

    // ---- one press, one command ----------------------------------------------------------------

    [Fact]
    public void OnePress_TakesBackOneMoveSegment_AndNotTheWholeWalk()
    {
        // D-097 made a click a segment rather than a walk. Undo has to match it: a player who took
        // three steps to look at something wants the third one back, not the whole trip.
        var session = Deployed();
        var opening = session.State;

        var first = Segments(session)[0];
        session.Submit(first);
        var afterFirst = session.State;

        var second = Segments(session).First(m => m.UnitId == first.UnitId);
        session.Submit(second);
        Assert.NotEqual(afterFirst, session.State);

        Assert.True(session.Undo());
        Assert.Equal(afterFirst, session.State);

        Assert.True(session.Undo());
        Assert.Equal(opening, session.State);
    }

    [Fact]
    public void TheButton_NamesTheSegmentItWouldTakeBack()
    {
        var session = Deployed();
        var move = Segments(session)[0];

        session.Submit(move);

        Assert.True(session.CanUndo);
        Assert.Null(session.UndoBlockedReason);
        Assert.Equal("undo move segment to " + BoardCoords.Of(move.To), session.UndoDescription);
    }

    [Fact]
    public void TheDescriptionAndTheReason_AreNeverBothEmptyAndNeverBothSet()
    {
        // The reason-sibling invariant: exactly one of the two is speaking at any moment.
        var session = Fresh();
        AssertInvariant(session);

        // Step 1 is a decision the button has to be able to name too, so it is walked through here
        // rather than skipped past.
        session.SettleDraftOrder();
        AssertInvariant(session);

        session.Submit(session.Legal.OfType<DeployCommand>().First());
        AssertInvariant(session);

        session = Deployed();
        AssertInvariant(session);

        session.Submit(session.Legal.OfType<EndActivationCommand>().First());
        AssertInvariant(session);

        static void AssertInvariant(GameSession session)
        {
            Assert.Equal(session.CanUndo, session.UndoBlockedReason is null);
            Assert.Equal(session.CanUndo, session.UndoDescription.Length > 0);
        }
    }

    // ---- the boundaries, one named refusal each -------------------------------------------------

    [Fact]
    public void WithNothingPlayed_TheReasonIsThereIsNothingToUndo()
    {
        var session = Fresh();

        Assert.False(session.CanUndo);
        Assert.False(session.Undo());
        Assert.Equal("nothing to undo", session.UndoBlockedReason);
        Assert.Equal(string.Empty, session.UndoDescription);
    }

    [Fact]
    public void OnceTheEnemyHasActed_TheRoundIsCommitted()
    {
        // The hardest boundary of the set. An enemy activation is information the player did not
        // have when they moved, and a rewind past it is a rewind into a fight that no longer exists.
        var session = Deployed();
        session.Submit(session.Legal.OfType<EndActivationCommand>().First());

        Assert.True(session.AwaitingEnemy);
        while (session.AwaitingEnemy)
        {
            session.ResolveEnemyActivation();
        }

        Assert.False(session.CanUndo);
        Assert.False(session.Undo());
        Assert.Equal("enemy has acted — round is committed", session.UndoBlockedReason);
    }

    [Fact]
    public void OnceTheTurnIsEnded_TheClosedActivationCannotBeReopened()
    {
        var session = Deployed();
        var before = session.State;

        session.Submit(session.Legal.OfType<EndActivationCommand>().First());
        Assert.NotEqual(before, session.State);

        Assert.False(session.CanUndo);
        Assert.False(session.Undo());
        Assert.Equal(
            "end turn is committed — a closed activation cannot be reopened",
            session.UndoBlockedReason);
    }

    [Fact]
    public void AClosedActivationWhoseSlotWentToTheOtherPlayer_IsNotTheirsToTakeBack()
    {
        // Hotseat is one button and two people. Once the slot has passed to the other player, the
        // button in front of them is not a handle on the activation that just finished.
        Assert.Equal(
            GameSession.UndoBlock.NotYours,
            GameSession.BlockOn(
                chosen: true,
                drewFromTheSeed: false,
                turnedTheRound: false,
                endedTheTurn: false,
                closedTheActivation: true,
                actorTeam: Team.PlayerA,
                slotTeam: Team.PlayerB));

        Assert.Equal(
            "that was Player A's activation — the slot has passed on",
            GameSession.UndoWords(GameSession.UndoBlock.NotYours, Team.PlayerA));
    }

    [Fact]
    public void AClosedActivationWhoseSlotWentToTheEnemy_IsStillTheActingPlayersUntilTheEnemyMoves()
    {
        // The counterpart, and the reason the rule is about the slot rather than about the
        // activation being closed: the enemy holding a slot it has not used yet has revealed
        // nothing, so the action that closed the activation is still one press from coming back.
        Assert.Equal(
            GameSession.UndoBlock.None,
            GameSession.BlockOn(
                chosen: true,
                drewFromTheSeed: false,
                turnedTheRound: false,
                endedTheTurn: false,
                closedTheActivation: true,
                actorTeam: Team.PlayerA,
                slotTeam: Team.Enemy));
    }

    [Fact]
    public void AnEnemysOwnCommand_IsNeverOfferedBack()
    {
        Assert.Equal(
            GameSession.UndoBlock.EnemyActed,
            GameSession.BlockOn(
                chosen: false,
                drewFromTheSeed: false,
                turnedTheRound: false,
                endedTheTurn: false,
                closedTheActivation: false,
                actorTeam: Team.Enemy,
                slotTeam: Team.Enemy));
    }

    [Fact]
    public void ASeededDraw_ClosesTheDoorBehindIt()
    {
        Assert.Equal(
            GameSession.UndoBlock.Randomised,
            GameSession.BlockOn(
                chosen: true,
                drewFromTheSeed: true,
                turnedTheRound: false,
                endedTheTurn: false,
                closedTheActivation: false,
                actorTeam: Team.PlayerA,
                slotTeam: Team.PlayerA));

        Assert.Equal(
            "a seeded roll has been made — its result is on the table",
            GameSession.UndoWords(GameSession.UndoBlock.Randomised, Team.PlayerA));
    }

    [Fact]
    public void NoDrawHasBeenConsumedYet_SoTheSeedBoundaryIsArmedRatherThanFiring()
    {
        // As-built: Core advances no generator state anywhere, so this boundary has never yet been
        // the reason a rewind was refused. It is wired to GameState.RngState rather than to a list
        // of commands, so the first draw that lands arms it without anybody editing this file.
        var session = Deployed();
        session.Submit(Segments(session)[0]);

        Assert.Equal(session.State.Seed, session.State.RngState);
        Assert.True(session.CanUndo);
    }

    [Fact]
    public void TheRoundTurning_ClosesTheDoorBehindIt()
    {
        Assert.Equal(
            GameSession.UndoBlock.RoundTurned,
            GameSession.BlockOn(
                chosen: true,
                drewFromTheSeed: false,
                turnedTheRound: true,
                endedTheTurn: false,
                closedTheActivation: true,
                actorTeam: Team.PlayerA,
                slotTeam: Team.PlayerA));

        Assert.Equal(
            "the round has turned — the new plans are on the table",
            GameSession.UndoWords(GameSession.UndoBlock.RoundTurned, Team.PlayerA));
    }

    // ---- Pluck ---------------------------------------------------------------------------------

    [Fact]
    public void APluckSpend_IsTakenBackWhileTheActivationIsStillOpen()
    {
        // A spend is a declaration until something resolves on it: it costs no action point, closes
        // no activation and reveals nothing, so it crosses none of the boundaries.
        var session = WithVanguardAtFullPluck();
        var vanguard = session.State.Units.First(u => u.Kind == UnitKind.Vanguard);

        session.Select(vanguard.Id);
        Assert.True(session.CanSpendVerve);

        session.SpendVerve();

        Assert.Equal(vanguard.Id, session.State.ActiveUnitId);
        Assert.True(session.CanUndo);
        Assert.Equal("undo Wrecking Weight", session.UndoDescription);
    }

    [Fact]
    public void EverySpender_IsNamedTheWayThePlayerKnowsIt()
    {
        // Prose reads the design names, never the identifiers (MASTER_DESIGN §15) — the meter is
        // Pluck on screen and Verve in the C#, and this is a screen string.
        var id = new UnitId(0);

        Assert.Equal(
            "undo Wrecking Weight",
            GameSession.DescribeUndo(null, new SpendVerveCommand(id, VerveSpend.WreckingWeight)));
        Assert.Equal(
            "undo Double Nock",
            GameSession.DescribeUndo(null, new SpendVerveCommand(id, VerveSpend.DoubleNock)));
        Assert.Equal(
            "undo Stagger Shot",
            GameSession.DescribeUndo(null, new AbilityCommand(id, Ability.StaggerShot, id)));
    }

    // ---- the recorded log carries only what was committed ---------------------------------------

    [Fact]
    public void TheCommandLog_RecordsOnlyCommittedCommands()
    {
        // The point of the whole design. Undo is a truncate-and-replay over the same log the export
        // is built from, so a command that was taken back was never played as far as the record is
        // concerned — and the record has to be byte-identical to the fight that was actually played.
        var rewound = Recording();
        var directly = Recording();

        var abandoned = Segments(rewound)[0];
        var kept = Segments(rewound).First(m => !m.To.Equals(abandoned.To));

        rewound.Submit(abandoned);
        Assert.True(rewound.Undo());
        rewound.Submit(kept);

        directly.Submit(kept);

        Assert.Equal(directly.State, rewound.State);
        Assert.Equal(directly.State.GetHashCode(), rewound.State.GetHashCode());

        Assert.Equal(directly.RenderCombatLog(), rewound.RenderCombatLog());
        Assert.Equal(directly.RenderCombatLog().GetHashCode(), rewound.RenderCombatLog().GetHashCode());
        Assert.Equal(directly.RecordedLineCount, rewound.RecordedLineCount);
        Assert.True(rewound.RecordingIsComplete);

        static GameSession Recording()
        {
            var session = new GameSession();
            session.SetRecording(true);
            session.StartFight(FightLibrary.ById(Board), GameSession.DefaultSeed);

            session.SettleDraftOrder();

            while (session.Legal.OfType<DeployCommand>().FirstOrDefault() is { } deploy)
            {
                session.Submit(deploy);
            }

            return session;
        }
    }

    [Fact]
    public void AnAbandonedSegment_LeavesNoTraceInTheExport()
    {
        var session = new GameSession();
        session.SetRecording(true);
        session.StartFight(FightLibrary.ById(Board), GameSession.DefaultSeed);

        session.SettleDraftOrder();

        while (session.Legal.OfType<DeployCommand>().FirstOrDefault() is { } deploy)
        {
            session.Submit(deploy);
        }

        string beforeAnything = session.RenderCombatLog();

        session.Submit(Segments(session)[0]);
        Assert.NotEqual(beforeAnything, session.RenderCombatLog());

        Assert.True(session.Undo());
        Assert.Equal(beforeAnything, session.RenderCombatLog());
    }

    // ---- fixture -------------------------------------------------------------------------------

    // A Vanguard who has already earned her spend, next to somebody to spend it on. Built the way
    // VerveUiTests builds one: the meter is charged by play, and no authored fight hands one over.
    private static GameSession WithVanguardAtFullPluck()
    {
        var rows = new List<string>();
        for (int y = 0; y < 3; y++)
        {
            rows.Add(new string(BoardLayout.Open, 7));
        }

        var board = BoardLayout.Parse(rows);

        var vanguard = Unit.FromTemplate(new UnitId(0), UnitKind.Vanguard, Team.PlayerA) with
        {
            Position = new Coord(1, 1),
            IsDeployed = true,
            Verve = Verve.Cap,
        };

        var husk = Unit.FromTemplate(new UnitId(1), UnitKind.Husk, Team.Enemy) with
        {
            Position = new Coord(3, 1),
            IsDeployed = true,
        };

        var state = new GameState
        {
            Seed = 1,
            RngState = 1,
            Fight = new FightDefinition { Number = 1, Name = "Pluck", Board = board },
            Board = board,
            Units = new[] { vanguard, husk },
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

        return session;
    }
}
