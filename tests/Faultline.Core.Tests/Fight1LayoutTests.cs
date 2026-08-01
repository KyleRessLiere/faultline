using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// Guards the board constraints in Brief §2 "Board" for the one authored fight. These are cheap and
/// catch a hand-edited layout drifting out of spec.
/// </summary>
public class Fight1LayoutTests
{
    private static readonly FightDefinition Fight = FightLibrary.Fight1();

    [Fact]
    public void Board_IsSevenBySeven()
    {
        Assert.Equal(7, Fight.Board.Width);
        Assert.Equal(7, Fight.Board.Height);
    }

    [Fact]
    public void CentreThreeByThree_IsClearAtStart()
    {
        for (int y = 2; y <= 4; y++)
        {
            for (int x = 2; x <= 4; x++)
            {
                Assert.Equal(TileType.Open, Fight.Board.At(new Coord(x, y)));
            }
        }
    }

    [Fact]
    public void SpikeCount_IsWithinTheAuthoredRange()
    {
        int spikes = Fight.Board.AllCoords().Count(c => Fight.Board.At(c) == TileType.Spikes);

        Assert.InRange(spikes, 2, 3);
    }

    [Fact]
    public void WallsAndPits_SitOnTheOuterTwoRings()
    {
        foreach (var coord in Fight.Board.AllCoords())
        {
            var tile = Fight.Board.At(coord);
            if (tile == TileType.Wall || tile == TileType.Pit)
            {
                Assert.True(
                    RingOf(coord) <= 1,
                    tile + " at " + coord + " is on ring " + RingOf(coord) + ", not the outer two.");
            }
        }
    }

    [Fact]
    public void DeploymentZones_AreWalkableAndDoNotOverlap()
    {
        Assert.NotEmpty(Fight.DeploymentZoneA);
        Assert.NotEmpty(Fight.DeploymentZoneB);
        Assert.Empty(Fight.DeploymentZoneA.Intersect(Fight.DeploymentZoneB));

        foreach (var tile in Fight.DeploymentZoneA.Concat(Fight.DeploymentZoneB))
        {
            Assert.True(Movement.IsWalkable(Fight.Board.At(tile)), tile + " is not walkable.");
        }
    }

    [Fact]
    public void DeploymentZones_AreLargeEnoughForTheirRosters()
    {
        Assert.True(Fight.DeploymentZoneA.Count >= Fight.RosterA.Count);
        Assert.True(Fight.DeploymentZoneB.Count >= Fight.RosterB.Count);
    }

    [Fact]
    public void DeploymentZones_SitInOppositeCorners()
    {
        // A holds the bottom-left corner, B the top-right one.
        Assert.All(Fight.DeploymentZoneA, c => Assert.True(c.X <= 1 && c.Y >= 5));
        Assert.All(Fight.DeploymentZoneB, c => Assert.True(c.X >= 5 && c.Y <= 1));
    }

    [Fact]
    public void EnemySpawns_AreWalkableDistinctAndOnOppositeEdges()
    {
        Assert.NotEmpty(Fight.Enemies);
        Assert.Equal(Fight.Enemies.Count, Fight.Enemies.Select(e => e.At).Distinct().Count());

        foreach (var spawn in Fight.Enemies)
        {
            Assert.True(Movement.IsWalkable(Fight.Board.At(spawn.At)), spawn.At + " is not walkable.");
            Assert.True(spawn.At.Y == 0 || spawn.At.Y == Fight.Board.Height - 1);
        }

        Assert.Contains(Fight.Enemies, e => e.At.Y == 0);
        Assert.Contains(Fight.Enemies, e => e.At.Y == Fight.Board.Height - 1);
    }

    [Fact]
    public void EnemySpawns_DoNotOverlapDeploymentZones()
    {
        var zones = Fight.DeploymentZoneA.Concat(Fight.DeploymentZoneB).ToHashSet();

        Assert.All(Fight.Enemies, e => Assert.DoesNotContain(e.At, zones));
    }

    [Fact]
    public void Roster_IsTwoUnitsPerPlayer()
    {
        Assert.Equal(2, Fight.RosterA.Count);
        Assert.Equal(2, Fight.RosterB.Count);
        Assert.Empty(Fight.RosterA.Intersect(Fight.RosterB));
    }

    [Fact]
    public void Enemies_AreOnlyTheArchetypesFightOneAuthorises()
    {
        // Brief §3: fight 1 is Husks plus a Lobber.
        Assert.All(
            Fight.Enemies,
            e => Assert.True(e.Kind == UnitKind.Husk || e.Kind == UnitKind.Lobber));
    }

    private static int RingOf(Coord c)
    {
        int fromLeft = c.X;
        int fromTop = c.Y;
        int fromRight = Fight.Board.Width - 1 - c.X;
        int fromBottom = Fight.Board.Height - 1 - c.Y;

        int min = fromLeft;
        if (fromTop < min)
        {
            min = fromTop;
        }

        if (fromRight < min)
        {
            min = fromRight;
        }

        if (fromBottom < min)
        {
            min = fromBottom;
        }

        return min;
    }
}
