using System.Linq;
using Faultline.Core;
using Xunit;

namespace Faultline.Core.Tests;

/// <summary>
/// The Fisher's reach and her meter. Two things are being pinned here: the heavy earns a tile the
/// basic does not, and the haul itself is worth a point on top of whatever the landing was worth.
/// </summary>
public sealed class FisherKitTests
{
    // ---- reach ---------------------------------------------------------------------------

    [Fact]
    public void Reel_ReachesFourTiles()
    {
        Assert.Equal(4, AbilityDefinition.For(Ability.Reel).Range);
    }

    [Fact]
    public void TheBasicFlick_StillStopsAtThree_SoOnlyTheHeavyEarnedTheReach()
    {
        Assert.Equal(3, UnitTemplate.For(UnitKind.Threadcaster).Range);
        Assert.Equal(3, UnitTemplate.For(UnitKind.Threadcaster).BasicReach);
    }

    [Fact]
    public void AtFourTiles_ReelIsOffered_AndTheFlickIsNot()
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Threadcaster, 0, 0)
            .Enemy(UnitKind.Husk, 4, 0, hp: 20)
            .Build();

        var caster = state.Find(UnitKind.Threadcaster);
        var husk = state.Find(UnitKind.Husk);

        Assert.Contains(husk.Id, Abilities.LegalTargets(state, caster));
        TestPlay.AssertLegal(state, new AbilityCommand(caster.Id, Ability.Reel, husk.Id));

        TestPlay.AssertIllegal(state, new AttackCommand(caster.Id, husk.Id));
        TestPlay.AssertIllegal(state, new AttackCommand(caster.Id, husk.Id, AttackMode.Pull));
    }

    // The line flies over rock and body alike — there is no line of sight in this game (D-010), and
    // the extra tile of reach did not quietly introduce one.
    [Fact]
    public void Reel_AtFourTiles_IsStillOfferedThroughAWall()
    {
        var state = BoardBuilder.Rows(".#...")
            .PlayerA(UnitKind.Threadcaster, 0, 0)
            .Enemy(UnitKind.Husk, 4, 0, hp: 20)
            .Build();

        var caster = state.Find(UnitKind.Threadcaster);
        var husk = state.Find(UnitKind.Husk);

        Assert.Contains(husk.Id, Abilities.LegalTargets(state, caster));
    }

    // ---- the haul charges on its own length ----------------------------------------------

    [Fact]
    public void Reel_DraggingThreeTiles_ChargesAPluckForTheHaulAlone()
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Threadcaster, 0, 0)
            .Enemy(UnitKind.Husk, 4, 0, hp: 20)
            .Build();

        var caster = state.Find(UnitKind.Threadcaster);
        var husk = state.Find(UnitKind.Husk);

        var result = state.Step(new AbilityCommand(caster.Id, Ability.Reel, husk.Id));

        Assert.Equal(3, result.Single<UnitPushed>().Path.Count);
        Assert.False(result.Has<Collision>());
        Assert.False(result.Has<SpikeHit>());

        Assert.Equal(1, result.NewState.Get(caster.Id).Verve);
        Assert.Equal(VerveSource.LongPull, result.Single<VerveCharged>().Source);
    }

    [Fact]
    public void Reel_DraggingTwoTiles_ChargesNothingForTheHaul()
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Threadcaster, 0, 0)
            .Enemy(UnitKind.Husk, 3, 0, hp: 20)
            .Build();

        var caster = state.Find(UnitKind.Threadcaster);
        var husk = state.Find(UnitKind.Husk);

        var result = state.Step(new AbilityCommand(caster.Id, Ability.Reel, husk.Id));

        Assert.Equal(2, result.Single<UnitPushed>().Path.Count);
        Assert.False(result.Has<VerveCharged>());
        Assert.Equal(0, result.NewState.Get(caster.Id).Verve);
    }

    // ---- double pay -----------------------------------------------------------------------

    [Fact]
    public void Reel_DraggingThreeTilesIntoACollision_ChargesAPluckOnTopOfTheSlam()
    {
        // Four tiles of travel out of a range-4 Reel: three requested plus the Stagger the target is
        // already carrying. It walks 3 and the fourth step runs into the Fisher herself.
        var built = BoardBuilder.Open(5, 1)
            .PlayerA(UnitKind.Threadcaster, 0, 0)
            .Enemy(UnitKind.Husk, 4, 0, hp: 20)
            .Build();

        var caster = built.Find(UnitKind.Threadcaster);
        var husk = built.Find(UnitKind.Husk);
        var state = built.WithUnit(husk with { Staggered = true });

        var result = state.Step(new AbilityCommand(caster.Id, Ability.Reel, husk.Id));

        Assert.Equal(3, result.Single<UnitPushed>().Path.Count);
        Assert.True(result.Has<Collision>());

        var charges = result.All<VerveCharged>();
        Assert.Equal(2, charges.Count);
        Assert.Contains(charges, c => c.Source == VerveSource.LongPull);
        Assert.Contains(charges, c => c.Source == VerveSource.Collision);
        Assert.All(charges, c => Assert.Equal(caster.Id, c.UnitId));
        Assert.Equal(2, result.NewState.Get(caster.Id).Verve);
    }

    [Fact]
    public void Reel_DraggingThreeTilesOntoSpikes_ChargesAPluckOnTopOfTheHazard()
    {
        var built = BoardBuilder.Rows(".^...")
            .PlayerA(UnitKind.Threadcaster, 0, 0)
            .Enemy(UnitKind.Husk, 4, 0, hp: 20)
            .Build();

        var caster = built.Find(UnitKind.Threadcaster);
        var husk = built.Find(UnitKind.Husk);

        var result = built.Step(new AbilityCommand(caster.Id, Ability.Reel, husk.Id));

        Assert.Equal(3, result.Single<UnitPushed>().Path.Count);
        Assert.True(result.Has<SpikeHit>());

        var charges = result.All<VerveCharged>();
        Assert.Equal(2, charges.Count);
        Assert.Contains(charges, c => c.Source == VerveSource.LongPull);
        Assert.Contains(charges, c => c.Source == VerveSource.Hazard);
        Assert.Equal(2, result.NewState.Get(caster.Id).Verve);
    }

    // The length gate is the whole point: a slam is worth one point, not two, unless the haul that
    // set it up was itself long.
    [Fact]
    public void Reel_ShortDragIntoACollision_StillChargesOnlyForTheSlam()
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Threadcaster, 0, 0)
            .Enemy(UnitKind.Anchor, 1, 0, hp: 20)
            .Enemy(UnitKind.Husk, 4, 0, hp: 20)
            .Build();

        var caster = state.Find(UnitKind.Threadcaster);
        var husk = state.Find(UnitKind.Husk);

        var result = state.Step(new AbilityCommand(caster.Id, Ability.Reel, husk.Id));

        Assert.Equal(2, result.Single<UnitPushed>().Path.Count);
        Assert.True(result.Has<Collision>());

        Assert.Equal(1, result.NewState.Get(caster.Id).Verve);
        Assert.Equal(VerveSource.Collision, result.Single<VerveCharged>().Source);
    }

    // ---- the gate holds for everybody else -------------------------------------------------

    [Fact]
    public void TheBasicPull_CannotReachThreeTiles_SoItNeverChargesForLength()
    {
        var built = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Threadcaster, 0, 0)
            .Enemy(UnitKind.Husk, 3, 0, hp: 20)
            .Build();

        var caster = built.Find(UnitKind.Threadcaster);
        var husk = built.Find(UnitKind.Husk);

        // Even Staggered the basic drag is 2, one short of the gate.
        var state = built.WithUnit(husk with { Staggered = true });
        var result = state.Step(new AttackCommand(caster.Id, husk.Id, AttackMode.Pull));

        Assert.Equal(2, result.Single<UnitPushed>().Path.Count);
        Assert.DoesNotContain(result.All<VerveCharged>(), c => c.Source == VerveSource.LongPull);
    }

    [Fact]
    public void AGrapplersOwnLongPull_ChargesNobody_BecauseEnemiesNeverCharge()
    {
        var built = BoardBuilder.Open(6, 1)
            .Enemy(UnitKind.Grappler, 0, 0)
            .PlayerA(UnitKind.Vanguard, 3, 0)
            .Build();

        var grappler = built.Find(UnitKind.Grappler);
        var vanguard = built.Find(UnitKind.Vanguard);

        var state = built.WithUnit(vanguard with { Staggered = true });
        var result = state.Step(new AttackCommand(grappler.Id, vanguard.Id, AttackMode.Pull));

        Assert.False(result.Has<VerveCharged>());
    }

    // ---- the card says what the rule does ---------------------------------------------------

    [Fact]
    public void TheFishersCard_NamesTheHaulAlongsideTheLanding()
    {
        Assert.True(Verve.Charges(UnitKind.Threadcaster, VerveSource.LongPull));
        Assert.True(Verve.Charges(UnitKind.Threadcaster, VerveSource.Collision));
        Assert.True(Verve.Charges(UnitKind.Threadcaster, VerveSource.Hazard));

        Assert.Contains("3 tiles", Verve.ConditionFor(UnitKind.Threadcaster));
        Assert.False(Verve.Charges(UnitKind.Vanguard, VerveSource.LongPull));
        Assert.False(Verve.Charges(UnitKind.Archer, VerveSource.LongPull));
        Assert.False(Verve.Charges(UnitKind.Wardbearer, VerveSource.LongPull));
    }
}
