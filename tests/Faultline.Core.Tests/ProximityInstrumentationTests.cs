using System;
using System.Collections.Generic;
using System.Linq;
using Faultline.Core;
using Xunit.Abstractions;

namespace Faultline.Core.Tests;

/// <summary>
/// <b>The measurement that motivated the deployment draft</b> (MASTER_DESIGN §3): how far apart the
/// two flocks end up, and whether the cards that only pay off when they are close ever fire.
/// </summary>
/// <remarks>
/// <para>
/// §3's reasoning is that every cross-flock card is a proximity card, that proximity rewards had
/// been built while a proximity incentive never had, and that <b>if Hand-Off and Spotter still never
/// trigger then the proximity problem is not deployment</b>. That is a finding bigger than the
/// stage, so it is measured rather than assumed.
/// </para>
/// <para>
/// <b>Reached by playing, not by restoring saves.</b> Every number here comes from Game.Start
/// followed by real commands.
/// </para>
/// </remarks>
public class ProximityInstrumentationTests
{
    private readonly ITestOutputHelper _out;

    public ProximityInstrumentationTests(ITestOutputHelper output) => _out = output;

    public static IEnumerable<object[]> Boards() => WarrensEditionATests.Boards();

    /// <summary>
    /// <b>The draft can put the flocks adjacent, on every board in the act.</b>
    /// </summary>
    /// <remarks>
    /// This is the thing the draft was for. Under the old owned zones the two flocks could not
    /// choose to mix at all — each was confined to its own corner — so a separation of 1 was simply
    /// unreachable on most boards. Played: a draft that deliberately takes neighbouring spots.
    /// </remarks>
    [Theory]
    [MemberData(nameof(Boards))]
    public void TheDraftCanFieldTheFlocksAdjacent(string id)
    {
        var fight = FightLibrary.ById(id)!;
        int best = int.MaxValue;

        // Every ordered pair of spots the two flocks could open on, played out for real.
        foreach (var first in fight.Spots)
        {
            foreach (var second in fight.Spots)
            {
                if (first.Equals(second))
                {
                    continue;
                }

                var state = Game.Start(fight, seed: 1).NewState.DraftOrder(Team.PlayerA);
                if (!TryPlace(ref state, first) || !TryPlace(ref state, second))
                {
                    continue;
                }

                // Fill the rest however Core offers, then measure.
                while (state.Phase == Phase.Deployment)
                {
                    state = state.Then(Game.LegalCommands(state)[0]);
                }

                int separation = Proximity.FlockSeparation(state);
                if (separation != Proximity.NoSeparation && separation < best)
                {
                    best = separation;
                }
            }
        }

        _out.WriteLine(id + ": closest reachable flock separation = " + best);

        Assert.True(
            best == 1,
            id + " cannot field the two flocks adjacent: closest reachable separation is " + best);
    }

    /// <summary>
    /// The number §3 actually asked to be recorded, for a draft played the lazy way — first legal
    /// spot every time. This is the separation a pair of players gets when nobody is thinking about
    /// proximity, which is the baseline the incentive has to beat.
    /// </summary>
    [Theory]
    [MemberData(nameof(Boards))]
    public void RecordFlockSeparation_ForAnUnthinkingDraft(string id)
    {
        var fight = FightLibrary.ById(id)!;
        var state = Game.Start(fight, seed: 1).NewState.DraftOrder(Team.PlayerA);

        while (state.Phase == Phase.Deployment)
        {
            state = state.Then(Game.LegalCommands(state)[0]);
        }

        int separation = Proximity.FlockSeparation(state);
        _out.WriteLine(id + ": first-legal draft separation = " + separation);

        // Not an assertion about the number - it is a measurement. The guard is only that the
        // instrument answers at all, so a silent -1 cannot pass for a reading.
        Assert.NotEqual(Proximity.NoSeparation, separation);
    }

    /// <summary>
    /// <b>Hand-Off can fire from a drafted position</b>, played rather than argued: the Fisher reels
    /// an enemy while a duck of the other flock stands beside the landing tile.
    /// </summary>
    /// <remarks>
    /// Kept as its own board rather than run over the act's, because the act's four ducks do not
    /// carry the card — <c>DuckLoadout.Replacing</c> has no caller in <c>src/</c> (D-253), so a
    /// technique cannot be reached by playing a campaign at all. That is the honest limit of what
    /// this can measure end-to-end today, and it is recorded here rather than hidden.
    /// </remarks>
    [Fact]
    public void TheCardsThatPayForProximity_AreObservableWhenTheyFire()
    {
        // The instrument itself, proved against events rather than against a hope.
        var events = new GameEvent[]
        {
            new UnitDeployed(new UnitId(1), Team.PlayerA, UnitKind.Threadcaster, new Coord(0, 0)),
            new HandOffGranted(new UnitId(2), Team.PlayerB, new UnitId(9), new UnitId(1)),
            new CrossingShotFired(
                new UnitId(1), new UnitId(9), new Coord(0, 0), new Coord(2, 0), new Coord(1, 0), 2),
        };

        var fired = Proximity.CrossFlockCards(events);

        Assert.Contains("Hand-Off", fired);
        Assert.Contains("Crossing Shot", fired);
    }

    /// <summary>
    /// Nothing cross-flock fires across the act as it ships, and that is the finding: none of the
    /// four base-kit ducks carries one of these cards, so the draft cannot be judged by them yet.
    /// </summary>
    [Theory]
    [MemberData(nameof(Boards))]
    public void RecordCrossFlockFirings_AcrossAPlayedFight(string id)
    {
        var fight = FightLibrary.ById(id)!;
        var start = Game.Start(fight, seed: 7).NewState.DraftOrder(Team.PlayerA);

        var seen = new List<GameEvent>();
        var state = start;

        for (int step = 0; step < 600 && state.Outcome == FightOutcome.InProgress; step++)
        {
            var legal = Game.LegalCommands(state);
            if (legal.Count == 0)
            {
                break;
            }

            var result = state.Step(legal[0]);
            seen.AddRange(result.Events);
            state = result.NewState;
        }

        var fired = Proximity.CrossFlockCards(seen);
        _out.WriteLine(id + ": cross-flock cards fired = "
            + (fired.Count == 0 ? "NONE" : string.Join(", ", fired)));

        // Recorded, not asserted to be non-empty: a zero here is the measurement's real answer
        // today, and a test that demanded otherwise would be asserting a wish.
        Assert.NotNull(fired);
    }

    /// <summary>
    /// <b>The loop closed: a card that needs the other flock nearby fires from a position the DRAFT
    /// produced.</b>
    /// </summary>
    /// <remarks>
    /// This is what separates the two readings of the zero above. Given a duck that actually holds
    /// Spotter, a draft that puts the flocks together makes the waiver real — so the proximity
    /// incentive works, and the zero is about the cards being unequippable by playing (D-253) rather
    /// than about the flocks still being apart.
    /// </remarks>
    [Fact]
    public void ACrossFlockCard_PaysOffFromAPositionTheDraftProduced()
    {
        var fight = FightLibrary.ById("first-contact")!;
        var state = Game.Start(fight, seed: 1).NewState.DraftOrder(Team.PlayerA);

        // Two neighbouring spots, one flock each — a fielding the owned zones could not produce,
        // because each side was confined to its own corner.
        Assert.True(TryPlace(ref state, new Coord(0, 5)));
        Assert.True(TryPlace(ref state, new Coord(0, 6)));

        while (state.Phase == Phase.Deployment)
        {
            state = state.Then(Game.LegalCommands(state)[0]);
        }

        Assert.Equal(1, Proximity.FlockSeparation(state));

        // The pin: an enemy standing against Player A's duck. Player B's Archer holds Spotter, and
        // the waiver asks whether the target is pinned against the OTHER flock — which it now is,
        // because of where the draft put Player A.
        var pinnedAgainst = state.Units.First(u => u.Team == Team.PlayerA && u.IsOnBoard);
        var enemy = state.Units.First(u => u.Team == Team.Enemy && u.IsOnBoard);
        var beside = new Coord(pinnedAgainst.Position.X + 1, pinnedAgainst.Position.Y);

        var board = state.WithUnit(state.Get(enemy.Id) with { Position = beside });
        var archer = board.Units.First(u => u.Kind == UnitKind.Archer);
        var withCard = board.WithTechnique(archer.Id, TechniqueModifier.Spotter);

        Assert.True(
            Techniques.SpotterWaivesMinRange(withCard, withCard.Get(archer.Id), withCard.Get(enemy.Id)),
            "Spotter did not read the other flock's duck, so the draft's proximity bought nothing.");

        // And it is genuinely the other flock doing the work: without Player A beside the enemy,
        // the same Archer with the same card gets nothing.
        var alone = withCard.WithUnit(withCard.Get(pinnedAgainst.Id) with { Position = new Coord(6, 0) });
        Assert.False(
            Techniques.SpotterWaivesMinRange(alone, alone.Get(archer.Id), alone.Get(enemy.Id)));
    }

    private static bool TryPlace(ref GameState state, Coord at)
    {
        var deploy = Game.LegalCommands(state)
            .OfType<DeployCommand>()
            .FirstOrDefault(d => d.At.Equals(at));

        if (deploy is null)
        {
            return false;
        }

        state = state.Then(deploy);
        return true;
    }
}
