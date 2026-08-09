using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// D-099: the Archer's bow needs room. Nothing she shoots with reaches the tile next to her, which
/// is what makes closing on her an answer rather than a slower way of dying.
/// </summary>
public class ArcherMinimumRangeTests
{
    [Fact]
    public void Archer_CannotShootAnAdjacentEnemy()
    {
        var state = Board(enemyAt: 1);
        var archer = state.Find(UnitKind.Archer);
        var husk = state.Find(UnitKind.Husk);

        Assert.False(Combat.CanAttack(state, state.Get(archer.Id), state.Get(husk.Id), out _));
        TestPlay.AssertIllegal(state, new AttackCommand(archer.Id, husk.Id));
    }

    /// <summary>
    /// The band and its falloff (MASTER_DESIGN §4, locked af): 2–4, with the 4 damage at exactly 3.
    /// </summary>
    [Theory]
    [InlineData(2, 2)]
    [InlineData(3, 4)]
    [InlineData(4, 2)]
    public void Archer_ShootsEverythingFromTwoTilesOut(int distance, int expected)
    {
        var state = Board(enemyAt: distance);
        var archer = state.Find(UnitKind.Archer);
        var husk = state.Find(UnitKind.Husk);

        Assert.True(Combat.CanAttack(state, state.Get(archer.Id), state.Get(husk.Id), out int damage));
        Assert.Equal(expected, damage);
        TestPlay.AssertLegal(state, new AttackCommand(archer.Id, husk.Id));
    }

    [Fact]
    public void Archer_StillCannotReachPastHerRange()
    {
        var state = Board(enemyAt: 5);

        Assert.False(Combat.CanAttack(
            state, state.Get(state.Find(UnitKind.Archer).Id), state.Get(state.Find(UnitKind.Husk).Id), out _));
    }

    // A rule the basic shot obeys and the ability does not is not a rule: she would simply use the
    // other button.
    [Fact]
    public void StaggerShot_ObeysTheSameMinimum()
    {
        var state = Board(enemyAt: 1);
        var archer = state.Find(UnitKind.Archer);
        var husk = state.Find(UnitKind.Husk);

        Assert.DoesNotContain(
            husk.Id,
            Abilities.LegalTargets(state, state.Get(archer.Id), AbilityDefinition.For(Ability.StaggerShot)));

        TestPlay.AssertIllegal(state, new AbilityCommand(archer.Id, Ability.StaggerShot, husk.Id));
    }

    [Fact]
    public void StaggerShot_StillFiresFromTwoOut()
    {
        var state = Board(enemyAt: 2);
        var archer = state.Find(UnitKind.Archer);
        var husk = state.Find(UnitKind.Husk);

        TestPlay.AssertLegal(state, new AbilityCommand(archer.Id, Ability.StaggerShot, husk.Id));
    }

    // Nothing else gained a minimum. The enemy shooters in particular are unchanged, because giving
    // every bow a dead zone is a different ruling on every board rather than one on the Archer.
    [Theory]
    [InlineData(UnitKind.Lobber)]
    [InlineData(UnitKind.Perch)]
    public void EnemyShooters_StillFireAtWhatIsOnTopOfThem(UnitKind kind)
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(kind, 1, 0)
            .Build();

        var shooter = state.Find(kind);
        var vanguard = state.Find(UnitKind.Vanguard);

        Assert.Equal(0, shooter.Template.MinRange);
        Assert.True(Combat.CanAttack(state, state.Get(shooter.Id), state.Get(vanguard.Id), out _));
    }

    [Fact]
    public void TheFisher_KeepsHerPointBlankShot()
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Threadcaster, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0)
            .Build();

        var fisher = state.Find(UnitKind.Threadcaster);
        var husk = state.Find(UnitKind.Husk);

        Assert.Equal(0, fisher.Template.MinRange);
        Assert.True(Combat.CanAttack(state, state.Get(fisher.Id), state.Get(husk.Id), out _));
    }

    // The answer to being closed on: she has Move 3, and stepping back opens the lane again. Worth
    // pinning, because a minimum range with no way out of it would be a trap rather than a weakness.
    [Fact]
    public void WalkingBackwards_OpensTheShotAgain()
    {
        var state = Board(enemyAt: 1);
        var archer = state.Find(UnitKind.Archer);
        var husk = state.Find(UnitKind.Husk);

        var stepped = state.Then(new MoveCommand(archer.Id, new Coord(0, 1)));

        // (0,1) is two steps from (1,0) the way this board measures, so the lane is open again.
        Assert.Equal(2, stepped.Get(archer.Id).Position.DistanceTo(stepped.Get(husk.Id).Position));
        TestPlay.AssertLegal(stepped, new AttackCommand(archer.Id, husk.Id));
    }

    [Fact]
    public void TheMinimumIsOnTheStatBlock_SoTheUiCanSaySo()
    {
        Assert.Equal(2, UnitTemplate.For(UnitKind.Archer).MinRange);
        Assert.True(UnitTemplate.For(UnitKind.Archer).HasMinRange);
        Assert.False(UnitTemplate.For(UnitKind.Vanguard).HasMinRange);
    }

    // MASTER_DESIGN §4's exception, which shipped unbuilt: the dead zone is about the bow's arc, and
    // there is no arc to bend when she is firing down off a ledge.
    [Fact]
    public void FromHighGround_SheMayShootTheEnemyStandingRightBelowHer()
    {
        var state = BoardBuilder.Rows("H.....", "......")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 12)
            .Build();

        var archer = state.Find(UnitKind.Archer);
        var husk = state.Find(UnitKind.Husk);

        Assert.Equal(1, archer.Position.DistanceTo(husk.Position));
        Assert.True(Combat.CanAttack(state, archer, husk, out _));
        TestPlay.AssertLegal(state, new AttackCommand(archer.Id, husk.Id));
    }

    // The half that keeps the exception from eating the rule. Level with her on the same ledge is
    // still somebody in her face, and the bow still has nowhere to go.
    [Fact]
    public void OnTheSameLedge_AdjacentIsStillTooClose()
    {
        var state = BoardBuilder.Rows("HH....", "......")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 12)
            .Build();

        var archer = state.Find(UnitKind.Archer);
        var husk = state.Find(UnitKind.Husk);

        Assert.False(Combat.CanAttack(state, archer, husk, out _));
        TestPlay.AssertIllegal(state, new AttackCommand(archer.Id, husk.Id));
    }

    // And the ordinary case is untouched: on the flat, adjacent is the dead zone it always was.
    [Fact]
    public void OnFlatGround_AdjacentIsStillRejected()
    {
        var state = Board(1);

        var archer = state.Find(UnitKind.Archer);
        var husk = state.Find(UnitKind.Husk);

        Assert.False(Combat.CanAttack(state, archer, husk, out _));
        TestPlay.AssertIllegal(state, new AttackCommand(archer.Id, husk.Id));
    }

    // "The same min range" (MASTER_DESIGN §4) reads as the same *rule*, exception included: it is the
    // same bow and the same arc, and the exception is about the arc. A ledge from which she may shoot
    // the enemy below but not shove it would be two rules where the fiction has one.
    [Fact]
    public void StaggerShot_FromHighGround_AlsoReachesTheEnemyStandingRightBelowHer()
    {
        var state = BoardBuilder.Rows("H.....", "......")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 12)
            .Build();

        var archer = state.Find(UnitKind.Archer);
        var husk = state.Find(UnitKind.Husk);
        var shot = AbilityDefinition.For(Ability.StaggerShot);

        Assert.Equal(1, archer.Position.DistanceTo(husk.Position));
        Assert.Contains(husk.Id, Abilities.LegalTargets(state, archer, shot));

        var after = state.Then(new AbilityCommand(archer.Id, Ability.StaggerShot, husk.Id));

        Assert.Equal(12 - shot.Damage, after.Get(husk.Id).Hp);
        Assert.Equal(new Coord(1 + shot.Push, 0), after.Get(husk.Id).Position);
    }

    // And the same half that keeps the basic shot's exception honest keeps this one honest: level
    // with her on the ledge, the bow still has nowhere to go.
    [Fact]
    public void StaggerShot_OnTheSameLedge_IsStillTooClose()
    {
        var state = BoardBuilder.Rows("HH....", "......")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 12)
            .Build();

        var archer = state.Find(UnitKind.Archer);
        var husk = state.Find(UnitKind.Husk);

        Assert.DoesNotContain(
            husk.Id,
            Abilities.LegalTargets(state, archer, AbilityDefinition.For(Ability.StaggerShot)));
        TestPlay.AssertIllegal(state, new AbilityCommand(archer.Id, Ability.StaggerShot, husk.Id));
    }

    // Double Nock buys a second attack action, and a second attack is the same bow fired twice: both
    // shots route through Combat.CanAttack, so both inherit the exception. That is true by routing
    // rather than by rule — nothing states it, so nothing would catch it going away. Hence the pin.
    [Fact]
    public void DoubleNock_FromHighGround_TakesBothShotsAtTheEnemiesStandingRightBelowHer()
    {
        var state = BoardBuilder.Rows("H.....", "......")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 12)
            .Enemy(UnitKind.Husk, 0, 1, hp: 12)
            .Build();

        var archer = state.Find(UnitKind.Archer).Id;
        var beside = state.Units.Single(u => u.Position == new Coord(1, 0)).Id;
        var below = state.Units.Single(u => u.Position == new Coord(0, 1)).Id;
        state = state.WithUnit(state.Get(archer) with { Verve = Verve.Cap });

        // Adjacent is not the sweet spot, so the downhill exception fires at the OUTER band's damage
        // plus the ledge bonus (§4's band arithmetic applied without a special case). What range 1
        // should pay is not a number the doc states — see DECISIONS D-269.
        var template = UnitTemplate.For(UnitKind.Archer);
        int expected = template.OffSpotDamage + Combat.HighGroundBonus;

        var armed = state.Then(new SpendVerveCommand(archer, VerveSpend.DoubleNock));
        Assert.Equal(1, armed.Get(archer).ExtraAttacks);
        Assert.Equal(1, armed.Get(archer).Position.DistanceTo(armed.Get(beside).Position));
        Assert.Equal(1, armed.Get(archer).Position.DistanceTo(armed.Get(below).Position));

        var first = armed.Step(new AttackCommand(archer, beside));
        var attacked = first.Single<UnitAttacked>();
        Assert.True(attacked.FromHighGround);
        Assert.Equal(expected, attacked.Damage);
        Assert.Equal(12 - expected, first.NewState.Get(beside).Hp);

        // The owed attack is gone and the activation is still open: what follows is the second shot.
        var between = first.NewState;
        Assert.Equal(0, between.Get(archer).ExtraAttacks);
        Assert.False(between.Get(archer).HasActivated);

        Assert.True(Combat.CanAttack(between, between.Get(archer), between.Get(below), out int damage));
        Assert.Equal(expected, damage);
        TestPlay.AssertLegal(between, new AttackCommand(archer, below));

        var second = between.Step(new AttackCommand(archer, below));
        var again = second.Single<UnitAttacked>();
        Assert.True(again.FromHighGround);
        Assert.Equal(expected, again.Damage);
        Assert.Equal(12 - expected, second.NewState.Get(below).Hp);
    }

    // And the same half that keeps the basic shot's exception honest keeps the second shot honest:
    // an owed attack is not a licence to fire at somebody standing on the ledge with her.
    [Fact]
    public void DoubleNock_OnTheSameLedge_TheSecondShotIsStillTooClose()
    {
        var state = BoardBuilder.Rows("HH....", "......")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 18)
            .Enemy(UnitKind.Husk, 2, 0, hp: 18)
            .Enemy(UnitKind.Husk, 3, 0, hp: 18)
            .Build();

        var archer = state.Find(UnitKind.Archer).Id;
        var ledgeMate = state.Units.Single(u => u.Position == new Coord(1, 0)).Id;
        var target = state.Units.Single(u => u.Position == new Coord(2, 0)).Id;
        var spare = state.Units.Single(u => u.Position == new Coord(3, 0)).Id;
        state = state.WithUnit(state.Get(archer) with { Verve = Verve.Cap });

        var armed = state.Then(new SpendVerveCommand(archer, VerveSpend.DoubleNock));

        // The first shot refuses him, and spends the owed attack on somebody two tiles out.
        Assert.False(Combat.CanAttack(armed, armed.Get(archer), armed.Get(ledgeMate), out _));
        TestPlay.AssertNotLegal(armed, new AttackCommand(archer, ledgeMate));
        TestPlay.AssertIllegal(armed, new AttackCommand(archer, ledgeMate));

        var between = armed.Then(new AttackCommand(archer, target));
        Assert.Equal(0, between.Get(archer).ExtraAttacks);

        // There is a second shot to take — so the refusal below is the minimum range, not an
        // activation that has run out.
        TestPlay.AssertLegal(between, new AttackCommand(archer, spare));

        Assert.False(Combat.CanAttack(between, between.Get(archer), between.Get(ledgeMate), out _));
        TestPlay.AssertNotLegal(between, new AttackCommand(archer, ledgeMate));
        TestPlay.AssertIllegal(between, new AttackCommand(archer, ledgeMate));
    }

    // Nothing else moved. Stagger Shot is the only ability in the game with a minimum range at all,
    // so lifting it downhill cannot have loosened anything else by accident.
    [Fact]
    public void StaggerShotIsTheOnlyAbilityWithAMinimumRange()
    {
        Assert.Equal(
            new[] { Ability.StaggerShot },
            AbilityDefinition.All().Where(d => d.MinRange > 0).Select(d => d.Ability).ToArray());
    }

    private static GameState Board(int enemyAt) =>
        BoardBuilder.Open(6, 2)
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, enemyAt, 0, hp: 12)
            .Build();
}
