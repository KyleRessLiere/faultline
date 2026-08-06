using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// The eight technique modifiers of MASTER_DESIGN §8.6 that this stage builds. One Common and one
/// Uncommon per class, each paired with the control that does not carry the card — a modifier that
/// cannot be told apart from the printed action is not a modifier.
/// </summary>
public class TechniqueModifierTests
{
    // ---- Vanguard · Follow-In (C · TRAFFIC · Basic) ---------------------------------------------

    [Fact]
    public void FollowIn_HeEntersTheTileThePushedTargetLeft()
    {
        var state = Line(out var vanguard, out var husk);

        var stepped = state
            .WithTechnique(vanguard, TechniqueModifier.FollowIn)
            .Then(new AttackCommand(
                vanguard, husk, AttackMode.Damage, DisplacementAim.Default, TechniqueOption.FollowIn));

        Assert.Equal(TestPlay.At(2, 0), stepped.Get(husk).Position);
        Assert.Equal(TestPlay.At(1, 0), stepped.Get(vanguard).Position);

        // The control: the same shove without the election leaves him where he stood. "May enter" is
        // a choice, and a choice taken for the player is not one.
        var stayed = state
            .WithTechnique(vanguard, TechniqueModifier.FollowIn)
            .Then(new AttackCommand(vanguard, husk));

        Assert.Equal(TestPlay.At(0, 0), stayed.Get(vanguard).Position);
    }

    [Fact]
    public void FollowIn_ElectedWithoutTheCard_IsRefusedRatherThanIgnored()
    {
        var state = Line(out var vanguard, out var husk);

        TestPlay.AssertIllegal(
            state,
            new AttackCommand(
                vanguard, husk, AttackMode.Damage, DisplacementAim.Default, TechniqueOption.FollowIn));
    }

    [Fact]
    public void FollowIn_ATargetThatDidNotBudge_LeavesNoTileToEnter()
    {
        // Back to the wall: the shove is a collision for damage, and the body never leaves its tile.
        var state = BoardBuilder.Rows("..#")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 18)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard).Id;
        var husk = state.Find(UnitKind.Husk).Id;

        var result = state
            .WithTechnique(vanguard, TechniqueModifier.FollowIn)
            .Then(new AttackCommand(
                vanguard, husk, AttackMode.Damage, DisplacementAim.Default, TechniqueOption.FollowIn));

        Assert.Equal(TestPlay.At(1, 0), result.Get(husk).Position);
        Assert.Equal(TestPlay.At(0, 0), result.Get(vanguard).Position);
    }

    // ---- Vanguard · Rattling Impact (U · IMPACT/RELAY) ------------------------------------------

    [Fact]
    public void RattlingImpact_HisFirstCollisionEachRound_MarksTheEnemyForTheOtherFlock()
    {
        var state = BoardBuilder.Rows("..#")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 18)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard).Id;
        var husk = state.Find(UnitKind.Husk).Id;

        var result = state
            .WithTechnique(vanguard, TechniqueModifier.RattlingImpact)
            .Step(new AttackCommand(vanguard, husk));

        Assert.True(result.Has<Rattled>());
        Assert.Equal(Team.PlayerB, result.Single<Rattled>().ForFlock);
        Assert.Equal(Team.PlayerB, result.NewState.Get(husk).RattledFor);

        // The control: the same collision off a Vanguard without the card marks nothing.
        var plain = state.Step(new AttackCommand(vanguard, husk));

        Assert.False(plain.Has<Rattled>());
        Assert.Null(plain.NewState.Get(husk).RattledFor);
    }

    [Fact]
    public void RattlingImpact_TheOtherFlockSpendsTheMarkForATile_AndHisOwnFlockCannot()
    {
        // Set directly rather than earned, so this measures the mark's arithmetic and not the
        // collision that made it — a collision also Staggers, and two +1s are not one rule.
        var state = BoardBuilder.Open(9, 1)
            .PlayerB(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 3, 0, hp: 18)
            .Build();

        var archer = state.Find(UnitKind.Archer).Id;
        var husk = state.Find(UnitKind.Husk).Id;

        int printed = AbilityDefinition.For(Ability.StaggerShot).Push;

        var owed = state.WithUnit(state.Get(husk) with { RattledFor = Team.PlayerB });
        var spent = owed.Step(new AbilityCommand(archer, Ability.StaggerShot, husk));

        Assert.Equal(printed + Techniques.RattledDistanceBonus, spent.Single<UnitPushed>().Distance);
        Assert.Null(spent.NewState.Get(husk).RattledFor);

        // Marked for the other flock: this Archer's shove is the printed one, and the mark survives
        // for whoever it was actually owed to.
        var wrongFlock = state.WithUnit(state.Get(husk) with { RattledFor = Team.PlayerA });
        var unhelped = wrongFlock.Step(new AbilityCommand(archer, Ability.StaggerShot, husk));

        Assert.Equal(printed, unhelped.Single<UnitPushed>().Distance);
        Assert.Equal(Team.PlayerA, unhelped.NewState.Get(husk).RattledFor);
    }

    // ---- Fisher · Short Line (C · CONTROL · Reel) -----------------------------------------------

    [Fact]
    public void ShortLine_SheChoosesWhereTheDragStops()
    {
        var state = Fisher(out var fisher, out var husk);

        // Unaided, Reel hauls all the way in: four tiles apart, so three tiles of drag.
        var full = state.Then(new AbilityCommand(fisher, Ability.Reel, husk));
        Assert.Equal(TestPlay.At(1, 0), full.Get(husk).Position);

        var stopped = state
            .WithTechnique(fisher, TechniqueModifier.ShortLine)
            .Then(new AbilityCommand(
                fisher, Ability.Reel, husk, null, DisplacementAim.Default, TechniqueOption.None, 1));

        Assert.Equal(TestPlay.At(3, 0), stopped.Get(husk).Position);
    }

    [Fact]
    public void ShortLine_CanOnlyEverShorten_AndIsRefusedWithoutTheCard()
    {
        var state = Fisher(out var fisher, out var husk);

        TestPlay.AssertIllegal(
            state,
            new AbilityCommand(
                fisher, Ability.Reel, husk, null, DisplacementAim.Default, TechniqueOption.None, 1));

        // A stop beyond the end of the drag is the whole drag. The card names a tile on the path, and
        // there is no tile past the end of one.
        var beyond = state
            .WithTechnique(fisher, TechniqueModifier.ShortLine)
            .Then(new AbilityCommand(
                fisher, Ability.Reel, husk, null, DisplacementAim.Default, TechniqueOption.None, 9));

        Assert.Equal(TestPlay.At(1, 0), beyond.Get(husk).Position);
    }

    // ---- Fisher · Hand-Off (U · RELAY) -----------------------------------------------------------

    [Fact]
    public void HandOff_HerDragLandsBesideTheOtherFlock_AndGrantsThatDuckAPush()
    {
        var state = HandOffBoard(out var fisher, out var wardbearer, out var husk);

        var dragged = state
            .WithTechnique(fisher, TechniqueModifier.HandOff)
            .Step(new AbilityCommand(fisher, Ability.Reel, husk));

        Assert.Equal(TestPlay.At(1, 1), dragged.NewState.Get(husk).Position);
        Assert.True(dragged.Has<HandOffGranted>());
        Assert.Equal(Team.PlayerB, dragged.Single<HandOffGranted>().Owner);
        Assert.Equal(husk, dragged.NewState.Get(wardbearer).HandOffTarget);

        // The receiving owner spends it, and that election is the consent (MASTER_DESIGN §8.5).
        var pushed = Handing(dragged.NewState, Team.PlayerB)
            .Step(new AttackCommand(
                wardbearer, husk, AttackMode.Damage, DisplacementAim.Default, TechniqueOption.HandOff));

        Assert.Equal(Techniques.HandOffPush, pushed.Single<UnitPushed>().Distance);
        Assert.Equal(TestPlay.At(1, 0), pushed.NewState.Get(husk).Position);
        Assert.Null(pushed.NewState.Get(wardbearer).HandOffTarget);

        // The control: not spending it is a legal answer, and the Wardbearer shoves nobody.
        var declined = Handing(dragged.NewState, Team.PlayerB)
            .Step(new AttackCommand(wardbearer, husk));

        Assert.False(declined.Has<UnitPushed>());
        Assert.Equal(TestPlay.At(1, 1), declined.NewState.Get(husk).Position);
    }

    [Fact]
    public void HandOff_ElectedWithoutAGrant_IsRefused()
    {
        var state = HandOffBoard(out _, out var wardbearer, out var husk);

        TestPlay.AssertIllegal(
            state,
            new AttackCommand(
                wardbearer, husk, AttackMode.Damage, DisplacementAim.Default, TechniqueOption.HandOff));
    }

    // ---- Archer · Spotter (C · RELAY) ------------------------------------------------------------

    [Fact]
    public void Spotter_SheShootsIntoHerDeadZone_WhenTheOtherFlockIsHoldingTheEnemy()
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerB(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 18)
            .PlayerA(UnitKind.Vanguard, 2, 0)
            .Build();

        var archer = state.Find(UnitKind.Archer).Id;
        var husk = state.Find(UnitKind.Husk).Id;

        // The control: her bow's minimum range is the rule, and it holds without the card.
        TestPlay.AssertIllegal(state, new AttackCommand(archer, husk));

        var spotted = state.WithTechnique(archer, TechniqueModifier.Spotter);
        Assert.True(Combat.CanAttack(spotted, spotted.Get(archer), spotted.Get(husk), out _));

        var shot = spotted.Step(new AttackCommand(archer, husk));
        Assert.True(shot.Has<UnitAttacked>());
    }

    [Fact]
    public void Spotter_WithNobodyFromTheOtherFlockBesideIt_TheDeadZoneStands()
    {
        // Her own flock's duck is not "the other flock's duck". The card is a RELAY card and the
        // relation it names is the only one that opens the shot.
        var state = BoardBuilder.Open(6, 1)
            .PlayerB(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 18)
            .PlayerB(UnitKind.Wardbearer, 2, 0)
            .Build();

        var archer = state.Find(UnitKind.Archer).Id;
        var husk = state.Find(UnitKind.Husk).Id;

        TestPlay.AssertIllegal(
            state.WithTechnique(archer, TechniqueModifier.Spotter),
            new AttackCommand(archer, husk));
    }

    // ---- Archer · Crossing Shot (U · RELAY, reaction) --------------------------------------------

    [Fact]
    public void CrossingShot_TheOtherFlockMovesAnEnemyThroughHerLine_AndSheFiresUnasked()
    {
        var state = CrossingBoard(out var vanguard, out var archer, out var husk);
        int hp = state.Get(husk).Hp;

        var shot = state
            .WithTechnique(archer, TechniqueModifier.CrossingShot)
            .Step(new AttackCommand(vanguard, husk));

        var fired = shot.Single<CrossingShotFired>();
        Assert.Equal(archer, fired.ArcherId);
        Assert.Equal(Techniques.CrossingShotDamage, fired.Damage);
        Assert.Equal(
            hp - UnitTemplate.For(UnitKind.Vanguard).Damage - Techniques.CrossingShotDamage,
            shot.NewState.Get(husk).Hp);

        // The control: no card, no reaction.
        var quiet = state.Step(new AttackCommand(vanguard, husk));
        Assert.False(quiet.Has<CrossingShotFired>());
    }

    [Fact]
    public void CrossingShot_TheInitiatingPreviewShowsIt_BeforeAnythingIsCommitted()
    {
        // §8.6 states exactly one thing about the reaction's interface, and this is it. The reacting
        // player is never asked; the initiating one is never surprised.
        var state = CrossingBoard(out var vanguard, out var archer, out var husk)
            .WithTechnique(archer, TechniqueModifier.CrossingShot);

        var outlook = Abilities.Outlook(state, new AttackCommand(vanguard, husk));

        Assert.NotNull(outlook!.Reaction);
        Assert.Equal(archer, outlook.Reaction!.ArcherId);
        Assert.Equal(husk, outlook.Reaction.TargetId);
        Assert.Equal(Techniques.CrossingShotDamage, outlook.Reaction.Damage);

        // And the promise is kept: the shot the preview named is the shot that lands.
        var fired = state.Step(new AttackCommand(vanguard, husk)).Single<CrossingShotFired>();

        Assert.Equal(outlook.Reaction.ArcherId, fired.ArcherId);
        Assert.Equal(outlook.Reaction.TargetId, fired.TargetId);
        Assert.Equal(outlook.Reaction.Damage, fired.Damage);
    }

    [Fact]
    public void CrossingShot_FiresOnceARound_AndAgainWhenTheRoundTurnsOver()
    {
        var state = CrossingBoard(out var vanguard, out var archer, out var husk)
            .WithTechnique(archer, TechniqueModifier.CrossingShot);

        var after = state.Step(new AttackCommand(vanguard, husk));
        Assert.True(after.Has<CrossingShotFired>());
        Assert.Equal(state.Round, after.NewState.Get(archer).CrossingShotRound);

        // A second crossing in the same round draws nothing.
        var reopened = Handing(after.NewState, Team.PlayerA)
            .WithUnit(after.NewState.Get(vanguard) with
            {
                HasActed = false,
                MoveClosed = false,
                MoveSpent = 0,
                HasActivated = false,

                // Back into reach of the body he just shoved: this asks about the latch, not about
                // how far a Vanguard can walk in a round.
                Position = TestPlay.At(2, 2),
            });

        var again = reopened.Step(new AttackCommand(vanguard, husk));

        Assert.False(again.Has<CrossingShotFired>());
    }

    // ---- Wardbearer · Stored Force (C · GUARD/IMPACT) --------------------------------------------

    [Fact]
    public void StoredForce_EachTileHisResistanceCancels_BanksOneToACapOfTwo()
    {
        var state = Shoved(out var wardbearer, out var stalker);

        var banked = state
            .WithTechnique(wardbearer, TechniqueModifier.StoredForce)
            .Then(new AttackCommand(stalker, wardbearer, AttackMode.Push));

        // Push 1 against resistance 2: one tile asked, one tile cancelled, one Force.
        Assert.Equal(1, banked.Get(wardbearer).StoredForce);

        // The control: no card, no Force, and the shove is eaten exactly as before.
        var plain = state.Then(new AttackCommand(stalker, wardbearer, AttackMode.Push));
        Assert.Equal(0, plain.Get(wardbearer).StoredForce);
    }

    [Fact]
    public void StoredForce_ATipTileSpearHitMaySpendItAsAPush()
    {
        var state = BoardBuilder.Open(8, 1)
            .PlayerB(UnitKind.Wardbearer, 0, 0)
            .Enemy(UnitKind.Husk, 2, 0, hp: 18)
            .Build();

        var wardbearer = state.Find(UnitKind.Wardbearer).Id;
        var husk = state.Find(UnitKind.Husk).Id;

        var armed = state
            .WithTechnique(wardbearer, TechniqueModifier.StoredForce)
            .WithUnit(state.Get(wardbearer) with
            {
                Loadout = DuckLoadout.Empty.With(TechniqueModifier.StoredForce),
                StoredForce = Techniques.StoredForceCap,
            });

        var spent = armed.Step(new AbilityCommand(
            wardbearer, Ability.SpearThrust, null, Direction.Right,
            DisplacementAim.Default, TechniqueOption.StoredForce));

        Assert.Equal(Techniques.StoredForceCap, spent.Single<UnitPushed>().Distance);
        Assert.Equal(TestPlay.At(4, 0), spent.NewState.Get(husk).Position);
        Assert.Equal(0, spent.NewState.Get(wardbearer).StoredForce);

        // The control: the Spear displaces nothing without the spend (D-068).
        var plain = armed.Step(
            new AbilityCommand(wardbearer, Ability.SpearThrust, null, Direction.Right));

        Assert.False(plain.Has<UnitPushed>());
        Assert.Equal(Techniques.StoredForceCap, plain.NewState.Get(wardbearer).StoredForce);
    }

    // ---- Wardbearer · Shelter Step (U · GUARD/RELAY · Guard Stance) ------------------------------

    [Fact]
    public void ShelterStep_ARedirectThatMovesHim_BanksTheTileHeLeftForTheDuckHeCovered()
    {
        var guarding = ShelterFixture(out var wardbearer, out var vanguard, out var grappler, card: true);
        var stood = guarding.Get(wardbearer).Position;

        var redirected = guarding.Step(new AttackCommand(grappler, vanguard, AttackMode.Pull));

        // The redirect landed on the guard and actually moved him, which is the card's whole clause.
        Assert.NotEqual(stood, redirected.NewState.Get(wardbearer).Position);

        var banked = redirected.Single<StepBanked>();
        Assert.Equal(vanguard, banked.UnitId);
        Assert.Equal(Team.PlayerA, banked.Owner);
        Assert.Equal(stood, banked.To);
        Assert.Equal(stood, redirected.NewState.Get(vanguard).BankedStepTo);

        // Banked, not taken: the duck is still where it was until its owner says otherwise.
        Assert.NotEqual(stood, redirected.NewState.Get(vanguard).Position);

        var taken = redirected.NewState.Then(new TakeBankedStepCommand(vanguard));
        Assert.Equal(stood, taken.Get(vanguard).Position);
        Assert.Null(taken.Get(vanguard).BankedStepTo);
    }

    [Fact]
    public void ShelterStep_WithoutTheCard_NothingIsBanked_AndThereIsNoStepToTake()
    {
        var guarding = ShelterFixture(out _, out var vanguard, out var grappler, card: false);

        var redirected = guarding.Step(new AttackCommand(grappler, vanguard, AttackMode.Pull));

        Assert.False(redirected.Has<StepBanked>());
        Assert.Null(redirected.NewState.Get(vanguard).BankedStepTo);
        TestPlay.AssertIllegal(redirected.NewState, new TakeBankedStepCommand(vanguard));
    }

    // ---- fixtures ---------------------------------------------------------------------------------

    /// <summary>
    /// The same board with the activation slot handed to a named side. Turn order is not what any of
    /// these tests is about, and driving a specific duck's card is: the alternative is a page of
    /// pass commands in front of every assertion.
    /// </summary>
    private static GameState Handing(GameState state, Team team) => state with
    {
        ActiveTeam = team,
        NextPlayerTeam = team.IsPlayer() ? team : Team.PlayerA,
        ActiveUnitId = null,
    };

    private static GameState Line(out UnitId vanguard, out UnitId husk)
    {
        var state = BoardBuilder.Open(9, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 18)
            .Build();

        vanguard = state.Find(UnitKind.Vanguard).Id;
        husk = state.Find(UnitKind.Husk).Id;
        return state;
    }

    private static GameState Fisher(out UnitId fisher, out UnitId husk)
    {
        var state = BoardBuilder.Open(9, 1)
            .PlayerA(UnitKind.Threadcaster, 0, 0)
            .Enemy(UnitKind.Husk, 4, 0, hp: 18)
            .Build();

        fisher = state.Find(UnitKind.Threadcaster).Id;
        husk = state.Find(UnitKind.Husk).Id;
        return state;
    }

    private static GameState HandOffBoard(out UnitId fisher, out UnitId wardbearer, out UnitId husk)
    {
        var state = BoardBuilder.Open(6, 3)
            .PlayerA(UnitKind.Threadcaster, 0, 1)
            .Enemy(UnitKind.Husk, 4, 1, hp: 18)
            .PlayerB(UnitKind.Wardbearer, 1, 2)
            .Build();

        fisher = state.Find(UnitKind.Threadcaster).Id;
        wardbearer = state.Find(UnitKind.Wardbearer).Id;
        husk = state.Find(UnitKind.Husk).Id;
        return state;
    }

    private static GameState CrossingBoard(out UnitId vanguard, out UnitId archer, out UnitId husk)
    {
        var state = BoardBuilder.Open(6, 3)
            .PlayerA(UnitKind.Vanguard, 1, 2)
            .Enemy(UnitKind.Husk, 2, 2, hp: 18)
            .PlayerB(UnitKind.Archer, 2, 0)
            .Build();

        vanguard = state.Find(UnitKind.Vanguard).Id;
        archer = state.Find(UnitKind.Archer).Id;
        husk = state.Find(UnitKind.Husk).Id;
        return state;
    }

    private static GameState Shoved(out UnitId wardbearer, out UnitId stalker)
    {
        var state = BoardBuilder.Open(8, 1)
            .Enemy(UnitKind.Stalker, 0, 0)
            .PlayerB(UnitKind.Wardbearer, 1, 0)
            .Active(Team.Enemy)
            .Build();

        wardbearer = state.Find(UnitKind.Wardbearer).Id;
        stalker = state.Find(UnitKind.Stalker).Id;
        return state;
    }

    /// <summary>
    /// A Wardbearer holding Guard Stance one tile below the Vanguard he covers, with a Grappler three
    /// tiles off to the left. The guard is Staggered so the drag survives his push resistance 2: the
    /// card is about a redirect that MOVES him, and a redirect his own shrug eats moves nobody.
    /// </summary>
    private static GameState ShelterFixture(
        out UnitId wardbearer, out UnitId vanguard, out UnitId grappler, bool card)
    {
        var state = BoardBuilder.Open(8, 4)
            .PlayerB(UnitKind.Wardbearer, 2, 2)
            .PlayerA(UnitKind.Vanguard, 2, 1)
            .Enemy(UnitKind.Grappler, 0, 1)
            .Build();

        wardbearer = state.Find(UnitKind.Wardbearer).Id;
        vanguard = state.Find(UnitKind.Vanguard).Id;
        grappler = state.Find(UnitKind.Grappler).Id;

        if (card)
        {
            state = state.WithTechnique(wardbearer, TechniqueModifier.ShelterStep);
        }

        var wardId = wardbearer;


        state = Handing(state, Team.PlayerB).Then(new AbilityCommand(wardId, Ability.GuardStance));
        state = Handing(state, Team.Enemy);

        // Set rather than earned: a Stagger comes from a collision, and staging one here would be a
        // second fixture in front of the one under test.
        return state.WithUnit(state.Get(wardId) with { Staggered = true });
    }
}
