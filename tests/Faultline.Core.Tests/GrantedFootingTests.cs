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
    }

    // Three refusals, not three tiles and not an unspendable wall. The arithmetic is untouched by
    // them — Footing left it (D-143) — and each shove he turns aside costs him one.
    [Fact]
    public void QuarryKing_ThreeTokensAreThreeRefusals()
    {
        var start = Game.Start(FightLibrary.ById("quarry-king"), seed: 1).NewState;
        var king = start.Units.First(u => u.Kind == UnitKind.QuarryKing);

        Assert.Equal(2, Displacement.EffectiveDistance(start, king, DisplacementKind.Push, 2, out _));

        var state = start;
        for (int spent = 1; spent <= 3; spent++)
        {
            var events = new System.Collections.Generic.List<GameEvent>();
            var before = state.Get(king.Id).Position;
            state = Displacement.Resolve(
                state, king.Id, before + new Coord(-1, 0), DisplacementKind.Push, 1,
                refused: true, events: events);

            Assert.Equal(before, state.Get(king.Id).Position);
            Assert.Equal(3 - spent, state.Get(king.Id).Footing);
        }

        // Out of refusals, he shoves like anybody.
        var last = new System.Collections.Generic.List<GameEvent>();
        var start2 = state.Get(king.Id).Position;
        state = Displacement.Resolve(
            state, king.Id, start2 + new Coord(-1, 0), DisplacementKind.Push, 1,
            refused: true, events: last);

        Assert.NotEqual(start2, state.Get(king.Id).Position);
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
