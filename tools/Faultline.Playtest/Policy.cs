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
            MoveCommand => 40,
            RescueCommand => 30,
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
    public override string Intent => "Prefers abilities and displacement over swinging. The player the game is designed for.";

    /// <inheritdoc/>
    public override Command Choose(GameState state, IReadOnlyList<Command> legal, DeterministicRng rng) =>
        Best(legal, c => c switch
        {
            AbilityCommand => 100,
            FinishClingingCommand => 95,
            AttackCommand => 60,
            DeployCommand => 50,
            MoveCommand => 40,
            RescueCommand => 35,
            _ => 0,
        });
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
            MoveCommand => 45,
            AbilityCommand => 40,
            AttackCommand => 35,
            FinishClingingCommand => 30,
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
