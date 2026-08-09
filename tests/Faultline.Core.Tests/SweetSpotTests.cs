using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// The Archer's sweet spot (MASTER_DESIGN §4 and §5, locked af): <b>range 2–4, 4 damage at exactly
/// 3, 2 either side of it, and +1 Pluck on a sweet-spot hit</b>. One number carrying her gun, her
/// income and her footwork.
/// </summary>
/// <remarks>
/// The band's failure mode is not a crash — it is a number that is right in the rules and wrong on a
/// screen, so these assert on what the resolution actually emitted rather than on the template.
/// </remarks>
public class SweetSpotTests
{
    /// <summary>The whole band, asserted on rendered resolution and not on a query.</summary>
    [Theory]
    [InlineData(2, 2)]
    [InlineData(3, 4)]
    [InlineData(4, 2)]
    public void TheBand_DealsWhatTheDocPrints(int distance, int expected)
    {
        var state = Board(distance);
        var archer = state.Find(UnitKind.Archer).Id;
        var husk = state.Find(UnitKind.Husk).Id;

        var result = state.Step(new AttackCommand(archer, husk));

        Assert.Equal(expected, result.Single<UnitAttacked>().Damage);
        Assert.Equal(expected, result.Single<UnitDamaged>().Amount);
        Assert.Equal(18 - expected, result.NewState.Get(husk).Hp);
    }

    /// <summary>Range 1 is the dead zone and range 5 is off the end of the band.</summary>
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void OutsideTheBand_SheCannotShootAtAll(int distance)
    {
        var state = Board(distance);

        Assert.False(Combat.CanAttack(
            state,
            state.Get(state.Find(UnitKind.Archer).Id),
            state.Get(state.Find(UnitKind.Husk).Id),
            out _));

        TestPlay.AssertIllegal(
            state, new AttackCommand(state.Find(UnitKind.Archer).Id, state.Find(UnitKind.Husk).Id));
    }

    /// <summary>
    /// The preview is the resolution. Asserted at every band tile, because a falloff applied in the
    /// rules and not in the preview is the exact shape of bug this change invites.
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void ThePreview_ShowsWhatTheShotWillActuallyDeal(int distance)
    {
        var state = Board(distance);
        var archer = state.Get(state.Find(UnitKind.Archer).Id);
        var husk = state.Get(state.Find(UnitKind.Husk).Id);

        Assert.True(Combat.CanAttack(state, archer, husk, out int previewed));

        var result = state.Step(new AttackCommand(archer.Id, husk.Id));

        Assert.Equal(previewed, result.Single<UnitAttacked>().Damage);
    }

    /// <summary>High ground stacks on top of the band rather than replacing it: the spot from a ledge is 6.</summary>
    [Fact]
    public void HighGround_StacksOnTopOfTheSpot()
    {
        var state = BoardBuilder.Rows("H.....", "......")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 3, 0, hp: 18)
            .Build();

        var result = state.Step(new AttackCommand(
            state.Find(UnitKind.Archer).Id, state.Find(UnitKind.Husk).Id));

        var shot = result.Single<UnitAttacked>();
        Assert.True(shot.FromHighGround);
        Assert.True(shot.SweetSpot);
        Assert.Equal(6, shot.Damage);
    }

    /// <summary>
    /// The dead zone's own exception still works: from a ledge she may take an adjacent lower target.
    /// </summary>
    /// <remarks>
    /// What that shot is WORTH is the open question — range 1 is not the sweet spot, so the band
    /// arithmetic pays the outer number, and §4 does not say whether it should. Recorded in D-269 with
    /// Point Blank, which asks the same question from the other side.
    /// </remarks>
    [Fact]
    public void TheDownhillException_StillFires()
    {
        var state = BoardBuilder.Rows("H.....", "......")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 0, 1, hp: 18)
            .Build();

        var archer = state.Get(state.Find(UnitKind.Archer).Id);
        var husk = state.Get(state.Find(UnitKind.Husk).Id);

        Assert.True(Combat.CanAttack(state, archer, husk, out int damage));
        Assert.Equal(UnitTemplate.For(UnitKind.Archer).OffSpotDamage + Combat.HighGroundBonus, damage);
    }

    /// <summary>
    /// Two shots at the spot are two charges — the spender feeding the meter back is intended (§5).
    /// </summary>
    [Fact]
    public void DoubleNockAtTheSpot_ChargesTwice()
    {
        var state = BoardBuilder.Open(5, 4)
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 3, 0, hp: 18)
            .Enemy(UnitKind.Husk, 2, 1, hp: 18)
            .Build();

        var archer = state.Find(UnitKind.Archer).Id;
        var first = state.Units.Single(u => u.Position == new Coord(3, 0)).Id;
        var second = state.Units.Single(u => u.Position == new Coord(2, 1)).Id;

        state = state.WithUnit(state.Get(archer) with { Verve = Verve.Cap });
        int before = state.Get(archer).Verve;

        var after = state
            .Then(new SpendVerveCommand(archer, VerveSpend.DoubleNock))
            .Then(new AttackCommand(archer, first))
            .Then(new AttackCommand(archer, second));

        // Four out for the spend, two back for the two hits.
        Assert.Equal(before - Verve.CostOf(VerveSpend.DoubleNock) + 2, after.Get(archer).Verve);
    }

    /// <summary>
    /// One command, one charge. Nothing multi-target ships yet, so this pins the seam rather than a
    /// behaviour: a second sweet-spot hit inside the SAME command would still pay once.
    /// </summary>
    [Fact]
    public void OneCommand_ChargesOnceHoweverManyBodiesItNames()
    {
        var state = Board(3);
        var archer = state.Find(UnitKind.Archer).Id;
        var husk = state.Find(UnitKind.Husk).Id;

        var result = state.Step(new AttackCommand(archer, husk));

        Assert.Single(result.All<VerveCharged>(), c => c.Source == VerveSource.SweetSpot);
    }

    /// <summary>Seed plus command log still replays exactly with a banded gun in the fight.</summary>
    [Fact]
    public void Replay_WithTheNewGun_ReachesTheIdenticalState()
    {
        var first = Game.Start(FightLibrary.Fight1(), seed: 20260808).NewState;
        var (played, log) = TestPlay.PlayFirstLegal(first, maxSteps: 400);

        var second = Game.Start(FightLibrary.Fight1(), seed: 20260808).NewState;
        var replayed = TestPlay.Replay(second, log);

        Assert.NotEmpty(log);
        Assert.Equal(played, replayed);
        Assert.Equal(played.GetHashCode(), replayed.GetHashCode());
    }

    /// <summary>
    /// The unit card and the inspector print the condition in short form (locked af), and the "+1" is
    /// the part the old wording left implicit.
    /// </summary>
    [Fact]
    public void TheChargeLine_ReadsPlusOneOnASweetSpotHit()
    {
        Assert.Equal("a sweet-spot hit", Verve.ConditionFor(UnitKind.Archer));
        Assert.Equal("+1 on a sweet-spot hit", Verve.ChargeLineFor(UnitKind.Archer));
    }

    /// <summary>Every class that earns gets the same short form, derived rather than written twice.</summary>
    [Theory]
    [InlineData(UnitKind.Vanguard)]
    [InlineData(UnitKind.Archer)]
    [InlineData(UnitKind.Threadcaster)]
    [InlineData(UnitKind.Wardbearer)]
    public void EveryChargeLine_IsThePlusFormOfItsCondition(UnitKind kind)
    {
        Assert.Equal("+1 on " + Verve.ConditionFor(kind), Verve.ChargeLineFor(kind));
    }

    /// <summary>A class that earns from nothing says nothing, rather than "+1 on ".</summary>
    [Fact]
    public void AClassThatEarnsNothing_HasNoChargeLine()
    {
        Assert.Equal(string.Empty, Verve.ChargeLineFor(UnitKind.Husk));
    }

    /// <summary>
    /// Long Shot reads the shot's own flag, so an intercepted sweet-spot kill still pays. Guard's tile
    /// is what the event's To names, and re-measuring it would have lost the bet on exactly the shots
    /// where the board moved under it.
    /// </summary>
    [Fact]
    public void LongShot_PaysOnAnInterceptedSweetSpotKill()
    {
        var state = BoardBuilder.Open(6, 2)
            .PlayerA(UnitKind.Archer, 0, 0)
            .PlayerB(UnitKind.Wardbearer, 2, 0)
            .Enemy(UnitKind.Husk, 3, 0, hp: 2)
            .Build();

        var archer = state.Find(UnitKind.Archer).Id;
        var husk = state.Find(UnitKind.Husk).Id;

        state = state.WithWind(archer, SecondWind.LongKill);

        var result = state.Step(new AttackCommand(archer, husk));
        var shot = result.All<UnitAttacked>().Single(a => a.AttackerId == archer);

        Assert.True(shot.SweetSpot);
        Assert.Contains(result.All<VerveCharged>(), c => c.Source == VerveSource.LongKill);
        Assert.Equal(2, result.NewState.Get(archer).Verve);
    }

    private static GameState Board(int enemyAt) =>
        BoardBuilder.Open(7, 2)
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, enemyAt, 0, hp: 18)
            .Build();
}
