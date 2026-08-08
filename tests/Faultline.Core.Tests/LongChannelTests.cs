using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// The 9×5 sample that proves board size is an authoring axis (MASTER_DESIGN §3, locked ac).
/// </summary>
/// <remarks>
/// It exists to prove the pipeline end to end at a non-default, non-square size — load, certify,
/// round-trip and play — not to grow the pool. Every assertion here is one the eight 7×7 boards
/// already pass; the point is that none of them was secretly asking about 7.
/// </remarks>
public class LongChannelTests
{
    private const string Id = "sz-01-the-long-channel";

    private static FightDefinition Fight() => FightLibrary.ById(Id)!;

    [Fact]
    public void ItLoadsWithoutErrors_AtTheSizeItDeclares()
    {
        var result = FightLibrary.LoadAll().Single(r => r.Fight?.Id == Id);

        Assert.Empty(result.Errors);
        Assert.Equal(9, result.Fight!.Board.Width);
        Assert.Equal(5, result.Fight.Board.Height);
        Assert.True(result.Fight.SizeDeclared);
    }

    /// <summary>
    /// A declared size is a decision, so the off-7×7 lint leaves it alone. The board is otherwise
    /// held to the same guidelines every other board is.
    /// </summary>
    [Fact]
    public void DeclaringItsSize_SilencesTheOffSevenLint_AndNothingElse()
    {
        var result = FightLibrary.LoadAll().Single(r => r.Fight?.Id == Id);

        Assert.DoesNotContain(result.Lints, l => l.Code == FightIssueCode.BoardNotSevenBySeven);
        Assert.DoesNotContain(result.Lints, l => l.Code == FightIssueCode.CentreNotClear);
        Assert.DoesNotContain(result.Lints, l => l.Code == FightIssueCode.HazardOffOuterRings);
        Assert.DoesNotContain(result.Lints, l => l.Code == FightIssueCode.SpikeCountOutOfRange);
    }

    /// <summary>
    /// §3's spot floor applies at any size: spots must outnumber the ducks that draft from them.
    /// </summary>
    [Fact]
    public void TheSpotFloor_AppliesAtNineByFive()
    {
        var fight = Fight();
        int ducks = fight.RosterA.Count + fight.RosterB.Count;

        Assert.True(fight.Spots.Count > ducks);
        Assert.InRange(fight.Spots.Count, 6, 8);
        Assert.All(fight.Spots, s => Assert.True(fight.Board.InBounds(s)));
    }

    /// <summary>
    /// Reachability certification at the declared size, never an assumed 7 — and on this board it
    /// is comfortable, which is the finding: length is itself a defence.
    /// </summary>
    [Fact]
    public void ItCertifies_AtTheSizeItDeclares()
    {
        var fight = Fight();
        var state = Game.Start(fight, seed: 1).NewState;

        Assert.Empty(Threat.UnsafeSides(fight));
        Assert.True(Threat.HasSafeDeployment(fight));

        // Every spot is clean here, which is the strict form the length hands over for free.
        Assert.Equal(fight.Spots.Count, Threat.SafeDeploymentTiles(state, Team.PlayerA).Count);
    }

    /// <summary>Round-trips through the writer with its size still declared.</summary>
    [Fact]
    public void ItRoundTrips_AndKeepsItsDeclaredSize()
    {
        var fight = Fight();
        var text = FightWriter.Write(fight);

        Assert.Contains("size: 9x5", text);

        var back = FightParser.Parse(text);

        Assert.Empty(back.Errors);
        Assert.Equal(fight, back.Fight);
    }

    /// <summary>
    /// Played, not asserted about: the draft runs and the fight reaches a conclusion on a board
    /// that is neither 7 wide nor square.
    /// </summary>
    [Fact]
    public void ItPlaysToAConclusion()
    {
        var start = Game.Start(Fight(), seed: 12345).NewState.DraftOrder(Team.PlayerA);
        var (played, _, steps) = TestPlay.PlayWithAi(start, 4000);

        Assert.True(steps < 4000, "The long channel did not terminate inside the step bound.");
        Assert.NotEqual(FightOutcome.InProgress, played.Outcome);
        Assert.All(played.Units.Where(u => u.IsOnBoard), u => Assert.True(played.Board.InBounds(u.Position)));
    }

    /// <summary>
    /// The board edge is the DECLARED edge. Nothing may stand at x=7 or y=5 because the board is
    /// nine wide and five tall, and nothing may fall off the end of a row that is longer than 7.
    /// </summary>
    [Fact]
    public void TheEdgeIsTheDeclaredEdge_NotSeven()
    {
        var board = Fight().Board;

        Assert.True(board.InBounds(new Coord(8, 4)));
        Assert.False(board.InBounds(new Coord(9, 4)));
        Assert.False(board.InBounds(new Coord(8, 5)));

        // The column a 7-wide assumption would have stopped at is ordinary ground here.
        Assert.True(board.InBounds(new Coord(7, 2)));
    }
}
