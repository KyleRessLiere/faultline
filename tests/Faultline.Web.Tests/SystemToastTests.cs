using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Faultline.Core;
using Faultline.Web.Shell;
using Faultline.Web.Shell.Playtest;

namespace Faultline.Web.Tests;

/// <summary>
/// The rule with no exceptions: nothing occupies a layout row between the turn-order strip and the
/// board. Every sentence that used to have a band of its own is a toast now, and these are the
/// decisions that used to live in <c>@if</c> blocks in the markup.
/// </summary>
/// <remarks>
/// No bUnit, by the same rule the rest of this project's shell tests follow: nothing renders a
/// component here, so what a toast says and when it says it lives in <see cref="SystemToasts"/> and
/// <see cref="BattleMessages"/> where a test can reach it. What is NOT asserted here is the pixel
/// claim — that the stack is out of flow and the board's region is unchanged — because that is a
/// claim about a real layout engine and it belongs in <c>tools/ui-checks/board-fill-acceptance.mjs</c>.
/// </remarks>
public sealed class SystemToastTests
{
    private static readonly SystemMessage Reload =
        new(BattleMessages.ReloadKey, "Reloaded mid-run.", SystemTone.Info);

    private static readonly SystemMessage Refusal =
        new(BattleMessages.ProblemKey, "Core refused that: nope.", SystemTone.Warn);

    // ---- the queue -----------------------------------------------------------------------------

    [Fact]
    public void AConditionThatIsTrue_PutsExactlyOneToastUp()
    {
        var toasts = new SystemToasts();

        var result = toasts.Sync(new[] { Reload });

        Assert.True(result.Changed);
        Assert.Equal(new[] { BattleMessages.ReloadKey }, result.Added);
        Assert.Single(toasts.Live);
    }

    [Fact]
    public void AConditionThatStaysTrue_DoesNotStackUpACopyPerRender()
    {
        // The host syncs after every paint, and the battle screen repaints on every click. Identity
        // is the key rather than the wording precisely so that cannot become a column of the same
        // sentence forty deep.
        var toasts = new SystemToasts();
        toasts.Sync(new[] { Reload });

        for (int i = 0; i < 40; i++)
        {
            Assert.False(toasts.Sync(new[] { Reload }).Changed);
        }

        Assert.Single(toasts.Live);
    }

    [Fact]
    public void DismissingAToast_TakesItDownAndKeepsItDownWhileTheConditionHolds()
    {
        var toasts = new SystemToasts();
        toasts.Sync(new[] { Reload });

        Assert.True(toasts.Dismiss(BattleMessages.ReloadKey));
        Assert.Empty(toasts.Live);

        // Still reloaded, still on that node — and a notice that came straight back would make the
        // ✕ a button that does nothing.
        toasts.Sync(new[] { Reload });
        Assert.Empty(toasts.Live);
    }

    [Fact]
    public void TheEightSecondClockAndTheCloseButton_AreTheSameDoor()
    {
        // Expiry calls Dismiss. One way for a toast to leave means one thing to get right, and it
        // means a clock that fires on a message somebody already closed does nothing.
        var toasts = new SystemToasts();
        toasts.Sync(new[] { Reload });
        toasts.Dismiss(BattleMessages.ReloadKey);

        Assert.False(toasts.Dismiss(BattleMessages.ReloadKey));
        Assert.Empty(toasts.Live);
    }

    [Fact]
    public void OnceTheConditionClears_TheDismissalIsForgottenWithIt()
    {
        // A dismissal belongs to the condition, not to the session. The next time the run is
        // reloaded mid-fight the player has to be told again.
        var toasts = new SystemToasts();
        toasts.Sync(new[] { Reload });
        toasts.Dismiss(BattleMessages.ReloadKey);

        toasts.Sync(System.Array.Empty<SystemMessage>());
        toasts.Sync(new[] { Reload });

        Assert.Single(toasts.Live);
    }

    [Fact]
    public void AConditionThatGoesAway_TakesItsToastWithIt()
    {
        var toasts = new SystemToasts();
        toasts.Sync(new[] { Reload, Refusal });

        Assert.Equal(2, toasts.Live.Count);

        var result = toasts.Sync(new[] { Reload });

        Assert.True(result.Changed);
        Assert.Single(toasts.Live);
        Assert.Equal(BattleMessages.ReloadKey, toasts.Live[0].Key);
    }

    [Fact]
    public void EightSecondsIsTheLifetime_AndItIsStatedOnceRatherThanTypedIntoAComponent()
    {
        Assert.Equal(8000, SystemToasts.LifetimeMs);
    }

    // ---- what the battle screen actually posts --------------------------------------------------

    [Fact]
    public void AQuietOneOffBattle_PostsNothingAtAll()
    {
        // The common case, and the one the board-fill measurement is taken in: no toast, no overlay,
        // no row, the whole region is the board's.
        var (session, runs) = Fresh();
        session.StartFight(FightLibrary.ById("hz-10-bone-yard"), GameSession.DefaultSeed);

        Assert.Empty(BattleMessages.Current(session, runs));
    }

    [Fact]
    public async Task ARunReloadedMidFight_PostsTheReloadNoticeAsAToastAndNotAsABand()
    {
        var (session, runs) = await Reloaded();

        var messages = BattleMessages.Current(session, runs);

        var notice = Assert.Single(messages, m => m.Key == BattleMessages.ReloadKey);
        Assert.Equal(SystemTone.Info, notice.Tone);

        // It says what D-050 actually does, in full: a toast cannot be re-opened, so it may not be
        // an abbreviation of a sentence living somewhere else.
        Assert.Contains("restarts from deployment", notice.Text);
        Assert.Contains("seed, node", notice.Text);
    }

    [Fact]
    public async Task ARefusal_IsTheLoudToastAndComesFirst()
    {
        var (session, runs) = await Reloaded();

        // Two conditions true at once: the refusal is the only one of them that means something
        // went wrong, so it is on top.
        runs.Vote("nowhere", "nowhere");

        var messages = BattleMessages.Current(session, runs);

        Assert.NotNull(runs.Problem);
        Assert.Equal(BattleMessages.ProblemKey, messages[0].Key);
        Assert.Equal(SystemTone.Warn, messages[0].Tone);
    }

    [Fact]
    public void EveryBattleMessageHasADistinctKey()
    {
        // The dedupe is by key. Two conditions sharing one would silently suppress each other, and
        // the one that lost would be a sentence nobody ever sees.
        var keys = new HashSet<string>
        {
            BattleMessages.ReloadKey, BattleMessages.ProblemKey, BattleMessages.FrozenKey,
        };

        Assert.Equal(3, keys.Count);
    }

    // ---- fixtures ------------------------------------------------------------------------------

    private static (GameSession Session, RunSession Runs) Fresh()
    {
        var session = new GameSession();
        var runs = new RunSession(new RunStore(new FightFiles(new FakeJsRuntime())), session);
        return (session, runs);
    }

    /// <summary>A run that was inside a fight, written to storage, and read back the way a reload does.</summary>
    private static async Task<(GameSession Session, RunSession Runs)> Reloaded()
    {
        var storage = new FakeJsRuntime();

        var first = new RunSession(new RunStore(new FightFiles(storage)), new GameSession());
        await first.StartAsync(77);
        first.Enter();

        var session = new GameSession();
        var runs = new RunSession(new RunStore(new FightFiles(storage)), session);
        await runs.LoadAsync();
        runs.ResumeBoard();

        Assert.True(runs.RestartedByReload);
        return (session, runs);
    }
}
