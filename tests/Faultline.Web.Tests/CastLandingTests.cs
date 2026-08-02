using System;
using System.Collections.Generic;
using System.Linq;
using Faultline.Core;
using Faultline.Web.Shell;

namespace Faultline.Web.Tests;

/// <summary>
/// Aiming a Cast at the tile that matters. A drain is the whole reason to spend three Pluck on a
/// throw, so a cone that offers one and a click that does nothing with it is the feature failing at
/// the only landing anybody cares about.
/// </summary>
public sealed class CastLandingTests
{
    [Fact]
    public void TheDrainIsOfferedAsALanding()
    {
        var session = Aimed(out _, out _);

        Assert.Contains(new Coord(3, 3), session.CastLandings.Keys);
    }

    [Fact]
    public void ClickingTheDrain_SubmitsTheCastThatLandsThere()
    {
        var session = Aimed(out var fisher, out var anchor);

        Assert.True(session.CastLandings.TryGetValue(new Coord(3, 3), out var command));

        var cast = Assert.IsType<SpendVerveCommand>(command);
        Assert.Equal(VerveSpend.Cast, cast.Spend);
        Assert.Equal(anchor.Id, cast.TargetId);
        Assert.Equal(new Coord(3, 3), cast.To);

        session.Submit(command!);

        var landed = session.State.Units.First(u => u.Id == anchor.Id);
        Assert.Equal(new Coord(3, 3), landed.Position);
        Assert.True(landed.Clinging);
        Assert.Equal(3, session.State.Units.First(u => u.Id == fisher.Id).Verve);
    }

    // Every one of her four tiles is a separate decision, so every one has to key to its own command.
    [Fact]
    public void AllFourSidesAreOffered_EachToItsOwnTile()
    {
        var session = Aimed(out _, out _);

        var landings = session.CastLandings;

        Assert.Equal(4, landings.Count);
        foreach (var tile in new[] { new Coord(3, 3), new Coord(2, 4), new Coord(4, 4), new Coord(3, 5) })
        {
            Assert.True(landings.ContainsKey(tile), tile.ToString());
            Assert.Equal(tile, ((SpendVerveCommand)landings[tile]).To);
        }
    }

    // The reported position had the Fisher's move already spent, which is the one thing the plain
    // fixture does not carry. Since D-097 the move half is a budget and an action closes it, so a
    // spender offered after a walk is worth pinning separately.
    [Fact]
    public void TheDrainIsStillOffered_AfterHerMoveIsSpent()
    {
        var session = Aimed(out var fisher, out var anchor, moveSpent: true);

        Assert.True(session.State.Units.First(u => u.Id == fisher.Id).HasMoved);
        Assert.True(session.CastLandings.TryGetValue(new Coord(3, 3), out var command));

        session.Submit(command!);

        Assert.True(session.State.Units.First(u => u.Id == anchor.Id).Clinging);
    }

    // Fisher (3,4) on 1 HP with a full meter, drain at (3,3), Anchor at (2,5) already grabbed.
    private static GameSession Aimed(out Unit fisher, out Unit anchor, bool moveSpent = false)
    {
        var board = BoardLayout.Parse(new[]
        {
            ".........",
            ".........",
            ".........",
            "...O.....",
            ".........",
            ".........",
            ".........",
        });

        var units = new List<Unit>
        {
            Unit.FromTemplate(new UnitId(0), UnitKind.Threadcaster, Team.PlayerA)
                with
                {
                    Position = new Coord(3, 4), IsDeployed = true, Hp = 1, Verve = Verve.Cap,
                    MoveSpent = moveSpent ? UnitTemplate.For(UnitKind.Threadcaster).Move : 0,
                },
            Unit.FromTemplate(new UnitId(1), UnitKind.Anchor, Team.Enemy)
                with { Position = new Coord(2, 5), IsDeployed = true, Hp = 3 },
            Unit.FromTemplate(new UnitId(2), UnitKind.Grappler, Team.Enemy)
                with { Position = new Coord(5, 0), IsDeployed = true },
        };

        var state = new GameState
        {
            Seed = 1,
            RngState = 1,
            Fight = new FightDefinition { Number = 209, Name = "The Trench", Board = board },
            Board = board,
            Units = units,
            Round = 4,
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

        fisher = units[0];
        anchor = units[1];

        session.Select(fisher.Id);
        session.ToggleCast();
        session.AimCastAt(anchor.Id);
        return session;
    }
}
