using System.Linq;
using Faultline.Core;
using Faultline.Web.Shell;

namespace Faultline.Web.Tests;

/// <summary>
/// The battle picker's test bench: what a tester sets is what the board opens with.
/// </summary>
/// <remarks>
/// It is a <b>dev affordance for isolating a board</b>, not a way to show a state is reachable —
/// which is why these tests check that the numbers arrive, and never that a run could produce them.
/// </remarks>
public class TestLoadoutTests
{
    private const string Board = "first-contact";

    private static FightDefinition Fight() => FightLibrary.ById(Board);

    /// <summary>
    /// An untouched bench changes nothing, so a board played from the picker takes exactly the path
    /// a shipped board takes.
    /// </summary>
    [Fact]
    public void UntouchedIsNull_SoAnUneditedPickerCannotChangeAFight()
    {
        var bench = new TestLoadout();

        Assert.False(bench.IsCustom);
        Assert.Null(bench.Build(Fight()));
    }

    /// <summary>Total health is the ceiling, and the duck opens on it.</summary>
    [Fact]
    public void TotalHealth_ReachesTheBoard()
    {
        var fight = Fight();
        var bench = new TestLoadout();
        bench.For(Team.PlayerA, 0).MaxHp = 40;

        Assert.True(bench.IsCustom);

        var session = new GameSession();
        session.StartFight(fight, GameSession.DefaultSeed, bench.Build(fight));

        var duck = session.State.Units.First(u => u.Team == Team.PlayerA);

        Assert.Equal(40, duck.MaxHp);
        Assert.Equal(40, duck.Hp);
    }

    /// <summary>Starting hit points are separate from the ceiling — a pre-damaged duck is a state.</summary>
    [Fact]
    public void StartingHitPoints_AreSeparateFromTheCeiling()
    {
        var fight = Fight();
        var bench = new TestLoadout();
        bench.For(Team.PlayerA, 0).MaxHp = 40;
        bench.For(Team.PlayerA, 0).Hp = 3;

        var session = new GameSession();
        session.StartFight(fight, GameSession.DefaultSeed, bench.Build(fight));

        var duck = session.State.Units.First(u => u.Team == Team.PlayerA);

        Assert.Equal(40, duck.MaxHp);
        Assert.Equal(3, duck.Hp);
    }

    /// <summary>
    /// <b>A ceiling can be raised and never lowered</b>, and the bench says the same number the board
    /// will use rather than accepting one it will not.
    /// </summary>
    /// <remarks>
    /// Core's rule, not the bench's: a loadout naming a smaller maximum is a stale save quietly
    /// nerfing a class rather than an intended change (<c>Game.WithRaisedMax</c>). Not worth
    /// rewriting for a dev tool — to make a duck fragile, lower its starting hit points, which is
    /// unclamped from below.
    /// </remarks>
    [Fact]
    public void ACeilingCanBeRaised_NeverLowered_AndTheBenchSaysSo()
    {
        var fight = Fight();
        var wardbearer = UnitTemplate.For(fight.RosterB[0]).MaxHp;

        var bench = new TestLoadout();
        bench.For(Team.PlayerB, 0).MaxHp = 5;

        // The bench reports what will happen, not what was typed.
        Assert.Equal(wardbearer, bench.MaxHpOf(fight.RosterB[0], Team.PlayerB, 0));

        var session = new GameSession();
        session.StartFight(fight, GameSession.DefaultSeed, bench.Build(fight));

        var duck = session.State.Units.First(u => u.Team == Team.PlayerB);

        Assert.Equal(wardbearer, duck.MaxHp);
    }

    /// <summary>
    /// Hit points above the ceiling are clamped to it: a duck opening above its own maximum is a
    /// state the rules cannot otherwise produce.
    /// </summary>
    [Fact]
    public void HitPointsAboveTheCeiling_AreClampedToIt()
    {
        var fight = Fight();
        var bench = new TestLoadout();
        bench.For(Team.PlayerB, 0).Hp = 99;

        var session = new GameSession();
        session.StartFight(fight, GameSession.DefaultSeed, bench.Build(fight));

        var duck = session.State.Units.First(u => u.Team == Team.PlayerB);

        Assert.Equal(duck.MaxHp, duck.Hp);
    }

    /// <summary>The way to make a duck fragile: lower what it starts on, not its ceiling.</summary>
    [Fact]
    public void LoweringStartingHitPoints_IsHowADuckIsMadeFragile()
    {
        var fight = Fight();
        var bench = new TestLoadout();
        bench.For(Team.PlayerA, 0).Hp = 2;

        var session = new GameSession();
        session.StartFight(fight, GameSession.DefaultSeed, bench.Build(fight));

        var duck = session.State.Units.First(u => u.Team == Team.PlayerA);

        Assert.Equal(2, duck.Hp);
        Assert.Equal(UnitTemplate.For(fight.RosterA[0]).MaxHp, duck.MaxHp);
    }

    /// <summary>An item is in the duck's pocket when the fight opens.</summary>
    [Fact]
    public void AnItem_IsInThePocketOnTheFirstRound()
    {
        var fight = Fight();
        var bench = new TestLoadout();
        bench.For(Team.PlayerA, 1).Item = Consumable.DriedMinnow;

        var session = new GameSession();
        session.StartFight(fight, GameSession.DefaultSeed, bench.Build(fight));

        var duck = session.State.Units.Where(u => u.Team == Team.PlayerA).ElementAt(1);

        Assert.Equal(Consumable.DriedMinnow, duck.Loadout.Pocket);
    }

    /// <summary>
    /// <b>An ability card reaches the duck</b> — and this is the surface that makes the cross-flock
    /// techniques reachable at all outside a test, since nothing in a played run can equip one
    /// (D-253).
    /// </summary>
    [Fact]
    public void AnAbilityCard_ReachesTheDuck()
    {
        var fight = Fight();
        var bench = new TestLoadout();
        bench.For(Team.PlayerB, 1).Techniques.Add(TechniqueModifier.Spotter);

        var session = new GameSession();
        session.StartFight(fight, GameSession.DefaultSeed, bench.Build(fight));

        var duck = session.State.Units.Where(u => u.Team == Team.PlayerB).ElementAt(1);

        Assert.True(duck.Loadout.Has(TechniqueModifier.Spotter));
    }

    /// <summary>Each slot is its own duck; editing one leaves the rest as the board rosters them.</summary>
    [Fact]
    public void EditingOneDuck_LeavesTheOthersAsTheBoardRostersThem()
    {
        var fight = Fight();
        var bench = new TestLoadout();
        bench.For(Team.PlayerA, 0).MaxHp = 40;

        var session = new GameSession();
        session.StartFight(fight, GameSession.DefaultSeed, bench.Build(fight));

        var others = session.State.Units
            .Where(u => u.Team.IsPlayer())
            .Skip(1)
            .ToList();

        Assert.All(others, u => Assert.Equal(UnitTemplate.For(u.Kind).MaxHp, u.MaxHp));
    }

    /// <summary>Reset puts every duck back, and the bench stops being custom.</summary>
    [Fact]
    public void Reset_PutsEveryDuckBack()
    {
        var bench = new TestLoadout();
        bench.For(Team.PlayerA, 0).MaxHp = 40;
        bench.For(Team.PlayerB, 1).Item = Consumable.OldRope;

        bench.Reset();

        Assert.False(bench.IsCustom);
        Assert.Null(bench.Build(Fight()));
    }

    /// <summary>
    /// A benched board still plays: the loadout goes in through the same seam a run uses, so nothing
    /// downstream can tell the difference.
    /// </summary>
    [Fact]
    public void ABenchedBoard_StillDrafts()
    {
        var fight = Fight();
        var bench = new TestLoadout();
        bench.For(Team.PlayerA, 0).MaxHp = 40;
        bench.For(Team.PlayerA, 0).Item = Consumable.BrambleSalve;

        var session = new GameSession();
        session.StartFight(fight, GameSession.DefaultSeed, bench.Build(fight));
        session.DeployEveryone();

        Assert.Equal(Phase.Battle, session.State.Phase);
        Assert.Equal(4, session.State.Units.Count(u => u.Team.IsPlayer() && u.IsDeployed));
    }
}
