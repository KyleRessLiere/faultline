using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// Where a unit's Footing tokens come from. A fight's `footing:` grant hands tokens to an archetype
/// that has none of its own; it is not a reassignment, and it never takes away the tokens a stat
/// block already carries (D-101).
/// </summary>
public class GrantedFootingTests
{
    // Found by playing: "quarry king footing is not being respected". It was not — he was walking
    // into every fight on zero. The grant was assigned unconditionally, so a fight that granted
    // nothing wrote a zero over the three tokens his stat block carries, and the boss whose whole
    // identity is that you have to break him first arrived already broken.
    [Fact]
    public void QuarryKing_WalksOntoTheBoardWithHisThreeTokens()
    {
        var start = Game.Start(FightLibrary.ById("quarry-king"), seed: 1).NewState;
        var king = start.Units.First(u => u.Kind == UnitKind.QuarryKing);

        Assert.Equal(3, UnitTemplate.For(UnitKind.QuarryKing).Footing);
        Assert.Equal(3, king.Footing);
        Assert.True(king.Template.FootingNegates);
    }

    [Fact]
    public void QuarryKing_WithHisTokensIntact_IgnoresEveryShove()
    {
        var start = Game.Start(FightLibrary.ById("quarry-king"), seed: 1).NewState;
        var king = start.Units.First(u => u.Kind == UnitKind.QuarryKing);

        foreach (int distance in new[] { 1, 2, 3 })
        {
            Assert.Equal(
                0, Displacement.EffectiveDistance(start, king, DisplacementKind.Push, distance, false, out _));
        }
    }

    // The grant still does its own job for everybody else.
    [Fact]
    public void AFightGrant_StillHandsTokensToArchetypesWithNone()
    {
        var granted = Game.Start(FightLibrary.ById("hz-01-dig-in"), seed: 1).NewState;
        var enemies = granted.Units.Where(u => u.Team == Team.Enemy).ToList();

        Assert.NotEmpty(enemies);
        Assert.All(enemies, u => Assert.Equal(1, u.Footing));
    }

    // A fight that grants nothing leaves every archetype on whatever it brought.
    [Fact]
    public void AFightWithNoGrant_LeavesEveryStatBlockAlone()
    {
        var start = Game.Start(FightLibrary.Fight1(), seed: 1).NewState;

        Assert.All(start.Units, u => Assert.Equal(u.Template.Footing, u.Footing));
    }
}
