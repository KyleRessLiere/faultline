using System;
using System.Collections.Generic;
using Faultline.Core;

namespace Faultline.Web.Shell;

/// <summary>
/// The mutable thing the scenario creator paints onto, and the <see cref="FightDefinition"/> it
/// turns into.
/// </summary>
/// <remarks>
/// This model is deliberately shaped like the file format rather than like the game: terrain,
/// deploy slots and enemies share one grid, and a tile can only be one of them. It answers no rules
/// questions — whether the result is playable is decided by round-tripping it through
/// <see cref="FightWriter"/> and <see cref="FightParser"/>, which is the same code path an authored
/// file takes.
/// </remarks>
public sealed class ScenarioDraft
{
    /// <summary>Smallest board the creator offers.</summary>
    public const int MinSize = 5;

    /// <summary>Largest board the creator offers.</summary>
    public const int MaxSize = 9;

    private readonly HashSet<Coord> _zoneA = new();
    private readonly HashSet<Coord> _zoneB = new();
    private readonly Dictionary<Coord, UnitKind> _enemies = new();
    private TileType[] _tiles = Array.Empty<TileType>();

    private ScenarioDraft()
    {
    }

    /// <summary>Stable slug written as <c>id:</c>.</summary>
    public string Id { get; set; } = "my-scenario";

    /// <summary>Display name.</summary>
    public string Name { get; set; } = "My Scenario";

    /// <summary>One-line description shown in the picker.</summary>
    public string Description { get; set; } = "A scenario built in the creator.";

    /// <summary>One-based index into the run.</summary>
    public int Number { get; set; } = 99;

    /// <summary>Column count.</summary>
    public int Width { get; private set; }

    /// <summary>Row count.</summary>
    public int Height { get; private set; }

    /// <summary>Player A's classes, in deployment order.</summary>
    public List<UnitKind> RosterA { get; } = new() { UnitKind.Vanguard, UnitKind.Archer };

    /// <summary>Player B's classes, in deployment order.</summary>
    public List<UnitKind> RosterB { get; } = new() { UnitKind.Threadcaster, UnitKind.Wardbearer };

    /// <summary>A blank 7x7 with a 2x2 deploy zone in each of two opposite corners.</summary>
    /// <returns>A draft that is already playable in one click.</returns>
    public static ScenarioDraft Blank()
    {
        var draft = new ScenarioDraft();
        draft.Resize(7, 7);
        return draft;
    }

    /// <summary>Every coordinate on the draft board, row-major.</summary>
    /// <returns>Coordinates top row first.</returns>
    public IEnumerable<Coord> AllCoords()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                yield return new Coord(x, y);
            }
        }
    }

    /// <summary>Terrain under a tile.</summary>
    /// <param name="at">Tile to read.</param>
    /// <returns>Its terrain.</returns>
    public TileType TileAt(Coord at) => _tiles[(at.Y * Width) + at.X];

    /// <summary>Whether a tile is a deploy slot.</summary>
    /// <param name="at">Tile to read.</param>
    /// <param name="team">Which player's zone to test.</param>
    /// <returns>Whether that player may deploy there.</returns>
    public bool IsZone(Coord at, Team team) => (team == Team.PlayerA ? _zoneA : _zoneB).Contains(at);

    /// <summary>The enemy standing on a tile, if any.</summary>
    /// <param name="at">Tile to read.</param>
    /// <returns>Its enemy kind, or <c>null</c>.</returns>
    public UnitKind? EnemyAt(Coord at) => _enemies.TryGetValue(at, out var kind) ? kind : null;

    /// <summary>Resizes the board, keeping whatever still fits.</summary>
    /// <param name="width">New column count, clamped to <see cref="MinSize"/>..<see cref="MaxSize"/>.</param>
    /// <param name="height">New row count, same clamp.</param>
    public void Resize(int width, int height)
    {
        int w = Clamp(width);
        int h = Clamp(height);
        bool fresh = _tiles.Length == 0;

        var kept = new TileType[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                kept[(y * w) + x] = !fresh && x < Width && y < Height ? TileAt(new Coord(x, y)) : TileType.Open;
            }
        }

        Width = w;
        Height = h;
        _tiles = kept;

        Drop(_zoneA);
        Drop(_zoneB);

        var gone = new List<Coord>();
        foreach (var pair in _enemies)
        {
            if (!InBounds(pair.Key))
            {
                gone.Add(pair.Key);
            }
        }

        foreach (var coord in gone)
        {
            _enemies.Remove(coord);
        }

        // Shrinking can take every slot of a zone off the board, which is an error the designer did
        // not ask for. Put that zone back in its corner rather than leave the draft unplayable.
        if (_zoneA.Count == 0)
        {
            Seed(_zoneA, left: true, top: false);
        }

        if (_zoneB.Count == 0)
        {
            Seed(_zoneB, left: false, top: true);
        }
    }

    /// <summary>Paints terrain, clearing whatever occupied the tile.</summary>
    /// <param name="at">Tile to paint.</param>
    /// <param name="tile">Terrain to write.</param>
    public void PaintTerrain(Coord at, TileType tile)
    {
        Clear(at);
        _tiles[(at.Y * Width) + at.X] = tile;
    }

    /// <summary>Toggles a deploy slot. The tile underneath becomes Open, as the format requires.</summary>
    /// <param name="at">Tile to paint.</param>
    /// <param name="team">Whose slot.</param>
    public void PaintZone(Coord at, Team team)
    {
        var zone = team == Team.PlayerA ? _zoneA : _zoneB;
        bool already = zone.Contains(at);
        Clear(at);

        if (!already)
        {
            zone.Add(at);
        }
    }

    /// <summary>Places an enemy, or clears the tile when one is already standing there.</summary>
    /// <param name="at">Tile to paint.</param>
    /// <param name="kind">Enemy archetype.</param>
    public void PlaceEnemy(Coord at, UnitKind kind)
    {
        bool already = _enemies.ContainsKey(at);
        Clear(at);

        if (!already)
        {
            _enemies[at] = kind;
        }
    }

    /// <summary>Returns a tile to plain open floor with nothing on it.</summary>
    /// <param name="at">Tile to erase.</param>
    public void Erase(Coord at)
    {
        Clear(at);
        _tiles[(at.Y * Width) + at.X] = TileType.Open;
    }

    /// <summary>Assembles the draft into a fight definition.</summary>
    /// <returns>A definition ready for <see cref="FightWriter.Write"/>.</returns>
    public FightDefinition ToDefinition()
    {
        var zoneA = new List<Coord>();
        var zoneB = new List<Coord>();
        var enemies = new List<EnemySpawn>();

        // Row-major order is what the parser collects in, and unit ids follow it. Emitting in the
        // same order keeps a saved file's ids identical to the draft that was just played.
        foreach (var coord in AllCoords())
        {
            if (_zoneA.Contains(coord))
            {
                zoneA.Add(coord);
            }

            if (_zoneB.Contains(coord))
            {
                zoneB.Add(coord);
            }

            if (_enemies.TryGetValue(coord, out var kind))
            {
                enemies.Add(new EnemySpawn(kind, coord));
            }
        }

        return new FightDefinition
        {
            Id = CustomFightStore.Slug(Id),
            Number = Number,
            Name = string.IsNullOrWhiteSpace(Name) ? string.Empty : Name.Trim(),
            Description = Description?.Trim() ?? string.Empty,
            Board = Board.Create(Width, Height, _tiles),
            RosterA = RosterA.ToArray(),
            RosterB = RosterB.ToArray(),
            DeploymentZoneA = zoneA,
            DeploymentZoneB = zoneB,
            Enemies = enemies,
        };
    }

    private static int Clamp(int value) => value < MinSize ? MinSize : value > MaxSize ? MaxSize : value;

    private bool InBounds(Coord at) => at.X >= 0 && at.Y >= 0 && at.X < Width && at.Y < Height;

    private void Clear(Coord at)
    {
        _zoneA.Remove(at);
        _zoneB.Remove(at);
        _enemies.Remove(at);
        _tiles[(at.Y * Width) + at.X] = TileType.Open;
    }

    private void Drop(HashSet<Coord> zone)
    {
        var gone = new List<Coord>();
        foreach (var coord in zone)
        {
            if (!InBounds(coord))
            {
                gone.Add(coord);
            }
        }

        foreach (var coord in gone)
        {
            zone.Remove(coord);
        }
    }

    private void Seed(HashSet<Coord> zone, bool left, bool top)
    {
        for (int y = 0; y < 2; y++)
        {
            for (int x = 0; x < 2; x++)
            {
                var at = new Coord(left ? x : Width - 1 - x, top ? y : Height - 1 - y);
                zone.Add(at);
                _enemies.Remove(at);
                _tiles[(at.Y * Width) + at.X] = TileType.Open;
            }
        }
    }
}
