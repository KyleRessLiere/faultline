using Faultline.Core;

namespace Faultline.Playtest;

/// <summary>
/// How a simulated player decides. This is the only thing that varies between runs.
/// </summary>
/// <remarks>
/// <para>
/// The point of the whole harness. A run is a seed plus a command log, and the same seed played the
/// same way replays byte-identically — so ten runs at one seed with one policy are ten copies of one
/// run and tell you nothing. Variation has to come from the decisions, not the dice.
/// </para>
/// <para>
/// A policy chooses from <see cref="Game.LegalCommands"/> and nothing else, so it can never make an
/// illegal move or need to know a rule. It is a taste, not an AI.
/// </para>
/// </remarks>
public abstract class Policy
{
    /// <summary>Name used in reports.</summary>
    public abstract string Name { get; }

    /// <summary>One line on what this player is trying to do.</summary>
    public abstract string Intent { get; }

    /// <summary>Picks one of the legal commands.</summary>
    /// <param name="state">Board as it stands.</param>
    /// <param name="legal">Everything Core says is legal.</param>
    /// <param name="rng">Deterministic source, seeded from the run.</param>
    /// <returns>The chosen command.</returns>
    public abstract Command Choose(GameState state, IReadOnlyList<Command> legal, DeterministicRng rng);

    /// <summary>Scores every legal command and takes the best, ties broken by order.</summary>
    /// <param name="legal">Legal commands.</param>
    /// <param name="score">Scoring function; higher wins.</param>
    /// <returns>The best command.</returns>
    protected static Command Best(IReadOnlyList<Command> legal, Func<Command, int> score)
    {
        var best = legal[0];
        int bestScore = score(best);

        for (int i = 1; i < legal.Count; i++)
        {
            int s = score(legal[i]);
            if (s > bestScore)
            {
                best = legal[i];
                bestScore = s;
            }
        }

        return best;
    }

    /// <summary>
    /// What a move is worth to a policy that scores by command type, once the AP turn is priced in.
    /// </summary>
    /// <remarks>
    /// The taste policies rank a <see cref="MoveCommand"/> above nothing and below acting, which was
    /// a complete opinion while the halves were separate. Under an AP pool the interesting question
    /// is not whether to move but how far, and a move that walks its purse out in front of somebody
    /// it can no longer hit is worth barely more than standing still.
    /// </remarks>
    /// <param name="state">Board as it stands.</param>
    /// <param name="command">Move being priced.</param>
    /// <param name="worth">What this policy thinks moving is worth in the ordinary case.</param>
    /// <returns>The move's score.</returns>
    protected static int Walk(GameState state, MoveCommand command, int worth) =>
        Budget.Waste(state, command) ? 5 : worth;
}

/// <summary>Takes the first legal command every time. The control group.</summary>
public sealed class FirstLegalPolicy : Policy
{
    /// <inheritdoc/>
    public override string Name => "first-legal";

    /// <inheritdoc/>
    public override string Intent => "Takes whatever Core offers first. Not trying to win — the baseline everything else is measured against.";

    /// <inheritdoc/>
    public override Command Choose(GameState state, IReadOnlyList<Command> legal, DeterministicRng rng) => legal[0];
}

/// <summary>Swings at things. The player who has not noticed the board is a weapon.</summary>
public sealed class BrawlerPolicy : Policy
{
    /// <inheritdoc/>
    public override string Name => "brawler";

    /// <inheritdoc/>
    public override string Intent => "Attacks whenever it can, moves otherwise, and never uses an ability. The damage-race player the design is arguing with.";

    /// <inheritdoc/>
    public override Command Choose(GameState state, IReadOnlyList<Command> legal, DeterministicRng rng) =>
        Best(legal, c => c switch
        {
            AttackCommand => 100,
            FinishClingingCommand => 90,
            DeployCommand => 50,
            MoveCommand m => Walk(state, m, 40),
            RescueCommand => 30,

            // Below a swing at a body and above nothing. The brawler has not noticed the board, and
            // a wall is the board — but it does swing at what is in front of it (D-281).
            AttackStructureCommand => 20,
            AbilityCommand => 10,
            _ => 0,
        });
}

/// <summary>Shoves first and asks later. The player who has read the brief.</summary>
public sealed class ShoverPolicy : Policy
{
    /// <inheritdoc/>
    public override string Name => "shover";

    /// <inheritdoc/>
    public override string Intent => "Prefers abilities and displacement over swinging, and spends Verve the moment it can. The player the game is designed for.";

    /// <inheritdoc/>
    public override Command Choose(GameState state, IReadOnlyList<Command> legal, DeterministicRng rng) =>
        Best(legal, c => c switch
        {
            // Cast is the one spend with a choice worth scoring: the same grab lands somebody in a
            // drain or on open floor depending only on which tile is picked, and a policy that took
            // the first tile offered would measure the enumeration order rather than the ability.
            SpendVerveCommand { Spend: VerveSpend.Cast, To: { } to } => 120 + Landing.Worth(state, to),

            // Everything else spends the instant it can afford to, which is naive on purpose. A
            // policy that held Verve for the right moment would be measuring the policy's judgement;
            // this one measures how much the game hands out and how much of it a player can use.
            SpendVerveCommand => 110,
            AbilityCommand => 100,
            FinishClingingCommand => 95,
            AttackCommand => 60,
            DeployCommand => 50,
            MoveCommand m => Walk(state, m, 40),
            RescueCommand => 35,

            // The shover's whole thesis is that the board breaks masonry better than a swing does,
            // so the swing is its last resort rather than its answer (D-281).
            AttackStructureCommand => 20,
            _ => 0,
        });
}

/// <summary>
/// Whose wall a piece of masonry is: <c>+1</c> for one the players are meant to bring down,
/// <c>-1</c> for the one they are meant to keep standing.
/// </summary>
/// <remarks>
/// <para>
/// One copy, because every policy that prices structure damage had the same hole and none of the
/// team forks elsewhere in the harness could catch it: masonry has no team, so a term that read only
/// the amount was unconditionally positive and paid a Protect board's own players to demolish the
/// thing they were defending. A four-face cut of <c>lk-09-the-pumphouse</c> was demolished by its own
/// side before round 5, every run.
/// </para>
/// <para>
/// <b>Read off the structure that was hit, never off the board's objective.</b> A blocker is scenery
/// on any board and stays positive whatever the objective is (D-114) — <c>broken-bridge</c>'s masonry
/// <i>is</i> the crossing, and a policy that would not break it could not cross.
/// </para>
/// </remarks>
internal static class Masonry
{
    internal static int Sign(GameState state, Coord at) =>
        state.StructureAt(at) is { IsBlocker: false, Role: ObjectiveKind.Protect } ? -1 : 1;
}

/// <summary>
/// How much a shove-scoring policy likes putting somebody on a given tile: a drain takes the unit
/// out of the run, spikes take three, open floor takes nothing.
/// </summary>
internal static class Landing
{
    internal static int Worth(GameState state, Coord tile) => state.Board.At(tile) switch
    {
        TileType.Pit => 30,
        TileType.Spikes => 15,
        _ => 0,
    };
}

/// <summary>Rescues its own, and would rather reposition than trade hits.</summary>
public sealed class CarefulPolicy : Policy
{
    /// <inheritdoc/>
    public override string Name => "careful";

    /// <inheritdoc/>
    public override string Intent => "Pulls people out of pits before anything else and prefers moving to swinging. Tests whether caution is ever rewarded.";

    /// <inheritdoc/>
    public override Command Choose(GameState state, IReadOnlyList<Command> legal, DeterministicRng rng) =>
        Best(legal, c => c switch
        {
            RescueCommand => 120,
            DeployCommand => 50,
            MoveCommand m => Walk(state, m, 45),
            AbilityCommand => 40,
            AttackCommand => 35,
            FinishClingingCommand => 30,

            // Nothing swings back at a duck that hits a wall, which is exactly this policy's taste
            // — but it would still rather reposition (D-281).
            AttackStructureCommand => 32,
            _ => 0,
        });
}

/// <summary>Picks at random from what is legal, from a seeded source.</summary>
public sealed class RandomPolicy : Policy
{
    /// <summary>Creates a random policy with a label.</summary>
    /// <param name="tag">Distinguishes one random walk from another in reports.</param>
    public RandomPolicy(string tag) => Name = "random-" + tag;

    /// <inheritdoc/>
    public override string Name { get; }

    /// <inheritdoc/>
    public override string Intent => "Uniform over legal commands, from a seeded source. Explores states a policy with taste never reaches.";

    /// <inheritdoc/>
    public override Command Choose(GameState state, IReadOnlyList<Command> legal, DeterministicRng rng) =>
        legal[rng.Next(legal.Count)];
}
