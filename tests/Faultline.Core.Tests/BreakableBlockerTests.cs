using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// Breakable blockers: masonry that stands in the way and can be brought down, authored with the
/// <c>X</c> board character and the <c>blocker-hp:</c> key (DECISIONS.md D-114).
/// </summary>
/// <remarks>
/// A blocker is deliberately the same <see cref="Structure"/> an objective builds — same occupancy,
/// same damage rules, same rubble — and differs in exactly one thing: it is nobody's win condition.
/// These tests pin both halves, because reusing the type is only safe while the one difference holds.
/// </remarks>
public class BreakableBlockerTests
{
    // ---- authoring ---------------------------------------------------------------------------

    [Fact]
    public void Blocker_IsBuiltFromTheGridMarkWithTheKeysHitPoints()
    {
        var fight = Parse(Board("blocker-hp: 6", "X"));

        Assert.Equal(new[] { new Coord(3, 3) }, fight.Blockers);
        Assert.Equal(6, fight.BlockerHp);

        // The terrain underneath is Open, exactly as under a deploy slot or a spawn letter — which is
        // what lets the tile become walkable floor when the masonry goes.
        Assert.Equal(TileType.Open, fight.Board.At(new Coord(3, 3)));
    }

    [Fact]
    public void Blocker_StartsTheFightStandingOnItsTile_AndIsNotAnObjective()
    {
        var state = Game.Start(Parse(Board("blocker-hp: 6", "X")), seed: 1).NewState;
        var blocker = state.StructureAt(new Coord(3, 3));

        Assert.NotNull(blocker);
        Assert.Equal(6, blocker!.Hp);
        Assert.Equal(6, blocker.MaxHp);
        Assert.True(blocker.IsBlocker);
        Assert.False(blocker.IsSiegeTarget);
        Assert.Equal(ObjectiveKind.KillAll, state.Fight.Objective.Kind);
    }

    [Fact]
    public void BlockerWithNoHitPoints_IsAnError()
    {
        var result = FightParser.Parse(Board(null, "X"));

        Assert.Null(result.Fight);
        Assert.Contains(result.Errors, e => e.Code == FightIssueCode.BlockerHpMissing);
    }

    [Fact]
    public void BlockerWithZeroHitPoints_IsAnError()
    {
        var result = FightParser.Parse(Board("blocker-hp: 0", "X"));

        Assert.Null(result.Fight);
        Assert.Contains(result.Errors, e => e.Code == FightIssueCode.BlockerHpMissing);
    }

    [Fact]
    public void BlockerHpWithNoBlockerOnTheBoard_IsAnError()
    {
        var result = FightParser.Parse(Board("blocker-hp: 6", "."));

        Assert.Null(result.Fight);
        Assert.Contains(result.Errors, e => e.Code == FightIssueCode.BlockerHpUnused);
    }

    [Fact]
    public void BlockerCharacter_CannotBeUsedAsASpawnSymbol()
    {
        var result = FightParser.Parse(Board("blocker-hp: 6", "X").Replace("spawn h = Husk", "spawn X = Husk"));

        Assert.Null(result.Fight);
        Assert.Contains(result.Errors, e => e.Code == FightIssueCode.MalformedLine);
    }

    [Fact]
    public void Blocker_RoundTripsThroughTheWriter()
    {
        var original = Parse(Board("blocker-hp: 6", "X"));

        var text = FightWriter.Write(original);
        var reparsed = FightParser.Parse(text);

        Assert.NotNull(reparsed.Fight);
        Assert.Empty(reparsed.Errors);
        Assert.Contains("blocker-hp: 6", text);
        Assert.Contains(FightParser.Blocker.ToString(), text);
        Assert.Equal(original, reparsed.Fight);
    }

    // ---- physics -----------------------------------------------------------------------------

    [Fact]
    public void Blocker_BlocksMovementWhileStanding()
    {
        var state = Blocked();
        var vanguard = state.Find(UnitKind.Vanguard);

        Assert.True(state.IsOccupied(new Coord(1, 0)));
        Assert.DoesNotContain(new Coord(1, 0), Movement.Reachable(state, vanguard).Keys);
        Assert.DoesNotContain(new Coord(2, 0), Movement.Reachable(state, vanguard).Keys);
    }

    [Fact]
    public void Blocker_StopsBlockingOnceItIsRubble()
    {
        var state = Blocked();

        state = Objectives.Damage(state, new Coord(1, 0), 6, DamageSource.Collision, new List<GameEvent>());

        Assert.Null(state.StructureAt(new Coord(1, 0)));
        Assert.False(state.IsOccupied(new Coord(1, 0)));

        var reachable = Movement.Reachable(state, state.Find(UnitKind.Vanguard));
        Assert.Contains(new Coord(1, 0), reachable.Keys);
        Assert.Contains(new Coord(2, 0), reachable.Keys);
    }

    [Fact]
    public void Blocker_IsNeverBesieged_ItIsNobodysAltar()
    {
        var state = BoardBuilder.Open(4, 1)
            .Enemy(UnitKind.Husk, 0, 0)
            .PlayerA(UnitKind.Vanguard, 3, 0)
            .Blockers(6, new Coord(1, 0))
            .Build();

        var events = new List<GameEvent>();
        state = Objectives.Besiege(state, state.Find(UnitKind.Husk).Id, events);

        Assert.Equal(6, state.StructureAt(new Coord(1, 0))!.Hp);
        Assert.Empty(events);
    }

    [Fact]
    public void Blocker_FallingNeitherWinsNorLosesAKillAllFight()
    {
        var state = Blocked();

        var events = new List<GameEvent>();
        state = Objectives.Damage(state, new Coord(1, 0), 6, DamageSource.Collision, events);
        state = Objectives.Check(state, endOfRound: true, events);

        Assert.Equal(FightOutcome.InProgress, state.Outcome);
        Assert.DoesNotContain(events, e => e is FightWon or FightLost);
    }

    [Fact]
    public void Blocker_DoesNotCountAsAStructureStanding()
    {
        var state = Blocked();

        // The question AnyStructureStanding answers is "has the Protect altar fallen / is the Destroy
        // target down". A wall somebody has to knock through to cross the map is neither.
        Assert.False(Objectives.AnyStructureStanding(state));
        Assert.NotNull(state.StructureAt(new Coord(1, 0)));
    }

    // ---- what it costs to break one ----------------------------------------------------------

    [Fact]
    public void SixHitPoints_IsThreeSpearThrusts_OrOneCollisionAndOne()
    {
        // The arithmetic the ruling turns on, read off the constants rather than retyped: an attack
        // chips masonry for a flat 2 whatever swung it (D-060), a collision lands its full 4.
        Assert.Equal(2, Objectives.AttackDamageToStructure);
        Assert.Equal(4, Displacement.CollisionDamage);
        Assert.Equal(6, (3 * Objectives.AttackDamageToStructure));
        Assert.Equal(6, Displacement.CollisionDamage + Objectives.AttackDamageToStructure);
    }

    [Fact]
    public void SpearThrust_ChipsABlockerForTwo_LikeAnyOtherMasonry()
    {
        var state = BoardBuilder.Open(4, 1)
            .PlayerB(UnitKind.Wardbearer, 0, 0)
            .Enemy(UnitKind.Husk, 3, 0)
            .Blockers(6, new Coord(1, 0))
            .Build();

        var result = state.Step(
            new AbilityCommand(state.Find(UnitKind.Wardbearer).Id, Ability.SpearThrust, null, Direction.Right));

        Assert.Equal(
            Objectives.AttackDamageToStructure,
            result.Single<StructureDamaged>().Amount);
        Assert.Equal(4, result.NewState.StructureAt(new Coord(1, 0))!.Hp);
    }

    [Fact]
    public void ShovingAUnitIntoABlocker_TakesTheFullCollisionOffIt()
    {
        var state = BoardBuilder.Open(5, 1)
            .PlayerA(UnitKind.Vanguard, 3, 0)
            .Enemy(UnitKind.Husk, 2, 0)
            .Blockers(6, new Coord(1, 0))
            .Build();

        var events = new List<GameEvent>();
        state = Displacement.ResolveAuto(
            state,
            state.Find(UnitKind.Husk).Id,
            new Coord(3, 0),
            DisplacementKind.Push,
            2,
            events,
            by: state.Find(UnitKind.Vanguard).Id);

        Assert.Equal(6 - Displacement.CollisionDamage, state.StructureAt(new Coord(1, 0))!.Hp);
    }

    // ---- helpers -----------------------------------------------------------------------------

    private static GameState Blocked() =>
        BoardBuilder.Open(4, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 3, 0)
            .Blockers(6, new Coord(1, 0))
            .Build();

    private static FightDefinition Parse(string text)
    {
        var result = FightParser.Parse(text);
        Assert.True(result.Fight is not null, string.Join(" | ", result.Errors));
        return result.Fight!;
    }

    /// <summary>A 7x7 board with <paramref name="centre"/> written into the middle tile.</summary>
    private static string Board(string? extraKey, string centre) =>
        string.Join(
            "\n",
            "id: blocker-fixture",
            "name: Blocker Fixture",
            "spawn h = Husk",
            "roster a: Vanguard",
            "roster b: Archer",
            extraKey ?? string.Empty,
            "board:",
            "  h....HB",
            "  .^...^.",
            "  .......",
            "  ..." + centre + "...",
            "  .O...O.",
            "  A......",
            "  A.....h") + "\n";
}
