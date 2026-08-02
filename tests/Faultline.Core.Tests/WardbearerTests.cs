using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// The Wardbearer's kit as D-058 rewrote it: no hold aura, 7 HP behind push resistance 2, and a
/// choice each activation between the basic attack, Spear Thrust and Guard Stance.
/// </summary>
public class WardbearerTests
{
    // ---- stat block --------------------------------------------------------------------

    [Fact]
    public void Wardbearer_HasSevenHitPointsAndPushResistanceTwo()
    {
        var template = UnitTemplate.For(UnitKind.Wardbearer);

        Assert.Equal(7, template.MaxHp);
        Assert.Equal(2, template.PushResistance);
        Assert.False(template.HoldAura);
    }

    [Fact]
    public void Wardbearer_ReusesTheSamePushResistanceArithmeticAsTheColossus()
    {
        Assert.Equal(
            UnitTemplate.For(UnitKind.Colossus).PushResistance,
            UnitTemplate.For(UnitKind.Wardbearer).PushResistance);
    }

    // D-057: a shove reduced to nothing is still a shove, and still says so.
    [Fact]
    public void PushResistance_AShoveOfOne_MovesItNowhereAndIsStillReportedAtDistanceZero()
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerB(UnitKind.Wardbearer, 2, 0)
            .Enemy(UnitKind.Stalker, 1, 0)
            .Build();

        var wardbearer = state.Find(UnitKind.Wardbearer);
        var stalker = state.Find(UnitKind.Stalker);

        var result = EnemyTurn(state).Step(new AttackCommand(stalker.Id, wardbearer.Id, AttackMode.Push));

        Assert.Equal(new Coord(2, 0), result.NewState.Get(wardbearer.Id).Position);

        var pushed = result.Single<UnitPushed>();
        Assert.Equal(wardbearer.Id, pushed.UnitId);
        Assert.Equal(0, pushed.Distance);
        Assert.Empty(pushed.Path);
    }

    [Theory]
    [InlineData(1, false, 0)]
    [InlineData(2, false, 0)]
    [InlineData(3, false, 1)]
    [InlineData(1, true, 0)]
    [InlineData(2, true, 1)]
    [InlineData(3, true, 2)]
    public void PushResistance_StaggerStacksOnTopOfIt(int requested, bool staggered, int expected)
    {
        var state = BoardBuilder.Open(8, 1)
            .PlayerB(UnitKind.Wardbearer, 3, 0)
            .Enemy(UnitKind.Husk, 0, 0)
            .Build();

        var wardbearer = state.Find(UnitKind.Wardbearer);
        state = state.WithUnit(state.Get(wardbearer.Id) with { Staggered = staggered });

        int distance = Displacement.EffectiveDistance(
            state, state.Get(wardbearer.Id), DisplacementKind.Push, requested, false, out _);

        Assert.Equal(expected, distance);
    }

    [Fact]
    public void PushResistance_DoesNotTouchPull()
    {
        var state = BoardBuilder.Open(8, 1)
            .PlayerB(UnitKind.Wardbearer, 3, 0)
            .Enemy(UnitKind.Grappler, 0, 0)
            .Build();

        var wardbearer = state.Get(state.Find(UnitKind.Wardbearer).Id);

        Assert.Equal(
            2, Displacement.EffectiveDistance(state, wardbearer, DisplacementKind.Pull, 2, false, out _));
    }

    // ---- Spear Thrust ------------------------------------------------------------------

    [Fact]
    public void SpearThrust_HitsBothTilesAhead_ForOneDamageAndPushOneEach()
    {
        var state = BoardBuilder.Open(8, 1)
            .PlayerB(UnitKind.Wardbearer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 6)
            .Enemy(UnitKind.Husk, 2, 0, hp: 6)
            .Build();

        var wardbearer = state.Find(UnitKind.Wardbearer);
        var near = state.Units[1];
        var far = state.Units[2];

        var result = state.Step(new AbilityCommand(wardbearer.Id, Ability.SpearThrust, null, Direction.Right));

        Assert.Equal(5, result.NewState.Get(near.Id).Hp);
        Assert.Equal(5, result.NewState.Get(far.Id).Hp);
        Assert.Equal(new Coord(2, 0), result.NewState.Get(near.Id).Position);
        Assert.Equal(new Coord(3, 0), result.NewState.Get(far.Id).Position);
    }

    // The rule the resolution order exists for: the far target vacates its tile before the near one
    // is shoved, so the near one walks into it instead of slamming into it.
    [Fact]
    public void SpearThrust_ResolvesTheFarTargetFirst_SoTheNearOneFollowsIntoTheTileItLeft()
    {
        var state = BoardBuilder.Open(8, 1)
            .PlayerB(UnitKind.Wardbearer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 6)
            .Enemy(UnitKind.Husk, 2, 0, hp: 6)
            .Build();

        var wardbearer = state.Find(UnitKind.Wardbearer);
        var far = state.Units[2];

        var result = state.Step(new AbilityCommand(wardbearer.Id, Ability.SpearThrust, null, Direction.Right));

        // Near-first would have shoved the near Husk into the far one for 2 apiece.
        Assert.False(result.Has<Collision>());

        // And the far target's shove is reported before the near target's.
        var pushes = result.All<UnitPushed>();
        Assert.Equal(2, pushes.Count);
        Assert.Equal(far.Id, pushes[0].UnitId);
    }

    // The other half of the same rule: a far target that could not move is the wall the near one
    // collides with.
    [Fact]
    public void SpearThrust_WhenTheFarTargetIsBlocked_TheNearOneCollidesIntoIt()
    {
        var state = BoardBuilder.Rows("...#")
            .PlayerB(UnitKind.Wardbearer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 6)
            .Enemy(UnitKind.Husk, 2, 0, hp: 6)
            .Build();

        var wardbearer = state.Find(UnitKind.Wardbearer);
        var near = state.Units[1];
        var far = state.Units[2];

        var result = state.Step(new AbilityCommand(wardbearer.Id, Ability.SpearThrust, null, Direction.Right));

        // Far: 1 from the thrust, 2 into the wall. Near: 1 from the thrust, 2 into the far Husk,
        // which takes another 2 for being collided with.
        Assert.Equal(new Coord(2, 0), result.NewState.Get(far.Id).Position);
        Assert.Equal(new Coord(1, 0), result.NewState.Get(near.Id).Position);
        Assert.Equal(1, result.NewState.Get(far.Id).Hp);
        Assert.Equal(3, result.NewState.Get(near.Id).Hp);
        Assert.Equal(2, result.All<Collision>().Count);
        Assert.True(result.NewState.Get(near.Id).Staggered);
        Assert.True(result.NewState.Get(far.Id).Staggered);
    }

    [Fact]
    public void SpearThrust_KillsTheFarTarget_AndTheNearOneTakesItsTile()
    {
        var state = BoardBuilder.Open(8, 1)
            .PlayerB(UnitKind.Wardbearer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 6)
            .Enemy(UnitKind.Runt, 2, 0)
            .Build();

        var wardbearer = state.Find(UnitKind.Wardbearer);
        var near = state.Units[1];
        var runt = state.Find(UnitKind.Runt);

        var result = state.Step(new AbilityCommand(wardbearer.Id, Ability.SpearThrust, null, Direction.Right));

        Assert.False(result.NewState.Get(runt.Id).IsOnBoard);
        Assert.Equal(new Coord(2, 0), result.NewState.Get(near.Id).Position);
    }

    [Fact]
    public void SpearThrust_HitsOnlyEnemies_AnAllyOnTheLineIsUntouched()
    {
        var state = BoardBuilder.Open(8, 1)
            .PlayerB(UnitKind.Wardbearer, 0, 0)
            .PlayerA(UnitKind.Archer, 1, 0)
            .Enemy(UnitKind.Husk, 2, 0, hp: 6)
            .Build();

        var wardbearer = state.Find(UnitKind.Wardbearer);
        var archer = state.Find(UnitKind.Archer);
        var husk = state.Find(UnitKind.Husk);

        var result = state.Step(new AbilityCommand(wardbearer.Id, Ability.SpearThrust, null, Direction.Right));

        Assert.Equal(archer.Hp, result.NewState.Get(archer.Id).Hp);
        Assert.Equal(new Coord(1, 0), result.NewState.Get(archer.Id).Position);
        Assert.Equal(5, result.NewState.Get(husk.Id).Hp);
    }

    // D-010: there is no line of sight in this game, and Spear Thrust is a fixed shape rather than a
    // ray-cast. Terrain on the near tile does not shield the far one.
    [Fact]
    public void SpearThrust_HasNoLineOfSight_AWallOnTheNearTileDoesNotShieldTheFarOne()
    {
        var state = BoardBuilder.Rows(".#..")
            .PlayerB(UnitKind.Wardbearer, 0, 0)
            .Enemy(UnitKind.Husk, 2, 0, hp: 6)
            .Build();

        var wardbearer = state.Find(UnitKind.Wardbearer);
        var husk = state.Find(UnitKind.Husk);

        var result = state.Step(new AbilityCommand(wardbearer.Id, Ability.SpearThrust, null, Direction.Right));

        Assert.Equal(5, result.NewState.Get(husk.Id).Hp);
        Assert.Equal(new Coord(3, 0), result.NewState.Get(husk.Id).Position);
    }

    [Fact]
    public void SpearThrust_DownAnEmptyLine_IsNotOffered()
    {
        var state = BoardBuilder.Open(6, 3)
            .PlayerB(UnitKind.Wardbearer, 0, 1)
            .Enemy(UnitKind.Husk, 1, 1)
            .Build();

        var wardbearer = state.Find(UnitKind.Wardbearer);
        var descriptor = Abilities.DescriptorFor(wardbearer, Ability.SpearThrust);

        Assert.Equal(new[] { Direction.Right }, Abilities.LegalLines(state, wardbearer, descriptor));
        TestPlay.AssertIllegal(
            state, new AbilityCommand(wardbearer.Id, Ability.SpearThrust, null, Direction.Down));
    }

    [Fact]
    public void SpearThrust_CostsTheActionOnly_SoTheWardbearerMayStillMove()
    {
        var state = BoardBuilder.Open(8, 1)
            .PlayerB(UnitKind.Wardbearer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 6)
            .Build();

        var wardbearer = state.Find(UnitKind.Wardbearer);

        var result = state.Step(new AbilityCommand(wardbearer.Id, Ability.SpearThrust, null, Direction.Right));

        Assert.False(result.Has<ActivationEnded>());
        Assert.False(result.NewState.Get(wardbearer.Id).HasMoved);
        Assert.True(result.NewState.Get(wardbearer.Id).HasActed);
        Assert.Contains(Game.LegalCommands(result.NewState), c => c is MoveCommand);
    }

    [Fact]
    public void SpearThrust_Preview_MatchesWhatResolveActuallyDoes()
    {
        var state = BoardBuilder.Rows("...#")
            .PlayerB(UnitKind.Wardbearer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 6)
            .Enemy(UnitKind.Husk, 2, 0, hp: 6)
            .Build();

        var wardbearer = state.Find(UnitKind.Wardbearer);

        var previews = Abilities.PreviewLine(state, wardbearer, Direction.Right);
        var after = state.Then(new AbilityCommand(wardbearer.Id, Ability.SpearThrust, null, Direction.Right));

        Assert.Equal(2, previews.Count);
        foreach (var preview in previews)
        {
            Assert.Equal(preview.Destination, after.Get(preview.UnitId).Position);
        }
    }

    // ---- activation economy ------------------------------------------------------------

    [Fact]
    public void Wardbearer_PicksOneOfThreeEachActivation()
    {
        var state = BoardBuilder.Open(8, 1)
            .PlayerB(UnitKind.Wardbearer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 6)
            .Build();

        var wardbearer = state.Find(UnitKind.Wardbearer);
        var husk = state.Find(UnitKind.Husk);

        var opening = Game.LegalCommands(state);
        Assert.Contains(opening, c => c is AttackCommand a && a.UnitId == wardbearer.Id);
        Assert.Contains(opening, c => c is AbilityCommand a && a.Ability == Ability.SpearThrust);
        Assert.Contains(opening, c => c is AbilityCommand a && a.Ability == Ability.GuardStance);

        // Spending the action on one of them takes the other two off the table.
        var after = state.Then(new AbilityCommand(wardbearer.Id, Ability.GuardStance));
        var left = Game.LegalCommands(after);

        Assert.DoesNotContain(left, c => c is AbilityCommand);
        Assert.DoesNotContain(left, c => c is AttackCommand);
        TestPlay.AssertIllegal(after, new AttackCommand(wardbearer.Id, husk.Id));
    }

    // ---- Guard Stance: the stance itself ------------------------------------------------

    [Fact]
    public void GuardStance_CostsTheActionHalfOnly_UnlikeBullRush()
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerB(UnitKind.Wardbearer, 0, 0)
            .Enemy(UnitKind.Husk, 5, 0)
            .Build();

        var wardbearer = state.Find(UnitKind.Wardbearer);

        var result = state.Step(new AbilityCommand(wardbearer.Id, Ability.GuardStance));

        Assert.False(result.Has<ActivationEnded>());
        Assert.False(result.NewState.Get(wardbearer.Id).HasMoved);
        Assert.True(result.NewState.Get(wardbearer.Id).HasActed);
        Assert.Contains(Game.LegalCommands(result.NewState), c => c is MoveCommand);
    }

    [Fact]
    public void GuardStance_IsAVisibleFlagOnTheUnit_AndAnnouncesItself()
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerB(UnitKind.Wardbearer, 2, 0)
            .Enemy(UnitKind.Husk, 5, 0)
            .Build();

        var wardbearer = state.Find(UnitKind.Wardbearer);

        var result = state.Step(new AbilityCommand(wardbearer.Id, Ability.GuardStance));

        Assert.True(result.NewState.Get(wardbearer.Id).Guarding);

        var announced = result.Single<GuardStanceChanged>();
        Assert.Equal(wardbearer.Id, announced.UnitId);
        Assert.Equal(new Coord(2, 0), announced.At);
        Assert.True(announced.Active);
    }

    [Fact]
    public void GuardStance_SurvivesTheRoundBoundary_AndExpiresAtTheWardbearersNextActivation()
    {
        var state = BoardBuilder.Open(6, 3)
            .PlayerB(UnitKind.Wardbearer, 0, 0)
            .Enemy(UnitKind.Warden, 5, 2)
            .Build();

        var wardbearer = state.Find(UnitKind.Wardbearer);
        var warden = state.Find(UnitKind.Warden);

        state = state.Then(new AbilityCommand(wardbearer.Id, Ability.GuardStance));
        Assert.True(state.Get(wardbearer.Id).Guarding);

        state = state.Then(new EndActivationCommand(wardbearer.Id));
        Assert.True(state.Get(wardbearer.Id).Guarding);

        // The round turns over and it is still standing guard: the stance covers the enemy round it
        // was declared to cover, which is the point of it not clearing at end of round.
        state = state.Then(new EndActivationCommand(warden.Id));
        Assert.Equal(2, state.Round);
        Assert.True(state.Get(wardbearer.Id).Guarding);

        var result = state.Step(new EndActivationCommand(wardbearer.Id));

        Assert.False(result.NewState.Get(wardbearer.Id).Guarding);
        Assert.False(result.Single<GuardStanceChanged>().Active);
    }

    [Fact]
    public void GuardStance_OnceExpired_TheAllyTakesItsOwnHitsAgain()
    {
        var state = GuardedArcher();
        var wardbearer = state.Find(UnitKind.Wardbearer);
        var archer = state.Find(UnitKind.Archer);
        var husk = state.Find(UnitKind.Husk);

        var lapsed = state.WithUnit(state.Get(wardbearer.Id) with { Guarding = false });
        var result = EnemyTurn(lapsed).Step(new AttackCommand(husk.Id, archer.Id));

        Assert.Equal(archer.Hp - 1, result.NewState.Get(archer.Id).Hp);
        Assert.Equal(wardbearer.Hp, result.NewState.Get(wardbearer.Id).Hp);
        Assert.False(result.Has<GuardIntercepted>());
    }

    // ---- Guard Stance: redirect --------------------------------------------------------

    [Fact]
    public void GuardStance_RedirectsAnAttackOnAnAdjacentAlly_ToTheWardbearer()
    {
        var state = GuardedArcher();
        var wardbearer = state.Find(UnitKind.Wardbearer);
        var archer = state.Find(UnitKind.Archer);
        var husk = state.Find(UnitKind.Husk);

        var result = EnemyTurn(state).Step(new AttackCommand(husk.Id, archer.Id));

        Assert.Equal(archer.Hp, result.NewState.Get(archer.Id).Hp);
        Assert.Equal(6, result.NewState.Get(wardbearer.Id).Hp);

        var intercepted = result.Single<GuardIntercepted>();
        Assert.Equal(wardbearer.Id, intercepted.UnitId);
        Assert.Equal(archer.Id, intercepted.AllyId);
        Assert.Equal(husk.Id, intercepted.AttackerId);
        Assert.Equal(wardbearer.Position, intercepted.At);
        Assert.Equal(archer.Position, intercepted.AllyAt);

        Assert.Equal(wardbearer.Id, result.Single<UnitAttacked>().TargetId);
    }

    // The vector is preserved and re-applied from the guard's own tile: an ally about to be dragged
    // east ends up with the guard travelling east, from where the guard stands.
    [Fact]
    public void GuardStance_RedirectsADisplacement_PreservingTheVectorFromItsOwnTile()
    {
        var state = BoardBuilder.Open(8, 2)
            .PlayerB(UnitKind.Wardbearer, 2, 1)
            .PlayerA(UnitKind.Archer, 2, 0)
            .Enemy(UnitKind.Grappler, 5, 0)
            .Build();

        var wardbearer = state.Find(UnitKind.Wardbearer);
        var archer = state.Find(UnitKind.Archer);
        var grappler = state.Find(UnitKind.Grappler);

        state = state.WithUnit(state.Get(wardbearer.Id) with { Guarding = true });

        var result = EnemyTurn(state).Step(new AttackCommand(grappler.Id, archer.Id, AttackMode.Pull));

        // The Archer would have been pulled east toward the Grappler. The Wardbearer travels east
        // instead — along its own row, not into the Grappler's.
        Assert.Equal(new Coord(2, 0), result.NewState.Get(archer.Id).Position);
        Assert.Equal(new Coord(4, 1), result.NewState.Get(wardbearer.Id).Position);

        var pushed = result.Single<UnitPushed>();
        Assert.Equal(wardbearer.Id, pushed.UnitId);
        Assert.Equal(DisplacementKind.Pull, pushed.Kind);
        Assert.Equal(2, pushed.Distance);
        Assert.Equal(new[] { new Coord(3, 1), new Coord(4, 1) }, pushed.Path);
    }

    [Fact]
    public void GuardStance_RedirectedShove_ObeysItsOwnPushResistanceAndStagger()
    {
        var state = BoardBuilder.Open(6, 2)
            .PlayerB(UnitKind.Wardbearer, 1, 1)
            .PlayerA(UnitKind.Archer, 1, 0)
            .Enemy(UnitKind.Stalker, 0, 0)
            .Build();

        var wardbearer = state.Find(UnitKind.Wardbearer);
        var archer = state.Find(UnitKind.Archer);
        var stalker = state.Find(UnitKind.Stalker);

        state = state.WithUnit(state.Get(wardbearer.Id) with { Guarding = true, Staggered = true });

        var result = EnemyTurn(state).Step(new AttackCommand(stalker.Id, archer.Id, AttackMode.Push));

        // Push 1, +1 for the Stagger, -2 for the resistance: nothing moves, and the Stagger is spent
        // paying for it. The shove is still reported, at the distance it actually travelled (D-057).
        Assert.Equal(new Coord(1, 1), result.NewState.Get(wardbearer.Id).Position);
        Assert.Equal(new Coord(1, 0), result.NewState.Get(archer.Id).Position);
        Assert.False(result.NewState.Get(wardbearer.Id).Staggered);

        var pushed = result.Single<UnitPushed>();
        Assert.Equal(wardbearer.Id, pushed.UnitId);
        Assert.Equal(0, pushed.Distance);
        Assert.Empty(pushed.Path);
    }

    [Fact]
    public void GuardStance_SeveralRedirectsInOneRound_AllLandOnTheWardbearer()
    {
        var state = BoardBuilder.Open(5, 3)
            .PlayerB(UnitKind.Wardbearer, 1, 1)
            .PlayerA(UnitKind.Archer, 2, 1)
            .Enemy(UnitKind.Husk, 2, 0)
            .Enemy(UnitKind.Husk, 2, 2)
            .Build();

        var wardbearer = state.Find(UnitKind.Wardbearer);
        var archer = state.Find(UnitKind.Archer);
        var first = state.Units[2];
        var second = state.Units[3];

        state = EnemyTurn(state.WithUnit(state.Get(wardbearer.Id) with { Guarding = true }));

        state = state.Then(new AttackCommand(first.Id, archer.Id));
        state = state.Then(new EndActivationCommand(first.Id));
        var result = state.Step(new AttackCommand(second.Id, archer.Id));

        Assert.Equal(archer.Hp, result.NewState.Get(archer.Id).Hp);
        Assert.Equal(5, result.NewState.Get(wardbearer.Id).Hp);
    }

    [Fact]
    public void GuardStance_DoesNotCoverAnAllyItIsNotAdjacentTo()
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerB(UnitKind.Wardbearer, 0, 0)
            .PlayerA(UnitKind.Archer, 3, 0)
            .Enemy(UnitKind.Husk, 4, 0)
            .Build();

        var wardbearer = state.Find(UnitKind.Wardbearer);
        var archer = state.Find(UnitKind.Archer);
        var husk = state.Find(UnitKind.Husk);

        state = state.WithUnit(state.Get(wardbearer.Id) with { Guarding = true });

        var result = EnemyTurn(state).Step(new AttackCommand(husk.Id, archer.Id));

        Assert.Equal(archer.Hp - 1, result.NewState.Get(archer.Id).Hp);
        Assert.Equal(wardbearer.Hp, result.NewState.Get(wardbearer.Id).Hp);
    }

    [Fact]
    public void GuardStance_DoesNotCoverTheOtherSide()
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Archer, 0, 0)
            .PlayerB(UnitKind.Wardbearer, 1, 0)
            .Enemy(UnitKind.Husk, 2, 0, hp: 6)
            .Build();

        var wardbearer = state.Find(UnitKind.Wardbearer);
        var archer = state.Find(UnitKind.Archer);
        var husk = state.Find(UnitKind.Husk);

        state = state.WithUnit(state.Get(wardbearer.Id) with { Guarding = true });

        var result = state.Step(new AttackCommand(archer.Id, husk.Id));

        Assert.Equal(4, result.NewState.Get(husk.Id).Hp);
        Assert.Equal(wardbearer.Hp, result.NewState.Get(wardbearer.Id).Hp);
        Assert.False(result.Has<GuardIntercepted>());
    }

    [Fact]
    public void GuardStance_AGuardThatIsClinging_StopsIntercepting()
    {
        var state = GuardedArcher();
        var wardbearer = state.Find(UnitKind.Wardbearer);
        var archer = state.Find(UnitKind.Archer);
        var husk = state.Find(UnitKind.Husk);

        state = state.WithUnit(state.Get(wardbearer.Id) with { Clinging = true });

        var result = EnemyTurn(state).Step(new AttackCommand(husk.Id, archer.Id));

        Assert.Equal(archer.Hp - 1, result.NewState.Get(archer.Id).Hp);
        Assert.False(result.Has<GuardIntercepted>());
    }

    // ---- Guard Stance: mitigation ------------------------------------------------------

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(3, 2)]
    [InlineData(4, 2)]
    [InlineData(5, 3)]
    [InlineData(6, 3)]
    public void GuardStance_HalvesAttackDamage_RoundedUpMinimumOne(int dealt, int landed)
    {
        var state = GuardingWardbearer();
        var wardbearer = state.Find(UnitKind.Wardbearer);

        var events = new List<GameEvent>();
        var after = Combat.ApplyDamage(state, wardbearer.Id, dealt, DamageSource.Attack, events);

        Assert.Equal(7 - landed, after.Get(wardbearer.Id).Hp);
        Assert.Equal(landed, events.OfType<UnitDamaged>().Single().Amount);
    }

    // Called out on its own because rounding up is the whole ruling and 4 is where a reader expects
    // it to come out at 2 rather than at 3.
    [Fact]
    public void GuardStance_AFourDamageAttack_LandsAsExactlyTwo()
    {
        var state = GuardingWardbearer();
        var wardbearer = state.Find(UnitKind.Wardbearer);

        var events = new List<GameEvent>();
        var after = Combat.ApplyDamage(state, wardbearer.Id, 4, DamageSource.Attack, events);

        Assert.Equal(2, wardbearer.Hp - after.Get(wardbearer.Id).Hp);
    }

    [Theory]
    [InlineData(DamageSource.Collision)]
    [InlineData(DamageSource.Spikes)]
    [InlineData(DamageSource.Fall)]
    public void GuardStance_NeverMitigatesImpactDamage(DamageSource source)
    {
        var state = GuardingWardbearer();
        var wardbearer = state.Find(UnitKind.Wardbearer);

        var events = new List<GameEvent>();
        var after = Combat.ApplyDamage(state, wardbearer.Id, 4, source, events);

        Assert.Equal(4, wardbearer.Hp - after.Get(wardbearer.Id).Hp);
    }

    // The same rule on a real board: the redirect drags the guard into a wall and the wall is not
    // impressed by the stance.
    [Fact]
    public void GuardStance_ACollisionSufferedDuringTheStance_IsUnreduced()
    {
        var state = BoardBuilder.Rows(".......", "....#..")
            .PlayerB(UnitKind.Wardbearer, 3, 1)
            .PlayerA(UnitKind.Archer, 3, 0)
            .Enemy(UnitKind.Grappler, 6, 0)
            .Build();

        var wardbearer = state.Find(UnitKind.Wardbearer);
        var archer = state.Find(UnitKind.Archer);
        var grappler = state.Find(UnitKind.Grappler);

        state = state.WithUnit(state.Get(wardbearer.Id) with { Guarding = true });

        var result = EnemyTurn(state).Step(new AttackCommand(grappler.Id, archer.Id, AttackMode.Pull));

        Assert.Equal(new Coord(3, 1), result.NewState.Get(wardbearer.Id).Position);
        Assert.Equal(5, result.NewState.Get(wardbearer.Id).Hp);
        Assert.True(result.NewState.Get(wardbearer.Id).Staggered);
        Assert.Equal(Displacement.CollisionDamage, result.Single<Collision>().Damage);
    }

    // ---- Guard Stance: dying for it ----------------------------------------------------

    [Fact]
    public void GuardStance_ARedirectedAttackCanDownTheWardbearer()
    {
        var state = BoardBuilder.Open(4, 2)
            .PlayerB(UnitKind.Wardbearer, 1, 1, hp: 1)
            .PlayerA(UnitKind.Archer, 1, 0)
            .Enemy(UnitKind.Anchor, 0, 0)
            .Build();

        var wardbearer = state.Find(UnitKind.Wardbearer);
        var archer = state.Find(UnitKind.Archer);
        var anchor = state.Find(UnitKind.Anchor);

        state = state.WithUnit(state.Get(wardbearer.Id) with { Guarding = true });

        var result = EnemyTurn(state).Step(new AttackCommand(anchor.Id, archer.Id));

        // 2 damage halved to 1 is still enough, and dying in someone else's place is the deal.
        Assert.Equal(archer.Hp, result.NewState.Get(archer.Id).Hp);
        Assert.False(result.NewState.Get(wardbearer.Id).IsOnBoard);
        Assert.Equal(wardbearer.Id, result.Single<UnitDowned>().UnitId);
    }

    [Fact]
    public void GuardStance_ARedirectIntoAPit_LeavesTheWardbearerClingingAndTheNextHitVoidsIt()
    {
        var state = BoardBuilder.Rows(".......", "...O...")
            .PlayerB(UnitKind.Wardbearer, 2, 1)
            .PlayerA(UnitKind.Archer, 2, 0)
            .Enemy(UnitKind.Grappler, 5, 0)
            .Build();

        var wardbearer = state.Find(UnitKind.Wardbearer);
        var archer = state.Find(UnitKind.Archer);
        var grappler = state.Find(UnitKind.Grappler);

        state = state.WithUnit(state.Get(wardbearer.Id) with { Guarding = true });

        var pulled = EnemyTurn(state).Step(new AttackCommand(grappler.Id, archer.Id, AttackMode.Pull));

        Assert.True(pulled.NewState.Get(wardbearer.Id).Clinging);
        Assert.Equal(new Coord(3, 1), pulled.NewState.Get(wardbearer.Id).Position);
        Assert.Equal(new Coord(2, 0), pulled.NewState.Get(archer.Id).Position);

        // Any damage to a clinging unit finishes it, and the stance does nothing about that either.
        var events = new List<GameEvent>();
        var after = Combat.ApplyDamage(
            pulled.NewState, wardbearer.Id, 1, DamageSource.Attack, events);

        Assert.True(after.Get(wardbearer.Id).Voided);
        Assert.Single(events.OfType<Voided>());
    }

    // ---- Guard Stance: the telegraph ---------------------------------------------------

    [Fact]
    public void GuardStance_ADeclaredIntentAgainstAGuardedAlly_ReRoutesToTheWardbearer()
    {
        var state = BoardBuilder.Open(5, 3)
            .PlayerB(UnitKind.Wardbearer, 1, 1)
            .PlayerA(UnitKind.Archer, 2, 1)
            .Enemy(UnitKind.Anchor, 3, 1)
            .Build()
            .WithIntents();

        var wardbearer = state.Find(UnitKind.Wardbearer);
        var archer = state.Find(UnitKind.Archer);
        var anchor = state.Find(UnitKind.Anchor);

        var declared = Ai.IntentFor(state, anchor.Id)!;
        Assert.Equal(archer.Id, declared.TargetId);
        Assert.Null(declared.RedirectedTo);
        Assert.Equal(2, declared.Damage);

        var raised = state.Step(new AbilityCommand(wardbearer.Id, Ability.GuardStance));

        // The target is still the Archer — a guard does not steal the plan, only the blow — but the
        // telegraph now says who takes it, and for how much.
        var rerouted = Ai.IntentFor(raised.NewState, anchor.Id)!;
        Assert.Equal(archer.Id, rerouted.TargetId);
        Assert.Equal(wardbearer.Id, rerouted.RedirectedTo);
        Assert.Equal(1, rerouted.Damage);
        Assert.Contains(raised.All<IntentDeclared>(), e => e.Replanned && e.Intent.UnitId == anchor.Id);
    }

    [Fact]
    public void GuardStance_TheDeclaredIntentAndTheResolutionAgree()
    {
        var state = BoardBuilder.Open(5, 3)
            .PlayerB(UnitKind.Wardbearer, 1, 1)
            .PlayerA(UnitKind.Archer, 2, 1)
            .Enemy(UnitKind.Anchor, 3, 1)
            .Build()
            .WithIntents();

        var wardbearer = state.Find(UnitKind.Wardbearer);
        var archer = state.Find(UnitKind.Archer);
        var anchor = state.Find(UnitKind.Anchor);

        state = state.Then(new AbilityCommand(wardbearer.Id, Ability.GuardStance));
        state = state.Then(new EndActivationCommand(wardbearer.Id));

        var intent = Ai.IntentFor(state, anchor.Id)!;
        Assert.Equal(wardbearer.Id, intent.RedirectedTo);

        Assert.True(Game.IsEnemyTurn(state));
        var result = state.Step(Game.NextEnemyCommand(state)!);

        Assert.Equal(archer.Hp, result.NewState.Get(archer.Id).Hp);
        Assert.Equal(7 - intent.Damage, result.NewState.Get(wardbearer.Id).Hp);
        Assert.Equal(wardbearer.Id, result.Single<GuardIntercepted>().UnitId);
    }

    // The Stalker lines up a shove into a pit; the guard takes it on its own tile and its push
    // resistance eats the whole thing. The telegraph has to say that, not promise the pit.
    [Fact]
    public void GuardStance_ADisplacementIntent_TelegraphsTheGuardsOwnTravel()
    {
        var state = BoardBuilder.Rows(".....", ".O...", ".....")
            .PlayerB(UnitKind.Wardbearer, 2, 2)
            .PlayerA(UnitKind.Archer, 2, 1)
            .Enemy(UnitKind.Stalker, 4, 1)
            .Build();

        var wardbearer = state.Find(UnitKind.Wardbearer);
        var archer = state.Find(UnitKind.Archer);
        var stalker = state.Find(UnitKind.Stalker);

        // Unguarded, this is the Stalker's whole reason for existing: the Archer goes in the pit.
        var unguarded = Ai.Declare(state, state.Get(stalker.Id));
        Assert.Equal(IntentAction.Push, unguarded.Action);
        Assert.Equal(archer.Id, unguarded.TargetId);
        Assert.Null(unguarded.RedirectedTo);
        Assert.Equal(new Coord(1, 1), unguarded.DisplacementTo);

        state = state.WithUnit(state.Get(wardbearer.Id) with { Guarding = true });

        var intent = Ai.Declare(state, state.Get(stalker.Id));

        Assert.Equal(IntentAction.Push, intent.Action);
        Assert.Equal(archer.Id, intent.TargetId);
        Assert.Equal(wardbearer.Id, intent.RedirectedTo);
        Assert.Equal(Direction.Left, intent.DisplacementDirection);
        Assert.Equal(0, intent.DisplacementDistance);
        Assert.Equal(new Coord(2, 2), intent.DisplacementTo);

        // And playing it out does exactly that: the Stalker takes its flanking tile, shoves, and
        // nobody goes anywhere.
        state = EnemyTurn(state).WithIntents();
        var events = new List<GameEvent>();
        while (Game.NextEnemyCommand(state) is { } command)
        {
            var step = state.Step(command);
            events.AddRange(step.Events);
            state = step.NewState;

            if (command is EndActivationCommand)
            {
                break;
            }
        }

        Assert.Contains(events, e => e is UnitMoved m && m.UnitId == stalker.Id && m.To == new Coord(3, 1));
        Assert.Contains(events, e => e is GuardIntercepted g && g.UnitId == wardbearer.Id);

        var pushed = events.OfType<UnitPushed>().Single();
        Assert.Equal(wardbearer.Id, pushed.UnitId);
        Assert.Equal(0, pushed.Distance);

        Assert.Equal(new Coord(2, 1), state.Get(archer.Id).Position);
        Assert.Equal(new Coord(2, 2), state.Get(wardbearer.Id).Position);
        Assert.False(state.Get(archer.Id).Clinging);
    }

    // ---- fixtures ----------------------------------------------------------------------

    // A Wardbearer standing guard beside an Archer, with a Husk in reach of the Archer.
    private static GameState GuardedArcher()
    {
        var state = BoardBuilder.Open(5, 2)
            .PlayerB(UnitKind.Wardbearer, 1, 1)
            .PlayerA(UnitKind.Archer, 1, 0)
            .Enemy(UnitKind.Husk, 2, 0)
            .Build();

        var wardbearer = state.Find(UnitKind.Wardbearer);
        return state.WithUnit(state.Get(wardbearer.Id) with { Guarding = true });
    }

    private static GameState GuardingWardbearer()
    {
        var state = BoardBuilder.Open(4, 1)
            .PlayerB(UnitKind.Wardbearer, 1, 0)
            .Enemy(UnitKind.Husk, 3, 0)
            .Build();

        var wardbearer = state.Find(UnitKind.Wardbearer);
        return state.WithUnit(state.Get(wardbearer.Id) with { Guarding = true });
    }

    // Hands the activation slot to the enemy side without playing a player turn first, so a test can
    // arrange the board it wants and then let one enemy act on it.
    private static GameState EnemyTurn(GameState state)
    {
        foreach (var unit in state.Units.ToList())
        {
            if (unit.Team != Team.Enemy)
            {
                state = state.WithUnit(state.Get(unit.Id) with { HasActivated = true });
            }
        }

        return state with { ActiveTeam = Team.Enemy, NextPlayerTeam = Team.PlayerA, ActiveUnitId = null };
    }
}
