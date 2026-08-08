using Faultline.Core;

namespace Faultline.Playtest;

/// <summary>
/// The registry of player policies, so a name on the command line resolves to one.
/// </summary>
public static class Policies
{
    /// <summary>Every policy, in report order. Runnable by name; not what runs by default.</summary>
    /// <returns>The policies.</returns>
    public static Policy[] All() => new Policy[]
    {
        new FirstLegalPolicy(),
        new BrawlerPolicy(),
        new ShoverPolicy(),
        new CarefulPolicy(),
        new BoardFirstPolicy(),
        new BladeFirstPolicy(),
        new ObjectiveFirstPolicy(),
        new PreserverPolicy(),
        new RelayPolicy(),
        new RandomPolicy("a"),
        new RandomPolicy("b"),
        new RandomPolicy("c"),
        new RandomPolicy("d"),
        new RandomPolicy("e"),
        new RandomPolicy("f"),
    };

    /// <summary>
    /// The standing default policy set: <c>shover</c> — the only policy that trades bodies, so the
    /// only one that exercises the death economy — plus one board-first evaluator and one
    /// blade-first control. Everything else in <see cref="All"/> still runs, but only by name or
    /// before a milestone (`CLAUDE.md`, Session budget).
    /// </summary>
    /// <returns>The default three policies.</returns>
    public static Policy[] Default() => new Policy[]
    {
        new ShoverPolicy(),
        new BoardFirstPolicy(),
        new BladeFirstPolicy(),
    };

    /// <summary>Finds a policy by name.</summary>
    /// <param name="name">Policy name as it appears in reports.</param>
    /// <returns>The policy.</returns>
    /// <exception cref="ArgumentException">No policy has that name.</exception>
    public static Policy ByName(string name)
    {
        foreach (var policy in All())
        {
            if (string.Equals(policy.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return policy;
            }
        }

        throw new ArgumentException(
            "No policy called '" + name + "'. Known: "
            + string.Join(", ", All().Select(p => p.Name)) + ".",
            nameof(name));
    }
}
