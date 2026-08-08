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
    public void DeploymentSpots_AreWalkableAndDistinct()
    {
        // One shared list since §3's draft: there is no second zone for it to fail to overlap.
        Assert.NotEmpty(Fight.DeploymentSpots);
        Assert.Empty(Fight.DeploymentZoneA);
        Assert.Empty(Fight.DeploymentZoneB);
        Assert.Equal(Fight.DeploymentSpots.Count, Fight.DeploymentSpots.Distinct().Count());

        foreach (var tile in Fight.DeploymentSpots)
        {
            Assert.True(Movement.IsWalkable(Fight.Board.At(tile)), tile + " is not walkable.");
        }
    }

    /// <summary>
    /// §3's floor: spots must OUTNUMBER the ducks, or the draft is assignment rather than drafting.
    /// Asked of the two rosters together, because one pool answers both.
    /// </summary>
    [Fact]
    public void DeploymentSpots_OutnumberTheDucks()
    {
        int ducks = Fight.RosterA.Count + Fight.RosterB.Count;

        Assert.True(
            Fight.Spots.Count > ducks,
            "This board publishes " + Fight.Spots.Count + " spots for " + ducks + " ducks.");

        // §3's default band for a four-duck board.
        Assert.InRange(Fight.Spots.Count, 6, 8);
    }

    /// <summary>
    /// <b>Three clusters, not two corners.</b> The old zones were opposite corners because each side
    /// owned one; §3 unowned them, and this board answers that with a central pair as well, so the
    /// draft has somewhere to go that is neither corner.
    /// </summary>
    [Fact]
    public void DeploymentSpots_OfferACentreAsWellAsTheTwoCorners()
    {
        var southWest = Fight.Spots.Where(c => c.X <= 1 && c.Y >= 5).ToList();
        var northEast = Fight.Spots.Where(c => c.X >= 5 && c.Y <= 2).ToList();
        var centre = Fight.Spots.Where(c => c.X >= 2 && c.X <= 4 && c.Y >= 3 && c.Y <= 4).ToList();

        Assert.Equal(3, southWest.Count);
        Assert.Equal(3, northEast.Count);
        Assert.Equal(2, centre.Count);
        Assert.Equal(Fight.Spots.Count, southWest.Count + northEast.Count + centre.Count);
    }

    [Fact]
    public void EnemySpawns_AreWalkableDistinctAndOnOppositeEdges()
    {
        Assert.NotEmpty(Fight.Enemies);
        Assert.Equal(Fight.Enemies.Count, Fight.Enemies.Select(e => e.At).Distinct().Count());

        foreach (var spawn in Fight.Enemies)
        {
            Assert.True(Movement.IsWalkable(Fight.Board.At(spawn.At)), spawn.At + " is not walkable.");

            // Every enemy walks in off an edge; the west column carries the queued pair.
            Assert.True(
                spawn.At.X == 0 || spawn.At.X == Fight.Board.Width - 1
                || spawn.At.Y == 0 || spawn.At.Y == Fight.Board.Height - 1,
                spawn.At + " is not on an edge.");
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
