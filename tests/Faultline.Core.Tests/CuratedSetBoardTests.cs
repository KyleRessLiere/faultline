using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// The board and objective edits docs/CURATED_SET.md §6 calls for, pinned so they cannot drift back:
/// the first-contact re-cut, hz-02's reach line, as-05's tide, and tp-01's Warden.
/// </summary>
public class CuratedSetBoardTests
{
    // ---- first-contact -------------------------------------------------------------------------

    [Fact]
    public void FirstContact_IsLintClean()
    {
        var result = FightLibrary.LoadAll().Single(r => r.Fight is not null && r.Fight.Id == "first-contact");

        Assert.Empty(result.Errors);
        Assert.True(result.Lints.Count == 0, "first-contact — " + string.Join(" | ", result.Lints));
    }

    [Fact]
    public void FirstContact_QueuesTwoHusksInALineOnTheWestEdge()
    {
        // The re-cut's whole reason to exist: one Push from the Vanguard's basic puts the front Husk
        // into the back one, which is 2 to both and kills both.
        var fight = FightLibrary.ById("first-contact");

        Assert.Contains(fight.Enemies, e => e.Kind == UnitKind.Husk && e.At == new Coord(0, 2));
        Assert.Contains(fight.Enemies, e => e.Kind == UnitKind.Husk && e.At == new Coord(0, 3));
    }

    [Fact]
    public void FirstContact_IsStillThreeHusksAndOneLobber()
    {
        var kinds = FightLibrary.ById("first-contact").Enemies.Select(e => e.Kind).ToList();

        Assert.Equal(3, kinds.Count(k => k == UnitKind.Husk));
        Assert.Equal(1, kinds.Count(k => k == UnitKind.Lobber));
    }

    // ---- hz-02 the short way -------------------------------------------------------------------

    [Fact]
    public void TheShortWay_WinsByReachingTheFarRow()
    {
        var fight = FightLibrary.ById("hz-02-the-short-way");

        Assert.Equal(ObjectiveKind.Reach, fight.Objective.Kind);
        Assert.Equal(8, fight.TurnLimit);

        // Row 0 is the far side of the spike belt from Player A's corner; the belt runs across y=2.
        Assert.All(fight.Objective.Tiles, t => Assert.Equal(0, t.Y));
        Assert.Contains(new Coord(0, 0), fight.Objective.Tiles);
    }

    [Fact]
    public void TheShortWay_ReachTilesAreOpenAndNotNextToADeploySlot()
    {
        // A reach tile a player already stands beside would end the fight on round 1, which is the
        // opposite of "crossing IS the win".
        var fight = FightLibrary.ById("hz-02-the-short-way");
        var slots = fight.DeploymentZoneA.Concat(fight.DeploymentZoneB).ToList();

        foreach (var tile in fight.Objective.Tiles)
        {
            Assert.Equal(TileType.Open, fight.Board.At(tile));
            Assert.All(slots, s => Assert.True(
                s.DistanceTo(tile) > 1,
                tile + " is one step from deploy slot " + s + "."));
        }
    }

    [Fact]
    public void TheShortWay_IsStillLintCleanApartFromItsBoardSize()
    {
        var result = FightLibrary.LoadAll()
            .Single(r => r.Fight is not null && r.Fight.Id == "hz-02-the-short-way");

        Assert.Empty(result.Errors);
        Assert.DoesNotContain(FightIssueCode.ObjectiveTileNotOpen, result.Lints.Select(l => l.Code));
        Assert.DoesNotContain(FightIssueCode.TurnLimitBeatsObjective, result.Lints.Select(l => l.Code));
    }

    // ---- as-05 the door ------------------------------------------------------------------------

    [Fact]
    public void TheDoor_IsWonBySurvivingEightRounds()
    {
        var fight = FightLibrary.ById("as-05-the-door");

        Assert.Equal(ObjectiveKind.Survive, fight.Objective.Kind);
        Assert.Equal(8, fight.Objective.Deadline);
    }

    [Fact]
    public void TheDoor_ReinforcesOnRoundsThreeAndSix_AtTheNorthCorners()
    {
        var fight = FightLibrary.ById("as-05-the-door");

        Assert.Equal(new[] { 3, 6 }, fight.Waves.Select(w => w.Round).ToArray());

        foreach (var wave in fight.Waves)
        {
            Assert.Equal(
                new[] { new Coord(0, 0), new Coord(6, 0) },
                wave.Arrivals.Select(a => a.At).ToArray());
            Assert.All(wave.Arrivals, a => Assert.Equal(UnitKind.Husk, a.Kind));
        }
    }

    [Fact]
    public void TheDoor_ArrivalTilesAreOnTheBoardOpenAndOutsideBothDeployZones()
    {
        // The room the players hold is south of the wall band; an arrival landing inside it would
        // walk straight past the doorway the whole fight is about.
        var fight = FightLibrary.ById("as-05-the-door");
        var zones = fight.DeploymentZoneA.Concat(fight.DeploymentZoneB).ToHashSet();

        foreach (var arrival in fight.Waves.SelectMany(w => w.Arrivals))
        {
            Assert.True(fight.Board.InBounds(arrival.At), arrival.At + " is off the board.");
            Assert.Equal(TileType.Open, fight.Board.At(arrival.At));
            Assert.DoesNotContain(arrival.At, zones);
        }
    }

    [Fact]
    public void TheDoor_ReachesRoundEightWithPassivePlayersAndTheDefendersWin()
    {
        var fight = FightLibrary.ById("as-05-the-door");

        var (state, _, _) = TestPlay.PlayWithAi(Game.Start(fight, seed: 4242).NewState, maxSteps: 6000);

        Assert.Equal(Phase.Complete, state.Phase);
        Assert.NotEqual(FightOutcome.InProgress, state.Outcome);
    }

    // ---- tp-01 one door ------------------------------------------------------------------------

    [Fact]
    public void OneDoor_CorksTheGapWithAWardenRatherThanAnAnchor()
    {
        var fight = FightLibrary.ById("tp-01-one-door");

        var warden = Assert.Single(fight.Enemies, e => e.Kind == UnitKind.Warden);
        Assert.Equal(new Coord(4, 3), warden.At);
        Assert.DoesNotContain(fight.Enemies, e => e.Kind == UnitKind.Anchor);
    }

    [Fact]
    public void OneDoor_TheWardenIsStillInTheDoorwayAfterSixRounds()
    {
        // The point of the swap: Move 0 means the door stays shut, where the Anchor left in round 1.
        var fight = FightLibrary.ById("tp-01-one-door");
        var state = LibrarySoakTests.Drive(fight, rounds: 6);

        var warden = state.Units.Single(u => u.Kind == UnitKind.Warden);

        Assert.True(warden.IsAlive);
        Assert.Equal(new Coord(4, 3), warden.Position);
    }
}
