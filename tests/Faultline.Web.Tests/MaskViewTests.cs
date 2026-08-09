using Faultline.Web.Shell;

namespace Faultline.Web.Tests;

/// <summary>
/// The board mask: a rectangle of interest for reading one section of a board while it is being
/// authored. <b>A view and only a view</b> — it changes what is drawn and never what is legal.
/// </summary>
public class MaskViewTests
{
    /// <summary>With the mask off, every tile is inside it.</summary>
    [Fact]
    public void WithNoMask_EveryTileIsInside()
    {
        var view = new PlaytestView();

        Assert.False(view.Masked);
        Assert.True(view.InMask(0, 0));
        Assert.True(view.InMask(99, 99));
        Assert.Equal("whole board", view.MaskLabel);
    }

    /// <summary>The rectangle is half-open: it includes its origin and excludes the tile past its edge.</summary>
    [Theory]
    [InlineData(2, 3, true)]
    [InlineData(4, 5, true)]
    [InlineData(1, 3, false)]
    [InlineData(5, 3, false)]
    [InlineData(2, 2, false)]
    [InlineData(2, 6, false)]
    public void TheRegion_IncludesItsOriginAndExcludesThePastItsEdge(int x, int y, bool inside)
    {
        var view = new PlaytestView();
        view.SetMask(2, 3, 3, 3);

        Assert.Equal(inside, view.InMask(x, y));
    }

    /// <summary>Setting a region turns the mask on — asking for one is asking to see it.</summary>
    [Fact]
    public void SettingARegion_TurnsTheMaskOn()
    {
        var view = new PlaytestView();
        view.SetMask(1, 1, 2, 2);

        Assert.True(view.Masked);
        Assert.Equal("2×2 from 1,1", view.MaskLabel);
    }

    /// <summary>A region is never smaller than one tile, and never starts off the board's edge.</summary>
    [Fact]
    public void ARegion_IsClampedToSomethingYouCanActuallySee()
    {
        var view = new PlaytestView();
        view.SetMask(-4, -1, 0, -3);

        Assert.Equal(0, view.MaskX);
        Assert.Equal(0, view.MaskY);
        Assert.Equal(1, view.MaskWidth);
        Assert.Equal(1, view.MaskHeight);
    }

    /// <summary>Clearing keeps the rectangle, so the toggle brings back the region you last chose.</summary>
    [Fact]
    public void Clearing_KeepsTheRectangleForNextTime()
    {
        var view = new PlaytestView();
        view.SetMask(3, 4, 2, 2);
        view.ClearMask();

        Assert.False(view.Masked);
        Assert.True(view.InMask(0, 0));

        view.ToggleMask();

        Assert.True(view.Masked);
        Assert.Equal("2×2 from 3,4", view.MaskLabel);
    }

    /// <summary>The mask survives a reload, like every other way of looking at the board.</summary>
    [Fact]
    public void TheMask_RoundTripsThroughStorage()
    {
        var view = new PlaytestView();
        view.SetMask(5, 2, 4, 3);

        var back = new PlaytestView();
        back.Apply(view.Encode());

        Assert.True(back.Masked);
        Assert.Equal(5, back.MaskX);
        Assert.Equal(2, back.MaskY);
        Assert.Equal(4, back.MaskWidth);
        Assert.Equal(3, back.MaskHeight);
    }

    /// <summary>A malformed rectangle leaves the last good one standing rather than collapsing it.</summary>
    [Fact]
    public void AMalformedRectangle_LeavesTheLastOneStanding()
    {
        var view = new PlaytestView();
        view.SetMask(2, 2, 3, 3);
        view.Apply("maskrect=nonsense");

        Assert.Equal(2, view.MaskX);
        Assert.Equal(3, view.MaskWidth);
    }
}
