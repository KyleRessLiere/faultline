using System.Linq;
using Faultline.Core;
using Faultline.Web.Shell;

namespace Faultline.Web.Tests;

/// <summary>
/// The Draft UI's laws, from MASTER_DESIGN §3's Draft UI paragraph and the §7.5 laws it defers to.
/// </summary>
public class DraftUiTests
{
    private const string Board = "first-contact";

    private static GameSession Deploying(bool settle = true)
    {
        var session = new GameSession();
        session.StartFight(FightLibrary.ById(Board), GameSession.DefaultSeed);

        if (settle)
        {
            session.SettleDraftOrder(Team.PlayerA);
        }

        return session;
    }

    /// <summary>
    /// <b>Every spot is on the board from before the first pick.</b> §3: no fog — a published spot
    /// list is a published fact. The old highlight only showed the tiles one pending click could
    /// take, which left the board blank until somebody was mid-placement.
    /// </summary>
    [Fact]
    public void EverySpot_IsPublishedBeforeTheFirstPick()
    {
        var session = Deploying(settle: false);
        var fight = FightLibrary.ById(Board);

        Assert.NotEmpty(session.Spots);
        Assert.Equal(fight.Spots, session.Spots);

        // Before step 1 nothing is takeable, but every spot is still on the board.
        Assert.Empty(session.DeployTargets);
        Assert.All(session.Spots, s => Assert.NotEqual(SpotState.NotASpot, session.SpotAt(s)));
    }

    /// <summary>
    /// <b>Open, yours-to-take and taken are three distinct states</b>, and ordinary ground is a
    /// fourth answer rather than a silent one.
    /// </summary>
    [Fact]
    public void ASpot_ReadsAsOneOfThreeStates_AndOrdinaryGroundIsNotASpot()
    {
        var session = Deploying();

        var yours = session.DeployTargets.Keys.First();
        Assert.Equal(SpotState.Yours, session.SpotAt(yours));

        // A tile no rule publishes is not a spot, which is how the board tells them apart.
        var plain = new Coord(3, 3);
        Assert.DoesNotContain(plain, session.Spots);
        Assert.Equal(SpotState.NotASpot, session.SpotAt(plain));

        session.Submit(session.DeployTargets[yours]);
        Assert.Equal(SpotState.Taken, session.SpotAt(yours));
    }

    /// <summary>
    /// <b>A taken spot names who took it and which duck stands there</b> — §3, and the failure mode
    /// this codebase has hit before is greying it out with an empty reason.
    /// </summary>
    [Fact]
    public void ATakenSpot_NamesThePlayerAndTheDuck()
    {
        var session = Deploying();

        var at = session.DeployTargets.Keys.First();
        var deploy = session.DeployTargets[at];
        var duck = session.State.UnitById(deploy.UnitId);

        session.Submit(deploy);

        var said = session.SpotTakenBy(at);

        Assert.NotNull(said);
        Assert.Contains(UnitTemplate.For(duck.Kind).Name, said);
        Assert.NotEqual(string.Empty, said);

        // An untaken spot says nothing rather than saying something empty.
        var open = session.Spots.First(s => session.SpotAt(s) != SpotState.Taken);
        Assert.Null(session.SpotTakenBy(open));
    }

    /// <summary>
    /// The hover surfaces the reachability the agency lint already computes, rather than recomputing
    /// it: on this board nothing reaches any spot on round 1, which is the board's whole thesis.
    /// </summary>
    [Fact]
    public void HoveringASpot_SurfacesWhichEnemiesReachItOnRoundOne()
    {
        var session = Deploying();

        // first-contact is the board where nothing can hurt you before you have had a turn.
        Assert.All(session.Spots, s => Assert.Empty(session.EnemiesReaching(s)));

        // And the query does answer when something does reach: the Teeth has two hot spots.
        var teeth = new GameSession();
        teeth.StartFight(FightLibrary.ById("the-teeth"), GameSession.DefaultSeed);
        teeth.SettleDraftOrder();

        Assert.Contains(teeth.Spots, s => teeth.EnemiesReaching(s).Count > 0);
    }

    /// <summary>
    /// <b>Spots belong to nobody</b>, so a tile written as Player B's corner before the draft is a
    /// legal pick for Player A now.
    /// </summary>
    [Fact]
    public void EitherPlayer_MayTakeAnyOpenSpot()
    {
        var session = Deploying(settle: true);

        // Player A places first here; (6,0) was a Player B slot in the pre-draft board.
        Assert.Equal(Team.PlayerA, session.State.ActiveTeam);
        Assert.Contains(new Coord(6, 0), session.DeployTargets.Keys);
    }

    /// <summary>
    /// Nothing is placeable until step 1 is answered, and the only thing on offer is the answer.
    /// </summary>
    [Fact]
    public void BeforeStepOne_TheOnlyThingOnOfferIsTheBlindPick()
    {
        var session = Deploying(settle: false);

        Assert.NotEmpty(session.Legal);
        Assert.All(session.Legal, c => Assert.IsType<DraftOrderCommand>(c));
        Assert.Null(session.PendingDeployUnit);
    }
}
