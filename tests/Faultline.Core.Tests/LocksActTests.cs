using System;
using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// The Act 3 authoring contract, enforced.
///
/// <para>
/// <c>docs/level-design/2026-08-13/ACT3_BOARD_CRITERIA.md</c> is the specification; this is the
/// half of it a machine can check. It exists because thirty-odd boards authored against a prose
/// spec drift, and because the two failures the board pool review found — a board with no
/// question live on round 3, and a board whose impassable tiles are scattered points rather than
/// connected mass — are both measurable.
/// </para>
///
/// <para>
/// The gates that are NOT here are the ones a machine cannot judge: whether the round-3 question
/// is a good one, whether two routes are genuinely different, and whether a break is priced.
/// Those live in the twelve-pass protocol and in review.
/// </para>
/// </summary>
public class LocksActTests
{
    /// <summary>Every active board of the Locks. The act is authored under the `lk-` prefix.</summary>
    private static IReadOnlyList<FightDefinition> Act() =>
        FightLibrary.All()
            .Where(f => f.Id.StartsWith("lk-", StringComparison.Ordinal) && !f.IsRetired)
            .OrderBy(f => f.Number)
            .ToList();

    /// <summary>
    /// The act-wide gates (G15 objective distribution, G17 set variance) only mean something once
    /// the act is substantially authored — asserting a distribution over three boards would fail
    /// for the whole build rather than reporting a real defect. They go live at twenty.
    /// </summary>
    private const int ActIsComplete = 20;

    [Fact]
    public void G2_EveryBoard_NamesTheDecisionStillLiveOnRoundThree()
    {
        foreach (var fight in Act())
        {
            Assert.True(
                // Matched on the phrase rather than on punctuation. The gate's content is that the
                // board names the decision, not which mark follows the words.
                fight.DesignNotes.Any(n =>
                    n.StartsWith("THE ROUND-3 QUESTION", StringComparison.Ordinal)),
                $"{fight.Id} does not name its round-3 question. A board whose question expires "
                + "at the end of round 1 is an open brawl from round 2 (hz-07-standing-room is "
                + "the retired precedent).");
        }
    }

    [Fact]
    public void G3_EveryBoardOutsideTheOpenerBand_BuysItsQuestionWithMassOrWithAnObjective()
    {
        foreach (var fight in Act())
        {
            if (fight.Pool == FightPool.Opener)
            {
                continue;
            }

            if (fight.Objective.Kind != ObjectiveKind.KillAll)
            {
                // The second currency. Every non-kill-all board in Warrens v2 sits below the
                // blocking floor and every one of them is sound: an objective supplies the
                // round-3 question that architecture would otherwise have to buy.
                continue;
            }

            var tiles = fight.Board.Width * fight.Board.Height;
            var massed = MassedImpassableTiles(fight);
            var share = 100.0 * massed / tiles;

            Assert.True(
                share >= 15.0,
                $"{fight.Id} is kill-all with {massed}/{tiles} tiles ({share:F0}%) of impassable "
                + "terrain in connected formations of 3+, below the 15% floor. A lone pit is not "
                + "architecture — it is a shove-target: it feeds the displacement economy and "
                + "does nothing for the routing economy.");
        }
    }

    [Fact]
    public void G5_EveryBoardOutsideTheOpenerBand_OwnsItsMiddle()
    {
        foreach (var fight in Act())
        {
            if (fight.Pool == FightPool.Opener)
            {
                continue;
            }

            var board = fight.Board;
            var cx = board.Width / 2;
            var cy = board.Height / 2;
            var contested = false;

            for (int x = cx - 1; x <= cx + 1 && !contested; x++)
            {
                for (int y = cy - 1; y <= cy + 1 && !contested; y++)
                {
                    if (x < 0 || y < 0 || x >= board.Width || y >= board.Height)
                    {
                        continue;
                    }

                    if (board.At(new Coord(x, y)) != TileType.Open)
                    {
                        contested = true;
                    }
                }
            }

            // A structure or an objective tile in the middle owns it just as terrain does.
            contested = contested
                || fight.Blockers.Any(c => IsCentre(fight, c))
                || fight.Objective.Tiles.Any(c => IsCentre(fight, c));

            Assert.True(
                contested,
                $"{fight.Id} has an empty centre 3x3. This is the CentreNotClear lint inverted on "
                + "purpose: the good boards are the ones that override it, so as shipped it is "
                + "backwards. A drawable board's middle must be worth fighting over.");
        }
    }

    [Fact]
    public void G7_EveryBoard_IsSpotNativeWithSpotsOutnumberingDucks()
    {
        foreach (var fight in Act())
        {
            Assert.True(
                fight.DeploymentZoneA.Count == 0 && fight.DeploymentZoneB.Count == 0,
                $"{fight.Id} still carries zone-era A/B deployment letters. The Locks is authored "
                + "spot-native: spots are unowned and either flock may draft into any of them.");

            var ducks = fight.RosterA.Count + fight.RosterB.Count;

            Assert.True(
                fight.Spots.Count > ducks,
                $"{fight.Id} offers {fight.Spots.Count} spots for {ducks} ducks. Spots must "
                + "outnumber ducks or the draft is assignment rather than drafting.");

            Assert.InRange(fight.Spots.Count, ducks + 1, 8);
        }
    }

    [Fact]
    public void G9_EveryBoard_FieldsMoreThanOneKindUnlessTheUniformTideIsItsDeclaredQuestion()
    {
        foreach (var fight in Act())
        {
            var kinds = fight.Enemies.Select(e => e.Kind).Distinct().Count();

            if (kinds >= 2)
            {
                continue;
            }

            // as-05-the-door is the licensed precedent: twelve Husks, and the uniform tide IS the
            // question. A board may do that — it may not do it by accident.
            Assert.True(
                fight.DesignNotes.Any(n =>
                    n.Contains("uniform tide", StringComparison.OrdinalIgnoreCase)
                    || n.Contains("single kind", StringComparison.OrdinalIgnoreCase)),
                $"{fight.Id} fields one enemy kind without declaring that the uniform tide is its "
                + "question. A single-type roster is where one counter-tool becomes a general "
                + "solution.");
        }
    }

    [Fact]
    public void G14_EveryHardAndEliteBoard_CarriesAClockOrAnArrival()
    {
        foreach (var fight in Act())
        {
            if (fight.Pool != FightPool.Hard && fight.Pool != FightPool.Elite)
            {
                continue;
            }

            var pressured =
                fight.TurnLimit > 0
                || fight.Waves.Count > 0
                || fight.Objective.Kind != ObjectiveKind.KillAll;

            Assert.True(
                pressured,
                $"{fight.Id} is {fight.Pool} with no turn limit, no arrival and no objective. On a "
                + "kill-all board with no clock, a maximally slow and maximally cautious policy is "
                + "not merely viable, it is strictly optimal — there is no cost to spending an "
                + "extra round repositioning, ever.");
        }
    }

    [Fact]
    public void G15_TheAct_IsNotAnObjectiveMonoculture()
    {
        var act = Act();

        if (act.Count < ActIsComplete)
        {
            return;
        }

        var varied = act.Count(f => f.Objective.Kind != ObjectiveKind.KillAll);
        var share = 100.0 * varied / act.Count;

        Assert.True(
            share >= 40.0,
            $"The Locks ships {varied}/{act.Count} boards ({share:F0}%) that are not kill-all, "
            + "below the 40% floor. Warrens v2 is 35 of 40 kill-all, and an act whose boards are "
            + "all won the same way is solved the same way.");
    }

    [Fact]
    public void G17_TheAct_BalancesTheSetRatherThanTheBattle()
    {
        var act = Act();

        if (act.Count < ActIsComplete)
        {
            return;
        }

        var hazardFree = act.Count(f => Count(f, TileType.Pit) == 0 && Count(f, TileType.Spikes) == 0);

        Assert.True(
            hazardFree >= 5,
            $"Only {hazardFree} boards of {act.Count} carry no pit and no spikes. A map with no "
            + "hazards is not a lesser map, and the blocking floor pulls hard toward pit-and-wall "
            + "boards — this is the counterweight.");

        var pits = act.Sum(f => Count(f, TileType.Pit));
        var walls = act.Sum(f => Count(f, TileType.Wall));

        Assert.True(
            pits <= walls,
            $"The act holds {pits} pit tiles against {walls} wall tiles. Pits are not the game, "
            + "displacement is — the standard failure mode is fifty variations of shove them in "
            + "the hole.");

        var sevenBySeven = act.Count(f => f.Board.Width == 7 && f.Board.Height == 7);

        Assert.True(
            sevenBySeven <= (int)Math.Ceiling(act.Count * 0.6),
            $"{sevenBySeven} of {act.Count} boards are 7x7. Board size is an authoring axis — "
            + "sz-01's 9x5 is the precedent that a dimension can be the whole thesis.");
    }

    private static bool IsCentre(FightDefinition fight, Coord c)
    {
        var cx = fight.Board.Width / 2;
        var cy = fight.Board.Height / 2;

        return Math.Abs(c.X - cx) <= 1 && Math.Abs(c.Y - cy) <= 1;
    }

    private static int Count(FightDefinition fight, TileType tile)
    {
        var n = 0;

        for (int x = 0; x < fight.Board.Width; x++)
        {
            for (int y = 0; y < fight.Board.Height; y++)
            {
                if (fight.Board.At(new Coord(x, y)) == tile)
                {
                    n++;
                }
            }
        }

        return n;
    }

    /// <summary>
    /// Impassable tiles that belong to a connected formation of three or more. Lone pits and lone
    /// walls count toward nothing: a hazard outside a formation is seasoning, and the whole point
    /// of the measure is whether the terrain changes where you can WALK.
    /// </summary>
    private static int MassedImpassableTiles(FightDefinition fight)
    {
        var board = fight.Board;
        var impassable = new HashSet<Coord>();

        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                var c = new Coord(x, y);
                var tile = board.At(c);

                if (tile == TileType.Wall || tile == TileType.Pit)
                {
                    impassable.Add(c);
                }
            }
        }

        foreach (var blocker in fight.Blockers)
        {
            impassable.Add(blocker);
        }

        foreach (var tile in fight.Objective.Tiles)
        {
            impassable.Add(tile);
        }

        var seen = new HashSet<Coord>();
        var massed = 0;

        foreach (var start in impassable)
        {
            if (!seen.Add(start))
            {
                continue;
            }

            var component = new List<Coord> { start };
            var queue = new Queue<Coord>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var at = queue.Dequeue();

                foreach (var step in Neighbours(at))
                {
                    if (impassable.Contains(step) && seen.Add(step))
                    {
                        component.Add(step);
                        queue.Enqueue(step);
                    }
                }
            }

            if (component.Count >= 3)
            {
                massed += component.Count;
            }
        }

        return massed;
    }

    private static IEnumerable<Coord> Neighbours(Coord c)
    {
        yield return new Coord(c.X + 1, c.Y);
        yield return new Coord(c.X - 1, c.Y);
        yield return new Coord(c.X, c.Y + 1);
        yield return new Coord(c.X, c.Y - 1);
    }
}
