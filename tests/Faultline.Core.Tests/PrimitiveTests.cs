using System;
using Faultline.Core;

namespace Faultline.Core.Tests;

public class PrimitiveTests
{
    [Theory]
    [InlineData(0, 0, 0, 0, 0)]
    [InlineData(0, 0, 1, 0, 1)]
    [InlineData(0, 0, 1, 1, 2)]
    [InlineData(2, 3, -1, 3, 3)]
    public void Coord_DistanceIsOrthogonalSteps(int ax, int ay, int bx, int by, int expected)
    {
        Assert.Equal(expected, new Coord(ax, ay).DistanceTo(new Coord(bx, by)));
    }

    [Fact]
    public void Coord_AdjacencyIsFourWayNotEight()
    {
        var origin = new Coord(1, 1);

        Assert.True(origin.IsAdjacentTo(new Coord(1, 0)));
        Assert.True(origin.IsAdjacentTo(new Coord(0, 1)));
        Assert.False(origin.IsAdjacentTo(new Coord(0, 0)));
        Assert.False(origin.IsAdjacentTo(new Coord(2, 2)));
    }

    [Theory]
    [InlineData(Direction.Up, 0, -1)]
    [InlineData(Direction.Right, 1, 0)]
    [InlineData(Direction.Down, 0, 1)]
    [InlineData(Direction.Left, -1, 0)]
    public void Direction_OffsetsMatchTheGrid(Direction direction, int dx, int dy)
    {
        Assert.Equal(new Coord(dx, dy), direction.Offset());
        Assert.Equal(new Coord(0, 0), direction.Offset() + direction.Opposite().Offset());
    }

    [Theory]
    [InlineData(0, 0, 3, 0, Direction.Right)]
    [InlineData(0, 0, -3, 0, Direction.Left)]
    [InlineData(0, 0, 0, 3, Direction.Down)]
    [InlineData(0, 0, 0, -3, Direction.Up)]
    [InlineData(0, 0, 3, 1, Direction.Right)]
    [InlineData(0, 0, 1, 3, Direction.Down)]
    public void Direction_TowardSnapsToTheDominantAxis(int fx, int fy, int tx, int ty, Direction expected)
    {
        Assert.Equal(expected, Directions.Toward(new Coord(fx, fy), new Coord(tx, ty)));
    }

    [Fact]
    public void Direction_TowardBreaksDiagonalTiesHorizontally()
    {
        // Brief §2 pushes "directly away from the source"; a perfect diagonal has no such line, so
        // DECISIONS.md D-003 fixes the tie on the horizontal axis rather than leaving it undefined.
        Assert.Equal(Direction.Right, Directions.Toward(new Coord(0, 0), new Coord(2, 2)));
        Assert.Equal(Direction.Left, Directions.Toward(new Coord(0, 0), new Coord(-2, 2)));
    }

    [Fact]
    public void Direction_TowardTheSameTileIsUndefined()
    {
        Assert.Null(Directions.Toward(new Coord(3, 3), new Coord(3, 3)));
    }

    [Fact]
    public void Board_ParsesEveryLayoutCharacter()
    {
        var board = BoardLayout.Parse(new[] { ".#O", "^H." });

        Assert.Equal(TileType.Open, board.At(new Coord(0, 0)));
        Assert.Equal(TileType.Wall, board.At(new Coord(1, 0)));
        Assert.Equal(TileType.Pit, board.At(new Coord(2, 0)));
        Assert.Equal(TileType.Spikes, board.At(new Coord(0, 1)));
        Assert.Equal(TileType.HighGround, board.At(new Coord(1, 1)));
    }

    [Fact]
    public void Board_RejectsRaggedLayouts()
    {
        Assert.Throws<System.ArgumentException>(() => BoardLayout.Parse(new[] { "...", ".." }));
    }

    [Fact]
    public void Board_RejectsUnknownCharacters()
    {
        Assert.Throws<System.ArgumentException>(() => BoardLayout.Parse(new[] { "..z" }));
    }

    [Fact]
    public void Board_WithDoesNotMutateTheOriginal()
    {
        var original = BoardLayout.Parse(new[] { "..." });
        var changed = original.With(new Coord(1, 0), TileType.Wall);

        Assert.Equal(TileType.Open, original.At(new Coord(1, 0)));
        Assert.Equal(TileType.Wall, changed.At(new Coord(1, 0)));
    }

    [Fact]
    public void Board_RejectsOutOfBoundsAccess()
    {
        var board = BoardLayout.Parse(new[] { "..." });

        Assert.False(board.InBounds(new Coord(3, 0)));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => board.At(new Coord(3, 0)));
    }

    [Fact]
    public void Teams_PlayersAreAlliesAndOpposeEnemies()
    {
        Assert.False(Team.PlayerA.IsHostileTo(Team.PlayerB));
        Assert.True(Team.PlayerA.IsHostileTo(Team.Enemy));
        Assert.True(Team.Enemy.IsHostileTo(Team.PlayerB));
        Assert.Equal(Team.PlayerB, Team.PlayerA.OtherPlayer());
        Assert.Equal(Team.PlayerA, Team.PlayerB.OtherPlayer());
    }

    [Theory]
    [InlineData(UnitKind.Vanguard, 7, 3)]
    [InlineData(UnitKind.Archer, 4, 3)]
    [InlineData(UnitKind.Threadcaster, 4, 3)]
    [InlineData(UnitKind.Wardbearer, 6, 3)]
    [InlineData(UnitKind.Husk, 2, 3)]
    [InlineData(UnitKind.Lobber, 3, 2)]
    [InlineData(UnitKind.Anchor, 6, 1)]
    [InlineData(UnitKind.Grappler, 5, 3)]
    [InlineData(UnitKind.Stalker, 4, 4)]
    public void UnitTemplate_MatchesTheBriefStatTables(UnitKind kind, int maxHp, int move)
    {
        var template = UnitTemplate.For(kind);

        Assert.Equal(maxHp, template.MaxHp);
        Assert.Equal(move, template.Move);
    }

    [Fact]
    public void UnitTemplate_NoArchetypeStartsWithFooting()
    {
        // Footing is granted by a scenario's 'footing:' key, never by the archetype. A blanket token
        // on everyone shortens every shove by a tile and makes resisting a push the default.
        foreach (UnitKind kind in Enum.GetValues(typeof(UnitKind)))
        {
            Assert.Equal(0, UnitTemplate.For(kind).Footing);
        }
    }

    [Fact]
    public void UnitTemplate_OnlyTheArcherClimbsForFree()
    {
        Assert.True(UnitTemplate.For(UnitKind.Archer).FreeClimb);
        Assert.False(UnitTemplate.For(UnitKind.Vanguard).FreeClimb);
        Assert.False(UnitTemplate.For(UnitKind.Wardbearer).FreeClimb);
    }
}
