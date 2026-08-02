using Faultline.Core;
using Faultline.Web.Shell.Playtest;

namespace Faultline.Web.Tests;

/// <summary>
/// The two sentences the turn summary builds: who can act, and what the acting unit has left.
/// </summary>
/// <remarks>
/// Lifted out of the razor component so they can be tested without rendering. The wording is the
/// point — a summary that says "Player A to act" and nothing else is what these replaced.
/// </remarks>
public class TurnSummaryTextTests
{
    [Fact]
    public void Names_OneUnit_IsJustThatName()
    {
        Assert.Equal("Vanguard", PlaytestText.Names(new[] { "Vanguard" }));
    }

    [Fact]
    public void Names_TwoUnits_ReadsAsAPersonWouldSayIt()
    {
        Assert.Equal("Vanguard or Archer", PlaytestText.Names(new[] { "Vanguard", "Archer" }));
    }

    [Fact]
    public void Names_ThreeOrMore_CommasThenOr()
    {
        Assert.Equal(
            "Vanguard, Archer or Wardbearer",
            PlaytestText.Names(new[] { "Vanguard", "Archer", "Wardbearer" }));

        Assert.Equal(
            "A, B, C or D",
            PlaytestText.Names(new[] { "A", "B", "C", "D" }));
    }

    [Fact]
    public void Names_Nobody_IsEmptyRatherThanAStrayOr()
    {
        // The summary only asks for this when somebody can act, but a dangling " or " would be an
        // ugly way to find that out.
        Assert.Equal(string.Empty, PlaytestText.Names(System.Array.Empty<string>()));
        Assert.Equal(string.Empty, PlaytestText.Names(null!));
    }

    [Theory]
    [InlineData(false, false, "Move and action both unspent.")]
    [InlineData(true, false, "Move spent — action still to use.")]
    [InlineData(false, true, "Action spent — move still to use.")]
    [InlineData(true, true, "Move and action both spent.")]
    public void Halves_SaysWhichHalfIsLeft(bool moved, bool acted, string expected)
    {
        // An activation is one move plus one action in either order, so all four states are real and
        // each needs its own sentence.
        var unit = Unit.FromTemplate(new UnitId(0), UnitKind.Vanguard, Team.PlayerA)
            with { HasMoved = moved, HasActed = acted };

        Assert.Equal(expected, PlaytestText.Halves(unit));
    }

    [Fact]
    public void Halves_RefusesANullUnitRatherThanPrintingNonsense()
    {
        Assert.Throws<System.ArgumentNullException>(() => PlaytestText.Halves(null!));
    }
}
