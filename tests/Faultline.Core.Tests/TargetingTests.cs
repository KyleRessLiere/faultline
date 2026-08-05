using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// Why an action cannot be aimed, asked of Core instead of guessed at by the renderer. A greyed
/// button with no reason on it was the shell's only vocabulary: an Archer with a Lobber on her toes
/// and a Husk out of range was told her AP total and nothing else.
/// </summary>
public class TargetingTests
{
    // ---- the basic profile ---------------------------------------------------------------------

    [Fact]
    public void AnArcher_WithOnlyAnAdjacentEnemy_IsBlockedByTheDeadZone()
    {
        var state = BoardBuilder.Open(8, 3)
            .PlayerA(UnitKind.Archer, 1, 1)
            .Enemy(UnitKind.Lobber, 1, 0)
            .Enemy(UnitKind.Husk, 7, 1)
            .Build();

        var archer = state.Find(UnitKind.Archer);

        Assert.Equal(TargetingBlock.TooClose, Targeting.BlockOn(state, archer, AttackMode.Damage));
        Assert.Equal(
            TargetingBlock.TooClose,
            Targeting.BlockOn(state, archer, AbilityDefinition.For(Ability.StaggerShot)));
    }

    [Fact]
    public void AnArcher_WithNothingWithinRangeAtAll_IsSimplyOutOfRange()
    {
        var state = BoardBuilder.Open(8, 3)
            .PlayerA(UnitKind.Archer, 0, 1)
            .Enemy(UnitKind.Husk, 7, 1)
            .Build();

        Assert.Equal(
            TargetingBlock.OutOfRange,
            Targeting.BlockOn(state, state.Find(UnitKind.Archer), AttackMode.Damage));
    }

    // The other half of MASTER_DESIGN §4's exception, read through the reason query: the block has to
    // *flip*, not merely exist, or the ledge teaches nothing.
    [Fact]
    public void AnArcherOnAledge_WithTheEnemyBelowHer_IsNotBlockedAtAll()
    {
        var state = BoardBuilder.Rows("H.......", "........", "........")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Lobber, 1, 0)
            .Build();

        var archer = state.Find(UnitKind.Archer);

        Assert.Equal(TargetingBlock.None, Targeting.BlockOn(state, archer, AttackMode.Damage));
        Assert.Equal(
            TargetingBlock.None,
            Targeting.BlockOn(state, archer, AbilityDefinition.For(Ability.StaggerShot)));
        Assert.True(Targeting.HasAnyTarget(state, archer));
    }

    [Fact]
    public void AnArcherOnTheSameLedgeAsTheEnemy_IsStillInTheDeadZone()
    {
        var state = BoardBuilder.Rows("HH......", "........", "........")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Lobber, 1, 0)
            .Build();

        Assert.Equal(
            TargetingBlock.TooClose,
            Targeting.BlockOn(state, state.Find(UnitKind.Archer), AttackMode.Damage));
    }

    [Fact]
    public void AMeleeUnit_WithNobodyAdjacent_IsOutOfRange()
    {
        var state = BoardBuilder.Open(8, 3)
            .PlayerA(UnitKind.Vanguard, 0, 1)
            .Enemy(UnitKind.Husk, 4, 1)
            .Build();

        Assert.Equal(
            TargetingBlock.OutOfRange,
            Targeting.BlockOn(state, state.Find(UnitKind.Vanguard), AttackMode.Damage));
    }

    [Fact]
    public void APull_WithTheOnlyEnemyAlreadyTouching_SaysThereIsNowhereToPullItTo()
    {
        var state = BoardBuilder.Open(8, 3)
            .PlayerA(UnitKind.Threadcaster, 1, 1)
            .Enemy(UnitKind.Husk, 2, 1)
            .Build();

        var fisher = state.Find(UnitKind.Threadcaster);

        // The Fisher keeps her point-blank shot, so only the pull half is blocked.
        Assert.Equal(TargetingBlock.None, Targeting.BlockOn(state, fisher, AttackMode.Damage));
        Assert.Equal(TargetingBlock.NoRoomToPull, Targeting.BlockOn(state, fisher, AttackMode.Pull));
    }

    [Fact]
    public void AnActionTheArchetypeDoesNotHave_IsUnavailableRatherThanOutOfRange()
    {
        var state = BoardBuilder.Open(8, 3)
            .PlayerA(UnitKind.Vanguard, 1, 1)
            .Enemy(UnitKind.Husk, 2, 1)
            .Build();

        Assert.Equal(
            TargetingBlock.Unavailable,
            Targeting.BlockOn(state, state.Find(UnitKind.Vanguard), AttackMode.Pull));
    }

    // ---- abilities -----------------------------------------------------------------------------

    [Fact]
    public void Reel_WithTheOnlyEnemyAdjacent_HasNowhereToReelItTo()
    {
        var state = BoardBuilder.Open(8, 3)
            .PlayerA(UnitKind.Threadcaster, 1, 1)
            .Enemy(UnitKind.Husk, 2, 1)
            .Build();

        Assert.Equal(
            TargetingBlock.NoRoomToPull,
            Targeting.BlockOn(state, state.Find(UnitKind.Threadcaster), AbilityDefinition.For(Ability.Reel)));
    }

    [Fact]
    public void AStance_AimsAtNothing_SoNothingCanBeOutOfRangeOfIt()
    {
        var state = BoardBuilder.Open(8, 3)
            .PlayerA(UnitKind.Wardbearer, 0, 0)
            .Enemy(UnitKind.Husk, 7, 2)
            .Build();

        var ward = state.Find(UnitKind.Wardbearer);

        Assert.Equal(
            TargetingBlock.None,
            Targeting.BlockOn(state, ward, AbilityDefinition.For(Ability.GuardStance)));
        Assert.Equal(
            TargetingBlock.OutOfRange,
            Targeting.BlockOn(state, ward, AbilityDefinition.For(Ability.SpearThrust)));
    }

    // A charge is a move, so open ground ahead is a legal charge and the button is honest to leave
    // lit. Only being boxed in leaves it nothing — which is the lane the reason has to name.
    [Fact]
    public void ACharge_WithOpenGroundAhead_IsNotBlocked()
    {
        var state = BoardBuilder.Open(8, 3)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 7, 2)
            .Build();

        Assert.Equal(
            TargetingBlock.None,
            Targeting.BlockOn(state, state.Find(UnitKind.Vanguard), AbilityDefinition.For(Ability.BullRush)));
    }

    [Fact]
    public void ACharge_WithEveryLaneWalledOff_IsOutOfRange()
    {
        var state = BoardBuilder.Rows("###", "#.#", "###")
            .PlayerA(UnitKind.Vanguard, 1, 1)
            .Build();

        Assert.Equal(
            TargetingBlock.OutOfRange,
            Targeting.BlockOn(state, state.Find(UnitKind.Vanguard), AbilityDefinition.For(Ability.BullRush)));
    }

    // ---- the whole action row ------------------------------------------------------------------

    [Fact]
    public void AnArcherInTheDeadZone_HasNothingToAimAtAndOneStepFixesIt()
    {
        var state = BoardBuilder.Open(8, 3)
            .PlayerA(UnitKind.Archer, 1, 1)
            .Enemy(UnitKind.Lobber, 1, 0)
            .Enemy(UnitKind.Husk, 7, 1)
            .Build();

        var archer = state.Find(UnitKind.Archer);

        Assert.False(Targeting.HasAnyTarget(state, archer));
        Assert.Equal(Activation.StepCost, Targeting.MoveNeededToTarget(state, archer));
    }

    [Fact]
    public void AUnitThatAlreadyHasATarget_NeedsNoMovementAtAll()
    {
        var state = BoardBuilder.Open(8, 3)
            .PlayerA(UnitKind.Vanguard, 1, 1)
            .Enemy(UnitKind.Husk, 2, 1)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);

        Assert.True(Targeting.HasAnyTarget(state, vanguard));
        Assert.Equal(0, Targeting.MoveNeededToTarget(state, vanguard));
    }

    [Fact]
    public void AUnitNoWalkCanHelp_IsToldSoRatherThanSentWalking()
    {
        var state = BoardBuilder.Open(12, 3)
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 11, 2)
            .Build();

        Assert.Null(Targeting.MoveNeededToTarget(state, state.Find(UnitKind.Archer)));
    }

    [Fact]
    public void AUnitWhoseActivationIsSpent_IsSentNowhere()
    {
        var state = BoardBuilder.Open(8, 3)
            .PlayerA(UnitKind.Archer, 1, 1)
            .Enemy(UnitKind.Lobber, 1, 0)
            .Build();

        var spent = state.Find(UnitKind.Archer) with { HasActed = true, MoveClosed = true };

        Assert.False(Targeting.HasAnyTarget(state, spent));
        Assert.Null(Targeting.MoveNeededToTarget(state, spent));
    }

    // A clinging ally is a target the action row has, and it is the one a player most needs offered.
    [Fact]
    public void AClingingAllyBeside_CountsAsSomethingToAimAt()
    {
        var state = BoardBuilder.Rows("O.......", "........")
            .PlayerA(UnitKind.Archer, 0, 0)
            .PlayerA(UnitKind.Vanguard, 1, 0)
            .Build();

        state = state.WithUnit(state.Get(new UnitId(0)) with
        {
            Clinging = true,
            ClingingSinceRound = state.Round,
        });

        Assert.True(Targeting.HasAnyTarget(state, state.Get(new UnitId(1))));
    }
}
