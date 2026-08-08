using System.Linq;
using Faultline.Core;
using Faultline.Web.Shell;

namespace Faultline.Web.Tests;

/// <summary>Drives §3's choice phase from a shell test.</summary>
public static class SessionDraft
{
    /// <summary>
    /// Answers step 1 so that placements become legal, handing the first pick to
    /// <paramref name="placesFirst"/> without spending a coin.
    /// </summary>
    /// <remarks>
    /// <b>Nothing may be placed before step 1 is answered</b> (MASTER_DESIGN §3), so a fixture that
    /// jumps straight to the deploy loop finds no <see cref="DeployCommand"/> at all and leaves the
    /// session sitting in deployment. The two answers are opposites, so the preferences differ and
    /// no coin is drawn — a screen test should not have its board decided by a flip it never asked
    /// for. Tests about the coin submit their own <see cref="DraftOrderCommand"/>.
    /// </remarks>
    /// <param name="session">A session whose fight is in deployment.</param>
    /// <param name="placesFirst">Who places first, and so also activates first.</param>
    public static void SettleDraftOrder(this GameSession session, Team placesFirst = Team.PlayerA)
    {
        if (session.State.DraftOrder is not null)
        {
            return;
        }

        session.Submit(placesFirst == Team.PlayerA
            ? new DraftOrderCommand(DeploymentChoice.PlaceFirst, DeploymentChoice.PlaceSecond)
            : new DraftOrderCommand(DeploymentChoice.PlaceSecond, DeploymentChoice.PlaceFirst));
    }

    /// <summary>Settles step 1 and then places every duck on the first spot Core offers it.</summary>
    /// <param name="session">A session whose fight is in deployment.</param>
    public static void DeployEveryone(this GameSession session)
    {
        session.SettleDraftOrder();

        while (session.Legal.OfType<DeployCommand>().FirstOrDefault() is { } deploy)
        {
            session.Submit(deploy);
        }
    }
}
