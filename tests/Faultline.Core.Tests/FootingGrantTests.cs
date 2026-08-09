using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// Covers the <c>footing:</c> key. No archetype starts a fight with a Footing token, so a unit that
/// can shrug a tile off a shove is one the scenario asked for by name — these tests pin who gets one,
/// who does not, and that a granted token still behaves like the token the displacement rules already
/// knew about.
/// </summary>
public class FootingGrantTests
{
    /// <summary>
    /// Husk at (2,0) with an open tile at (3,0) and a pit at (4,0), so a Push 2 from the west either
    /// ends in the pit or, one tile shorter, does not. A Lobber in the south gives kind grants a second
    /// enemy archetype to miss.
    /// </summary>
    private const string Board = """
        ..h.O.B
        .H....B
        .......
        ^.....^
        .......
        AA.....
        AA...l#
        """;

    [Fact]
    public void Footing_AbsentKey_MeansEverybodyStartsOnZero()
    {
        var fight = Parsed(Fight());

        Assert.Empty(fight.FootingGrants);
        Assert.All(Start(fight), u => Assert.Equal(0, u.Footing));
    }

    [Fact]
    public void Footing_GrantedToOneSide_LeavesEveryOtherSideOnZero()
    {
        var units = Start(Parsed(Fight("footing: a=1")));

        Assert.All(units.Where(u => u.Team == Team.PlayerA), u => Assert.Equal(1, u.Footing));
        Assert.All(units.Where(u => u.Team != Team.PlayerA), u => Assert.Equal(0, u.Footing));
    }

    [Fact]
    public void Footing_GrantedToBothPlayerSides_LeavesTheEnemiesOnZero()
    {
        var units = Start(Parsed(Fight("footing: a=1 b=1")));

        Assert.All(units.Where(u => u.Team.IsPlayer()), u => Assert.Equal(1, u.Footing));
        Assert.All(units.Where(u => u.Team == Team.Enemy), u => Assert.Equal(0, u.Footing));
    }

    [Fact]
    public void Footing_GrantedToAUnitKind_ReachesOnlyThatKind()
    {
        var units = Start(Parsed(Fight("footing: Husk=2")));

        Assert.All(units.Where(u => u.Kind == UnitKind.Husk), u => Assert.Equal(2, u.Footing));
        Assert.All(units.Where(u => u.Kind != UnitKind.Husk), u => Assert.Equal(0, u.Footing));
    }

    [Fact]
    public void Footing_GrantedToAPlayerKind_ReachesThatKindOnTheSideThatFieldsIt()
    {
        var units = Start(Parsed(Fight("footing: Archer=1")));

        Assert.Equal(1, units.Single(u => u.Kind == UnitKind.Archer).Footing);
        Assert.All(units.Where(u => u.Kind != UnitKind.Archer), u => Assert.Equal(0, u.Footing));
    }

    [Fact]
    public void Footing_AKindGrantBeatsASideGrant_BecauseItIsMoreSpecific()
    {
        var units = Start(Parsed(Fight("footing: enemy=1 Husk=2")));

        Assert.All(units.Where(u => u.Kind == UnitKind.Husk), u => Assert.Equal(2, u.Footing));
        Assert.Equal(1, units.Single(u => u.Kind == UnitKind.Lobber).Footing);
        Assert.All(units.Where(u => u.Team.IsPlayer()), u => Assert.Equal(0, u.Footing));
    }

    [Fact]
    public void Footing_KindGrantWinsWhicheverOrderTheTokensAreWrittenIn()
    {
        var units = Start(Parsed(Fight("footing: Husk=2 enemy=1")));

        Assert.All(units.Where(u => u.Kind == UnitKind.Husk), u => Assert.Equal(2, u.Footing));
        Assert.Equal(1, units.Single(u => u.Kind == UnitKind.Lobber).Footing);
    }

    [Fact]
    public void Footing_TwoGrantsOfEqualSpecificity_TakeTheLastOne()
    {
        // The format's rule everywhere else is that a repeated key is silently the last one.
        var units = Start(Parsed(Fight("footing: enemy=1 enemy=3")));

        Assert.All(units.Where(u => u.Team == Team.Enemy), u => Assert.Equal(3, u.Footing));
    }

    [Fact]
    public void Footing_TargetsAndTheKeyItself_AreCaseInsensitive()
    {
        var fight = Parsed(Fight("FOOTING: A=1 husk=2"));

        Assert.Equal(1, fight.FootingFor(Team.PlayerA, UnitKind.Vanguard));
        Assert.Equal(2, fight.FootingFor(Team.Enemy, UnitKind.Husk));
    }

    [Fact]
    public void Footing_ZeroIsAGrantLikeAnyOther_AndChangesNothing()
    {
        var fight = Parsed(Fight("footing: a=0"));

        Assert.Equal(0, fight.FootingFor(Team.PlayerA, UnitKind.Archer));
        Assert.Single(fight.FootingGrants);
    }

    // --- The rules the token feeds ---------------------------------------------------------

    [Fact]
    public void Footing_AGrantedToken_IsStillSpentByTheEnemyPitRule()
    {
        Start(Parsed(Fight("footing: enemy=1")), out var state);
        var husk = state.Find(UnitKind.Husk);

        Assert.Equal(1, husk.Footing);

        var events = new List<GameEvent>();
        var after = Displacement.ResolveAuto(
            state, husk.Id, new Coord(1, 0), DisplacementKind.Push, 2, events);

        // The whole instance is refused, so it does not move at all — one token poorer, same tile.
        Assert.False(after.Get(husk.Id).Clinging);
        Assert.Equal(husk.Position, after.Get(husk.Id).Position);
        Assert.Equal(0, after.Get(husk.Id).Footing);
        Assert.Single(events.OfType<FootingSpent>());
    }

    [Fact]
    public void Footing_WithoutAGrant_TheSameShoveEndsInThePit()
    {
        Start(Parsed(Fight()), out var state);
        var husk = state.Find(UnitKind.Husk);

        var events = new List<GameEvent>();
        var after = Displacement.ResolveAuto(
            state, husk.Id, new Coord(1, 0), DisplacementKind.Push, 2, events);

        Assert.True(after.Get(husk.Id).Clinging);
        Assert.Empty(events.OfType<FootingSpent>());
    }

    // --- Errors and lints ------------------------------------------------------------------

    [Fact]
    public void Footing_UnknownTarget_IsAnError()
    {
        AssertError(FightIssueCode.UnknownFootingTarget, Fight("footing: c=1"));
        AssertError(FightIssueCode.UnknownFootingTarget, Fight("footing: PlayerA=1"));
        AssertError(FightIssueCode.UnknownFootingTarget, Fight("footing: Huskk=1"));
    }

    [Fact]
    public void Footing_NegativeCount_IsAnError()
    {
        AssertError(FightIssueCode.FootingCountNegative, Fight("footing: a=-1"));
    }

    [Fact]
    public void Footing_MalformedToken_IsABadValue()
    {
        AssertError(FightIssueCode.BadValue, Fight("footing: a1"));
        AssertError(FightIssueCode.BadValue, Fight("footing: a="));
        AssertError(FightIssueCode.BadValue, Fight("footing: =1"));
        AssertError(FightIssueCode.BadValue, Fight("footing: a=one"));
        AssertError(FightIssueCode.BadValue, Fight("footing: a = 1"));
    }

    [Fact]
    public void Footing_AGrantNobodyInTheFightMatches_IsALintNotAnError()
    {
        var result = FightParser.Parse(Fight("footing: Stalker=1"));

        Assert.True(result.Ok, result.Describe());
        Assert.Empty(result.Errors);
        Assert.Contains(result.Lints, l => l.Code == FightIssueCode.FootingGrantUnused);
    }

    [Fact]
    public void Footing_AGrantSomebodyMatches_IsNotLinted()
    {
        var result = FightParser.Parse(Fight("footing: enemy=1 Husk=1"));

        Assert.Empty(result.Issues);
    }

    // D-147 retired the lint. Player Footing has a spend trigger now — the owner is prompted — so a
    // grant on a player side is a scenario tool rather than a token that lands and rots.
    [Fact]
    public void Footing_AGrantOnAPlayerSide_IsNoLongerLinted()
    {
        var result = FightParser.Parse(Fight("footing: a=1"));

        Assert.True(result.Ok, result.Describe());
        Assert.Empty(result.Errors);
        Assert.DoesNotContain(result.Lints, l => l.Code == FightIssueCode.FootingGrantOnPlayers);
    }

    [Fact]
    public void Footing_AKindGrantThatReachesAPlayerRoster_IsNotLintedEither()
    {
        var result = FightParser.Parse(Fight("footing: Archer=1"));

        Assert.DoesNotContain(result.Lints, l => l.Code == FightIssueCode.FootingGrantOnPlayers);
    }

    [Fact]
    public void Footing_NoShippedFightIsLintedForAPlayerGrant()
    {
        foreach (var result in FightLibrary.LoadAll())
        {
            Assert.DoesNotContain(
                result.Lints,
                l => l.Code == FightIssueCode.FootingGrantOnPlayers);
        }
    }

    // --- Round trip ------------------------------------------------------------------------

    [Fact]
    public void Footing_RoundTripsThroughTheWriter_TokensAndOrderIntact()
    {
        var original = Parsed(Fight("footing: enemy=1 Husk=2 a=1"));

        var text = FightWriter.Write(original);
        var reparsed = Parsed(text);

        Assert.Contains("footing: enemy=1 Husk=2 a=1", text);
        Assert.Equal(original.FootingGrants, reparsed.FootingGrants);
        Assert.Equal(2, reparsed.FootingFor(Team.Enemy, UnitKind.Husk));
        Assert.Equal(1, reparsed.FootingFor(Team.Enemy, UnitKind.Lobber));
        Assert.Equal(1, reparsed.FootingFor(Team.PlayerA, UnitKind.Vanguard));
    }

    [Fact]
    public void Footing_WithNoGrants_WritesNoKeyAtAll()
    {
        Assert.DoesNotContain("footing:", FightWriter.Write(Parsed(Fight())));
    }

    [Fact]
    public void Footing_GrantsBuiltInMemory_SurviveAWriteAndAParse()
    {
        var fight = Parsed(Fight()) with
        {
            FootingGrants = new[]
            {
                FootingGrant.ForSide(Team.PlayerB, 2),
                FootingGrant.ForKind(UnitKind.Lobber, 3),
            },
        };

        var reparsed = Parsed(FightWriter.Write(fight));

        Assert.Equal(fight.FootingGrants, reparsed.FootingGrants);
        Assert.Equal(2, reparsed.FootingFor(Team.PlayerB, UnitKind.Wardbearer));
        Assert.Equal(3, reparsed.FootingFor(Team.Enemy, UnitKind.Lobber));
    }

    // --- Fixture -----------------------------------------------------------------------------

    private static string Fight(string footing = "") =>
        string.Join(
            "\n",
            "id: footing-yard",
            "number: 3",
            "name: Footing Yard",
            "pool: Ordinary",
            "description: A yard with one pit that matters.",
            string.Empty,
            "spawn h = Husk",
            "spawn l = Lobber",
            string.Empty,
            "roster a: Vanguard, Archer",
            "roster b: Threadcaster, Wardbearer",
            footing,
            string.Empty,
            "board:",
            string.Join("\n", Board.Replace("\r\n", "\n").Split('\n').Select(row => "  " + row)));

    private static FightDefinition Parsed(string text)
    {
        var result = FightParser.Parse(text);

        Assert.True(result.Ok, result.Describe() + ": " + string.Join(" | ", result.Issues));
        Assert.Empty(result.Errors);
        return result.Fight!;
    }

    private static IReadOnlyList<Unit> Start(FightDefinition fight) => Start(fight, out _);

    private static IReadOnlyList<Unit> Start(FightDefinition fight, out GameState state)
    {
        state = Game.Start(fight, seed: 7).NewState;
        return state.Units;
    }

    private static void AssertError(FightIssueCode code, string text)
    {
        var result = FightParser.Parse(text);

        Assert.False(result.Ok, result.Describe());
        Assert.Contains(result.Errors, e => e.Code == code);
    }
}
