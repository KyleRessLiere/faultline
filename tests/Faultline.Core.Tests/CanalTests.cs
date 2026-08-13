using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// Canal water and the sluices that move it (D-275) — the Locks' signature tile class.
/// </summary>
/// <remarks>
/// <para>
/// There is <b>no exhaustive switch over <see cref="TileType"/> anywhere in the codebase</b>: every
/// one has a <c>default</c>. So a new tile class does not fail to compile at the sites nobody taught
/// about — it behaves silently as open ground, and no other test in the suite reports it. These are
/// therefore written site by site rather than around a scenario, because the risk is omission and the
/// only defence against omission is naming each one.
/// </para>
/// <para>
/// The water level holds <b>no state of its own</b>. Everything here is derived from the authored
/// schedule, which gates are still standing, and what the board already says — which is why there is
/// no new field on <see cref="GameState"/> to compare and why replay is free.
/// </para>
/// </remarks>
public class CanalTests
{
    // ---- wading: the price of the step -----------------------------------------------------------

    [Fact]
    public void Walk_IntoTheCanal_IsAllowed_AndCostsTheSameSurchargeBramblesDo()
    {
        var state = BoardBuilder.Rows(".~..")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Build();

        var reachable = Movement.Reachable(state, state.Find(UnitKind.Vanguard));

        // Walkable, unlike a drain: wading is a decision a player is allowed to take.
        Assert.True(reachable.TryGetValue(new Coord(1, 0), out var wade));
        Assert.Equal(Activation.WadeCost, wade!.Cost);

        // And the surcharge is real: the tile past it costs the wade plus an ordinary step, so a
        // Vanguard's three points reach one tile less far through the water than over floor.
        Assert.Equal(Activation.WadeCost + Activation.StepCost, reachable[new Coord(2, 0)].Cost);
        Assert.DoesNotContain(new Coord(3, 0), reachable.Keys);
    }

    [Fact]
    public void Walk_TheWadeIsTiedToTheBrambleSurchargeRatherThanToATwo()
    {
        // The constant is defined AS BrambleCost, not as a second 2 that happens to agree today.
        Assert.Equal(Activation.BrambleCost, Activation.WadeCost);
    }

    [Fact]
    public void Walk_AnEnemyPaysNoSurcharge_ExactlyAsItPaysNoneForBrambles()
    {
        // Enemies keep movement-point semantics and terrain prices them as it always did
        // (Activation's own remarks). The canal follows the bramble precedent rather than inventing
        // a second economy.
        var state = BoardBuilder.Rows(".~..")
            .Enemy(UnitKind.Husk, 0, 0)
            .Build();

        var reachable = Movement.Reachable(state, state.Find(UnitKind.Husk));

        Assert.Equal(Activation.StepCost, reachable[new Coord(1, 0)].Cost);
        Assert.Equal(Activation.StepCost * 2, reachable[new Coord(2, 0)].Cost);
    }

    [Fact]
    public void Walk_SureFootedBuysAWayThroughBrambles_AndNotASwimmingCertificate()
    {
        var state = BoardBuilder.Rows(".~^.")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Build();

        var duck = state.Find(UnitKind.Vanguard).Id;
        var footed = state.WithUnlock(duck, Unlock.SureFooted);

        var reachable = Movement.Reachable(footed, footed.Get(duck));

        Assert.Equal(Activation.WadeCost, reachable[new Coord(1, 0)].Cost);
    }

    [Fact]
    public void Route_WhenTwoRoutesCostTheSame_TheRouterTakesTheDryOne()
    {
        // An enemy pays no surcharge, so the two ways to (1,1) genuinely tie on price: east then
        // south through the canal, or south then east over floor. The compass order puts east first,
        // so without the wade tie-break the router would wade — a costly tile it thinks is free,
        // which is a bug even when the arithmetic ties.
        var state = BoardBuilder.Rows(
                ".~.",
                "...")
            .Enemy(UnitKind.Husk, 0, 0)
            .Build();

        var reachable = Movement.Reachable(state, state.Find(UnitKind.Husk));
        var route = reachable[new Coord(1, 1)];

        Assert.Equal(2 * Activation.StepCost, route.Cost);
        Assert.DoesNotContain(new Coord(1, 0), route.Path);
        Assert.Contains(new Coord(0, 1), route.Path);
    }

    // ---- the shove-in outcome --------------------------------------------------------------------

    [Fact]
    public void ShovedIn_TakesNoDamage_IsStaggered_AndTheDisplacementStops()
    {
        // A shove of 2 that would otherwise carry the body clean across the water.
        var state = BoardBuilder.Rows("...~.")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 2, 0)
            .Build();

        var husk = state.Find(UnitKind.Husk);
        int before = husk.Hp;

        var events = new List<GameEvent>();
        var after = Displacement.Resolve(
            state, husk.Id, new Coord(0, 0), DisplacementKind.Push, 2, false, events);

        var soaked = after.Get(husk.Id);

        Assert.Equal(new Coord(3, 0), soaked.Position);
        Assert.Equal(before, soaked.Hp);
        Assert.True(soaked.Staggered);
        Assert.False(soaked.Clinging);
        Assert.Contains(events, e => e is Staggered s && s.UnitId == husk.Id);
        Assert.DoesNotContain(events, e => e is UnitDamaged);
        Assert.DoesNotContain(events, e => e is Collision);
        Assert.DoesNotContain(events, e => e is SpikeHit);
        Assert.DoesNotContain(events, e => e is Clinging);
    }

    [Fact]
    public void ShovedIn_ThePreviewSaysExactlyWhatTheResolutionDoes()
    {
        var state = BoardBuilder.Rows("...~.")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 2, 0)
            .Build();

        var husk = state.Find(UnitKind.Husk);
        var preview = Displacement.Preview(state, husk.Id, new Coord(0, 0), DisplacementKind.Push, 2);

        Assert.Equal(DisplacementStop.Water, preview.Stop);
        Assert.Equal(new Coord(3, 0), preview.Destination);
        Assert.Equal(0, preview.DamageToUnit);
        Assert.True(preview.WouldStagger);
        Assert.False(preview.WouldCling);
        Assert.False(preview.WouldDown);
        Assert.False(preview.IsNoOp);
    }

    [Fact]
    public void ShovedIn_IsNotADrain_SoAnEnemyDoesNotBraceAgainstIt()
    {
        // The enemy's refusal policy is drain-bound only. The canal must not read as a drain, or a
        // board would find every shove into it refused and the class would be unusable against the AI.
        var state = BoardBuilder.Rows("...~")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 2, 0, footing: 2)
            .Build();

        var husk = state.Find(UnitKind.Husk);

        Assert.False(Displacement.EnemyWouldRefuse(
            state, husk.Id, new Coord(0, 0), DisplacementKind.Push, 1));
    }

    [Fact]
    public void ThrownIn_IsTheSameAnswerAsBeingShovedIn()
    {
        // Being put down hard is being put down hard: the ground gives one answer to both verbs.
        var state = BoardBuilder.Rows("..~.")
            .PlayerA(UnitKind.Threadcaster, 1, 0)
            .Enemy(UnitKind.Husk, 3, 0)
            .Build();

        var fisher = state.Find(UnitKind.Threadcaster);
        var husk = state.Find(UnitKind.Husk);
        int before = husk.Hp;

        var events = new List<GameEvent>();
        var after = Throw.Resolve(state, fisher.Id, husk.Id, new Coord(2, 0), events);

        var soaked = after.Get(husk.Id);

        Assert.Equal(new Coord(2, 0), soaked.Position);
        Assert.Equal(before, soaked.Hp);
        Assert.True(soaked.Staggered);
        Assert.False(soaked.Clinging);
        Assert.Contains(events, e => e is Staggered);
    }

    // ---- the AI ladder ---------------------------------------------------------------------------

    [Fact]
    public void HazardRank_ANewFourthTierDoesNotChangeWhatHazardRanksThreeMeans()
    {
        // The shipped Stalker counts 3 and a Blunted Stalker counts 2. Adding the canal as a FOURTH
        // tier must leave both meaning exactly what they meant, which is why the water took the new
        // highest index rather than being slotted in among the existing three.
        Assert.Equal(3, UnitTemplate.For(UnitKind.Stalker).HazardRanks);
        Assert.Equal(2, UnitTemplate.For(UnitKind.BluntedStalker).HazardRanks);
    }

    [Fact]
    public void HazardRank_AStalkerAtThreeTiers_WillNotShoveIntoTheCanal()
    {
        // Water is rank 3, so a stat block that counts 3 tiers (max rank 2) never reaches it. If the
        // canal had been slotted in above the edge instead, this Stalker would have started using a
        // hazard nobody gave it.
        var state = BoardBuilder.Rows(
                "....",
                "...~",
                "....")
            .Enemy(UnitKind.Stalker, 1, 1)
            .PlayerA(UnitKind.Vanguard, 2, 1)
            .Build();

        var intent = PlanFor(state, UnitKind.Stalker);

        Assert.NotEqual(IntentAction.Push, intent.Action);
    }

    [Fact]
    public void HazardRank_WhenBothAreOnOffer_TheStalkerTakesTheBramblesAndNotTheCanal()
    {
        // Brambles are rank 1 and the canal is rank 3: 6 damage and a hard stop outranks a wetting
        // that costs nothing but the turn. The Stalker flanks from the water side and shoves east.
        var state = BoardBuilder.Rows(
                ".....",
                ".~.^.",
                ".....")
            .PlayerA(UnitKind.Vanguard, 2, 1)
            .Enemy(UnitKind.Stalker, 2, 2)
            .Build();

        var intent = PlanFor(state, UnitKind.Stalker);

        Assert.Equal(IntentAction.Push, intent.Action);

        // It moves to the canal side of the duck in order to shove it toward the brambles, which is
        // the ladder saying out loud that brambles outrank water.
        Assert.Equal(new Coord(1, 1), intent.MoveTo);
    }

    [Fact]
    public void HazardRank_TheEdgeIsStillReachableAfterTheClampMoved()
    {
        // The clamp used to be pinned at the edge and is now pinned at the deepest tier. A Stalker
        // must still trade on a board edge, or the change broke the tier it was meant to leave alone.
        var state = BoardBuilder.Rows(
                "...",
                "...",
                "...")
            .Enemy(UnitKind.Stalker, 1, 1)
            .PlayerA(UnitKind.Vanguard, 2, 1)
            .Build();

        var intent = PlanFor(state, UnitKind.Stalker);

        Assert.Equal(IntentAction.Push, intent.Action);
    }

    // ---- TerrainMutation, both ways --------------------------------------------------------------

    [Fact]
    public void Mutation_TheCanalGoesOnThroughMutate_AndComesOffThroughTheRoundEndSeam()
    {
        // The water level is TerrainMutation's second caller (D-191). A rise booked to a real round
        // is also the whole of "lowering the level": the existing seam puts the ground back with no
        // second mechanism.
        var state = BoardBuilder.Open(5, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 4, 0)
            .Build();

        var tile = new Coord(2, 0);

        var flooded = TerrainMutation.Mutate(state, tile, TileType.Water, state.Round);

        Assert.Equal(TileType.Water, flooded.Board.At(tile));
        Assert.True(TerrainMutation.IsMutated(flooded, tile));
        Assert.Equal(TileType.Open, TerrainMutation.Underlying(flooded, tile));

        var receded = PlayToNextRound(flooded);

        Assert.Equal(TileType.Open, receded.Board.At(tile));
        Assert.Empty(receded.TemporaryTerrain);
    }

    [Fact]
    public void Mutation_WaterOverBrambles_RecedesToBrambles_NotToFloor()
    {
        // Stacking is already correct and the canal inherits it rather than restating it.
        var state = BoardBuilder.Rows("..^..")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 4, 0)
            .Build();

        var tile = new Coord(2, 0);

        var flooded = TerrainMutation.Mutate(state, tile, TileType.Water, state.Round);
        Assert.Equal(TileType.Spikes, TerrainMutation.Underlying(flooded, tile));

        var receded = PlayToNextRound(flooded);
        Assert.Equal(TileType.Spikes, receded.Board.At(tile));
    }

    // ---- the sluice: publication and the flood ---------------------------------------------------

    [Fact]
    public void Sluice_ThePublishedNextStepIsReadableBeforeAnythingIsSpent()
    {
        var state = Locks(out _, out var gate);

        Assert.Equal(0, Sluice.Level(state));

        var next = Sluice.Next(state);
        Assert.NotNull(next);
        Assert.Equal(gate, next!.Gate);
        Assert.Equal(new[] { new Coord(3, 0), new Coord(4, 0) }, next.Tiles.ToArray());

        // Nothing is owed while the gate stands, and nothing has happened to the board.
        Assert.Empty(Sluice.Pending(state));
        Assert.Equal(TileType.Open, state.Board.At(new Coord(3, 0)));
    }

    [Fact]
    public void Sluice_AGateBroughtDownMidRound_PublishesItsTilesAtOnce_AndFloodsWhenTheRoundTurns()
    {
        var state = Locks(out var husk, out var gate);

        state = BreakTheGate(state, husk);

        // Published the instant the masonry falls: the whole of the telegraph, and the reason the
        // change is not a surprise when it lands.
        Assert.Null(state.StructureAt(gate));
        Assert.Equal(1, Sluice.Level(state));
        Assert.Equal(new[] { new Coord(3, 0), new Coord(4, 0) }, Sluice.Pending(state).ToArray());

        // And the board is still dry for the rest of the round.
        Assert.Equal(TileType.Open, state.Board.At(new Coord(3, 0)));

        var (risen, seam) = PlayToRoundSeam(state);

        Assert.Equal(TileType.Water, risen.Board.At(new Coord(3, 0)));
        Assert.Equal(TileType.Water, risen.Board.At(new Coord(4, 0)));
        Assert.Empty(Sluice.Pending(risen));
        Assert.Equal(2, seam.Events.OfType<CanalRose>().Count());
        Assert.All(seam.Events.OfType<CanalRose>(), e => Assert.Equal(gate, e.Gate));
    }

    [Fact]
    public void Sluice_TheFloodDefersUnderAStandingUnit_AndFlowsInOnceTheTileIsVacated()
    {
        // The one genuine design ruling, provisional: option 2 of the three
        // TerrainMutation.ExpiryBeneathUnit's remarks enumerate. It is the only one that preserves
        // the existing invariant — the ground still never changes under a body, it simply waits.
        var state = LocksWithADuckInThePath(out var wardbearer, out var husk);
        var under = new Coord(3, 0);

        Assert.Equal(under, state.Get(wardbearer).Position);

        state = BreakTheGate(state, husk);
        state = PlayToNextRound(state);

        // The clear tile took the water; the occupied one did not, and nothing happened to the duck.
        Assert.Equal(TileType.Water, state.Board.At(new Coord(4, 0)));
        Assert.Equal(TileType.Open, state.Board.At(under));

        var waiting = state.Get(wardbearer);
        Assert.Equal(waiting.MaxHp, waiting.Hp);
        Assert.Equal(under, waiting.Position);
        Assert.False(waiting.Staggered);

        // The debt is still on the books, which is what makes the deferral inspectable rather than
        // a change that quietly did not happen.
        Assert.Equal(new[] { under }, Sluice.Pending(state).ToArray());

        // Step away, and the canal comes in at the next round start.
        state = state.Then(new MoveCommand(wardbearer, new Coord(3, 1), new[] { new Coord(3, 1) }));
        state = state.Then(new EndActivationCommand(wardbearer));
        state = PlayToNextRound(state);

        Assert.Equal(TileType.Water, state.Board.At(under));
        Assert.Empty(Sluice.Pending(state));
    }

    [Fact]
    public void Sluice_TheLevelNeedsNoStateOfItsOwn_SoTwoIdenticalPlaysAgree()
    {
        // Determinism without a field: the level is a pure function of the authored schedule, which
        // gates stand, and what the board already says.
        var first = Play();
        var second = Play();

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.Equal(TileType.Water, first.Board.At(new Coord(4, 0)));

        static GameState Play()
        {
            var state = Locks(out var husk, out _);
            return PlayToNextRound(BreakTheGate(state, husk));
        }
    }

    // ---- the .fight format -----------------------------------------------------------------------

    [Fact]
    public void Format_TheCanalCharacterParsesAndSurvivesTheWriter()
    {
        var parsed = Parse(WaterBoard);

        Assert.Equal(TileType.Water, parsed.Board.At(new Coord(2, 2)));
        Assert.Equal(TileType.Water, parsed.Board.At(new Coord(3, 2)));

        var reparsed = FightParser.Parse(FightWriter.Write(parsed));

        Assert.True(reparsed.Ok, reparsed.Describe());
        Assert.Equal(parsed, reparsed.Fight);
        Assert.Equal(TileType.Water, reparsed.Fight!.Board.At(new Coord(2, 2)));
    }

    [Fact]
    public void Format_TheCanalCharacterIsReserved_SoNoSpawnCanStealIt()
    {
        var result = FightParser.Parse(
            Text(WaterBoard, spawns: "spawn h = Husk\nspawn " + BoardLayout.Water + " = Lobber"));

        Assert.Contains(result.Issues, i => i.Code == FightIssueCode.MalformedLine);
    }

    [Fact]
    public void Format_ASluiceLineParses_AndSurvivesTheWriterWhole()
    {
        var parsed = Parse(SluiceBoard, "blocker-hp: 8\nsluice: 3,3 = 2,2 3,2");

        var step = Assert.Single(parsed.SluiceSteps);
        Assert.Equal(new Coord(3, 3), step.Gate);
        Assert.Equal(new[] { new Coord(2, 2), new Coord(3, 2) }, step.Tiles.ToArray());

        var reparsed = FightParser.Parse(FightWriter.Write(parsed));

        Assert.True(reparsed.Ok, reparsed.Describe());
        Assert.Equal(parsed, reparsed.Fight);
    }

    [Fact]
    public void Format_ASluiceWhoseGateIsNotAStructure_IsRejected()
    {
        // "No standing structure" is exactly how a fallen gate reads, so a gate nobody built would
        // start the board flooded. Worth failing loudly rather than shipping.
        var result = FightParser.Parse(Text(SluiceBoard, "blocker-hp: 8\nsluice: 0,2 = 2,2 3,2"));

        Assert.Contains(result.Issues, i => i.Code == FightIssueCode.BadValue);
    }

    [Fact]
    public void Format_ASluiceLineWithNoEquals_IsRejectedByName()
    {
        var result = FightParser.Parse(Text(SluiceBoard, "blocker-hp: 8\nsluice: 3,3 2,2"));

        Assert.Contains(result.Issues, i => i.Code == FightIssueCode.MalformedLine);
    }

    // ---- what a person is shown ------------------------------------------------------------------

    [Fact]
    public void Log_NamesTheCanal_NeverTheEnumIdentifier()
    {
        var state = BoardBuilder.Open(3, 1).PlayerA(UnitKind.Vanguard, 0, 0).Build();

        string reverted = CombatLog.Detail(
            new TerrainReverted(new Coord(1, 0), TileType.Water, null), state);

        Assert.Contains("canal", reverted, System.StringComparison.Ordinal);
        Assert.DoesNotContain("Water", reverted, System.StringComparison.Ordinal);

        string rose = CombatLog.Detail(new CanalRose(new Coord(1, 0), new Coord(2, 0)), state);

        Assert.Contains("canal", rose, System.StringComparison.Ordinal);
        Assert.Contains("sluice", rose, System.StringComparison.Ordinal);
        Assert.True(CombatLog.IsHandled(new CanalRose(new Coord(1, 0), new Coord(2, 0))));
    }

    // ---- fixtures --------------------------------------------------------------------------------

    /// <summary>
    /// A canal in a line, held back by one sluice at (2,0), flooding (3,0) and (4,0). The Husk has
    /// hit points to spare so that slamming it into the gate does not also end the fight.
    /// </summary>
    private static GameState Locks(out UnitId husk, out Coord gate)
    {
        gate = new Coord(2, 0);

        var state = Cut()
            .PlayerA(UnitKind.Wardbearer, 5, 1)
            .Build();

        husk = state.Find(UnitKind.Husk).Id;
        return state;
    }

    /// <summary>The same cut, with a duck parked on the near flood tile.</summary>
    private static GameState LocksWithADuckInThePath(out UnitId wardbearer, out UnitId husk)
    {
        var state = Cut()
            .PlayerA(UnitKind.Wardbearer, 3, 0)
            .Build();

        wardbearer = state.Find(UnitKind.Wardbearer).Id;
        husk = state.Find(UnitKind.Husk).Id;
        return state;
    }

    private static BoardBuilder Cut() =>
        BoardBuilder.Rows(
                "......",
                "......")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 12)
            .Blockers(6, new Coord(2, 0))
            .Sluice(new Coord(2, 0), new Coord(3, 0), new Coord(4, 0));

    /// <summary>Slams the Husk into the sluice: one collision is 6, and the gate has 6.</summary>
    private static GameState BreakTheGate(GameState state, UnitId husk)
    {
        var events = new List<GameEvent>();
        return Displacement.Resolve(
            state, husk, new Coord(0, 0), DisplacementKind.Push, 1, false, events);
    }

    private static EnemyIntent PlanFor(GameState state, UnitKind kind)
    {
        var planned = Ai.DeclareAll(state, new List<GameEvent>());
        return planned.Intents.Single(i => planned.Get(i.UnitId).Kind == kind);
    }

    private static GameState PlayToNextRound(GameState state) => PlayToRoundSeam(state).State;

    private static (GameState State, StepResult Seam) PlayToRoundSeam(GameState state)
    {
        int round = state.Round;
        var result = state.PassCurrent();

        for (int i = 0; i < 60 && result.NewState.Round == round; i++)
        {
            result = result.NewState.PassCurrent();
        }

        TestPlay.Require(result.NewState.Round == round + 1, "the round never turned");
        return (result.NewState, result);
    }

    private const string WaterBoard = """
        ..l..BB
        .H...BB
        ..~~...
        ^.....^
        .......
        AA...H.
        AA.h..#
        """;

    private const string SluiceBoard = """
        ..l..BB
        .H...BB
        ..~~...
        ^..X..^
        .......
        AA...H.
        AA.h..#
        """;

    private static FightDefinition Parse(string board, string extraHeader = "")
    {
        var result = FightParser.Parse(Text(board, extraHeader));
        Assert.True(result.Ok, result.Describe());
        return result.Fight!;
    }

    private static string Text(
        string board,
        string extraHeader = "",
        string spawns = "spawn h = Husk\nspawn l = Lobber") =>
        string.Join(
            "\n",
            "id: canal-fixture",
            "number: 903",
            "name: Canal Fixture",
            "pool: Ordinary",
            "description: A cut of the canal.",
            string.Empty,
            spawns,
            string.Empty,
            "roster a: Vanguard, Archer",
            "roster b: Threadcaster, Wardbearer",
            extraHeader,
            string.Empty,
            "board:",
            string.Join("\n", board.Split('\n').Select(row => "  " + row.TrimEnd('\r'))));
}
