using System;
using System.Collections.Generic;
using System.Linq;
using Faultline.Core;
using Faultline.Web.Shell;
using Faultline.Web.Shell.Playtest;

namespace Faultline.Web.Tests;

/// <summary>
/// Aiming an ability when a unit has more than one of them. The shell used to fold every ability's
/// commands into a single tile map and to decide how to aim from whichever of a command's optional
/// fields happened to be set, which left Spear Thrust aimed at a charge destination it does not have
/// and Guard Stance with no way to be pressed at all. These pin the shape-driven replacement.
/// </summary>
public sealed class AbilityAimingTests
{
    // A battle-phase board built straight from Core's public surface. Core.Tests has a richer
    // BoardBuilder, but the shell's test project references only the Blazor project, and a shell
    // test needs no more board than the tiles it clicks.
    private sealed class Fixture
    {
        private readonly List<Unit> _units = new();
        private readonly int _width;
        private readonly int _height;
        private Team _active = Team.PlayerB;

        public Fixture(int width, int height)
        {
            _width = width;
            _height = height;
        }

        public Fixture Place(UnitKind kind, Team team, int x, int y)
        {
            _units.Add(Unit.FromTemplate(new UnitId(_units.Count), kind, team) with
            {
                Position = new Coord(x, y),
                IsDeployed = true,
            });

            return this;
        }

        public Fixture Active(Team team)
        {
            _active = team;
            return this;
        }

        public Fixture Guarding(UnitKind kind)
        {
            for (int i = 0; i < _units.Count; i++)
            {
                if (_units[i].Kind == kind)
                {
                    _units[i] = _units[i] with { Guarding = true };
                }
            }

            return this;
        }

        public GameState State()
        {
            var rows = new List<string>(_height);
            for (int y = 0; y < _height; y++)
            {
                rows.Add(new string(BoardLayout.Open, _width));
            }

            var board = BoardLayout.Parse(rows);

            return new GameState
            {
                Seed = 1,
                RngState = 1,
                Fight = new FightDefinition { Number = 1, Name = "Aiming", Board = board },
                Board = board,
                Units = _units,
                Round = 1,
                Phase = Phase.Battle,
                ActiveTeam = _active,
                NextPlayerTeam = _active.IsPlayer() ? _active : Team.PlayerA,
                Outcome = FightOutcome.InProgress,
            };
        }

        public GameSession Session()
        {
            var state = State();
            var session = new GameSession();

            // Hands the session a hand-built position the same way a run hands it one. No command is
            // being replayed, so the one passed in is only the recorder's label and it is off.
            session.AdoptRunStep(
                new EndActivationCommand(new UnitId(0)),
                state,
                new StepResult(state, Array.Empty<GameEvent>(), Game.LegalCommands(state)));

            return session;
        }
    }

    private static Unit Find(GameSession session, UnitKind kind) =>
        session.State.Units.First(u => u.Kind == kind);

    private static GameSession Wardbearer(out Unit ward, params (UnitKind Kind, int X, int Y)[] enemies)
    {
        var fixture = new Fixture(7, 5).Place(UnitKind.Wardbearer, Team.PlayerB, 3, 2);
        foreach (var enemy in enemies)
        {
            fixture.Place(enemy.Kind, Team.Enemy, enemy.X, enemy.Y);
        }

        var session = fixture.Session();
        ward = Find(session, UnitKind.Wardbearer);
        session.Select(ward.Id);
        return session;
    }

    // ---- picking which ability --------------------------------------------------------------

    [Fact]
    public void SelectedAbilities_ForTheWardbearer_OffersBothOfThem()
    {
        var session = Wardbearer(out _, (UnitKind.Husk, 4, 2));

        Assert.Equal(
            new[] { Ability.SpearThrust, Ability.GuardStance },
            session.SelectedAbilities.Select(a => a.Ability));
    }

    [Fact]
    public void ArmingAnAbility_AimsThatAbilityAndNoOther()
    {
        var session = Wardbearer(out _, (UnitKind.Husk, 4, 2));

        session.SetAbility(Ability.SpearThrust);

        Assert.Equal(ActionMode.Ability, session.Mode);
        Assert.Equal(Ability.SpearThrust, session.ArmedAbility);
        Assert.All(
            session.Targets.Values.OfType<AbilityCommand>(),
            c => Assert.Equal(Ability.SpearThrust, c.Ability));
    }

    [Fact]
    public void ArmingTheStance_ReplacesTheSpearsTilesRatherThanAddingToThem()
    {
        // The bug this whole change is about: two abilities merged into one map means neither is
        // aimed. Arming one has to empty out the other's tiles.
        var session = Wardbearer(out _, (UnitKind.Husk, 4, 2));
        session.SetAbility(Ability.SpearThrust);
        Assert.NotEmpty(session.Targets);

        session.SetAbility(Ability.GuardStance);

        Assert.Empty(session.Targets);
        Assert.Equal(Ability.GuardStance, session.ArmedAbility);
    }

    [Fact]
    public void EveryAbilityTheWardbearerHas_IsAvailableToArm()
    {
        var session = Wardbearer(out _, (UnitKind.Husk, 4, 2));

        Assert.True(session.IsAbilityAvailable(Ability.SpearThrust));
        Assert.True(session.IsAbilityAvailable(Ability.GuardStance));
        Assert.True(session.IsAvailable(ActionMode.Ability));
    }

    [Fact]
    public void AnAbilityWithNothingOnItsLine_IsNotOffered()
    {
        // No enemy anywhere near, so Core lists no Spear Thrust command — but the stance is always
        // available, and availability must be asked per ability rather than per mode.
        var session = Wardbearer(out _, (UnitKind.Husk, 0, 0));

        Assert.False(session.IsAbilityAvailable(Ability.SpearThrust));
        Assert.True(session.IsAbilityAvailable(Ability.GuardStance));
    }

    [Fact]
    public void SetMode_Ability_WithoutNamingOne_ArmsTheFirstUsable()
    {
        var session = Wardbearer(out _, (UnitKind.Husk, 4, 2));

        session.SetMode(ActionMode.Ability);

        Assert.Equal(Ability.SpearThrust, session.ArmedAbility);
    }

    [Fact]
    public void LeavingAbilityMode_DisarmsWhateverWasArmed()
    {
        var session = Wardbearer(out _, (UnitKind.Husk, 4, 2));
        session.SetAbility(Ability.SpearThrust);

        session.SetMode(ActionMode.Attack);

        Assert.Null(session.ArmedAbility);
        Assert.Empty(session.Targets.Values.OfType<AbilityCommand>());
    }

    // ---- Line: Spear Thrust -----------------------------------------------------------------

    [Fact]
    public void SpearThrust_MapsTheTilesTheLineHits_NotAChargeDestination()
    {
        var session = Wardbearer(out var ward, (UnitKind.Husk, 4, 2), (UnitKind.Lobber, 5, 2));
        session.SetAbility(Ability.SpearThrust);

        Assert.Equal(
            new[] { new Coord(4, 2), new Coord(5, 2) },
            session.Targets.Keys.OrderBy(c => c.X).ToArray());

        // The old code aimed any Direction ability with PreviewCharge, which for a unit standing at
        // (3,2) would have put the click on a tile the spear never touches.
        Assert.DoesNotContain(ward.Position, session.Targets.Keys);
    }

    [Fact]
    public void SpearThrust_WithOnlyOneEnemyOnTheLine_IsStillAimable()
    {
        var session = Wardbearer(out _, (UnitKind.Husk, 4, 2));
        session.SetAbility(Ability.SpearThrust);

        var tile = Assert.Single(session.Targets.Keys);
        Assert.Equal(new Coord(4, 2), tile);
    }

    [Fact]
    public void SpearThrust_CanBeAimedInEachOfTheFourDirections()
    {
        var session = Wardbearer(
            out _,
            (UnitKind.Husk, 3, 1),
            (UnitKind.Husk, 4, 2),
            (UnitKind.Husk, 3, 3),
            (UnitKind.Husk, 2, 2));

        session.SetAbility(Ability.SpearThrust);

        var byTile = session.Targets.ToDictionary(
            p => p.Key, p => ((AbilityCommand)p.Value).Direction!.Value);

        Assert.Equal(Direction.Up, byTile[new Coord(3, 1)]);
        Assert.Equal(Direction.Right, byTile[new Coord(4, 2)]);
        Assert.Equal(Direction.Down, byTile[new Coord(3, 3)]);
        Assert.Equal(Direction.Left, byTile[new Coord(2, 2)]);
    }

    [Fact]
    public void SpearThrust_Fired_Deals2ToTheNearEnemyAnd4ToTheOneBeyond()
    {
        var session = Wardbearer(out _, (UnitKind.Husk, 4, 2), (UnitKind.Lobber, 5, 2));
        var husk = Find(session, UnitKind.Husk);
        var lobber = Find(session, UnitKind.Lobber);
        session.SetAbility(Ability.SpearThrust);

        session.Submit(session.Targets[new Coord(4, 2)]);

        Assert.Equal(husk.Hp - 2, session.State.UnitById(husk.Id).Hp);
        Assert.Equal(lobber.Hp - 4, session.State.UnitById(lobber.Id).Hp);
    }

    [Fact]
    public void SpearThrust_Fired_MovesNobody()
    {
        var session = Wardbearer(out var ward, (UnitKind.Husk, 4, 2), (UnitKind.Lobber, 5, 2));
        var husk = Find(session, UnitKind.Husk);
        session.SetAbility(Ability.SpearThrust);

        session.Submit(session.Targets[new Coord(4, 2)]);

        Assert.Equal(new Coord(3, 2), session.State.UnitById(ward.Id).Position);
        Assert.Equal(new Coord(4, 2), session.State.UnitById(husk.Id).Position);
    }

    [Fact]
    public void SpearThrust_HighlightedTilesAndHoverText_DescribeTheSameHits()
    {
        // Requirement: the tiles that light up have to agree with the sentence beside them.
        var session = Wardbearer(out _, (UnitKind.Husk, 4, 2), (UnitKind.Lobber, 5, 2));
        session.SetAbility(Ability.SpearThrust);
        session.Hover(new Coord(4, 2));

        string text = session.PreviewText!;

        Assert.Contains("2 to " + Find(session, UnitKind.Husk).Name, text);
        Assert.Contains("4 to " + Find(session, UnitKind.Lobber).Name, text);
        Assert.Contains(new Coord(4, 2), session.Targets.Keys);
        Assert.Contains(new Coord(5, 2), session.Targets.Keys);
    }

    [Fact]
    public void SpearThrust_ProjectedPath_IsTheRunTheLineCovers()
    {
        var session = Wardbearer(out _, (UnitKind.Husk, 4, 2));
        session.SetAbility(Ability.SpearThrust);
        session.Hover(new Coord(4, 2));

        Assert.Equal(
            new[] { new Coord(4, 2), new Coord(5, 2) },
            session.ProjectedPath.OrderBy(c => c.X).ToArray());
    }

    // ---- Self: Guard Stance -----------------------------------------------------------------

    [Fact]
    public void GuardStance_ArmsWithoutClaimingAnyTileOnTheBoard()
    {
        var session = Wardbearer(out _, (UnitKind.Husk, 4, 2));

        session.SetAbility(Ability.GuardStance);

        Assert.Empty(session.Targets);
        Assert.Empty(session.RangeTiles);
    }

    [Fact]
    public void GuardStance_IsIssuedFromTheActionPanel_NotByClickingATile()
    {
        var session = Wardbearer(out var ward, (UnitKind.Husk, 4, 2));
        session.SetAbility(Ability.GuardStance);

        var command = session.SelfAbilityCommand;

        Assert.NotNull(command);
        Assert.Equal(Ability.GuardStance, command!.Ability);
        Assert.Equal(ward.Id, command.UnitId);
    }

    [Fact]
    public void SelfAbilityCommand_IsOfferedOnlyWhileAStanceIsArmed()
    {
        var session = Wardbearer(out _, (UnitKind.Husk, 4, 2));

        session.SetAbility(Ability.SpearThrust);

        Assert.Null(session.SelfAbilityCommand);
    }

    [Fact]
    public void GuardStance_Submitted_PutsTheStanceUp()
    {
        var session = Wardbearer(out var ward, (UnitKind.Husk, 4, 2));
        session.SetAbility(Ability.GuardStance);

        session.Submit(session.SelfAbilityCommand!);

        Assert.True(session.State.UnitById(ward.Id).Guarding);
    }

    [Fact]
    public void AGuardingUnit_SaysSoInTheStatusFlags()
    {
        var session = new Fixture(7, 5)
            .Place(UnitKind.Wardbearer, Team.PlayerB, 3, 2)
            .Place(UnitKind.Husk, Team.Enemy, 4, 2)
            .Guarding(UnitKind.Wardbearer)
            .Session();

        string flags = PlaytestText.Flags(Find(session, UnitKind.Wardbearer));

        Assert.Contains(PlaytestText.GuardName.ToLowerInvariant(), flags);
    }

    [Fact]
    public void TheStanceGoingUpAndLapsing_BothReadAsSentencesInTheLog()
    {
        // Without a case of its own the event fell through to its own type name — "GuardStanceChanged"
        // in the middle of the transcript.
        var state = new Fixture(3, 1).Place(UnitKind.Wardbearer, Team.PlayerB, 1, 0).State();
        var ward = state.Units[0];

        string up = EventText.Describe(new GuardStanceChanged(ward.Id, ward.Position, true), state);
        string down = EventText.Describe(new GuardStanceChanged(ward.Id, ward.Position, false), state);

        Assert.Contains(PlaytestText.GuardName, up);
        Assert.Contains(PlaytestText.GuardName, down);
        Assert.DoesNotContain(nameof(GuardStanceChanged), up);
        Assert.DoesNotContain(nameof(GuardStanceChanged), down);
        Assert.NotEqual(up, down);
    }

    [Fact]
    public void TheStanceName_IsCoresName_NotTheShells()
    {
        Assert.Equal(AbilityDefinition.For(Ability.GuardStance).Name, PlaytestText.GuardName);
        Assert.Equal(AbilityDefinition.For(Ability.GuardStance).Summary, PlaytestText.GuardSummary);
    }

    [Fact]
    public void TheStance_LapsesWhenTheWardbearerActivatesAgain()
    {
        var session = new Fixture(7, 5)
            .Place(UnitKind.Wardbearer, Team.PlayerB, 3, 2)
            .Place(UnitKind.Husk, Team.Enemy, 6, 4)
            .Guarding(UnitKind.Wardbearer)
            .Session();

        var ward = Find(session, UnitKind.Wardbearer);
        Assert.True(session.State.UnitById(ward.Id).Guarding);

        session.Select(ward.Id);
        session.SetMode(ActionMode.Move);
        session.Submit(session.Targets.Values.First());

        Assert.False(session.State.UnitById(ward.Id).Guarding);
    }

    // ---- Direction: Bull Rush, the regression risk -------------------------------------------

    [Fact]
    public void BullRush_StillAimsAtTheEnemyTheChargeWouldReach()
    {
        var session = new Fixture(7, 1)
            .Active(Team.PlayerA)
            .Place(UnitKind.Vanguard, Team.PlayerA, 0, 0)
            .Place(UnitKind.Husk, Team.Enemy, 3, 0)
            .Session();

        var vanguard = Find(session, UnitKind.Vanguard);
        session.Select(vanguard.Id);
        session.SetAbility(Ability.BullRush);

        var tile = Assert.Single(session.Targets.Keys);
        Assert.Equal(new Coord(3, 0), tile);
        Assert.Equal(Ability.BullRush, ((AbilityCommand)session.Targets[tile]).Ability);
    }

    [Fact]
    public void BullRush_StillFiresTheChargeAndTheShove()
    {
        var session = new Fixture(7, 1)
            .Active(Team.PlayerA)
            .Place(UnitKind.Vanguard, Team.PlayerA, 0, 0)
            .Place(UnitKind.Husk, Team.Enemy, 3, 0)
            .Session();

        var vanguard = Find(session, UnitKind.Vanguard);
        var husk = Find(session, UnitKind.Husk);
        session.Select(vanguard.Id);
        session.SetAbility(Ability.BullRush);

        session.Submit(session.Targets[new Coord(3, 0)]);

        Assert.Equal(new Coord(2, 0), session.State.UnitById(vanguard.Id).Position);
        Assert.Equal(new Coord(5, 0), session.State.UnitById(husk.Id).Position);
    }

    [Fact]
    public void BullRush_HoverStillReadsAsACharge()
    {
        var session = new Fixture(7, 1)
            .Active(Team.PlayerA)
            .Place(UnitKind.Vanguard, Team.PlayerA, 0, 0)
            .Place(UnitKind.Husk, Team.Enemy, 3, 0)
            .Session();

        session.Select(Find(session, UnitKind.Vanguard).Id);
        session.SetAbility(Ability.BullRush);
        session.Hover(new Coord(3, 0));

        Assert.StartsWith("Charge ", session.PreviewText);
        Assert.Equal(
            new[] { new Coord(1, 0), new Coord(2, 0), new Coord(4, 0), new Coord(5, 0) },
            session.ProjectedPath.OrderBy(c => c.X).ToArray());
    }

    // ---- Enemy targeting, unchanged ----------------------------------------------------------

    [Fact]
    public void AnEnemyTargetedAbility_StillAimsAtTheEnemysTile()
    {
        var session = new Fixture(7, 1)
            .Active(Team.PlayerA)
            .Place(UnitKind.Archer, Team.PlayerA, 0, 0)
            .Place(UnitKind.Husk, Team.Enemy, 2, 0)
            .Session();

        session.Select(Find(session, UnitKind.Archer).Id);
        session.SetAbility(Ability.StaggerShot);

        var tile = Assert.Single(session.Targets.Keys);
        Assert.Equal(new Coord(2, 0), tile);
    }
}
