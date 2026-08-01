using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// Fight 1 used to be a hard-coded <c>FightDefinition</c> in C#; it is now authored as
/// <c>Fights/Data/first-contact.fight</c>. These tests pin the exact values the old constructor
/// produced, so the move to text is provably a no-op rather than a re-design.
/// </summary>
/// <remarks>
/// Order is asserted, not just membership: deployment slots and enemy spawns are consumed in list
/// order, that order fixes unit ids, and unit ids decide activation order and every tie-break in the
/// enemy priority lists. A reordered zone is a different game from the same seed and command log.
/// </remarks>
public class FightMigrationTests
{
    /// <summary>The terrain rows the hard-coded fight built, with placement stripped out.</summary>
    private static readonly string[] LegacyRows =
    {
        "#..O...",
        ".H.^...",
        "O.....#",
        ".^...^.",
        "#.....O",
        ".....H.",
        "...O..#",
    };

    [Fact]
    public void Fight1_Number_IsOne()
    {
        Assert.Equal(1, FightLibrary.Fight1().Number);
    }

    [Fact]
    public void Fight1_Board_MatchesTheHardCodedLayoutTileForTile()
    {
        Assert.Equal(BoardLayout.Parse(LegacyRows), FightLibrary.Fight1().Board);
    }

    [Fact]
    public void Fight1_Board_IsStillSevenBySeven()
    {
        var board = FightLibrary.Fight1().Board;

        Assert.Equal(7, board.Width);
        Assert.Equal(7, board.Height);
    }

    [Fact]
    public void Fight1_Rosters_AreUnchanged()
    {
        var fight = FightLibrary.Fight1();

        Assert.Equal(new[] { UnitKind.Vanguard, UnitKind.Archer }, fight.RosterA);
        Assert.Equal(new[] { UnitKind.Threadcaster, UnitKind.Wardbearer }, fight.RosterB);
    }

    [Fact]
    public void Fight1_DeploymentZoneA_IsUnchangedInOrder()
    {
        Assert.Equal(
            new[] { new Coord(0, 5), new Coord(1, 5), new Coord(0, 6), new Coord(1, 6) },
            FightLibrary.Fight1().DeploymentZoneA);
    }

    [Fact]
    public void Fight1_DeploymentZoneB_IsUnchangedInOrder()
    {
        Assert.Equal(
            new[] { new Coord(5, 0), new Coord(6, 0), new Coord(5, 1), new Coord(6, 1) },
            FightLibrary.Fight1().DeploymentZoneB);
    }

    [Fact]
    public void Fight1_Enemies_AreUnchangedInOrder()
    {
        Assert.Equal(
            new[]
            {
                new EnemySpawn(UnitKind.Husk, new Coord(2, 0)),
                new EnemySpawn(UnitKind.Lobber, new Coord(4, 0)),
                new EnemySpawn(UnitKind.Husk, new Coord(2, 6)),
                new EnemySpawn(UnitKind.Husk, new Coord(4, 6)),
            },
            FightLibrary.Fight1().Enemies);
    }

    [Fact]
    public void Fight1_ProtectedZone_IsStillEmpty()
    {
        Assert.Empty(FightLibrary.Fight1().ProtectedZone);
    }

    [Fact]
    public void Fight1_PlacementTiles_AreOpenUnderneath()
    {
        // The old layout data had no terrain under a spawn or a deploy slot either.
        var fight = FightLibrary.Fight1();

        foreach (var coord in fight.DeploymentZoneA)
        {
            Assert.Equal(TileType.Open, fight.Board.At(coord));
        }

        foreach (var coord in fight.DeploymentZoneB)
        {
            Assert.Equal(TileType.Open, fight.Board.At(coord));
        }

        foreach (var spawn in fight.Enemies)
        {
            Assert.Equal(TileType.Open, fight.Board.At(spawn.At));
        }
    }

    [Fact]
    public void Fight1_LoadedTwice_IsTheSameDefinition()
    {
        // FightDefinition is a record whose list members compare by reference, so compare field by field.
        var a = FightLibrary.Fight1();
        var b = FightLibrary.Fight1();

        Assert.Equal(a.Id, b.Id);
        Assert.Equal(a.Number, b.Number);
        Assert.Equal(a.Name, b.Name);
        Assert.Equal(a.Description, b.Description);
        Assert.Equal(a.Board, b.Board);
        Assert.Equal(a.RosterA, b.RosterA);
        Assert.Equal(a.RosterB, b.RosterB);
        Assert.Equal(a.DeploymentZoneA, b.DeploymentZoneA);
        Assert.Equal(a.DeploymentZoneB, b.DeploymentZoneB);
        Assert.Equal(a.Enemies, b.Enemies);
        Assert.Equal(a.ProtectedZone, b.ProtectedZone);
    }

    [Fact]
    public void Fight1_FromTheParsedFile_ReplaysIdenticallyFromTheSameSeed()
    {
        // The point of pinning zone and spawn order: same file, same seed, same log, same state.
        var first = Game.Start(FightLibrary.Fight1(), seed: 20250731).NewState;
        var (played, log) = TestPlay.PlayFirstLegal(first, maxSteps: 400);

        var second = Game.Start(FightLibrary.Fight1(), seed: 20250731).NewState;
        var replayed = TestPlay.Replay(second, log);

        Assert.NotEmpty(log);
        Assert.Equal(played, replayed);
        Assert.Equal(played.GetHashCode(), replayed.GetHashCode());
    }
}
