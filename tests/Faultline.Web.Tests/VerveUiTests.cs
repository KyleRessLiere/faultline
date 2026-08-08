using System;
using System.Collections.Generic;
using System.Linq;
using Faultline.Core;
using Faultline.Web.Shell;
using Faultline.Web.Shell.Playtest;

namespace Faultline.Web.Tests;

/// <summary>
/// The shell's half of Verve: what it offers, what it refuses to offer, and what it says happened.
/// </summary>
/// <remarks>
/// The point of most of these is that the shell reads legality off Core's list rather than working it
/// out again. Twice already this project has shipped a bug from the shell inferring a Core concept
/// from data that happened to be lying around (D-069) — and Verve is a rich seam for it, because half
/// its legality is invisible on the unit: Slingshot needs a Reel to have just landed.
/// </remarks>
public sealed class VerveUiTests
{
    private sealed class Fixture
    {
        private readonly List<Unit> _units = new();
        private readonly int _width;
        private readonly int _height;
        private Team _active = Team.PlayerA;

        public Fixture(int width, int height)
        {
            _width = width;
            _height = height;
        }

        public Fixture Place(
            UnitKind kind, Team team, int x, int y, int verve = 0, bool guarding = false, int? hp = null)
        {
            var unit = Unit.FromTemplate(new UnitId(_units.Count), kind, team);
            _units.Add(unit with
            {
                Position = new Coord(x, y),
                IsDeployed = true,
                Verve = verve,
                Guarding = guarding,
                Hp = hp ?? unit.Hp,
            });

            return this;
        }

        public Fixture Active(Team team)
        {
            _active = team;
            return this;
        }

        public GameSession Session()
        {
            var rows = new List<string>(_height);
            for (int y = 0; y < _height; y++)
            {
                rows.Add(new string(BoardLayout.Open, _width));
            }

            var board = BoardLayout.Parse(rows);

            var state = new GameState
            {
                Seed = 1,
                RngState = 1,
                Fight = new FightDefinition { Number = 1, Name = "Verve", Board = board },
                Board = board,
                Units = _units,
                Round = 1,
                Phase = Phase.Battle,
                ActiveTeam = _active,
                NextPlayerTeam = _active.IsPlayer() ? _active : Team.PlayerA,
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

    private static Unit Find(GameSession session, UnitKind kind) =>
        session.State.Units.First(u => u.Kind == kind);

    // ---- what the shell offers -----------------------------------------------------------

    [Fact]
    public void AUnitWithEnoughVerve_IsOfferedItsSpender()
    {
        var session = new Fixture(7, 3)
            .Place(UnitKind.Vanguard, Team.PlayerA, 1, 1, verve: Verve.Cap)
            .Place(UnitKind.Husk, Team.Enemy, 3, 1)
            .Session();

        session.Select(Find(session, UnitKind.Vanguard).Id);

        Assert.True(session.CanSpendVerve);
        Assert.Equal(VerveSpend.WreckingWeight, session.VerveSpendCommand!.Spend);
    }

    [Fact]
    public void AUnitBelowTheCost_IsNotOfferedIt()
    {
        var session = new Fixture(7, 3)
            .Place(UnitKind.Vanguard, Team.PlayerA, 1, 1, verve: 1)
            .Place(UnitKind.Husk, Team.Enemy, 3, 1)
            .Session();

        session.Select(Find(session, UnitKind.Vanguard).Id);

        Assert.False(session.CanSpendVerve);
        Assert.Null(session.VerveSpendCommand);
    }

    [Fact]
    public void AFisherWithSomebodyInReach_IsOfferedCast()
    {
        var session = new Fixture(7, 3)
            .Place(UnitKind.Threadcaster, Team.PlayerA, 1, 1, verve: Verve.Cap)
            .Place(UnitKind.Husk, Team.Enemy, 2, 1)
            .Session();

        session.Select(Find(session, UnitKind.Threadcaster).Id);

        Assert.True(session.CanSpendVerve);
        Assert.Equal(VerveSpend.Cast, session.VerveSpendCommand!.Spend);
    }

    [Fact]
    public void AFisherWithNobodyAdjacent_IsNotOfferedCast()
    {
        // Still the point of reading legality off Core: she has the class, the meter and the
        // activation, and the spend is illegal on a fact about the board rather than about her.
        var session = new Fixture(7, 3)
            .Place(UnitKind.Threadcaster, Team.PlayerA, 1, 1, verve: Verve.Cap)
            .Place(UnitKind.Husk, Team.Enemy, 5, 1)
            .Session();

        session.Select(Find(session, UnitKind.Threadcaster).Id);

        Assert.False(session.CanSpendVerve);
    }

    [Fact]
    public void AWardbearerAtFullHealth_IsNotOfferedPreen()
    {
        // Nothing to patch up, so nothing to spend on.
        var session = new Fixture(7, 3)
            .Place(UnitKind.Wardbearer, Team.PlayerB, 1, 1, verve: Verve.Cap)
            .Place(UnitKind.Husk, Team.Enemy, 2, 1)
            .Active(Team.PlayerB)
            .Session();

        session.Select(Find(session, UnitKind.Wardbearer).Id);

        Assert.False(session.CanSpendVerve);
    }

    [Fact]
    public void AHurtWardbearer_IsOfferedPreen()
    {
        var session = new Fixture(7, 3)
            .Place(UnitKind.Wardbearer, Team.PlayerB, 1, 1, verve: Verve.Cap, hp: 6)
            .Place(UnitKind.Husk, Team.Enemy, 2, 1)
            .Active(Team.PlayerB)
            .Session();

        session.Select(Find(session, UnitKind.Wardbearer).Id);

        Assert.True(session.CanSpendVerve);
        Assert.Equal(VerveSpend.Preen, session.VerveSpendCommand!.Spend);
    }

    [Fact]
    public void SpendingSubmitsCoresOwnCommand_AndTheMeterDrops()
    {
        var session = new Fixture(7, 3)
            .Place(UnitKind.Vanguard, Team.PlayerA, 1, 1, verve: Verve.Cap)
            .Place(UnitKind.Husk, Team.Enemy, 3, 1)
            .Session();

        var vanguard = Find(session, UnitKind.Vanguard).Id;
        session.Select(vanguard);
        session.SpendVerve();

        var after = session.State.UnitById(vanguard);
        Assert.Equal(Verve.Cap - Verve.CostOf(VerveSpend.WreckingWeight), after.Verve);
        Assert.True(after.WreckingWeightArmed);

        // And it is gone from the offer, because one spend is all an activation gets.
        Assert.False(session.CanSpendVerve);
    }

    [Fact]
    public void SpendingWhenNothingIsOffered_DoesNothing()
    {
        var session = new Fixture(7, 3)
            .Place(UnitKind.Vanguard, Team.PlayerA, 1, 1)
            .Place(UnitKind.Husk, Team.Enemy, 3, 1)
            .Session();

        var vanguard = Find(session, UnitKind.Vanguard).Id;
        session.Select(vanguard);
        session.SpendVerve();

        Assert.Equal(0, session.State.UnitById(vanguard).Verve);
    }

    // ---- what the shell says ---------------------------------------------------------------

    [Fact]
    public void EveryClassWithAMeter_HasItsConditionAndSpenderInWords()
    {
        var session = new Fixture(7, 3)
            .Place(UnitKind.Vanguard, Team.PlayerA, 1, 1, verve: 2)
            .Session();

        var vanguard = Find(session, UnitKind.Vanguard);
        string title = PlaytestText.VerveTitle(vanguard);

        Assert.Contains(Naming.Meter + " 2/" + Verve.Cap, title);
        Assert.Contains(Verve.NameOf(VerveSpend.WreckingWeight), title);
        Assert.Contains(Verve.ConditionFor(UnitKind.Vanguard), title);
    }

    [Fact]
    public void TheTooltipSaysHowMuchMoreIsNeeded_UntilItIsAffordable()
    {
        var session = new Fixture(7, 3)
            .Place(UnitKind.Vanguard, Team.PlayerA, 1, 1)
            .Place(UnitKind.Archer, Team.PlayerA, 2, 1, verve: Verve.Cap)
            .Session();

        Assert.Contains("2 more for", PlaytestText.VerveTitle(Find(session, UnitKind.Vanguard)));
        Assert.Contains("ready", PlaytestText.VerveTitle(Find(session, UnitKind.Archer)));
    }

    [Fact]
    public void AClassWithNoMeter_HasNoTooltip()
    {
        var session = new Fixture(7, 3)
            .Place(UnitKind.Vanguard, Team.PlayerA, 1, 1)
            .Place(UnitKind.Husk, Team.Enemy, 3, 1)
            .Session();

        Assert.Equal(string.Empty, PlaytestText.VerveTitle(Find(session, UnitKind.Husk)));
    }

    [Fact]
    public void AWastedCharge_SaysSoRatherThanReadingLikeAGain()
    {
        var session = new Fixture(7, 3).Place(UnitKind.Vanguard, Team.PlayerA, 1, 1).Session();
        var vanguard = Find(session, UnitKind.Vanguard).Id;

        string earned = EventText.Describe(
            new VerveCharged(vanguard, VerveSource.Collision, new Coord(1, 1), 3, false),
            session.State);

        string wasted = EventText.Describe(
            new VerveCharged(vanguard, VerveSource.Guard, new Coord(1, 1), Verve.Cap, true),
            session.State);

        Assert.Contains("+1 " + Naming.MeterLower, earned);
        Assert.Contains("a collision", earned);

        Assert.Contains("+0 " + Naming.MeterLower, wasted);
        Assert.Contains("full", wasted);
        Assert.DoesNotContain("+1", wasted);
    }

    [Fact]
    public void ASpendAndASwap_ReadAsSomethingRatherThanAsATypeName()
    {
        var session = new Fixture(7, 3)
            .Place(UnitKind.Threadcaster, Team.PlayerA, 1, 1)
            .Place(UnitKind.Husk, Team.Enemy, 2, 1)
            .Session();

        var caster = Find(session, UnitKind.Threadcaster).Id;
        var husk = Find(session, UnitKind.Husk).Id;

        string spent = EventText.Describe(
            new VerveSpent(caster, VerveSpend.Cast, new Coord(1, 1), 3, 2), session.State);
        string thrown = EventText.Describe(
            new UnitPushed(husk, new Coord(2, 1), new Coord(3, 1), new[] { new Coord(3, 1) },
                DisplacementKind.Throw, 1),
            session.State);

        Assert.Contains(Verve.NameOf(VerveSpend.Cast), spent);
        Assert.Contains("Fisher", spent);
        Assert.Contains("thrown", thrown);

        Assert.NotEqual(nameof(VerveSpent), spent);
        Assert.NotEqual(nameof(UnitPushed), thrown);
    }

    [Fact]
    public void EveryVerveSourceHasWording_WithNoFallthroughInventingOne()
    {
        // The fallthrough here says it does not know. A default case that produced a plausible
        // sentence is exactly how "Charge 3 to (4,0)" and "hold position" shipped.
        foreach (VerveSource source in Enum.GetValues(typeof(VerveSource)))
        {
            string text = EventText.VerveSourceText(source);
            Assert.DoesNotContain("no wording written", text);
            Assert.NotEqual(source.ToString(), text);
        }
    }

    // ---- the meter ticks ------------------------------------------------------------------

    [Fact]
    public void AChargePutsATickInTheAnimation()
    {
        var events = new GameEvent[]
        {
            new VerveCharged(new UnitId(0), VerveSource.Collision, new Coord(2, 2), 1, false),
        };

        var beats = BoardAnimation.Plan(events);

        var tick = Assert.Single(beats);
        Assert.Equal(BoardBeatKind.Charge, tick.Kind);
        Assert.Equal(new UnitId(0), tick.UnitId);
        Assert.Equal(new Coord(2, 2), tick.Tile);
        Assert.True(BoardAnimation.BeatMs(BoardBeatKind.Charge, 100) > 0);
    }

    [Fact]
    public void AWastedChargeTicksToo()
    {
        // Seeing nothing happen is the feedback: it is what tells a player at the cap to go and spend.
        var beats = BoardAnimation.Plan(new GameEvent[]
        {
            new VerveCharged(new UnitId(0), VerveSource.Guard, new Coord(0, 0), Verve.Cap, true),
        });

        Assert.Equal(BoardBeatKind.Charge, Assert.Single(beats).Kind);
    }
}

/// <summary>
/// The deployment threat overlay after D-089: one enemy at a time, on hover, and never the union.
/// </summary>
public sealed class DeploymentThreatTests
{
    private static (PlaytestView View, GameState State) Deploying(string fightId)
    {
        var state = Game.Start(FightLibrary.ById(fightId), seed: 0).NewState;
        return (new PlaytestView(), state);
    }

    [Fact]
    public void HoveringOneEnemy_ShowsThatEnemysReach_FromCore()
    {
        var (view, state) = Deploying("the-teeth");
        var enemy = state.Units.First(u => u.Team == Team.Enemy && u.IsOnBoard);

        var shown = view.ThreatFrom(state, enemy.Id);

        Assert.NotEmpty(shown);
        Assert.Equal(
            Threat.ForUnit(state, enemy).OrderBy(c => c.X).ThenBy(c => c.Y),
            shown.OrderBy(c => c.X).ThenBy(c => c.Y));
    }

    [Fact]
    public void OneEnemysReach_IsAFractionOfTheUnion()
    {
        // The reason the union was dropped: on a 7x7 it is nearly the whole board, and a board
        // painted almost entirely red says only "somewhere is dangerous".
        var (view, state) = Deploying("the-teeth");
        var enemy = state.Units.First(u => u.Team == Team.Enemy && u.IsOnBoard);

        Assert.True(view.ThreatFrom(state, enemy.Id).Count < Threat.DamageRound1(state).Count);
    }

    [Fact]
    public void HoveringNothingOrAPlayerUnit_ShowsNothing()
    {
        var (view, state) = Deploying("the-teeth");
        var player = state.Units.First(u => u.Team.IsPlayer());

        Assert.Empty(view.ThreatFrom(state, player.Id));
        Assert.Empty(view.ThreatFrom(state, null));
    }

    [Fact]
    public void CoreStillKnowsTheUnion_BecauseTheBoardLintNeedsIt()
    {
        // D-080's law did not go away with its overlay: the validation that campaign boards must
        // offer a safe deployment is the durable half, and it reads exactly this.
        var (_, state) = Deploying("the-teeth");

        Assert.NotEmpty(Threat.DamageRound1(state));

        // Since Stage C1 no shipped campaign board reports an unsafe side, so the second half is
        // asked of a board authored to break the law rather than of one that used to.
        //
        // The swarm is parked on the WHOLE spot list, not on one player's doorstep. §3's spots
        // belong to neither player, so a board can be short of safe tiles for everybody or for
        // nobody — "unsafe for B while A is fine" is a state the shared pool cannot produce.
        var parked = FightParser.Parse(string.Join(
            "\n",
            "id: the-teeth",
            "name: Teeth, with the swarm parked on the spots",
            "roster a: Vanguard, Threadcaster",
            "roster b: Wardbearer, Archer",
            "spawn h = Husk",
            "design: The swarm is parked on the spots deliberately - the thesis is that there is",
            "design: nowhere clean to stand, so the draft is about who eats the first hit.",
            "board:",
            "  ....h**",
            "  ....h.*",
            "  .......",
            "  .......",
            "  .......",
            "  *h.....",
            "  **.....")).Fight!;

        Assert.Empty(Threat.UnsafeSides(FightLibrary.ById("the-teeth")));
        Assert.NotEmpty(Threat.UnsafeSides(parked));
    }
}
