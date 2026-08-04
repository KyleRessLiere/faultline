using Faultline.Web.Shell.Playtest;

namespace Faultline.Web.Tests;

/// <summary>
/// The developer panel's state: what it opens on, what it remembers, and — the claim that matters —
/// that a release build cannot be talked into opening it.
/// </summary>
/// <remarks>
/// No bUnit here, by the same rule the rest of this project follows: the component is markup over
/// this class, and every decision worth pinning is a decision this class makes.
/// <see cref="DevBuild.ShowDevTools"/> is process-wide, so every test that writes it puts it back in
/// a <c>finally</c> — CLAUDE.md forbids a test that depends on the order the suite runs in.
/// </remarks>
public sealed class DevPanelTests
{
    [Fact]
    public void TheDefaults_AreCollapsedOnBattlesAndNotExpanded()
    {
        var dev = new DevPanelState();

        Assert.False(dev.Open);
        Assert.Equal(DevTab.Battles, dev.Tab);
        Assert.False(dev.Expanded);
    }

    [Fact]
    public void Show_OpensThePanelOnTheTabItWasGiven()
    {
        var dev = new DevPanelState();

        dev.Show(DevTab.Replay);

        Assert.True(dev.Open);
        Assert.Equal(DevTab.Replay, dev.Tab);
    }

    [Fact]
    public void Toggle_OpensThenClosesAgain()
    {
        var dev = new DevPanelState();

        dev.Toggle();
        Assert.True(dev.Open);

        dev.Toggle();
        Assert.False(dev.Open);
    }

    [Fact]
    public void EveryMutation_RaisesChangedSoTheScreenRedraws()
    {
        var dev = new DevPanelState();
        int changes = 0;
        dev.Changed += () => changes++;

        dev.Toggle();
        dev.Show(DevTab.State);
        dev.ToggleExpanded();
        dev.Collapse();
        dev.Close();

        Assert.Equal(5, changes);
    }

    [Fact]
    public void Collapse_PutsAnExpandedDrawerBackInItsDockWithoutClosingIt()
    {
        var dev = new DevPanelState();
        dev.Show(DevTab.Ai);
        dev.ToggleExpanded();

        Assert.True(dev.Expanded);

        dev.Collapse();

        Assert.False(dev.Expanded);
        Assert.True(dev.Open);
    }

    [Fact]
    public void Expansion_IsRememberedPerTab()
    {
        // Blowing the panel up is a property of the question being asked, not of the panel: a
        // command log wants the screen and an overlay switch does not.
        var dev = new DevPanelState();
        dev.Show(DevTab.Replay);
        dev.ToggleExpanded();

        dev.Show(DevTab.Overlays);
        Assert.False(dev.Expanded);

        dev.Show(DevTab.Replay);
        Assert.True(dev.Expanded);
    }

    [Fact]
    public void Toggle_OnAReleaseBuild_LeavesThePanelClosed()
    {
        bool was = DevBuild.ShowDevTools;
        try
        {
            DevBuild.ShowDevTools = false;
            var dev = new DevPanelState();

            Assert.False(DevPanelState.Available);

            dev.Toggle();
            dev.Show(DevTab.State);
            dev.ToggleExpanded();

            Assert.False(dev.Open);
            Assert.False(dev.Expanded);
            Assert.Equal(DevTab.Battles, dev.Tab);
        }
        finally
        {
            DevBuild.ShowDevTools = was;
        }
    }

    [Fact]
    public void Apply_OnAReleaseBuild_CannotRestoreAnOpenPanel()
    {
        // The stored preference is the one path a release build could be talked into showing the
        // panel: a key written by an internal build, read back by a shipped one.
        var internalBuild = new DevPanelState();
        internalBuild.Show(DevTab.Replay);
        internalBuild.ToggleExpanded();
        string stored = internalBuild.Encode();

        bool was = DevBuild.ShowDevTools;
        try
        {
            DevBuild.ShowDevTools = false;
            var dev = new DevPanelState();

            dev.Apply(stored);

            Assert.False(dev.Open);
            Assert.False(dev.Expanded);
        }
        finally
        {
            DevBuild.ShowDevTools = was;
        }
    }

    [Fact]
    public void EncodeAndApply_RoundTripTheTabAndItsExpansion()
    {
        var written = new DevPanelState();
        written.Show(DevTab.State);
        written.ToggleExpanded();
        written.Show(DevTab.Overlays);

        var read = new DevPanelState();
        read.Apply(written.Encode());

        Assert.True(read.Open);
        Assert.Equal(DevTab.Overlays, read.Tab);
        Assert.False(read.Expanded);

        read.Show(DevTab.State);
        Assert.True(read.Expanded);
    }

    [Fact]
    public void Apply_LeavesTheDefaultsAloneWhenTheStoredLineIsRubbish()
    {
        var dev = new DevPanelState();

        dev.Apply("tab=NoSuchTab;open;exp=;;garbage");

        Assert.False(dev.Open);
        Assert.Equal(DevTab.Battles, dev.Tab);
        Assert.False(dev.Expanded);
    }
}
