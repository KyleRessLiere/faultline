using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// Edition A of the eight Warrens combat boards, held to the constraints MASTER_DESIGN §8.8 puts on
/// the generator's proof log rather than to the guideline lints.
/// </summary>
/// <remarks>
/// <para>
/// The lints describe a symmetrical skirmish and several of these boards deliberately are not one —
/// a bramble board with its brambles on the outer rings has no middle to own, and a siege has one
/// front. What §8.8 makes non-negotiable is different and is what this file asserts: every combat
/// board is 7×7, no deployment tile takes unavoidable damage before its owner has had a turn, and
/// every hazard-thesis board opens with a <em>previewable beneficial</em> hazard play.
/// </para>
/// <para>
/// <b>Reached by playing.</b> The bramble opener is not asserted off the board's shape — the boards
/// that failed this constraint before also had brambles on them. The fight is started, both flocks
/// are deployed on tiles the threat query calls safe, and the opener is found in the legal command
/// list through <see cref="Abilities.Outlook"/>, which is the same projection the shell draws
/// (D-151). Then it is taken, and the damage that lands is checked against what was previewed.
/// </para>
/// </remarks>
public class WarrensEditionATests
{
    /// <summary>The act-1 combat nodes, in the order §8 draws them.</summary>
    public static IEnumerable<object[]> Boards() => new[]
    {
        new object[] { "first-contact" },
        new object[] { "cb-06-bait-and-break" },
        new object[] { "the-teeth" },
        new object[] { "the-shrine" },
        new object[] { "broken-bridge" },
        new object[] { "high-road" },
        new object[] { "break-the-gate" },
        new object[] { "hz-09-the-trench" },
    };

    // ---- §8.8 proof-log constraints -----------------------------------------------------------

    [Theory]
    [MemberData(nameof(Boards))]
    public void EveryCombatBoard_IsSevenBySeven(string id)
    {
        // D-165. §3's "7×7 default (format supports larger)" licenses a bigger board; §8.8's
        // "every combat board is 7×7" spends that licence somewhere other than an act's fights.
        var board = FightLibrary.ById(id)!.Board;

        Assert.Equal(7, board.Width);
        Assert.Equal(7, board.Height);
    }

    [Theory]
    [MemberData(nameof(Boards))]
    public void EveryCombatBoard_FieldsBothRostersOutOfEveryEnemysRoundOneReach(string id)
    {
        var fight = FightLibrary.ById(id)!;
        var start = Game.Start(fight, seed: 1).NewState;

        Assert.True(
            Threat.SafeDeploymentTiles(start, Team.PlayerA).Count >= fight.RosterA.Count,
            id + " cannot field Player A's roster on tiles no enemy can damage on round 1.");
        Assert.True(
            Threat.SafeDeploymentTiles(start, Team.PlayerB).Count >= fight.RosterB.Count,
            id + " cannot field Player B's roster on tiles no enemy can damage on round 1.");
    }

    [Theory]
    [MemberData(nameof(Boards))]
    public void EveryCombatBoard_ParsesWithoutErrors_AndReplaysToTheSameState(string id)
    {
        var fight = FightLibrary.ById(id)!;

        Assert.Empty(FightParser.Parse(FightWriter.Write(fight)).Errors);

        var (played, log) = TestPlay.PlayFirstLegal(Game.Start(fight, seed: 7).NewState, maxSteps: 400);
        var replayed = TestPlay.Replay(Game.Start(fight, seed: 7).NewState, log);

        Assert.NotEmpty(log);
        Assert.Equal(played, replayed);
        Assert.Equal(played.GetHashCode(), replayed.GetHashCode());
    }

    // ---- the-teeth: the previewable beneficial hazard play -------------------------------------

    /// <summary>
    /// §8.8 names the old Teeth as the board that failed this constraint: its signature hazard read
    /// as self-harm. Edition A puts a Husk one tile north of the middle tooth and gives both flocks a
    /// round-one displacement onto it, so the first thing the brambles do on this board is 6 damage
    /// to somebody else.
    /// </summary>
    [Fact]
    public void TheTeeth_OpensWithAPreviewableSixDamageBrambleShove()
    {
        var tooth = new Coord(3, 4);

        Assert.Equal(TileType.Spikes, FightLibrary.ById("the-teeth")!.Board.At(tooth));

        // Deployment is a decision, so the opener is looked for across the safe fieldings rather
        // than off whichever one the zone happens to list first: on a 7x7 a target far enough away
        // to obey D-080 is exactly at the edge of "walk two, act once", so WHICH duck stands on the
        // near tile is the whole play. What has to exist is one safe fielding that has it.
        Command? command = null;
        ActionOutlook? outlook = null;
        GameState before = default!;

        foreach (var fielding in SafeFieldings("the-teeth"))
        {
            Assert.Equal(1, fielding.Round);
            (before, command, outlook) = FindBrambleOpener(fielding, tooth);

            if (command is not null)
            {
                break;
            }
        }

        Assert.NotNull(command);

        // What the player is shown, before anything is committed: a named target, a route onto the
        // tooth, 6 damage, and a hard stop there.
        Assert.Equal(DisplacementStop.Spikes, outlook!.Displacement!.Stop);
        Assert.Equal(tooth, outlook.Displacement.Destination);
        Assert.Equal(6, outlook.Displacement.DamageToUnit);
        Assert.True(outlook.Displacement.WouldDown, "the preview does not promise the kill it makes.");

        var target = before.UnitById(outlook.Displacement.UnitId);
        Assert.Equal(Team.Enemy, target.Team);

        // And then it is taken, so the preview is checked against the board rather than trusted.
        var step = Game.Apply(before, command!);

        Assert.Contains(
            step.Events.OfType<UnitDamaged>(),
            e => e.UnitId == target.Id && e.Amount == 6 && e.Source == DamageSource.Spikes);
        Assert.False(step.NewState.UnitById(target.Id).IsAlive);
    }

    // ---- broken-bridge: no class is required to open a crossing --------------------------------

    /// <summary>
    /// §8.8 asks for 6 HP breakable blockers that any flock can open. What the board must guarantee
    /// is a gradient rather than a key: the flat structure chip works for everybody, and a collision
    /// is the faster route.
    /// </summary>
    [Fact]
    public void BrokenBridge_ACrossingOpensToTheFlatChipFromAnybody_AndFasterToCollisions()
    {
        var fight = FightLibrary.ById("broken-bridge")!;

        Assert.Equal(6, fight.BlockerHp);
        Assert.Equal(new[] { new Coord(2, 2), new Coord(4, 4) }, fight.Blockers);

        // Three swings from any unit on the board, whatever it swings (D-060) — the baseline that
        // always exists.
        Assert.Equal(3, Ceiling(fight.BlockerHp, Objectives.AttackDamageToStructure));

        // MASTER_DESIGN §7 prices a structure collision at 6, which would open a crossing in one.
        // The shipped constant is 4, so today it is one slam and one swing (D-166).
        Assert.Equal(1, Ceiling(fight.BlockerHp, 6));
        Assert.Equal(2, Ceiling(fight.BlockerHp, Displacement.CollisionDamage));
    }

    // ---- high-road: the ridge is owned, not taxed ----------------------------------------------

    [Fact]
    public void HighRoad_TheRidgeIsFiveTilesAndCostsNoEntryTax()
    {
        var board = FightLibrary.ById("high-road")!.Board;

        for (int y = 1; y <= 5; y++)
        {
            Assert.Equal(TileType.HighGround, board.At(new Coord(3, y)));
        }

        // D-152 deleted the climb surcharge on both sides, so the ridge costs what open ground costs
        // and what it is worth is what it does once you are standing on it. Asked of a duck and of a
        // Husk, because the deleted surcharge used to apply to one of them and not the other.
        foreach (var kind in new[] { UnitKind.Archer, UnitKind.Vanguard, UnitKind.Husk })
        {
            var unit = BoardBuilder.Rows("..").Enemy(kind, 0, 0).Build().Find(kind);

            Assert.Equal(
                Movement.StepCost(TileType.Open, unit),
                Movement.StepCost(TileType.HighGround, unit));
        }
    }

    [Fact]
    public void HighRoad_TheGrapplerPrefersTheArcher_AndTheRidgeAboveHer()
    {
        // Already in the rules (Ai.PickGrab ranks HighGround first, then the Archer), so the board's
        // job is only to field a Grappler with both choices in front of it. Verified through the
        // declared intent rather than by reading the ranking back.
        var state = BoardBuilder.Rows(
                ".......",
                ".......",
                "...H...",
                ".......",
                ".......",
                ".......",
                ".......")
            .Enemy(UnitKind.Grappler, 3, 0)
            .PlayerA(UnitKind.Vanguard, 2, 0)
            .PlayerB(UnitKind.Archer, 5, 0)
            .Active(Team.Enemy)
            .Build();

        var intent = Ai.Declare(state, state.Find(UnitKind.Grappler));

        Assert.Equal(state.Find(UnitKind.Archer).Id, intent.TargetId);
    }

    // ---- the-shrine: the claw names the shrine and predicts the hit points ----------------------

    [Fact]
    public void TheShrine_ARaiderIntentNamesTheShrineAndPredictsTheHitPointsLeft()
    {
        var fight = FightLibrary.ById("the-shrine")!;
        var shrine = new Coord(3, 3);

        Assert.Equal(ObjectiveKind.Protect, fight.Objective.Kind);
        Assert.Equal(12, fight.Objective.Hp);
        Assert.Equal(shrine, Assert.Single(fight.Objective.Tiles));

        // A Raider stood next to the shrine, which is where this board's clock lives.
        var state = BoardBuilder.Rows(
                ".......",
                ".......",
                ".......",
                ".......",
                ".......",
                ".......",
                ".......")
            .Enemy(UnitKind.Raider, 2, 3)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Objective(ObjectiveKind.Protect, hp: 12, tiles: shrine)
            .Active(Team.Enemy)
            .Build();

        var intent = Ai.Declare(state, state.Find(UnitKind.Raider));

        // The intent NAMES the shrine — a tile rather than a unit id, which is how a plan aimed at a
        // structure is carried — and the number on it is the flat chip the resolution will take, not
        // the Raider's weapon (D-164).
        Assert.Equal(IntentAction.Attack, intent.Action);
        Assert.Null(intent.TargetId);
        Assert.Equal(shrine, intent.TargetPosition);
        Assert.Equal(Objectives.AttackDamageToStructure, intent.Damage);

        // And the prediction, read off what a player is actually shown rather than off the flag.
        var status = StructureStatus.For(state, shrine)!;

        Assert.Equal("Shrine 12/12", status.Label);
        Assert.Equal(10, status.HpAfter(intent.Damage));
    }

    // ---- helpers -------------------------------------------------------------------------------

    private static int Ceiling(int total, int each) => (total + each - 1) / each;

    /// <summary>
    /// Every fielding that puts both rosters on tiles <see cref="Threat"/> calls safe — every
    /// assignment of safe tiles to slots, not just the zone's own order.
    /// </summary>
    private static IEnumerable<GameState> SafeFieldings(string id)
    {
        var fight = FightLibrary.ById(id)!;
        var start = Game.Start(fight, seed: 1).NewState.DraftOrder(Team.PlayerA);

        // §3's spots belong to neither player, so both sides are answered by the same tiles. Two
        // per-side cursors would deal one spot twice and the second placement would be refused for
        // a reason none of these tests is about: one pool, one cursor, and the permutation runs
        // over the draft's whole sequence of picks rather than over two independent zones.
        var safe = Threat.SafeDeploymentTiles(start, Team.PlayerA);
        int picks = fight.RosterA.Count + fight.RosterB.Count;

        foreach (var order in Selections(safe, picks))
        {
            var state = start;
            int slot = 0;

            while (state.Phase == Phase.Deployment)
            {
                var legal = Game.LegalCommands(state);
                var undeployed = state.Units.First(u => u.Team == state.ActiveTeam && !u.IsDeployed);
                var tile = order[slot++];

                state = state.Then(legal.OfType<DeployCommand>()
                    .First(d => d.UnitId == undeployed.Id && d.At == tile));
            }

            yield return state;
        }
    }

    /// <summary>
    /// Every ordered selection of <paramref name="count"/> distinct tiles, in a fixed order — one
    /// per way the whole draft could deal the pool out across its slots.
    /// </summary>
    private static IEnumerable<IReadOnlyList<Coord>> Selections(IReadOnlyList<Coord> tiles, int count)
    {
        if (count <= 0)
        {
            yield return new Coord[0];
            yield break;
        }

        for (int i = 0; i < tiles.Count; i++)
        {
            var rest = tiles.Where((_, index) => index != i).ToList();
            foreach (var tail in Selections(rest, count - 1))
            {
                yield return new[] { tiles[i] }.Concat(tail).ToList();
            }
        }
    }

    /// <summary>
    /// Walks round one looking for a command whose projection lands an enemy on <paramref name="tooth"/>.
    /// A unit with nothing to offer walks toward the tooth and is asked again; when it runs out of
    /// activation the next one is asked.
    /// </summary>
    private static (GameState State, Command? Command, ActionOutlook? Outlook) FindBrambleOpener(
        GameState state, Coord tooth)
    {
        for (int step = 0; step < 60 && state.Round == 1; step++)
        {
            var legal = Game.LegalCommands(state);

            foreach (var candidate in legal)
            {
                var outlook = Abilities.Outlook(state, candidate);
                if (outlook?.Displacement is { } push
                    && push.Destination == tooth
                    && push.DamageToUnit == 6
                    && state.UnitById(push.UnitId).Team == Team.Enemy)
                {
                    return (state, candidate, outlook);
                }
            }

            var enemy = Game.NextEnemyCommand(state);
            if (enemy is not null)
            {
                state = state.Then(enemy);
                continue;
            }

            // One tile at a time, and the preview is re-read after each - which is how a player
            // walks. Spending the whole pool on movement and then asking is how the first cut of
            // this search missed an opener the board actually has. Ties broken by the fixed
            // coordinate order so the walk never depends on unit ids.
            var move = legal.OfType<MoveCommand>()
                .Where(m =>
                {
                    var from = state.UnitById(m.UnitId).Position;
                    return m.To.DistanceTo(from) == 1 && m.To.DistanceTo(tooth) < from.DistanceTo(tooth);
                })
                .OrderBy(m => m.To.DistanceTo(tooth))
                .ThenBy(m => m.To.X)
                .ThenBy(m => m.To.Y)
                .FirstOrDefault();

            var next = (Command?)move ?? legal.OfType<EndActivationCommand>().FirstOrDefault();
            if (next is null)
            {
                break;
            }

            state = state.Then(next);
        }

        return (state, null, null);
    }
}
