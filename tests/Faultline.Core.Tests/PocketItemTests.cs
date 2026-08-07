using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// The §8.6 tactical one-shots added after the first five: what each one does, and — the point of the
/// suite — that none of them invented a second mechanism to do it with.
/// </summary>
/// <remarks>
/// Greased Feather and Chalk Mark are both "+1 distance" cards, and D-156 already ruled how a +1
/// distance mark composes: it rides the <b>requested</b> distance beside Wrecking Weight's tile, so it
/// meets Stagger, push resistance and the hold aura instead of stepping around them, and it is spent by
/// the attempt rather than by the result. Every assertion below is a different way of asking whether
/// these two obey that ruling rather than a private copy of it (D-190).
/// </remarks>
public class PocketItemTests
{
    // ---- Greased Feather · this duck's next displacement +1 --------------------------------------

    [Fact]
    public void GreasedFeather_ArmsTheCarrier_AndItsNextDisplacementAsksForOneMoreTile()
    {
        var state = Archer(out var archer, out var husk);
        int printed = AbilityDefinition.For(Ability.StaggerShot).Push;

        // Unaided first, so the test measures the feather and not the shot.
        Assert.Equal(printed, state.Step(new AbilityCommand(archer, Ability.StaggerShot, husk))
            .Single<UnitPushed>().Distance);

        var armed = state.WithPocket(archer, Consumable.GreasedFeather)
            .Then(new UseConsumableCommand(archer));

        Assert.True(armed.Get(archer).GreasedFeatherArmed);
        Assert.Null(armed.Get(archer).Loadout.Pocket);

        var shoved = armed.Step(new AbilityCommand(archer, Ability.StaggerShot, husk));

        Assert.Equal(printed + Consumables.GreaseDistanceBonus, shoved.Single<UnitPushed>().Distance);
        Assert.Equal(husk, shoved.Single<GreasedFeatherSpent>().TargetId);

        // One displacement, not a standing bonus.
        Assert.False(shoved.NewState.Get(archer).GreasedFeatherArmed);
    }

    [Fact]
    public void GreasedFeather_RidesTheRequest_SoPushResistanceStillEatsIt()
    {
        // The Anchor shrugs off a tile of every shove. If the feather were added to the *effective*
        // distance it would sail past that; because it rides the request, the resistance meets it.
        var state = BoardBuilder.Open(9, 1)
            .PlayerB(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Anchor, 3, 0, hp: 18)
            .Build();

        var archer = state.Find(UnitKind.Archer).Id;
        var anchor = state.Find(UnitKind.Anchor).Id;

        Assert.True(state.Get(anchor).Template.PushResistance > 0);

        int plain = state.Step(new AbilityCommand(archer, Ability.StaggerShot, anchor))
            .Single<UnitPushed>().Distance;

        var greased = state.WithPocket(archer, Consumable.GreasedFeather)
            .Then(new UseConsumableCommand(archer))
            .Step(new AbilityCommand(archer, Ability.StaggerShot, anchor));

        // The tile the feather bought survives resistance here, but it arrived through it rather than
        // around it: the request grew by one, and the shrug was subtracted afterwards exactly once.
        Assert.Equal(plain + Consumables.GreaseDistanceBonus, greased.Single<UnitPushed>().Distance);

        // And it is gone either way — spent by the attempt, not by the result (D-190).
        Assert.False(greased.NewState.Get(archer).GreasedFeatherArmed);
        Assert.True(greased.Has<GreasedFeatherSpent>());
    }

    [Fact]
    public void GreasedFeather_IsNotOfferedToADuckAlreadyCarryingOne()
    {
        var state = Archer(out var archer, out _);
        var armed = state.WithUnit(state.Get(archer) with { GreasedFeatherArmed = true })
            .WithPocket(archer, Consumable.GreasedFeather);

        // A silent no-op is a bug: the refusal is that the item buys nothing, and it shows up as an
        // absence from the legal list rather than as a use that changes nothing.
        Assert.Empty(Consumables.Legal(armed, armed.Get(archer)));
        TestPlay.AssertNotLegal(armed, new UseConsumableCommand(archer));
    }

    [Fact]
    public void GreasedFeather_LapsesAtTheRoundSeam_LikeEveryOtherMark()
    {
        var state = Archer(out var archer, out _);
        var armed = state.WithPocket(archer, Consumable.GreasedFeather)
            .Then(new UseConsumableCommand(archer));

        Assert.True(armed.Get(archer).GreasedFeatherArmed);

        // Played to the seam rather than set there: a restored round is exactly the save this repo
        // has been bitten by, so everyone passes until the round actually turns over.
        int round = armed.Round;
        var next = armed;
        for (int i = 0; i < 40 && next.Round == round; i++)
        {
            next = next.PassCurrent().NewState;
        }

        Assert.Equal(round + 1, next.Round);
        Assert.False(next.Get(archer).GreasedFeatherArmed);
    }

    // ---- Chalk Mark · the other flock's next displacement of it +1 -------------------------------

    [Fact]
    public void ChalkMark_WritesTheSameMarkRattlingImpactWrites_ForTheOtherFlock()
    {
        var state = Archer(out var archer, out var husk);
        var chalked = state.WithPocket(archer, Consumable.ChalkMark)
            .Step(new UseConsumableCommand(archer, husk));

        // PlayerB's Archer chalks it, so PlayerA is owed the tile — never her own flock.
        Assert.Equal(Team.PlayerA, chalked.NewState.Get(husk).RattledFor);

        var marked = chalked.Single<ChalkMarked>();
        Assert.Equal(husk, marked.UnitId);
        Assert.Equal(Team.PlayerA, marked.ForFlock);
        Assert.Equal(archer, marked.ByUnitId);
    }

    [Fact]
    public void ChalkMark_TheOtherFlockSpendsItForATile_AndTheChalkersOwnFlockCannot()
    {
        // Two flocks on the board so both halves of "the other flock" are real.
        // One archer either side of the husk, both at range 3, so each flock can genuinely shove it.
        var state = BoardBuilder.Open(9, 1)
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 3, 0, hp: 18)
            .PlayerB(UnitKind.Archer, 6, 0)
            .Build();

        // PlayerA acts first, so PlayerA is the flock that can legally empty a pocket right now — the
        // one-shot is free of the halves but not of whose turn it is.
        var chalker = state.Units.First(u => u.Team == Team.PlayerA).Id;
        var beneficiary = state.Units.First(u => u.Team == Team.PlayerB).Id;
        var husk = state.Find(UnitKind.Husk).Id;

        int printed = AbilityDefinition.For(Ability.StaggerShot).Push;

        var chalked = state.WithPocket(chalker, Consumable.ChalkMark)
            .Then(new UseConsumableCommand(chalker, husk));

        Assert.Equal(Team.PlayerB, chalked.Get(husk).RattledFor);

        // The chalker's own flock shoves it: printed distance, and the mark survives for its owner.
        var ownFlock = chalked.Step(new AbilityCommand(chalker, Ability.StaggerShot, husk));
        Assert.Equal(printed, ownFlock.Single<UnitPushed>().Distance);
        Assert.Equal(Team.PlayerB, ownFlock.NewState.Get(husk).RattledFor);

        // The flock it was drawn for shoves it: one more tile, and the chalk is gone. Handed over by
        // playing the activation out, not by editing whose turn it is.
        var handedOver = chalked;
        for (int i = 0; i < 10 && handedOver.ActiveTeam != Team.PlayerB; i++)
        {
            handedOver = handedOver.PassCurrent().NewState;
        }

        Assert.Equal(Team.PlayerB, handedOver.ActiveTeam);
        Assert.Equal(chalked.Round, handedOver.Round);

        var owed = handedOver.Step(new AbilityCommand(beneficiary, Ability.StaggerShot, husk));
        Assert.Equal(printed + Techniques.RattledDistanceBonus, owed.Single<UnitPushed>().Distance);
        Assert.Null(owed.NewState.Get(husk).RattledFor);
    }

    [Fact]
    public void ChalkMark_IsNotOfferedOnAnEnemyTheOtherFlockIsAlreadyOwedATileOn()
    {
        var state = Archer(out var archer, out var husk);

        // Already Rattled for the same flock the chalk would mark it for: re-chalking changes nothing,
        // so it is not on the list, and asking anyway names the reason rather than quietly passing.
        var already = state.WithUnit(state.Get(husk) with { RattledFor = Team.PlayerA })
            .WithPocket(archer, Consumable.ChalkMark);

        Assert.DoesNotContain(husk, Consumables.ChalkTargets(already, already.Get(archer)));
        TestPlay.AssertIllegal(already, new UseConsumableCommand(archer, husk));
    }

    [Fact]
    public void ChalkMark_RefusesWithAReason_WhenItNamesNobody()
    {
        var state = Archer(out var archer, out _).WithPocket(archer, Consumable.ChalkMark);

        // Every refusal names its reason — an aimless Chalk Mark must not spend itself for nothing.
        TestPlay.AssertIllegal(state, new UseConsumableCommand(archer));
    }

    // ---- the shared ruling ------------------------------------------------------------------------

    [Fact]
    public void BothNewMarks_AreOneMechanism_NotTwo()
    {
        // Chalk Mark writes Unit.RattledFor — the identical field Rattling Impact writes — so the
        // request site needed no second case and the two cannot drift (D-190). If a future writer
        // gives the chalk its own field, this fails and says why.
        var state = Archer(out var archer, out var husk);

        var chalked = state.WithPocket(archer, Consumable.ChalkMark)
            .Then(new UseConsumableCommand(archer, husk));

        var rattled = state.WithUnit(state.Get(husk) with { RattledFor = Team.PlayerA });

        Assert.Equal(rattled.Get(husk).RattledFor, chalked.Get(husk).RattledFor);
    }

    private static GameState Archer(out UnitId archer, out UnitId husk)
    {
        var state = BoardBuilder.Open(9, 1)
            .PlayerB(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 3, 0, hp: 18)
            .Build();

        archer = state.Find(UnitKind.Archer).Id;
        husk = state.Find(UnitKind.Husk).Id;
        return state;
    }
}
