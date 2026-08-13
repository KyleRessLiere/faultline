using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// Builds a small battle-phase state from string art so rule tests read like the board they test.
/// Layout characters match <see cref="BoardLayout"/>: '.' open, '#' wall, 'O' pit, '^' spikes,
/// 'H' high ground, '~' canal water.
/// </summary>
public sealed class BoardBuilder
{
    private readonly List<string> _rows;
    private readonly List<Placement> _placements = new();
    private readonly List<ReinforcementWave> _waves = new();
    private readonly List<Coord> _blockers = new();
    private readonly List<SluiceStep> _sluices = new();
    private int _blockerHp;
    private Team? _activeTeam;
    private int _seed = 1;
    private Faultline.Core.Objective _objective = Faultline.Core.Objective.KillAll;
    private int _turnLimit;
    private int _round = 1;

    private BoardBuilder(IEnumerable<string> rows) => _rows = new List<string>(rows);

    /// <summary>Starts a builder from layout rows, top row first.</summary>
    public static BoardBuilder Rows(params string[] rows) => new(rows);

    /// <summary>An entirely open board of the given size.</summary>
    public static BoardBuilder Open(int width, int height)
    {
        var rows = new List<string>(height);
        for (int y = 0; y < height; y++)
        {
            rows.Add(new string(BoardLayout.Open, width));
        }

        return new BoardBuilder(rows);
    }

    /// <summary>Places a unit. Units are given ids in the order they are added.</summary>
    public BoardBuilder Place(
        UnitKind kind, Team team, int x, int y, int? hp = null, int? footing = null, bool bedraggled = false)
    {
        _placements.Add(new Placement(kind, team, new Coord(x, y), hp, footing, bedraggled));
        return this;
    }

    /// <summary>Places a player-A unit.</summary>
    public BoardBuilder PlayerA(
        UnitKind kind, int x, int y, int? hp = null, int? footing = null, bool bedraggled = false) =>
        Place(kind, Team.PlayerA, x, y, hp, footing, bedraggled);

    /// <summary>Places a player-B unit.</summary>
    public BoardBuilder PlayerB(
        UnitKind kind, int x, int y, int? hp = null, int? footing = null, bool bedraggled = false) =>
        Place(kind, Team.PlayerB, x, y, hp, footing, bedraggled);

    /// <summary>Places an enemy unit.</summary>
    public BoardBuilder Enemy(UnitKind kind, int x, int y, int? hp = null, int? footing = null) =>
        Place(kind, Team.Enemy, x, y, hp, footing);

    /// <summary>Overrides which team holds the first activation slot.</summary>
    public BoardBuilder Active(Team team)
    {
        _activeTeam = team;
        return this;
    }

    /// <summary>Sets the run seed.</summary>
    public BoardBuilder Seed(int seed)
    {
        _seed = seed;
        return this;
    }

    /// <summary>Gives the fight an objective; structures it calls for are built with the state.</summary>
    public BoardBuilder Objective(ObjectiveKind kind, int rounds = 0, int hp = 0, params Coord[] tiles)
    {
        _objective = new Objective
        {
            Kind = kind,
            Rounds = rounds,
            Hp = hp > 0 ? hp : Faultline.Core.Objective.DefaultHpFor(kind),
            Tiles = tiles,
        };

        return this;
    }

    /// <summary>Marks tiles as breakable blockers, each with the given hit points.</summary>
    public BoardBuilder Blockers(int hp, params Coord[] tiles)
    {
        _blockerHp = hp;
        _blockers.AddRange(tiles);
        return this;
    }

    /// <summary>Caps the fight at a round.</summary>
    public BoardBuilder TurnLimit(int limit)
    {
        _turnLimit = limit;
        return this;
    }

    /// <summary>Schedules a wave of arrivals.</summary>
    public BoardBuilder Wave(int round, params EnemySpawn[] arrivals)
    {
        _waves.Add(new ReinforcementWave(round, arrivals));
        return this;
    }

    /// <summary>
    /// Adds one step of the board's water level: the gate tile that holds it back, then the tiles
    /// the canal takes when it comes down (D-275). Pair it with <see cref="Blockers"/> on the gate
    /// tile — a gate with no masonry on it reads as already fallen, which is the parser's own rule.
    /// </summary>
    public BoardBuilder Sluice(Coord gate, params Coord[] tiles)
    {
        _sluices.Add(new SluiceStep(gate, tiles));
        return this;
    }

    /// <summary>Starts the state on a round other than round 1.</summary>
    public BoardBuilder Round(int round)
    {
        _round = round;
        return this;
    }

    /// <summary>Produces a round-1 battle state with every unit already deployed.</summary>
    public GameState Build()
    {
        var board = BoardLayout.Parse(_rows);
        var units = new List<Unit>(_placements.Count);

        for (int i = 0; i < _placements.Count; i++)
        {
            var placement = _placements[i];
            var unit = Unit.FromTemplate(new UnitId(i), placement.Kind, placement.Team) with
            {
                Position = placement.At,
                IsDeployed = true,
            };

            if (placement.Hp.HasValue)
            {
                unit = unit with { Hp = placement.Hp.Value };
            }

            if (placement.Footing.HasValue)
            {
                unit = unit with { Footing = placement.Footing.Value };
            }

            if (placement.Bedraggled)
            {
                unit = unit with { Bedraggled = true };
            }

            units.Add(unit);
        }

        var active = _activeTeam ?? (_placements.Count > 0 ? _placements[0].Team : Team.PlayerA);

        // What BeginRound does after it opens on Player A: hand the slot to somebody who can actually
        // take it. A fixture whose active side has nobody activatable — every unit clinging, or every
        // unit Bedraggled — is a state the rules never produce, and a test built on one would be
        // testing the builder.
        if (!units.Any(u => u.Team == active && Game.CanActivate(u)))
        {
            foreach (var team in new[] { Team.PlayerA, Team.PlayerB, Team.Enemy })
            {
                if (units.Any(u => u.Team == team && Game.CanActivate(u)))
                {
                    active = team;
                    break;
                }
            }
        }

        var fight = new FightDefinition
        {
            Number = 1,
            Name = "Test",
            Board = board,
            Objective = _objective,
            TurnLimit = _turnLimit,
            Waves = _waves,
            Blockers = _blockers,
            BlockerHp = _blockerHp,
            SluiceSteps = _sluices,
        };

        return new GameState
        {
            Seed = _seed,
            RngState = _seed,
            Fight = fight,
            Board = board,
            Units = units,
            Structures = Objectives.Build(fight),
            Round = _round,
            Phase = Phase.Battle,
            ActiveTeam = active,
            NextPlayerTeam = active.IsPlayer() ? active : Team.PlayerA,
            ActiveUnitId = null,
            Outcome = FightOutcome.InProgress,
        };
    }

    private readonly record struct Placement(
        UnitKind Kind, Team Team, Coord At, int? Hp, int? Footing, bool Bedraggled = false);
}
