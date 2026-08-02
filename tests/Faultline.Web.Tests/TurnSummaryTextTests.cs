using Faultline.Core;
using Faultline.Web.Shell;
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

/// <summary>
/// The telegraph must describe every plan Core can declare. An enemy arrow that says one thing and
/// does another is worse than no arrow (D-061), and the failure mode here is a silent fallthrough:
/// a new <see cref="IntentAction"/> renders as "hold position" and the board quietly lies.
/// </summary>
public class IntentTelegraphTests
{
    [Fact]
    public void EveryIntentAction_HasATelegraphOfItsOwn()
    {
        // Reflection over the enum rather than a hand-listed set, so a new action fails here the
        // day it is added instead of the day somebody notices the arrow was wrong.
        var state = Game.Start(FightLibrary.ById("first-contact"), seed: 1).NewState;
        var enemy = state.Units.First(u => u.Team == Team.Enemy);
        var target = state.Units.First(u => u.Team.IsPlayer());

        foreach (IntentAction action in System.Enum.GetValues(typeof(IntentAction)))
        {
            var intent = new EnemyIntent(
                enemy.Id, enemy.Kind, enemy.Position, action,
                target.Id, target.Position, null, null, null, 1,
                new Coord(0, 0), 0);

            string line = EventText.Intent(state, intent);

            Assert.False(
                string.IsNullOrWhiteSpace(line),
                action + " renders as nothing.");

            Assert.DoesNotContain(
                "no telegraph written",
                line,
                System.StringComparison.Ordinal);

            if (action != IntentAction.Hold)
            {
                Assert.NotEqual("hold position", line);
            }
        }
    }

    [Fact]
    public void ARescueTelegraph_NamesTheAllyAndWhereItLands()
    {
        var state = Game.Start(FightLibrary.ById("first-contact"), seed: 1).NewState;
        var enemy = state.Units.First(u => u.Team == Team.Enemy);
        var ally = state.Units.Last(u => u.Team == Team.Enemy);

        var intent = new EnemyIntent(
            enemy.Id, enemy.Kind, enemy.Position, IntentAction.Rescue,
            ally.Id, ally.Position, null, null, null, 0,
            new Coord(2, 2), 0);

        string line = EventText.Intent(state, intent);

        Assert.Contains("haul", line, System.StringComparison.Ordinal);
        Assert.Contains("(2,2)", line, System.StringComparison.Ordinal);
    }
}
