using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

public class DeploymentTests
{
    [Fact]
    public void Start_PlacesEnemiesButLeavesPlayerUnitsUndeployed()
    {
        var result = Game.Start(FightLibrary.Fight1(), seed: 7);
        var state = result.NewState;

        Assert.Equal(Phase.Deployment, state.Phase);
        Assert.Equal(0, state.Round);
        Assert.Equal(Team.PlayerA, state.ActiveTeam);

        Assert.All(
            state.Units.Where(u => u.Team == Team.Enemy),
            u => Assert.True(u.IsDeployed));
        Assert.All(
            state.Units.Where(u => u.Team != Team.Enemy),
            u => Assert.False(u.IsDeployed));

        Assert.Equal(4, result.All<UnitDeployed>().Count);
        Assert.Equal(1, result.Single<FightStarted>().FightNumber);
    }

    /// <summary>
    /// <b>§3's snake, at the 2/2 roster it is written for: A · B · B · A.</b> Straight alternation
    /// would hand the first picker both best tiles in expectation; the second picker's back-to-back
    /// pair in the middle is what pays for picking second.
    /// </summary>
    [Fact]
    public void Deployment_Snakes_ABBA()
    {
        var state = Game.Start(FightLibrary.Fight1(), seed: 7).NewState.DraftOrder(Team.PlayerA);

        Assert.Equal(Team.PlayerA, state.ActiveTeam);
        state = DeployFirstLegal(state);

        Assert.Equal(Team.PlayerB, state.ActiveTeam);
        state = DeployFirstLegal(state);

        Assert.Equal(Team.PlayerB, state.ActiveTeam);
        state = DeployFirstLegal(state);

        Assert.Equal(Team.PlayerA, state.ActiveTeam);
    }

    /// <summary>The snake is anchored on the step-1 winner, not on Player A.</summary>
    [Fact]
    public void Deployment_Snakes_FromWhoeverWonStepOne()
    {
        var state = Game.Start(FightLibrary.Fight1(), seed: 7).NewState.DraftOrder(Team.PlayerB);

        Assert.Equal(Team.PlayerB, state.ActiveTeam);
        state = DeployFirstLegal(state);

        Assert.Equal(Team.PlayerA, state.ActiveTeam);
        state = DeployFirstLegal(state);

        Assert.Equal(Team.PlayerA, state.ActiveTeam);
        state = DeployFirstLegal(state);

        Assert.Equal(Team.PlayerB, state.ActiveTeam);
    }

    /// <summary>
    /// <b>No duck may be placed before the board knows who is placing.</b> Step 1 gates the draft.
    /// </summary>
    [Fact]
    public void Deployment_BeforeStepOneIsAnswered_PlacingIsIllegal()
    {
        var state = Game.Start(FightLibrary.Fight1(), seed: 7).NewState;
        var unit = state.Units.First(u => u.Team == Team.PlayerA);

        Assert.All(
            Game.LegalCommands(state),
            c => Assert.IsType<DraftOrderCommand>(c));

        TestPlay.AssertIllegal(state, new DeployCommand(unit.Id, new Coord(0, 5)));
    }

    /// <summary>Step 1 is asked once; there are no re-votes.</summary>
    [Fact]
    public void Deployment_AnsweringStepOneTwice_IsIllegal()
    {
        var state = Game.Start(FightLibrary.Fight1(), seed: 7).NewState.DraftOrder(Team.PlayerA);

        TestPlay.AssertIllegal(
            state,
            new DraftOrderCommand(DeploymentChoice.PlaceFirst, DeploymentChoice.PlaceSecond));
    }

    /// <summary>
    /// <b>A spot is owned by neither player</b> (§3), so a tile that used to be "Player B's corner"
    /// is a legal pick for Player A. This test asserted the opposite until the draft landed.
    /// </summary>
    [Fact]
    public void Deployment_LetsEitherPlayerTakeAnyOpenSpot()
    {
        var fight = FightLibrary.Fight1();
        var state = Game.Start(fight, seed: 7).NewState.DraftOrder(Team.PlayerA);
        var unit = state.Units.First(u => u.Team == Team.PlayerA);

        // (6,0) was written as a Player B slot. Player A may take it.
        Assert.Contains(new Coord(6, 0), fight.Spots);
        state = state.Then(new DeployCommand(unit.Id, new Coord(6, 0)));
        Assert.Equal(new Coord(6, 0), state.Get(unit.Id).Position);
    }

    /// <summary>A tile that is not a published spot is still not a placement.</summary>
    [Fact]
    public void Deployment_RejectsATileThatIsNotASpot()
    {
        var state = Game.Start(FightLibrary.Fight1(), seed: 7).NewState.DraftOrder(Team.PlayerA);
        var unit = state.Units.First(u => u.Team == Team.PlayerA);

        TestPlay.AssertIllegal(state, new DeployCommand(unit.Id, new Coord(3, 3)));
    }

    [Fact]
    public void Deployment_RejectsAnOccupiedTile()
    {
        var state = Game.Start(FightLibrary.Fight1(), seed: 7).NewState.DraftOrder(Team.PlayerA);
        var first = state.Units.First(u => u.Team == Team.PlayerA);
        var second = state.Units.Where(u => u.Team == Team.PlayerA).ElementAt(1);

        state = state.Then(new DeployCommand(first.Id, new Coord(0, 5)));
        state = state.Then(new DeployCommand(
            state.Units.First(u => u.Team == Team.PlayerB).Id,
            new Coord(6, 0)));

        // Back to B under the snake, and either way the taken spot is gone for everybody.
        TestPlay.AssertIllegal(state, new DeployCommand(second.Id, new Coord(0, 5)));
    }

    [Fact]
    public void Deployment_CompletingIt_StartsRoundOne()
    {
        var state = Game.Start(FightLibrary.Fight1(), seed: 7).NewState.DraftOrder(Team.PlayerA);

        StepResult? last = null;
        while (state.Phase == Phase.Deployment)
        {
            last = state.Step(Game.LegalCommands(state)[0]);
            state = last.NewState;
        }

        Assert.NotNull(last);
        Assert.True(last!.Has<DeploymentCompleted>());
        Assert.Equal(1, last.Single<RoundStarted>().Round);

        Assert.Equal(Phase.Battle, state.Phase);
        Assert.Equal(1, state.Round);
        Assert.Equal(Team.PlayerA, state.ActiveTeam);
        Assert.All(state.Units, u => Assert.True(u.IsDeployed));
    }

    /// <summary>
    /// <b>The initiative bundle (§3): winning step 1 wins placing first AND activating first.</b>
    /// The two halves are one prize, so the round opens on whoever won the placement question.
    /// </summary>
    [Fact]
    public void Deployment_InitiativeBundles_TheStepOneWinnerAlsoOpensTheRound()
    {
        var state = Game.Start(FightLibrary.Fight1(), seed: 7).NewState.DraftOrder(Team.PlayerB);

        while (state.Phase == Phase.Deployment)
        {
            state = state.Then(Game.LegalCommands(state)[0]);
        }

        Assert.Equal(Team.PlayerB, state.ActiveTeam);
        Assert.Equal(Team.PlayerB, state.NextPlayerTeam);
    }

    /// <summary>
    /// <b>Differing preferences resolve without a coin</b> — both players get what they asked for,
    /// and the rng cursor never moves.
    /// </summary>
    [Theory]
    [InlineData(DeploymentChoice.PlaceFirst, DeploymentChoice.PlaceSecond, Team.PlayerA)]
    [InlineData(DeploymentChoice.PlaceSecond, DeploymentChoice.PlaceFirst, Team.PlayerB)]
    public void StepOne_WhenPreferencesDiffer_NoCoinIsDrawn(
        DeploymentChoice a, DeploymentChoice b, Team expected)
    {
        var start = Game.Start(FightLibrary.Fight1(), seed: 7).NewState;
        var result = start.Step(new DraftOrderCommand(a, b));
        var resolved = result.Single<DraftOrderResolved>();

        Assert.False(resolved.ByCoin);
        Assert.Equal(-1, resolved.Coin);
        Assert.Equal(expected, resolved.PlacesFirst);
        Assert.Equal(start.RngState, result.NewState.RngState);
        Assert.True(result.NewState.DraftOrder!.GotWhatTheyWanted(Team.PlayerA));
        Assert.True(result.NewState.DraftOrder!.GotWhatTheyWanted(Team.PlayerB));
    }

    /// <summary>
    /// <b>Identical preferences fire the seeded coin</b> — the inversion against the map vote, where
    /// it is disagreement that costs a draw. One player is going to be disappointed either way.
    /// </summary>
    [Theory]
    [InlineData(DeploymentChoice.PlaceFirst)]
    [InlineData(DeploymentChoice.PlaceSecond)]
    public void StepOne_WhenPreferencesMatch_TheSeededCoinDecides(DeploymentChoice both)
    {
        var start = Game.Start(FightLibrary.Fight1(), seed: 7).NewState;
        var result = start.Step(new DraftOrderCommand(both, both));
        var resolved = result.Single<DraftOrderResolved>();

        Assert.True(resolved.ByCoin);
        Assert.InRange(resolved.Coin, 0, 1);
        Assert.NotEqual(start.RngState, result.NewState.RngState);

        // Exactly one player got what they asked for, whichever way it fell.
        var order = result.NewState.DraftOrder!;
        Assert.True(order.GotWhatTheyWanted(Team.PlayerA) ^ order.GotWhatTheyWanted(Team.PlayerB));
    }

    /// <summary>The same seed flips the same coin, which is what makes the draft replayable.</summary>
    [Fact]
    public void StepOne_TheCoin_IsSeeded()
    {
        var command = new DraftOrderCommand(DeploymentChoice.PlaceFirst, DeploymentChoice.PlaceFirst);

        var once = Game.Start(FightLibrary.Fight1(), seed: 12345).NewState.Then(command);
        var again = Game.Start(FightLibrary.Fight1(), seed: 12345).NewState.Then(command);
        var other = Game.Start(FightLibrary.Fight1(), seed: 999).NewState.Then(command);

        Assert.Equal(once.DraftOrder, again.DraftOrder);
        Assert.Equal(once.RngState, again.RngState);
        Assert.NotEqual(once.RngState, other.RngState);
    }

    /// <summary>
    /// <b>Every duck lands on a published spot, and no spot belongs to a side.</b> This used to
    /// assert that player A's ducks stood in zone A — the question §3's deployment draft deleted
    /// (locked y). A spot is owned by neither player; either may take any open one.
    /// </summary>
    [Fact]
    public void Deployment_PlacesEveryDuckOnAPublishedSpot_WhicheverSideOwnsTheDuck()
    {
        var fight = FightLibrary.Fight1();
        var state = Game.Start(fight, seed: 7).NewState;

        while (state.Phase == Phase.Deployment)
        {
            state = state.Then(Game.LegalCommands(state)[0]);
        }

        foreach (var unit in state.Units.Where(u => u.Team != Team.Enemy))
        {
            Assert.Contains(unit.Position, fight.Spots);
        }

        // No two ducks on one spot, and the spot list outnumbers the ducks that drafted from it.
        var taken = state.Units.Where(u => u.Team != Team.Enemy).Select(u => u.Position).ToList();
        Assert.Equal(taken.Count, taken.Distinct().Count());
        Assert.True(fight.Spots.Count >= taken.Count);
    }

    /// <summary>
    /// <b>The unequal-roster rule (D-256).</b> §3 fixes A·B·B·A and says the order "generalises to
    /// unequal rosters" without saying how, so this is the ruling: the serpentine
    /// <c>F S S F F S S F …</c> runs on, and <b>a slot whose owner has no ducks left passes to the
    /// other player rather than being dropped</b>.
    /// <para>
    /// Passing rather than dropping is what keeps the compensation intact — the second picker's
    /// back-to-back pair is the payment for picking second, and dropping exhausted slots would
    /// quietly hand it back to the player who already chose to place first.
    /// </para>
    /// </summary>
    [Theory]
    [InlineData(3, 1, "FSFF")]
    [InlineData(1, 3, "FSSS")]
    [InlineData(2, 1, "FSF")]
    [InlineData(1, 2, "FSS")]
    [InlineData(2, 2, "FSSF")]
    [InlineData(4, 1, "FSFFF")]
    public void Deployment_TheSnake_GeneralisesToUnequalRosters(int a, int b, string expected)
    {
        var state = Game.Start(Rosters(a, b), seed: 7).NewState.DraftOrder(Team.PlayerA);

        var actual = new System.Text.StringBuilder();
        while (state.Phase == Phase.Deployment)
        {
            actual.Append(state.ActiveTeam == Team.PlayerA ? 'F' : 'S');
            state = state.Then(Game.LegalCommands(state)[0]);
        }

        Assert.Equal(expected, actual.ToString());
    }

    /// <summary>A board with as many spots as the two rosters need, and nothing else going on.</summary>
    private static FightDefinition Rosters(int a, int b)
    {
        var result = FightParser.Parse(string.Join(
            "\n",
            "id: snake-fixture",
            "name: Snake Fixture",
            "roster a: " + string.Join(", ", Enumerable.Repeat("Vanguard", a)),
            "roster b: " + string.Join(", ", Enumerable.Repeat("Archer", b)),
            "spawn h = Husk",
            "board:",
            "  *******",
            "  .......",
            "  .......",
            "  ...h...",
            "  .......",
            "  .......",
            "  ......."));

        Assert.Empty(result.Errors);
        return result.Fight!;
    }

    private static GameState DeployFirstLegal(GameState state) =>
        state.Then(Game.LegalCommands(state)[0]);
}
