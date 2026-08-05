using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// The Footing rework (MASTER_DESIGN §3 "Statuses", Design Log (t); D-143 to D-148). Footing counts
/// whole instances: spending it refuses one displacement, impact and all, and it is outside the
/// distance arithmetic entirely. Players are asked; enemies apply the drain-only policy.
/// </summary>
public class FootingInstanceTests
{
    // ---- (a) the instance model ----------------------------------------------------------------

    // The headline: a refusal negates the *impact*, not just the travel. A shove into a wall is 4
    // and a Stagger for a unit standing against it; refusing it is 0 and nothing.
    [Fact]
    public void Refusal_NegatesTheWholeInstance_NoDamageAndNoStagger()
    {
        var state = BoardBuilder.Open(4, 1)
            .PlayerA(UnitKind.Vanguard, 1, 0)
            .Enemy(UnitKind.Husk, 0, 0, footing: 1)
            .Build();

        var husk = state.Find(UnitKind.Husk);
        var events = new List<GameEvent>();

        // Its back is to the board edge, so an unrefused shove is a collision.
        var landed = Displacement.Resolve(
            state, husk.Id, new Coord(1, 0), DisplacementKind.Push, 1, refused: false, events: events);
        Assert.Equal(UnitTemplate.For(UnitKind.Husk).MaxHp - Displacement.CollisionDamage, landed.Get(husk.Id).Hp);

        var refusedEvents = new List<GameEvent>();
        var refused = Displacement.Resolve(
            state, husk.Id, new Coord(1, 0), DisplacementKind.Push, 1,
            refused: true, events: refusedEvents);

        Assert.Equal(UnitTemplate.For(UnitKind.Husk).MaxHp, refused.Get(husk.Id).Hp);
        Assert.False(refused.Get(husk.Id).Staggered);
        Assert.Equal(husk.Position, refused.Get(husk.Id).Position);
        Assert.Empty(refusedEvents.OfType<Collision>());
        Assert.Empty(refusedEvents.OfType<Staggered>());
        Assert.Empty(refusedEvents.OfType<UnitDamaged>());
        Assert.Single(refusedEvents.OfType<DisplacementRefused>());
        Assert.Equal(0, refused.Get(husk.Id).Footing);
    }

    // A refused hazard entry is not a hazard entry: no cling, no sweep, nothing to void.
    [Fact]
    public void Refusal_NegatesAHazardEntry()
    {
        var state = BoardBuilder.Rows(".XO.".Replace('X', '.'))
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, footing: 1)
            .Build();

        var husk = state.Find(UnitKind.Husk);
        var events = new List<GameEvent>();

        var after = Displacement.Resolve(
            state, husk.Id, new Coord(0, 0), DisplacementKind.Push, 2, refused: true, events: events);

        Assert.False(after.Get(husk.Id).Clinging);
        Assert.Empty(events.OfType<Clinging>());
    }

    // A refused instance does not consume the Stagger it would have spent for its +1: there is no
    // displacement left for the bonus to apply to.
    [Fact]
    public void Refusal_DoesNotConsumeTheTargetsStagger()
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 2, 0, footing: 1)
            .Build();

        var husk = state.Find(UnitKind.Husk).Id;
        var staggered = state.WithUnit(state.Get(husk) with { Staggered = true });
        var events = new List<GameEvent>();

        var after = Displacement.Resolve(
            staggered, husk, new Coord(0, 0), DisplacementKind.Push, 1, refused: true, events: events);

        Assert.True(after.Get(husk).Staggered);
    }

    // ---- (b) stacks ----------------------------------------------------------------------------

    // Two tokens are two refusals. The reserved Footing-2 fixture is what makes that assertable
    // without re-tiering a shipped board.
    [Fact]
    public void Stacks_TwoFootingBuysTwoSeparateRefusals()
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.BracedHusk, 3, 0)
            .Build();

        var heavy = state.Find(UnitKind.BracedHusk).Id;
        Assert.Equal(2, state.Get(heavy).Footing);

        for (int refusal = 1; refusal <= 2; refusal++)
        {
            var events = new List<GameEvent>();
            var before = state.Get(heavy).Position;
            state = Displacement.Resolve(
                state, heavy, before + new Coord(-1, 0), DisplacementKind.Push, 1,
                refused: true, events: events);

            Assert.Equal(before, state.Get(heavy).Position);
            Assert.Equal(2 - refusal, state.Get(heavy).Footing);
        }

        // Out of tokens: the third shove lands.
        var last = new List<GameEvent>();
        var from = state.Get(heavy).Position;
        state = Displacement.Resolve(
            state, heavy, from + new Coord(-1, 0), DisplacementKind.Push, 1,
            refused: true, events: last);

        Assert.NotEqual(from, state.Get(heavy).Position);
        Assert.Empty(last.OfType<DisplacementRefused>());
    }

    // The fixture is a fixture: it exists for the rules, not for a board.
    [Fact]
    public void Stacks_TheFootingTwoFixtureIsFieldedByNoFight()
    {
        foreach (var parsed in FightLibrary.LoadAll())
        {
            var fight = parsed.Fight;
            if (fight is null)
            {
                continue;
            }

            Assert.DoesNotContain(fight.Enemies, e => e.Kind == UnitKind.BracedHusk);
            Assert.DoesNotContain(fight.RosterA, k => k == UnitKind.BracedHusk);
            Assert.DoesNotContain(fight.RosterB, k => k == UnitKind.BracedHusk);
        }
    }

    // ---- (c) enemy auto-spend, and what a refusal earns ----------------------------------------

    // Drain-only, and nothing else. This is what leaves the Fisher a bait line: a flick that is not
    // drain-bound is eaten, and the tokens stay on the board until she wants them gone.
    [Fact]
    public void AutoSpend_FiresOnlyOnADrainBoundInstance()
    {
        var drainBound = BoardBuilder.Rows("..O..")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, footing: 1)
            .Build();

        Assert.True(Displacement.EnemyWouldRefuse(
            drainBound, drainBound.Find(UnitKind.Husk).Id, new Coord(0, 0), DisplacementKind.Push, 1));

        foreach (string row in new[] { ".....", "..^..", "..#.." })
        {
            var other = BoardBuilder.Rows(row)
                .PlayerA(UnitKind.Vanguard, 0, 0)
                .Enemy(UnitKind.Husk, 1, 0, footing: 1)
                .Build();

            Assert.False(
                Displacement.EnemyWouldRefuse(
                    other, other.Find(UnitKind.Husk).Id, new Coord(0, 0), DisplacementKind.Push, 1),
                "refused a non-drain instance on row " + row);
        }
    }

    // A fully refused displacement earns its caster nothing — the same principle as a fully negated
    // absorb charging the Wardbearer nothing (D-088).
    [Fact]
    public void ARefusedReel_EarnsTheFisherNoPluck()
    {
        var state = BoardBuilder.Rows(".O...")
            .PlayerB(UnitKind.Threadcaster, 0, 0)
            .Enemy(UnitKind.Husk, 3, 0, footing: 1)
            .Build();

        var fisher = state.Find(UnitKind.Threadcaster).Id;
        var husk = state.Find(UnitKind.Husk).Id;

        var result = state.Step(new AbilityCommand(fisher, Ability.Reel, husk));

        Assert.Single(result.All<DisplacementRefused>());
        Assert.Empty(result.All<VerveCharged>());
        Assert.Equal(0, result.NewState.Get(fisher).Verve);
    }

    [Fact]
    public void ARefusedFlick_EarnsTheFisherNoPluck()
    {
        var state = BoardBuilder.Rows(".O.")
            .PlayerB(UnitKind.Threadcaster, 0, 0)
            .Enemy(UnitKind.Husk, 2, 0, footing: 1)
            .Active(Team.PlayerB)
            .Build();

        var fisher = state.Find(UnitKind.Threadcaster).Id;
        var husk = state.Find(UnitKind.Husk).Id;

        var result = state.Step(new AttackCommand(fisher, husk, AttackMode.Pull));

        Assert.Single(result.All<DisplacementRefused>());
        Assert.Empty(result.All<VerveCharged>());
        Assert.Equal(0, result.NewState.Get(fisher).Verve);
    }

    // ---- (d) the Cast threshold, on the reserved stacked fixture --------------------------------

    // The Footing-2 fixture refuses a Cast and is left holding nothing: two tokens, one refusal.
    [Fact]
    public void CastThreshold_TheFootingTwoFixtureRefusesOnceThenHoldsZero()
    {
        var state = BoardBuilder.Rows(
                ".....",
                ".O...",
                ".....")
            .PlayerA(UnitKind.Threadcaster, 2, 1)
            .Enemy(UnitKind.BracedHusk, 3, 1)
            .Build();

        var fisher = state.Find(UnitKind.Threadcaster).Id;
        var heavy = state.Find(UnitKind.BracedHusk).Id;
        var drain = new Coord(1, 1);

        var charged = state.WithUnit(state.Get(fisher) with { Verve = Verve.Cap });

        Assert.Equal(2, charged.Get(heavy).Footing);
        Assert.Equal(CastOutlook.Refused, Throw.Outlook(charged, charged.Get(heavy), drain));

        var result = charged.Step(new SpendVerveCommand(fisher, VerveSpend.Cast, heavy, drain));

        Assert.True(result.Has<CastRefused>());
        Assert.Equal(0, result.NewState.Get(heavy).Footing);
        Assert.Equal(new Coord(3, 1), result.NewState.Get(heavy).Position);

        // And with nothing left it cannot brace at all.
        Assert.Equal(
            CastOutlook.Lands,
            Throw.Outlook(result.NewState, result.NewState.Get(heavy), drain));
    }

    // ---- (e) the player prompt -----------------------------------------------------------------

    [Fact]
    public void Prompt_IsRaisedWhenAnEnemyDisplacesAPlayerUnitHoldingFooting()
    {
        var state = Grappled(out var grappler, out var archer);
        var result = state.Step(Game.NextEnemyCommand(state)!);

        Assert.True(Game.IsAwaitingFootingChoice(result.NewState));

        var asked = result.Single<FootingChoiceRequested>();
        Assert.Equal(archer, asked.TargetId);
        Assert.Equal(Footing.DisplacementCost, asked.Cost);
        Assert.Equal(1, asked.Held);

        // The board has not moved: nothing of the raising command has run.
        Assert.Equal(state.Get(archer).Position, result.NewState.Get(archer).Position);
        Assert.Equal(state.Get(grappler).Position, result.NewState.Get(grappler).Position);
    }

    // Hotseat: the prompt belongs to the *owning* player whatever slot raised it, and nothing else
    // is legal until it is answered. There is no timeout — the prompt waits.
    [Fact]
    public void Prompt_BelongsToTheOwner_AndBlocksEveryOtherCommand()
    {
        var state = Grappled(out _, out var archer);
        var asked = state.Step(Game.NextEnemyCommand(state)!).NewState;

        Assert.Equal(Team.Enemy, asked.ActiveTeam);
        Assert.Equal(Team.PlayerA, asked.FootingPrompt!.Owner);
        Assert.Null(Game.NextEnemyCommand(asked));

        var legal = Game.LegalCommands(asked);
        Assert.Equal(2, legal.Count);
        Assert.All(legal, c => Assert.IsType<FootingRefuseCommand>(c));
        Assert.Contains(legal, c => ((FootingRefuseCommand)c).Refuse);
        Assert.Contains(legal, c => !((FootingRefuseCommand)c).Refuse);

        TestPlay.AssertIllegal(asked, new EndActivationCommand(archer));
    }

    [Fact]
    public void Prompt_Declined_TheDisplacementResolvesNormally()
    {
        var state = Grappled(out _, out var archer);
        var asked = state.Step(Game.NextEnemyCommand(state)!).NewState;

        var answered = asked.Step(new FootingRefuseCommand(archer, false));

        Assert.False(Game.IsAwaitingFootingChoice(answered.NewState));
        Assert.NotEqual(state.Get(archer).Position, answered.NewState.Get(archer).Position);
        Assert.Equal(1, answered.NewState.Get(archer).Footing);
        Assert.Empty(answered.All<DisplacementRefused>());
    }

    [Fact]
    public void Prompt_Refused_TheDisplacementDoesNotHappenAndCostsOneFooting()
    {
        var state = Grappled(out _, out var archer);
        var asked = state.Step(Game.NextEnemyCommand(state)!).NewState;

        var answered = asked.Step(new FootingRefuseCommand(archer, true));

        Assert.False(Game.IsAwaitingFootingChoice(answered.NewState));
        Assert.Equal(state.Get(archer).Position, answered.NewState.Get(archer).Position);
        Assert.Equal(0, answered.NewState.Get(archer).Footing);
        Assert.Single(answered.All<DisplacementRefused>());
    }

    // An answer is scoped to the instance it answered, and is gone the moment that command finishes.
    [Fact]
    public void Prompt_TheAnswerDoesNotLeakIntoTheNextCommand()
    {
        var state = Grappled(out _, out var archer);
        var asked = state.Step(Game.NextEnemyCommand(state)!).NewState;
        var answered = asked.Step(new FootingRefuseCommand(archer, false)).NewState;

        Assert.Empty(answered.FootingAnswers);
    }

    // The prime directive, through an interrupt: seed plus the command log — refusal answers
    // included — reproduces the state exactly. The answer is a command, and it is RNG-free.
    [Fact]
    public void Prompt_ReplayThroughAPromptSequence_IsDeterministic()
    {
        var start = Grappled(out _, out var archer);
        var log = new List<Command>
        {
            Game.NextEnemyCommand(start)!,
            new FootingRefuseCommand(archer, true),
        };

        var first = Replay(start, log);
        var second = Replay(start, log);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.Equal(start.RngState, first.RngState);
        Assert.Equal(0, first.Get(archer).Footing);
    }

    // Both answers replay, and they replay to *different* fights — which is why a decline has to be
    // a command in the log rather than an absence.
    [Fact]
    public void Prompt_ADeclineAndARefusalReplayToDifferentStates()
    {
        var start = Grappled(out _, out var archer);
        var enemy = Game.NextEnemyCommand(start)!;

        var refused = Replay(start, new List<Command> { enemy, new FootingRefuseCommand(archer, true) });
        var declined = Replay(start, new List<Command> { enemy, new FootingRefuseCommand(archer, false) });

        Assert.NotEqual(refused, declined);
    }

    // ---- Bedraggled ----------------------------------------------------------------------------

    // A duck that comes back Bedraggled comes back with whatever Footing it was carrying: the flag's
    // only teeth are the omitted slot (MASTER_DESIGN §3).
    [Fact]
    public void Bedraggled_ReturnsWithItsUnspentFootingIntact()
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Wardbearer, 1, 0, footing: 1, bedraggled: true)
            .Enemy(UnitKind.Husk, 5, 0)
            .Build();

        var wardbearer = state.Find(UnitKind.Wardbearer);

        Assert.True(wardbearer.Bedraggled);
        Assert.Equal(1, wardbearer.Footing);
    }

    // ---- fixtures ------------------------------------------------------------------------------

    /// <summary>
    /// A Grappler in reach of an Archer holding one granted Footing. The Grappler's whole action is
    /// the pull, so its slot always produces a displacement instance, and the Archer has no push
    /// resistance to eat it before the prompt can be raised.
    /// </summary>
    private static GameState Grappled(out UnitId grappler, out UnitId archer)
    {
        var state = BoardBuilder.Open(7, 1)
            .PlayerA(UnitKind.Archer, 4, 0, footing: 1)
            .Enemy(UnitKind.Grappler, 1, 0)
            .Active(Team.Enemy)
            .Build();

        grappler = state.Find(UnitKind.Grappler).Id;
        archer = state.Find(UnitKind.Archer).Id;
        return state;
    }

    private static GameState Replay(GameState start, IReadOnlyList<Command> log)
    {
        var state = start;
        foreach (var command in log)
        {
            state = Game.Apply(state, command).NewState;
        }

        return state;
    }
}
