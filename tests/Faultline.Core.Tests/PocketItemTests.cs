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

    // ---- Thorn Pouch · brambles on one adjacent tile until round end ----------------------------

    [Fact]
    public void ThornPouch_GrowsRealBrambles_SoEveryRuleThatKnowsBramblesNeedsNoNewCase()
    {
        var state = Vanguard(out var duck, out _);
        var tile = new Coord(1, 0);

        Assert.Equal(TileType.Open, state.Board.At(tile));

        var scattered = state.WithPocket(duck, Consumable.ThornPouch)
            .Step(new UseConsumableCommand(duck, null, tile));

        // The board itself changed. Not a parallel list of pretend hazards — the tile *is* brambles,
        // which is what stops a second copy of the bramble rules existing (D-191).
        Assert.Equal(TileType.Spikes, scattered.NewState.Board.At(tile));

        var grew = scattered.Single<BramblesGrew>();
        Assert.Equal(tile, grew.At);
        Assert.Equal(state.Round, grew.ThroughRound);

        // And the way back is booked, remembering what the tile used to be rather than assuming.
        var booked = Assert.Single(scattered.NewState.TemporaryTerrain);
        Assert.Equal(tile, booked.At);
        Assert.Equal(TileType.Open, booked.Was);
    }

    [Fact]
    public void ThornPouch_FadesAtTheEndOfTheRoundItWasThrownIn()
    {
        var state = Vanguard(out var duck, out _);
        var tile = new Coord(1, 0);

        var scattered = state.WithPocket(duck, Consumable.ThornPouch)
            .Then(new UseConsumableCommand(duck, null, tile));

        int round = scattered.Round;

        // Played to the seam, not restored to it.
        var next = scattered;
        for (int i = 0; i < 40 && next.Round == round; i++)
        {
            next = next.PassCurrent().NewState;
        }

        Assert.Equal(round + 1, next.Round);
        Assert.Equal(TileType.Open, next.Board.At(tile));
        Assert.Empty(next.TemporaryTerrain);
    }

    [Fact]
    public void ThornPouch_WillNotScatterOnAnythingButOpenGroundBesideTheDuck()
    {
        var state = Vanguard(out var duck, out var husk);

        var holding = state.WithPocket(duck, Consumable.ThornPouch);

        // Not on a tile two away, not on the duck's own tile, and not under a body: the same filter
        // the Crate of Debris uses, so a pouch cannot delete a hazard or land somewhere unreachable.
        TestPlay.AssertIllegal(holding, new UseConsumableCommand(duck, null, new Coord(4, 0)));
        TestPlay.AssertIllegal(holding, new UseConsumableCommand(duck, null, holding.Get(duck).Position));
        TestPlay.AssertIllegal(holding, new UseConsumableCommand(duck, null, holding.Get(husk).Position));

        // Every refusal names its reason rather than quietly spending the pouch.
        Assert.Throws<IllegalCommandException>(
            () => holding.Then(new UseConsumableCommand(duck)));
    }

    // ---- Split Reed · swap with an adjacent ally, both owners consenting -------------------------

    [Fact]
    public void SplitReed_OffersASwap_AndMovesNobodyUntilTheOtherOwnerSaysYes()
    {
        var state = TwoFlocks(out var mine, out var theirs);

        var here = state.Get(mine).Position;
        var there = state.Get(theirs).Position;

        var offered = state.WithPocket(mine, Consumable.SplitReed)
            .Step(new UseConsumableCommand(mine, theirs));

        // The reed is spent and the offer stands — and nothing has moved. Consent is structural: the
        // other owner's yes is a command they may simply never send (D-192).
        Assert.Null(offered.NewState.Get(mine).Loadout.Pocket);
        Assert.Equal(mine, offered.NewState.Get(theirs).SplitReedOfferFrom);
        Assert.Equal(here, offered.NewState.Get(mine).Position);
        Assert.Equal(there, offered.NewState.Get(theirs).Position);
        Assert.False(offered.Has<DucksSwapped>());

        var announced = offered.Single<SplitReedOffered>();
        Assert.Equal(theirs, announced.UnitId);
        Assert.Equal(mine, announced.ByUnitId);
    }

    [Fact]
    public void SplitReed_TheOfferIsTheOtherOwnersToTake_AndNeverTakingItIsLegal()
    {
        var state = TwoFlocks(out var mine, out var theirs);

        var here = state.Get(mine).Position;
        var there = state.Get(theirs).Position;

        var offered = state.WithPocket(mine, Consumable.SplitReed)
            .Then(new UseConsumableCommand(mine, theirs));

        // The offerer cannot answer for the other body. There is no party-wide accept and no owner
        // field to ask — the command names one duck, and it is not this one.
        TestPlay.AssertNotLegal(offered, new TakeSplitReedCommand(mine));

        // Handed over by playing, and then it is on the answering owner's list.
        var toThem = offered;
        for (int i = 0; i < 10 && toThem.ActiveTeam != Team.PlayerB; i++)
        {
            toThem = toThem.PassCurrent().NewState;
        }

        TestPlay.AssertLegal(toThem, new TakeSplitReedCommand(theirs));

        var swapped = toThem.Step(new TakeSplitReedCommand(theirs));

        Assert.Equal(here, swapped.NewState.Get(theirs).Position);
        Assert.Equal(there, swapped.NewState.Get(mine).Position);
        Assert.Null(swapped.NewState.Get(theirs).SplitReedOfferFrom);

        var event_ = swapped.Single<DucksSwapped>();
        Assert.Equal(theirs, event_.UnitId);
        Assert.Equal(mine, event_.WithUnitId);
    }

    [Fact]
    public void SplitReed_IsAPlacement_SoNothingIsCollidedWithButLandingTerrainApplies()
    {
        // The ally stands on brambles. A swap is a placement: nobody travels, so no collision and no
        // Footing counter — but the duck that arrives on the thorns is bitten exactly as a walked
        // step is bitten (D-185, D-192).
        var state = BoardBuilder.Rows("^....")
            .PlayerA(UnitKind.Vanguard, 1, 0)
            .PlayerB(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 4, 0)
            .Build();

        var mine = state.Find(UnitKind.Vanguard).Id;
        var theirs = state.Find(UnitKind.Archer).Id;

        Assert.Equal(TileType.Spikes, state.Board.At(new Coord(0, 0)));

        var offered = state.WithPocket(mine, Consumable.SplitReed)
            .Then(new UseConsumableCommand(mine, theirs));

        var toThem = offered;
        for (int i = 0; i < 10 && toThem.ActiveTeam != Team.PlayerB; i++)
        {
            toThem = toThem.PassCurrent().NewState;
        }

        int before = toThem.Get(mine).Hp;
        var swapped = toThem.Step(new TakeSplitReedCommand(theirs));

        // The Vanguard landed on the brambles and paid the walk-on price. The Archer left them and
        // pays nothing — it did not arrive anywhere hostile.
        Assert.Equal(new Coord(0, 0), swapped.NewState.Get(mine).Position);
        Assert.Equal(before - Displacement.SpikeWalkDamage, swapped.NewState.Get(mine).Hp);

        var bite = swapped.Single<SpikeHit>();
        Assert.Equal(mine, bite.UnitId);
        Assert.True(bite.Voluntary);

        // A placement travels through nothing, so nothing was collided with and nobody was offered a
        // Footing refusal.
        Assert.False(swapped.Has<Collision>());
        Assert.False(swapped.Has<FootingChoiceRequested>());
        Assert.False(swapped.Has<UnitPushed>());
    }

    [Fact]
    public void SplitReed_RefusesAnOfferItCanNoLongerHonour_ByName()
    {
        var state = TwoFlocks(out var mine, out var theirs);

        var offered = state.WithPocket(mine, Consumable.SplitReed)
            .Then(new UseConsumableCommand(mine, theirs));

        // The offerer went over a ledge between the offer and the answer. The swap is not silently
        // performed and not silently dropped — it is refused, by name.
        var stranded = offered.WithUnit(offered.Get(mine) with { Clinging = true });

        Assert.False(Game.SplitReedSwapIsLegal(stranded, stranded.Get(theirs)));
        TestPlay.AssertIllegal(stranded, new TakeSplitReedCommand(theirs));

        // And a duck holding no offer at all is refused for its own reason.
        TestPlay.AssertIllegal(state, new TakeSplitReedCommand(theirs));
    }

    [Fact]
    public void SplitReed_WillNotReachPastAdjacency_OrTradeWithABodyOnALedge()
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .PlayerB(UnitKind.Archer, 3, 0)
            .Enemy(UnitKind.Husk, 5, 0)
            .Build();

        var mine = state.Find(UnitKind.Vanguard).Id;
        var far = state.Find(UnitKind.Archer).Id;
        var husk = state.Find(UnitKind.Husk).Id;

        var holding = state.WithPocket(mine, Consumable.SplitReed);

        Assert.Empty(Consumables.ReedPartners(holding, holding.Get(mine)));
        TestPlay.AssertIllegal(holding, new UseConsumableCommand(mine, far));

        // Never an enemy: §8.6 prints "an adjacent allied duck".
        TestPlay.AssertIllegal(holding, new UseConsumableCommand(mine, husk));

        // And aimless is refused rather than spent for nothing.
        TestPlay.AssertIllegal(holding, new UseConsumableCommand(mine));
    }

    [Fact]
    public void SplitReed_AnUnansweredOfferLapsesAtTheRoundSeam()
    {
        var state = TwoFlocks(out var mine, out var theirs);

        var offered = state.WithPocket(mine, Consumable.SplitReed)
            .Then(new UseConsumableCommand(mine, theirs));

        int round = offered.Round;
        var next = offered;
        for (int i = 0; i < 40 && next.Round == round; i++)
        {
            next = next.PassCurrent().NewState;
        }

        Assert.Equal(round + 1, next.Round);
        Assert.Null(next.Get(theirs).SplitReedOfferFrom);

        // The reed stays spent. Declining costs the answerer nothing and refunds the offerer nothing.
        Assert.Null(next.Get(mine).Loadout.Pocket);
    }

    // ---- Signal Whistle · swap two enemies in the queue, intents unchanged -----------------------

    [Fact]
    public void SignalWhistle_SwapsTwoEnemiesInThePublishedOrder()
    {
        var state = ThreeEnemies(out var duck, out var first, out var second, out var third);

        // The order as published before anything happens.
        Assert.Equal(new[] { first, second, third }, EnemyOrder(state));

        var blown = state.WithPocket(duck, Consumable.SignalWhistle)
            .Step(new UseConsumableCommand(duck, first, null, third));

        // TurnOrder — the strip the player actually reads — now names them the other way round.
        Assert.Equal(new[] { third, second, first }, EnemyOrder(blown.NewState));
    }

    [Fact]
    public void SignalWhistle_RepublishesTheOrderTheMomentItChanges()
    {
        var state = ThreeEnemies(out var duck, out var first, out var second, out var third);

        var blown = state.WithPocket(duck, Consumable.SignalWhistle)
            .Step(new UseConsumableCommand(duck, first, null, third));

        // §3 makes the order a contract, so a change to it is announced, not merely performed: the
        // event carries the whole resulting queue, so a renderer redraws the strip from the event and
        // never has to query state to notice (D-103, D-193).
        var announced = blown.Single<ActivationOrderChanged>();

        Assert.Equal(duck, announced.ByUnitId);
        Assert.Equal(new[] { third, second, first }, announced.EnemyOrder.ToArray());

        // And what it announces is exactly what the published order became — one contract, not two.
        Assert.Equal(EnemyOrder(blown.NewState), announced.EnemyOrder.ToArray());
    }

    [Fact]
    public void SignalWhistle_LeavesEveryIntentExactlyAsItWasDeclared()
    {
        var state = ThreeEnemies(out var duck, out var first, out var second, out var third)
            .WithIntents();

        Assert.NotEmpty(state.Intents);

        var blown = state.WithPocket(duck, Consumable.SignalWhistle)
            .Step(new UseConsumableCommand(duck, first, null, third));

        // Nothing re-declared: no IntentDeclared rides the swap. An intent's target is what is locked,
        // and its geometry resolves against the live board when it runs (D-021).
        Assert.False(blown.Has<IntentDeclared>());

        // And every intent is the same object it was, for every enemy — same action, same target,
        // same aim. The queue moved; the plans did not.
        foreach (var before in state.Intents)
        {
            var after = blown.NewState.Intents.Single(i => i.UnitId.Equals(before.UnitId));
            Assert.Equal(before, after);
        }
    }

    [Fact]
    public void SignalWhistle_WillNotReorderAnEnemyThatHasAlreadyActed()
    {
        var state = ThreeEnemies(out var duck, out var first, out var second, out var third);

        // "Two enemies that have not acted" is the whole gate: a spent slot has no place left in the
        // queue to trade, so swapping it would be a promise about a round that is over.
        var spent = state.WithUnit(state.Get(first) with { HasActivated = true })
            .WithPocket(duck, Consumable.SignalWhistle);

        Assert.DoesNotContain(first, Consumables.WhistleTargets(spent));
        TestPlay.AssertIllegal(spent, new UseConsumableCommand(duck, first, null, third));

        // The two that remain are still tradeable.
        TestPlay.AssertLegal(spent, new UseConsumableCommand(duck, second, null, third));
    }

    [Fact]
    public void SignalWhistle_RefusesOneEnemyTwice_AndOffersEachPairOnlyOnce()
    {
        var state = ThreeEnemies(out var duck, out var first, out _, out _);
        var holding = state.WithPocket(duck, Consumable.SignalWhistle);

        TestPlay.AssertIllegal(holding, new UseConsumableCommand(duck, first, null, first));
        TestPlay.AssertIllegal(holding, new UseConsumableCommand(duck, first));

        // Three enemies is three unordered pairs, offered once each. Offering (a,b) and (b,a) would be
        // two commands for one outcome, and a duplicate on the legal list is one a policy weights twice.
        var offered = Consumables.Legal(holding, holding.Get(duck))
            .OfType<UseConsumableCommand>()
            .ToList();

        Assert.Equal(3, offered.Count);
        Assert.Equal(3, offered.Distinct().Count());
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

    /// <summary>
    /// The enemy queue as the player reads it: what <see cref="TurnOrder"/> publishes, not what any
    /// private list happens to hold. Asserting on this rather than on <c>state.Units</c> is the point
    /// — a swap that moved a list without moving the strip would pass the wrong test.
    /// </summary>
    private static UnitId[] EnemyOrder(GameState state) =>
        TurnOrder.Upcoming(state)
            .Where(e => e.Kind == ActivationKind.Enemy && e.Round == state.Round && e.UnitId.HasValue)
            .Select(e => e.UnitId!.Value)
            .ToArray();

    private static GameState ThreeEnemies(
        out UnitId duck, out UnitId first, out UnitId second, out UnitId third)
    {
        var state = BoardBuilder.Open(9, 3)
            .PlayerA(UnitKind.Vanguard, 0, 1)
            .Enemy(UnitKind.Husk, 4, 0)
            .Enemy(UnitKind.Anchor, 6, 1)
            .Enemy(UnitKind.Husk, 8, 2)
            .Build();

        duck = state.Find(UnitKind.Vanguard).Id;

        var enemies = state.Units.Where(u => u.Team == Team.Enemy).Select(u => u.Id).ToList();
        first = enemies[0];
        second = enemies[1];
        third = enemies[2];
        return state;
    }

    private static GameState TwoFlocks(out UnitId mine, out UnitId theirs)
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .PlayerB(UnitKind.Archer, 1, 0)
            .Enemy(UnitKind.Husk, 5, 0)
            .Build();

        mine = state.Find(UnitKind.Vanguard).Id;
        theirs = state.Find(UnitKind.Archer).Id;
        return state;
    }

    private static GameState Vanguard(out UnitId duck, out UnitId husk)
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 3, 0)
            .Build();

        duck = state.Find(UnitKind.Vanguard).Id;
        husk = state.Find(UnitKind.Husk).Id;
        return state;
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
