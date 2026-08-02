using Faultline.Core;

namespace Faultline.Playtest;

/// <summary>
/// The registry of player policies, so a name on the command line resolves to one.
/// </summary>
public static class Policies
{
    /// <summary>Every policy, in report order.</summary>
    /// <returns>The policies.</returns>
    public static Policy[] All() => new Policy[]
    {
        new FirstLegalPolicy(),
        new BrawlerPolicy(),
        new ShoverPolicy(),
        new CarefulPolicy(),
        new BoardFirstPolicy(),
        new BladeFirstPolicy(),
        new PreserverPolicy(),
        new RandomPolicy("a"),
        new RandomPolicy("b"),
        new RandomPolicy("c"),
        new RandomPolicy("d"),
        new RandomPolicy("e"),
        new RandomPolicy("f"),
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
