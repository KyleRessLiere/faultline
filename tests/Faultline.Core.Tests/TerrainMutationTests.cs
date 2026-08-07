using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// Terrain that changes mid-fight and changes back, as a system rather than as one card's private
/// trick (D-191, D-210).
/// </summary>
/// <remarks>
/// <para>
/// A Thorn Pouch was the first rule in the game to change terrain during a fight. <b>Cracked</b> and
/// the collapse clock are the next two (MASTER_DESIGN §3, §13), so the booking, the stacking rule and
/// the change back live in <see cref="TerrainMutation"/> and the pouch is a call site. The tests here
/// are split accordingly: the ones that go through <c>UseConsumableCommand</c> prove the item still
/// works, and the ones that call <see cref="TerrainMutation.Mutate"/> directly prove the system works
/// for a caller that is not an item — which is the only thing that stops the next feature copying it.
/// </para>
/// <para>
/// <b>Reachability is stated in the names.</b> CLAUDE.md's earned practice is to reach a state by
/// playing rather than by restoring one. Everything reachable by play is played here. Two cases are
/// not reachable by any card that exists — stacking a mutation on a mutation, and mutating a tile to
/// something unwalkable — because the only mutation source today is a pouch whose own filter refuses
/// both. Those say so where they are named.
/// </para>
/// </remarks>
public class TerrainMutationTests
{
    // ---- the promotion itself -------------------------------------------------------------------

    [Fact]
    public void ThePromotion_AMutationWithNoItemBehindIt_StillBooksItsOwnWayBack()
    {
        // The whole claim. Nothing here is a consumable: this is the call Cracked and the collapse
        // clock will make, and reversion arrives with it rather than being written again.
        var state = Corridor(out _, out _);
        var tile = new Coord(3, 0);

        var mutated = TerrainMutation.Mutate(state, tile, TileType.Spikes, state.Round);

        Assert.Equal(TileType.Spikes, mutated.Board.At(tile));
        Assert.True(TerrainMutation.IsMutated(mutated, tile));
        Assert.Equal(TileType.Open, TerrainMutation.Underlying(mutated, tile));

        var next = PlayToNextRound(mutated);

        Assert.Equal(TileType.Open, next.Board.At(tile));
        Assert.Empty(next.TemporaryTerrain);
        Assert.False(TerrainMutation.IsMutated(next, tile));
    }

    [Fact]
    public void ThornPouch_IsACallSite_AndItsBramblesAreAnOrdinaryMutation()
    {
        var state = Corridor(out var duck, out _);
        var tile = new Coord(1, 0);

        var scattered = state.WithPocket(duck, Consumable.ThornPouch)
            .Then(new UseConsumableCommand(duck, null, tile));

        // Indistinguishable from the item-free mutation above, which is the point of promoting it.
        var direct = TerrainMutation.Mutate(state, tile, TileType.Spikes, state.Round);

        Assert.Equal(direct.Board, scattered.Board);
        Assert.Equal(direct.TemporaryTerrain, scattered.TemporaryTerrain);
    }

    // ---- creation beneath a standing unit (MASTER_DESIGN §14 #16, first half) --------------------

    [Fact]
    public void CreationBeneathAStandingUnit_IsRefusedByName_FollowingTheCrateOfDebrisPrecedent()
    {
        var state = Corridor(out var duck, out var husk);
        var under = state.Get(husk).Position;

        // §14 #16 leaves this unruled and names Crate of Debris ("adjacent open tile") as the nearest
        // precedent. The narrow reading ships: growing ground under a body raises a damage question
        // no card answers, and a rule that has to invent an answer to ship is shipping a guess.
        Assert.False(TerrainMutation.CanMutate(state, under));
        Assert.Throws<IllegalCommandException>(
            () => TerrainMutation.Mutate(state, under, TileType.Spikes, state.Round));

        // And off the board is its own refusal, with its own reason, rather than a shrug.
        Assert.False(TerrainMutation.CanMutate(state, new Coord(-1, 0)));
        Assert.Throws<IllegalCommandException>(
            () => TerrainMutation.Mutate(state, new Coord(-1, 0), TileType.Spikes, state.Round));

        // The pouch inherits the refusal rather than restating it.
        TestPlay.AssertIllegal(
            state.WithPocket(duck, Consumable.ThornPouch),
            new UseConsumableCommand(duck, null, under));
    }

    // ---- expiry beneath a standing unit (MASTER_DESIGN §14 #16, second half) ---------------------

    [Fact]
    public void ExpiryBeneathAStandingUnit_ChangesTheGroundAndLeavesTheBodyAlone()
    {
        // UNRULED. Creation can refuse to happen under a body; expiry cannot — a duck can walk onto
        // brambles a pouch grew, pay the walk-on price honestly, and still be standing there when the
        // round ends. The conservative answer ships behind TerrainMutation.ExpiryBeneathUnit.
        var state = Neighbours(out var thrower, out var walker, out _);
        var tile = new Coord(1, 0);

        var scattered = PassUntil(
            state.WithPocket(thrower, Consumable.ThornPouch)
                .Then(new UseConsumableCommand(thrower, null, tile)),
            walker);

        // Played onto the brambles, not placed on them: the walk-on price is paid for real.
        Assert.True(Movement.TryGetMove(scattered, scattered.Get(walker), tile, out var step));
        var standing = scattered.Then(new MoveCommand(walker, tile, step!.Path));

        Assert.Equal(tile, standing.Get(walker).Position);
        Assert.Equal(TileType.Spikes, standing.Board.At(tile));

        var before = standing.Get(walker);
        var (after, seam) = PlayToRoundSeam(standing);

        // The ground changed back under a body, and the case is in the log rather than invisible.
        var reverted = seam.All<TerrainReverted>().Single(e => e.At == tile);
        Assert.Equal(TileType.Open, reverted.Now);
        Assert.Equal(walker, reverted.Beneath);

        Assert.Equal(TileType.Open, after.Board.At(tile));

        // And the body is untouched: no damage, no displacement, no Footing, no Clinging, no second
        // walk-on price for ground it never walked onto.
        var now = after.Get(walker);
        Assert.Equal(before.Hp, now.Hp);
        Assert.Equal(before.Position, now.Position);
        Assert.Equal(before.Footing, now.Footing);
        Assert.False(now.Clinging);
        Assert.True(now.IsOnBoard);
    }

    [Fact]
    public void ExpiryOnAnEmptyTile_NamesNobody_SoTheStandingCaseIsLegibleInTheLog()
    {
        var state = Corridor(out var duck, out _);
        var tile = new Coord(1, 0);

        var scattered = state.WithPocket(duck, Consumable.ThornPouch)
            .Then(new UseConsumableCommand(duck, null, tile));

        var (_, seam) = PlayToRoundSeam(scattered);

        Assert.Null(seam.All<TerrainReverted>().Single(e => e.At == tile).Beneath);
    }

    // ---- save / load ----------------------------------------------------------------------------

    [Fact]
    public void SaveAndLoad_TheRoundTrippedCommandLog_RebuildsTheMutationAndItsWayBack()
    {
        // The save format is the seed plus the ordered command list (RunRecord). A board change that
        // did not survive the round trip would be a save that reloads onto different ground.
        var start = Corridor(out var duck, out _).WithPocket(duck, Consumable.ThornPouch);
        var tile = new Coord(1, 0);

        var log = new List<Command>();
        var state = start;

        void Play(Command command)
        {
            log.Add(command);
            state = Game.Apply(state, command).NewState;
        }

        Play(new UseConsumableCommand(duck, null, tile));
        var scattered = state;
        Assert.Equal(TileType.Spikes, scattered.Board.At(tile));

        int round = state.Round;
        for (int i = 0; i < 60 && state.Round == round; i++)
        {
            Play(new EndActivationCommand(state.CurrentUnit().Id));
        }

        Assert.Equal(round + 1, state.Round);
        Assert.Equal(TileType.Open, state.Board.At(tile));

        var record = new RunRecord
        {
            FightId = "terrain",
            FightNumber = 0,
            Seed = start.Seed,
            Commands = log,
        };

        Assert.True(RunRecord.TryParse(record.Render(), out var parsed));
        Assert.Equal(log, parsed.Commands);

        // Loaded: the mutation while it stands, and the way back once it has been taken.
        var midway = TestPlay.Replay(start, parsed.Commands.Take(1));
        Assert.Equal(TileType.Spikes, midway.Board.At(tile));
        Assert.Equal(scattered.TemporaryTerrain, midway.TemporaryTerrain);
        Assert.Equal(scattered, midway);

        var reloaded = TestPlay.Replay(start, parsed.Commands);
        Assert.Equal(state, reloaded);
        Assert.Equal(state.GetHashCode(), reloaded.GetHashCode());
    }

    // ---- undo -----------------------------------------------------------------------------------

    [Fact]
    public void Undo_DroppingTheTailAndReplaying_TakesTheMutatedTileBackWithIt()
    {
        // This is exactly what GameSession.Undo does — it never inverts anything, it rebuilds from
        // the seed with the last entry dropped. A board change undo forgot would be a desync, so the
        // thing worth asserting is that the rebuild lands on the identical board.
        var start = Corridor(out var duck, out _).WithPocket(duck, Consumable.ThornPouch);
        var tile = new Coord(1, 0);

        var scatter = new UseConsumableCommand(duck, null, tile);
        var after = Game.Apply(start, scatter).NewState;

        Assert.Equal(TileType.Spikes, after.Board.At(tile));

        var rewound = TestPlay.Replay(start, new List<Command>());

        Assert.Equal(TileType.Open, rewound.Board.At(tile));
        Assert.Empty(rewound.TemporaryTerrain);
        Assert.Equal(start, rewound);
        Assert.Equal(start.GetHashCode(), rewound.GetHashCode());
    }

    [Fact]
    public void Undo_AMutationDrawsNothingFromTheSeed_SoTheShellHasNoReasonToRefuseTheRewind()
    {
        // GameSession refuses to undo any command that advanced the generator ("a seeded roll has
        // been made"). Growing brambles must not look like one, or the promotion would ship a board
        // change the player cannot take back.
        var state = Corridor(out var duck, out _).WithPocket(duck, Consumable.ThornPouch);

        var scattered = state.Then(new UseConsumableCommand(duck, null, new Coord(1, 0)));

        Assert.Equal(state.RngState, scattered.RngState);
    }

    // ---- AI pathing ------------------------------------------------------------------------------

    [Fact]
    public void AiPathing_TheReachableField_PricesBramblesThatWereNotThereAtFightStart()
    {
        // Movement.Reachable is the search the movement highlight, the threat overlay and the enemy
        // planner all run. An AI that priced this tile as Open would be planning against a board that
        // does not exist.
        var state = AtTheEdgeOfReach(out var thrower, out var mover, out _);
        var tile = state.Get(thrower).Position.Step(Direction.Left);

        // The far edge of what the mover can afford, priced as ordinary ground.
        var before = Movement.Reachable(state, state.Get(mover));
        Assert.True(before.ContainsKey(tile));
        Assert.Equal(Activation.StepCost, Movement.StepCost(state.Board.At(tile), state.Get(mover)));

        // The mover has not acted, so nothing but the ground changes between the two searches.
        var scattered = state.WithPocket(thrower, Consumable.ThornPouch)
            .Then(new UseConsumableCommand(thrower, null, tile));

        var after = Movement.Reachable(scattered, scattered.Get(mover));

        // Both halves: the per-tile price, and the search that spends it.
        Assert.Equal(
            Activation.BrambleCost, Movement.StepCost(scattered.Board.At(tile), scattered.Get(mover)));
        Assert.False(
            after.ContainsKey(tile),
            "the field still reaches a tile the mover can no longer afford");

        // Nothing got cheaper and nothing new came into reach: the only change is the ground.
        Assert.All(after, reached => Assert.True(reached.Value.Cost >= before[reached.Key].Cost));
    }

    [Fact]
    public void AiPathing_AMutationThatBlocksATile_RoutesTheEnemyRoundIt_NoItemDoesThisYet()
    {
        // Not reachable by play: the only mutation source today is a pouch that grows brambles, and
        // brambles are walkable. This is the case Cracked and the collapse clock bring, so the system
        // is tested for it before they arrive rather than after.
        var state = BoardBuilder.Rows(
                ".....",
                "#.#.#",
                ".....")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 0, 2)
            .Build();

        var husk = state.Find(UnitKind.Husk);
        var gap = new Coord(1, 1);

        var open = PathField.To(state, husk, new Coord(0, 0));
        Assert.True(open.Reaches(gap));

        var blocked = TerrainMutation.Mutate(state, gap, TileType.Wall, state.Round);
        var field = PathField.To(blocked, blocked.Find(UnitKind.Husk), new Coord(0, 0));

        // The flow field the planner walks refuses a tile that was walkable at fight start.
        Assert.False(field.Reaches(gap));
        Assert.False(Movement.Reachable(blocked, blocked.Find(UnitKind.Husk)).ContainsKey(gap));

        // And it is temporary: the wall is gone when the round is.
        Assert.Equal(TileType.Open, PlayToNextRound(blocked).Board.At(gap));
    }

    // ---- action preview --------------------------------------------------------------------------

    [Fact]
    public void ActionPreview_ProjectsAgainstTheMutatedBoard_AndTheResolutionAgrees()
    {
        // D-184's lesson: a projection resolves against the board as it actually is. A preview that
        // read the fight's original terrain is exactly the class of lie Stage A existed to kill.
        int push = AbilityDefinition.For(Ability.StaggerShot).Push;

        var huskAt = new Coord(2, 0);
        var landing = new Coord(huskAt.X + push, 0);
        var throwerAt = new Coord(landing.X + 1, 0);

        // The thrower's flock holds the opening slot, so the ground is prepared before the shot is
        // taken — which is also the only order in which this play exists.
        var state = BoardBuilder.Open(throwerAt.X + 4, 1)
            .PlayerA(UnitKind.Vanguard, throwerAt.X, 0)
            .PlayerB(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Bulwark, huskAt.X, 0, hp: 40)
            .Build();

        var archer = state.Find(UnitKind.Archer).Id;
        var thrower = state.Find(UnitKind.Vanguard).Id;
        var body = state.Find(UnitKind.Bulwark).Id;

        var shot = new AbilityCommand(archer, Ability.StaggerShot, body);

        var clean = Abilities.Outlook(state, shot)!.Displacement!;
        TestPlay.Require(clean.Destination == landing, "the shove does not end where the test expects");
        Assert.False(clean.WouldStagger);

        var scattered = state.WithPocket(thrower, Consumable.ThornPouch)
            .Then(new UseConsumableCommand(thrower, null, landing));

        var projected = Abilities.Outlook(scattered, shot)!.Displacement!;

        // The preview grew teeth the instant the ground did.
        Assert.True(projected.DamageToUnit > clean.DamageToUnit);
        Assert.True(projected.WouldStagger);
        Assert.Equal(landing, projected.Destination);

        // No number is written down here. The projection is measured against its own resolution,
        // which is the only comparison a preview cannot rot away from.
        var resolved = PassUntil(scattered, archer).Step(shot);

        Assert.Equal(landing, resolved.NewState.Get(body).Position);
        Assert.Equal(
            projected.DamageToUnit,
            resolved.All<SpikeHit>().Where(h => h.UnitId == body).Sum(h => h.Damage));
        // Read off the event rather than the flag: this shot closes the last slot of the round, and
        // Stagger clears at the round seam, so the state afterwards has already forgotten it.
        Assert.Equal(
            projected.WouldStagger, resolved.All<Staggered>().Any(e => e.UnitId == body));
    }

    // ---- terrain over terrain ---------------------------------------------------------------------

    [Fact]
    public void Stacking_ASecondMutationKeepsTheFirstsWas_SoReversionNeverRestoresATemporaryTile_NoItemStacksYet()
    {
        // Not reachable by play: the pouch's own filter takes ordinary open ground only, so no card
        // can mutate a tile that is already mutated. The rule still has to be right before the second
        // source exists, because getting it wrong deletes a hazard that two effects each promised to
        // put back.
        var state = BoardBuilder.Rows(".^.....")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 6, 0)
            .Build();

        var tile = new Coord(1, 0);
        Assert.Equal(TileType.Spikes, state.Board.At(tile));

        var once = TerrainMutation.Mutate(state, tile, TileType.Wall, state.Round);
        Assert.Equal(TileType.Spikes, TerrainMutation.Underlying(once, tile));

        var twice = TerrainMutation.Mutate(once, tile, TileType.Open, state.Round);

        // The second mutation's "before" was itself temporary. Storing it would restore ground that
        // was never there and quietly delete the brambles the board was authored with.
        Assert.Equal(TileType.Open, twice.Board.At(tile));
        Assert.Equal(TileType.Spikes, TerrainMutation.Underlying(twice, tile));
        Assert.Single(twice.TemporaryTerrain);

        var next = PlayToNextRound(twice);
        Assert.Equal(TileType.Spikes, next.Board.At(tile));
        Assert.Empty(next.TemporaryTerrain);
    }

    [Fact]
    public void Stacking_TheClockTakesTheLaterRound_SoNeitherSourcesPromiseIsCutShort()
    {
        var state = Corridor(out _, out _);
        var tile = new Coord(3, 0);

        var longer = TerrainMutation.Mutate(state, tile, TileType.Spikes, state.Round + 1);
        var thenShorter = TerrainMutation.Mutate(longer, tile, TileType.Wall, state.Round);

        Assert.Equal(state.Round + 1, TerrainMutation.BookingAt(thenShorter, tile)!.Value.ThroughRound);

        // It survives the round the shorter booking asked for, and goes on the round the longer one did.
        var held = PlayToNextRound(thenShorter);
        Assert.Equal(TileType.Wall, held.Board.At(tile));

        var gone = PlayToNextRound(held);
        Assert.Equal(TileType.Open, gone.Board.At(tile));
    }

    // ---- fixtures ---------------------------------------------------------------------------------

    /// <summary>A one-wide board, so there is exactly one route and nothing to detour around.</summary>
    private static GameState Corridor(out UnitId duck, out UnitId husk)
    {
        var state = BoardBuilder.Open(7, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 6, 0)
            .Build();

        duck = state.Find(UnitKind.Vanguard).Id;
        husk = state.Find(UnitKind.Husk).Id;
        return state;
    }

    /// <summary>
    /// One duck to throw the pouch and a second, still holding its whole activation, to walk onto
    /// what the first one grew — one step away, so the walk is affordable. Two units, so no test has
    /// to disentangle the pouch's own cost from the ground's.
    /// </summary>
    private static GameState Neighbours(out UnitId thrower, out UnitId walker, out UnitId husk)
    {
        var state = BoardBuilder.Open(9, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .PlayerA(UnitKind.Wardbearer, 2, 0)
            .Enemy(UnitKind.Husk, 8, 0)
            .Build();

        thrower = state.Find(UnitKind.Vanguard).Id;
        walker = state.Find(UnitKind.Wardbearer).Id;
        husk = state.Find(UnitKind.Husk).Id;
        return state;
    }

    /// <summary>
    /// The thrower stands so that the tile it can reach is the last one the mover can afford. One
    /// tile of surcharge is then the difference between in reach and out of it, which is what makes
    /// the search's answer legible rather than merely arithmetic.
    /// </summary>
    private static GameState AtTheEdgeOfReach(out UnitId thrower, out UnitId mover, out UnitId husk)
    {
        var state = BoardBuilder.Open(9, 1)
            .PlayerA(UnitKind.Wardbearer, 0, 0)
            .PlayerA(UnitKind.Vanguard, 4, 0)
            .Enemy(UnitKind.Husk, 8, 0)
            .Build();

        mover = state.Find(UnitKind.Wardbearer).Id;
        thrower = state.Find(UnitKind.Vanguard).Id;
        husk = state.Find(UnitKind.Husk).Id;
        return state;
    }

    /// <summary>Passes slot after slot until the named duck can be commanded.</summary>
    private static GameState PassUntil(GameState state, UnitId id)
    {
        for (int i = 0; i < 20; i++)
        {
            var unit = state.Get(id);
            if (state.ActiveTeam == unit.Team
                && !unit.HasActivated
                && (state.ActiveUnitId is null || state.ActiveUnitId == id))
            {
                return state;
            }

            state = state.PassCurrent().NewState;
        }

        TestPlay.Require(false, "the slot never came round to that duck");
        return state;
    }

    private static GameState PlayToNextRound(GameState state) => PlayToRoundSeam(state).State;

    /// <summary>
    /// Plays the round out slot by slot and hands back the step that turned it, so a test can read
    /// what the seam emitted rather than inferring it from the state afterwards.
    /// </summary>
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
}
