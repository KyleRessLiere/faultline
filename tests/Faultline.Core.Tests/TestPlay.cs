using System;
using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>Small helpers for driving <see cref="Game"/> from tests.</summary>
public static class TestPlay
{
    /// <summary>Applies one command and returns the result.</summary>
    public static StepResult Step(this GameState state, Command command) => Game.Apply(state, command);

    /// <summary>Applies one command and returns only the next state.</summary>
    public static GameState Then(this GameState state, Command command) => Game.Apply(state, command).NewState;

    /// <summary>The single event of the given type, failing the test if there is not exactly one.</summary>
    public static T Single<T>(this StepResult result)
        where T : GameEvent => result.Events.OfType<T>().Single();

    /// <summary>Every event of the given type, in order.</summary>
    public static IReadOnlyList<T> All<T>(this StepResult result)
        where T : GameEvent => result.Events.OfType<T>().ToList();

    /// <summary>True when the result contains at least one event of the given type.</summary>
    public static bool Has<T>(this StepResult result)
        where T : GameEvent => result.Events.OfType<T>().Any();

    /// <summary>The unit with the given id.</summary>
    public static Unit Get(this GameState state, UnitId id) => state.UnitById(id);

    /// <summary>The first unit of a kind, for tests that place one of each.</summary>
    public static Unit Find(this GameState state, UnitKind kind) =>
        state.Units.First(u => u.Kind == kind);

    /// <summary>
    /// Plays a fight forward by always taking the first legal command. Deterministic given the state,
    /// which is what makes it usable as a replay fixture.
    /// </summary>
    public static (GameState State, List<Command> Log) PlayFirstLegal(GameState state, int maxSteps)
    {
        var log = new List<Command>();

        for (int i = 0; i < maxSteps; i++)
        {
            var legal = Game.LegalCommands(state);
            if (legal.Count == 0)
            {
                break;
            }

            var command = legal[0];
            log.Add(command);
            state = Game.Apply(state, command).NewState;
        }

        return (state, log);
    }

    /// <summary>
    /// Plays a fight forward with the enemy side driven by Core's planner and the players taking the
    /// first legal command. Every command — the AI's included — goes through <see cref="Game.Apply"/>
    /// and lands in the log, so the log alone replays the fight.
    /// </summary>
    public static (GameState State, List<Command> Log, int Steps) PlayWithAi(GameState start, int maxSteps)
    {
        var state = start;
        var log = new List<Command>();
        int steps = 0;

        for (; steps < maxSteps; steps++)
        {
            var command = Game.NextEnemyCommand(state);
            if (command is null)
            {
                var legal = Game.LegalCommands(state);
                if (legal.Count == 0)
                {
                    break;
                }

                command = legal[0];
            }

            log.Add(command);
            state = Game.Apply(state, command).NewState;
        }

        return (state, log, steps);
    }

    /// <summary>Declares intents for every enemy, as the start of a round would.</summary>
    public static GameState WithIntents(this GameState state) =>
        Ai.DeclareAll(state, new List<GameEvent>());

    /// <summary>Replays a recorded command log against a fresh state.</summary>
    public static GameState Replay(GameState start, IEnumerable<Command> log)
    {
        var state = start;
        foreach (var command in log)
        {
            state = Game.Apply(state, command).NewState;
        }

        return state;
    }

    /// <summary>Asserts a command is rejected as illegal.</summary>
    public static void AssertIllegal(GameState state, Command command) =>
        Assert.Throws<IllegalCommandException>(() => Game.Apply(state, command));

    /// <summary>Asserts a command appears in the legal set.</summary>
    public static void AssertLegal(GameState state, Command command) =>
        Assert.Contains(command, Game.LegalCommands(state));

    /// <summary>Asserts a command does not appear in the legal set.</summary>
    public static void AssertNotLegal(GameState state, Command command) =>
        Assert.DoesNotContain(command, Game.LegalCommands(state));

    /// <summary>
    /// Where a rescue by this unit would set the rescued one down, if nobody chose. The enemy
    /// planner's pick, and the natural default for a test that is not about the choice (D-082).
    /// </summary>
    public static Coord RescueTo(this GameState state, UnitId rescuerId) =>
        Pits.DefaultRescueDestination(state, state.UnitById(rescuerId))!.Value;

    /// <summary>A rescue command that sets the ally down wherever the default would put them.</summary>
    public static RescueCommand Rescue(this GameState state, UnitId rescuerId, UnitId clingingId) =>
        new RescueCommand(rescuerId, clingingId, state.RescueTo(rescuerId));

    /// <summary>Convenience for building a coordinate.</summary>
    public static Coord At(int x, int y) => new(x, y);

    // ---- camp loadouts, hung on a board fixture ----------------------------------------------------
    // A camp gives these out at the run layer; a rule test about what one *does* wants it on the board
    // without a run around it. Same records the run would carry in, set directly.

    /// <summary>The board with a mod fitted to one unit's spender.</summary>
    public static GameState WithMod(this GameState state, UnitId id, Mod mod) =>
        state.WithUnit(state.Get(id) with { Loadout = state.Get(id).Loadout.With(mod) });

    /// <summary>The board with an extra Pluck condition on one unit.</summary>
    public static GameState WithWind(this GameState state, UnitId id, SecondWind wind) =>
        state.WithUnit(state.Get(id) with { Loadout = state.Get(id).Loadout.With(wind) });

    /// <summary>The board with a tactical unlock on one unit.</summary>
    public static GameState WithUnlock(this GameState state, UnitId id, Unlock unlock) =>
        state.WithUnit(state.Get(id) with { Loadout = state.Get(id).Loadout.With(unlock) });

    /// <summary>The board with something in one unit's pocket.</summary>
    public static GameState WithPocket(this GameState state, UnitId id, Consumable item) =>
        state.WithUnit(state.Get(id) with { Loadout = state.Get(id).Loadout.WithPocket(item) });

    /// <summary>The board with one unit's meter set, for a test that needs it able to spend.</summary>
    public static GameState WithVerve(this GameState state, UnitId id, int verve) =>
        state.WithUnit(state.Get(id) with { Verve = verve });

    /// <summary>
    /// The next unit on the active team that can actually spend its activation. Clinging and
    /// Bedraggled units take no slot, so they are skipped here just as Core skips them.
    /// </summary>
    public static Unit CurrentUnit(this GameState state) =>
        state.Units.First(u =>
            u.Team == state.ActiveTeam && u.IsOnBoard && !u.Clinging && !u.Bedraggled && !u.HasActivated);

    /// <summary>Ends the activation of the next pending unit on the active team.</summary>
    public static StepResult PassCurrent(this GameState state) =>
        Game.Apply(state, new EndActivationCommand(state.CurrentUnit().Id));

    /// <summary>Throws when a condition a test depends on does not hold.</summary>
    public static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
