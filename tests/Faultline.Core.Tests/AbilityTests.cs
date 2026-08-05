using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

public class AbilityTests
{
    // --- Descriptors ------------------------------------------------------------------

    // Changed by D-058: the Wardbearer went from one passive ability to two active ones, so "exactly
    // one each" is no longer the rule. Three classes bring one; the Wardbearer brings two.
    [Fact]
    public void EveryPlayerClass_HasAtLeastOneAbility_AndTheWardbearerHasTwo()
    {
        Assert.Equal(5, AbilityDescriptor.All().Count);
        Assert.Equal(Ability.BullRush, AbilityDescriptor.ForKind(UnitKind.Vanguard)!.Ability);
        Assert.Equal(Ability.StaggerShot, AbilityDescriptor.ForKind(UnitKind.Archer)!.Ability);
        Assert.Equal(Ability.Reel, AbilityDescriptor.ForKind(UnitKind.Threadcaster)!.Ability);

        Assert.Equal(
            new[] { Ability.SpearThrust, Ability.GuardStance },
            AbilityDescriptor.AllForKind(UnitKind.Wardbearer).Select(d => d.Ability));

        foreach (var kind in new[] { UnitKind.Vanguard, UnitKind.Archer, UnitKind.Threadcaster })
        {
            Assert.Single(AbilityDescriptor.AllForKind(kind));
        }
    }

    [Fact]
    public void Wardbearer_NoLongerHasAPassiveAbilityAtAll()
    {
        Assert.All(
            AbilityDescriptor.AllForKind(UnitKind.Wardbearer),
            d => Assert.NotEqual(AbilityTargeting.Passive, d.Targeting));
    }

    [Fact]
    public void Enemies_HaveNoAbility()
    {
        foreach (var kind in new[] { UnitKind.Husk, UnitKind.Lobber, UnitKind.Anchor, UnitKind.Grappler, UnitKind.Stalker })
        {
            Assert.Null(AbilityDescriptor.ForKind(kind));
        }
    }

    [Fact]
    public void Descriptors_CarryRulesTextAndNumbersForTheUi()
    {
        foreach (var descriptor in AbilityDescriptor.All())
        {
            Assert.False(string.IsNullOrWhiteSpace(descriptor.Name));
            Assert.False(string.IsNullOrWhiteSpace(descriptor.Summary));
            Assert.False(string.IsNullOrWhiteSpace(descriptor.Effect));
        }

        Assert.Equal("2 dmg · push 1", AbilityDescriptor.For(Ability.StaggerShot).Effect);
        Assert.Equal("push 2", AbilityDescriptor.For(Ability.BullRush).Effect);
        Assert.Equal("pull to adjacent", AbilityDescriptor.For(Ability.Reel).Effect);
        // Per-tile damage prints per tile, nearest first: the ability no longer has one number.
        Assert.Equal("line 2 · 2/4 dmg", AbilityDescriptor.For(Ability.SpearThrust).Effect);
        Assert.Equal(new[] { 2, 4 }, AbilityDescriptor.For(Ability.SpearThrust).TileDamage);
        Assert.Equal(0, AbilityDescriptor.For(Ability.SpearThrust).Push);
        Assert.Equal("stance", AbilityDescriptor.For(Ability.GuardStance).Effect);
    }

    // Replaces Hold_IsPassiveAndNeverOffered, which asserted a rule D-058 deleted. Same fixture,
    // opposite expectation: the Wardbearer now always has something to spend its action on.
    [Fact]
    public void Wardbearer_IsOfferedBothOfItsAbilities()
    {
        var state = BoardBuilder.Open(5, 1)
            .PlayerB(UnitKind.Wardbearer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0)
            .Build();

        var wardbearer = state.Find(UnitKind.Wardbearer);

        Assert.True(Abilities.IsUsable(wardbearer));

        var abilities = Game.LegalCommands(state)
            .OfType<AbilityCommand>()
            .Select(c => c.Ability)
            .ToList();

        Assert.Contains(Ability.SpearThrust, abilities);
        Assert.Contains(Ability.GuardStance, abilities);
    }

    // --- Stagger Shot ------------------------------------------------------------------

    [Fact]
    public void StaggerShot_DealsOneAndPushesTheTargetOneAway()
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 2, 0, hp: 12)
            .Build();

        var archer = state.Find(UnitKind.Archer);
        var husk = state.Find(UnitKind.Husk);

        var result = state.Step(new AbilityCommand(archer.Id, Ability.StaggerShot, husk.Id));

        Assert.Equal(Ability.StaggerShot, result.Single<AbilityUsed>().Ability);
        Assert.Equal(10, result.NewState.Get(husk.Id).Hp);
        Assert.Equal(new Coord(3, 0), result.NewState.Get(husk.Id).Position);
    }

    [Fact]
    public void StaggerShot_IntoAWall_AddsCollisionDamageOnTop()
    {
        var state = BoardBuilder.Rows("...#")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 2, 0, hp: 12)
            .Build();

        var archer = state.Find(UnitKind.Archer);
        var husk = state.Find(UnitKind.Husk);

        var result = state.Step(new AbilityCommand(archer.Id, Ability.StaggerShot, husk.Id));

        // 1 from the shot, 2 from slamming into the wall.
        Assert.Equal(6, result.NewState.Get(husk.Id).Hp);
        Assert.True(result.NewState.Get(husk.Id).Staggered);
    }

    [Fact]
    public void StaggerShot_ThatKillsTheTarget_DoesNotThenPushACorpse()
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 2, 0, hp: 2)
            .Enemy(UnitKind.Anchor, 5, 0)
            .Build();

        var archer = state.Find(UnitKind.Archer);
        var husk = state.Find(UnitKind.Husk);

        var result = state.Step(new AbilityCommand(archer.Id, Ability.StaggerShot, husk.Id));

        Assert.Single(result.All<UnitDowned>());
        Assert.Empty(result.All<UnitPushed>());
    }

    [Fact]
    public void StaggerShot_BeyondRangeThree_IsNotOffered()
    {
        var state = BoardBuilder.Open(8, 1)
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 5, 0)
            .Build();

        var archer = state.Find(UnitKind.Archer);

        Assert.Empty(Abilities.LegalTargets(state, archer));
        TestPlay.AssertIllegal(state, new AbilityCommand(archer.Id, Ability.StaggerShot, state.Find(UnitKind.Husk).Id));
    }

    // --- Reel -------------------------------------------------------------------------

    [Fact]
    public void Reel_PullsTheTargetAllTheWayToAdjacent()
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerB(UnitKind.Threadcaster, 0, 0)
            .Enemy(UnitKind.Husk, 3, 0)
            .Build();

        var caster = state.Find(UnitKind.Threadcaster);
        var husk = state.Find(UnitKind.Husk);

        var result = state.Step(new AbilityCommand(caster.Id, Ability.Reel, husk.Id));

        Assert.Equal(new Coord(1, 0), result.NewState.Get(husk.Id).Position);
        Assert.True(result.NewState.Get(husk.Id).Position.IsAdjacentTo(caster.Position));
    }

    [Fact]
    public void Reel_ResolvesEveryTileOnTheWay_SoItCanDropTheTargetInAPit()
    {
        var state = BoardBuilder.Rows(".O...")
            .PlayerB(UnitKind.Threadcaster, 0, 0)
            .Enemy(UnitKind.Husk, 3, 0, footing: 0)
            .Enemy(UnitKind.Husk, 4, 0)
            .Build();

        var caster = state.Find(UnitKind.Threadcaster);
        var husk = state.Find(UnitKind.Husk);

        var result = state.Step(new AbilityCommand(caster.Id, Ability.Reel, husk.Id));

        Assert.True(result.NewState.Get(husk.Id).Clinging);
        Assert.Equal(new Coord(1, 0), result.NewState.Get(husk.Id).Position);
    }

    [Fact]
    public void Reel_AnEnemyWithFooting_RefusesTheDragOutright()
    {
        // Footing is scenario-granted, never automatic, so the Husk only has a token because this
        // fixture hands it one.
        var state = BoardBuilder.Rows(".O...")
            .PlayerB(UnitKind.Threadcaster, 0, 0)
            .Enemy(UnitKind.Husk, 3, 0, footing: 1)
            .Build();

        var caster = state.Find(UnitKind.Threadcaster);
        var husk = state.Find(UnitKind.Husk);

        var result = state.Step(new AbilityCommand(caster.Id, Ability.Reel, husk.Id));

        // Refusal is not shortening: the drag does not happen at all, so the Husk is exactly where
        // it stood rather than one tile short of the drain.
        Assert.False(result.NewState.Get(husk.Id).Clinging);
        Assert.Equal(new Coord(3, 0), result.NewState.Get(husk.Id).Position);
        Assert.Single(result.All<FootingSpent>());
        Assert.Single(result.All<DisplacementRefused>());
    }

    [Fact]
    public void Reel_IsNotOfferedAgainstAnAlreadyAdjacentTarget()
    {
        var state = BoardBuilder.Open(5, 1)
            .PlayerB(UnitKind.Threadcaster, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0)
            .Build();

        Assert.Empty(Abilities.LegalTargets(state, state.Find(UnitKind.Threadcaster)));
    }

    // --- Bull Rush ---------------------------------------------------------------------

    [Fact]
    public void BullRush_ChargesUpToThreeAndShovesTheFirstEnemyTwo()
    {
        var state = BoardBuilder.Open(8, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 3, 0, hp: 12)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        var husk = state.Find(UnitKind.Husk);

        var result = state.Step(new AbilityCommand(vanguard.Id, Ability.BullRush, null, Direction.Right));

        Assert.Equal(new Coord(2, 0), result.NewState.Get(vanguard.Id).Position);
        Assert.Equal(new Coord(5, 0), result.NewState.Get(husk.Id).Position);
        Assert.True(result.NewState.Get(vanguard.Id).Position.IsAdjacentTo(new Coord(3, 0)));
    }

    [Fact]
    public void BullRush_AgainstAnAdjacentEnemy_ShovesWithoutMoving()
    {
        var state = BoardBuilder.Open(8, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 12)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        var husk = state.Find(UnitKind.Husk);

        var result = state.Step(new AbilityCommand(vanguard.Id, Ability.BullRush, null, Direction.Right));

        Assert.Equal(new Coord(0, 0), result.NewState.Get(vanguard.Id).Position);
        Assert.Equal(new Coord(3, 0), result.NewState.Get(husk.Id).Position);
    }

    [Fact]
    public void BullRush_ConsumesBothHalvesOfTheActivation()
    {
        var state = BoardBuilder.Open(8, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 3, 0, hp: 12)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        var result = state.Step(new AbilityCommand(vanguard.Id, Ability.BullRush, null, Direction.Right));

        Assert.True(result.NewState.Get(vanguard.Id).HasActivated);
        Assert.False(result.Single<ActivationEnded>().Passed);
    }

    [Fact]
    public void BullRush_IsBlockedByAnAllyInTheLine()
    {
        var state = BoardBuilder.Open(8, 2)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .PlayerB(UnitKind.Wardbearer, 1, 0)
            .Enemy(UnitKind.Husk, 3, 0)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        var charge = Abilities.PreviewCharge(state, vanguard, Direction.Right);

        Assert.True(charge.IsNoOp);
        Assert.DoesNotContain(
            Game.LegalCommands(state),
            c => c is AbilityCommand a && a.Direction == Direction.Right);
    }

    [Fact]
    public void BullRush_CannotChargeUpOntoHighGround()
    {
        var state = BoardBuilder.Rows("..H..")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 4, 0)
            .Build();

        var charge = Abilities.PreviewCharge(state, state.Find(UnitKind.Vanguard), Direction.Right);

        Assert.Equal(new Coord(1, 0), charge.Destination);
        Assert.Null(charge.Contact);
    }

    [Fact]
    // D-126, MASTER_DESIGN §3: two of the three points, not the whole pool. The number lives in the
    // cost table and nowhere else, which is what makes the pre-move rules below fall out of it.
    public void BullRush_CostsTwoOfTheThreePoints()
    {
        Assert.Equal(2, Activation.CostOf(Ability.BullRush));
        Assert.Equal(Activation.BullRushCost, Activation.CostOf(Ability.BullRush));
        Assert.True(Activation.CostOf(Ability.BullRush) < Activation.FullPool);
    }

    [Fact]
    // Inverts the old full-pool pin. There was never a rule forbidding the pre-move — only the
    // price — so at 2 the movement-first grammar allows one tile of run-up with nothing added.
    public void BullRush_AfterOneTileOfWalking_IsLegalAndStillCharges()
    {
        var state = BoardBuilder.Open(9, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 4, 0, hp: 12)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        var husk = state.Find(UnitKind.Husk);

        var walked = state.Then(new MoveCommand(vanguard.Id, new Coord(1, 0)));
        Assert.Equal(Activation.CostOf(Ability.BullRush), Activation.Remaining(walked.Get(vanguard.Id)));

        var charge = new AbilityCommand(vanguard.Id, Ability.BullRush, null, Direction.Right);
        TestPlay.AssertLegal(walked, charge);

        var result = walked.Step(charge);

        // Charge mechanics untouched: up to 3 in a line, stop adjacent, first enemy pushed 2.
        Assert.Equal(new Coord(3, 0), result.NewState.Get(vanguard.Id).Position);
        Assert.Equal(new Coord(6, 0), result.NewState.Get(husk.Id).Position);
    }

    [Fact]
    // The other side of the same price: 3 - 2 = 1, and 1 < 2. Two tiles of walking is still enough
    // to lose the charge, which is what keeps the threat range at 4 rather than 5.
    public void BullRush_AfterTwoTilesOfWalking_IsUnaffordable()
    {
        var state = BoardBuilder.Open(9, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 5, 0, hp: 12)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);

        var walked = state.Then(new MoveCommand(vanguard.Id, new Coord(2, 0)));
        var moved = walked.Get(vanguard.Id);

        Assert.Equal(1, Activation.Remaining(moved));
        Assert.False(Activation.CanAfford(moved, Activation.CostOf(Ability.BullRush)));
        Assert.Equal(1, Activation.Shortfall(moved, Activation.CostOf(Ability.BullRush)));

        var charge = new AbilityCommand(vanguard.Id, Ability.BullRush, null, Direction.Right);
        TestPlay.AssertNotLegal(walked, charge);
        TestPlay.AssertIllegal(walked, charge);
    }

    [Fact]
    // The chaser's reach, deliberately: one past his walk, one short of the Archer's shot band.
    public void BullRush_ThreatensFourTiles_OneOfWalkAndThreeOfCharge()
    {
        var atFour = BoardBuilder.Open(10, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 4, 0, hp: 12)
            .Build();

        var vanguard = atFour.Find(UnitKind.Vanguard);
        var reached = atFour.Find(UnitKind.Husk);

        var hit = atFour
            .Then(new MoveCommand(vanguard.Id, new Coord(1, 0)))
            .Step(new AbilityCommand(vanguard.Id, Ability.BullRush, null, Direction.Right));

        Assert.True(hit.Has<UnitPushed>());
        Assert.Equal(new Coord(6, 0), hit.NewState.Get(reached.Id).Position);

        var atFive = BoardBuilder.Open(10, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 5, 0, hp: 12)
            .Build();

        var chaser = atFive.Find(UnitKind.Vanguard);
        var safe = atFive.Find(UnitKind.Husk);

        // One tile of walk is all the pool affords, and three of charge stops a tile short. The
        // reposition itself is still legal — the game never decides what is useful — it simply
        // touches nobody.
        var missed = atFive
            .Then(new MoveCommand(chaser.Id, new Coord(1, 0)))
            .Step(new AbilityCommand(chaser.Id, Ability.BullRush, null, Direction.Right));

        Assert.False(missed.Has<UnitPushed>());
        Assert.Equal(new Coord(5, 0), missed.NewState.Get(safe.Id).Position);
        Assert.Equal(new Coord(4, 0), missed.NewState.Get(chaser.Id).Position);
    }

    [Fact]
    public void BullRush_ShovingAnEnemyIntoAPit_LeavesItClinging()
    {
        var state = BoardBuilder.Rows("....O.")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 2, 0, footing: 0)
            .Enemy(UnitKind.Husk, 5, 0)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        var husk = state.Find(UnitKind.Husk);

        var result = state.Step(new AbilityCommand(vanguard.Id, Ability.BullRush, null, Direction.Right));

        Assert.True(result.NewState.Get(husk.Id).Clinging);
    }

    // --- Basic attack changes M2 brings ------------------------------------------------

    [Fact]
    public void VanguardBasicAttack_DealsOneAndPushesOne()
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 12)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        var husk = state.Find(UnitKind.Husk);

        var result = state.Step(new AttackCommand(vanguard.Id, husk.Id));

        Assert.Equal(10, result.NewState.Get(husk.Id).Hp);
        Assert.Equal(new Coord(2, 0), result.NewState.Get(husk.Id).Position);
    }

    [Fact]
    public void ThreadcasterBasicAttack_MayPullOneInsteadOfDealingDamage()
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerB(UnitKind.Threadcaster, 0, 0)
            .Enemy(UnitKind.Husk, 3, 0)
            .Build();

        var caster = state.Find(UnitKind.Threadcaster);
        var husk = state.Find(UnitKind.Husk);

        TestPlay.AssertLegal(state, new AttackCommand(caster.Id, husk.Id, AttackMode.Pull));
        var result = state.Step(new AttackCommand(caster.Id, husk.Id, AttackMode.Pull));

        Assert.Equal(new Coord(2, 0), result.NewState.Get(husk.Id).Position);
        Assert.Equal(husk.Hp, result.NewState.Get(husk.Id).Hp);
    }

    [Fact]
    public void OnlyTheThreadcaster_IsOfferedTheBasicPull()
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 2, 0)
            .Build();

        Assert.DoesNotContain(
            Game.LegalCommands(state),
            c => c is AttackCommand a && a.Mode == AttackMode.Pull);
    }

    // --- Preview fidelity ---------------------------------------------------------------

    [Fact]
    public void Preview_MatchesWhatResolveActuallyDoes()
    {
        var state = BoardBuilder.Rows("...#..")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 12)
            .Build();

        var husk = state.Find(UnitKind.Husk);
        var preview = Displacement.Preview(state, husk.Id, new Coord(0, 0), DisplacementKind.Push, 3);

        var events = new System.Collections.Generic.List<GameEvent>();
        var after = Displacement.Resolve(state, husk.Id, new Coord(0, 0), DisplacementKind.Push, 3, false, events);

        Assert.Equal(preview.Destination, after.Get(husk.Id).Position);
        Assert.Equal(preview.DamageToUnit, husk.Hp - after.Get(husk.Id).Hp);
        Assert.Equal(preview.WouldStagger, after.Get(husk.Id).Staggered);
        Assert.Equal(DisplacementStop.Collision, preview.Stop);
    }

    [Fact]
    public void Preview_ReportsAPitOutcomeBeforeItHappens()
    {
        // FootingWouldMatter is only true for a unit that has a token, and a token is something a
        // scenario granted — no archetype starts with one.
        var state = BoardBuilder.Rows("...O.")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, footing: 1)
            .Build();

        var preview = Displacement.Preview(
            state, state.Find(UnitKind.Husk).Id, new Coord(0, 0), DisplacementKind.Push, 2);

        Assert.Equal(DisplacementStop.Pit, preview.Stop);
        Assert.True(preview.WouldCling);
        Assert.True(preview.FootingWouldMatter);
    }

    [Fact]
    public void Preview_FlagsWhenADisplacementWouldDownTheTarget()
    {
        var state = BoardBuilder.Rows("..#")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 4)
            .Build();

        var preview = Displacement.Preview(
            state, state.Find(UnitKind.Husk).Id, new Coord(0, 0), DisplacementKind.Push, 1);

        Assert.True(preview.WouldDown);
        Assert.Equal(4, preview.DamageToUnit);
    }

    [Fact]
    public void Preview_AccountsForTheFootingTheDefenderWillActuallySpend()
    {
        // Without modelling the enemy's automatic Footing spend, a preview would promise a pit kill
        // the rules then refuse to deliver. Found by playing the shell, so it is pinned here.
        var state = BoardBuilder.Rows("...O.")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, footing: 1)
            .Build();

        var husk = state.Find(UnitKind.Husk);

        var naive = Displacement.Preview(state, husk.Id, new Coord(0, 0), DisplacementKind.Push, 2);
        var honest = Displacement.PreviewAuto(state, husk.Id, new Coord(0, 0), DisplacementKind.Push, 2);

        Assert.Equal(DisplacementStop.Pit, naive.Stop);
        Assert.NotEqual(DisplacementStop.Pit, honest.Stop);

        var events = new System.Collections.Generic.List<GameEvent>();
        var after = Displacement.ResolveAuto(state, husk.Id, new Coord(0, 0), DisplacementKind.Push, 2, events);

        Assert.Equal(honest.Destination, after.Get(husk.Id).Position);
        Assert.Equal(honest.WouldCling, after.Get(husk.Id).Clinging);
    }

    [Theory]
    [InlineData("...O.")]
    [InlineData("....#")]
    [InlineData("...^.")]
    [InlineData(".....")]
    public void Preview_AlwaysMatchesResolve_AcrossTerrain(string layout)
    {
        var state = BoardBuilder.Rows(layout)
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 18)
            .Build();

        var husk = state.Find(UnitKind.Husk);
        var preview = Displacement.PreviewAuto(state, husk.Id, new Coord(0, 0), DisplacementKind.Push, 3);

        var events = new System.Collections.Generic.List<GameEvent>();
        var after = Displacement.ResolveAuto(state, husk.Id, new Coord(0, 0), DisplacementKind.Push, 3, events);
        var moved = after.Get(husk.Id);

        Assert.Equal(preview.Destination, moved.Position);
        Assert.Equal(preview.DamageToUnit, husk.Hp - moved.Hp);
        Assert.Equal(preview.WouldCling, moved.Clinging);
        Assert.Equal(preview.WouldStagger, moved.Staggered);
    }

    [Fact]
    public void RangeTiles_CoverTheAbilityReach()
    {
        var state = BoardBuilder.Open(7, 7)
            .PlayerA(UnitKind.Archer, 3, 3)
            .Enemy(UnitKind.Husk, 6, 6)
            .Build();

        var tiles = Abilities.RangeTiles(state, state.Find(UnitKind.Archer));

        Assert.All(tiles, t => Assert.InRange(t.DistanceTo(new Coord(3, 3)), 1, 3));
        Assert.Contains(new Coord(3, 0), tiles);
        Assert.DoesNotContain(new Coord(3, 7), tiles);
    }
}
