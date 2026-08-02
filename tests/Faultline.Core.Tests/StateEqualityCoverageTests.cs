using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// Guards the foundation replay determinism stands on: that comparing two states actually compares
/// all of them.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="GameState"/>, <see cref="RunState"/> and <see cref="FightDefinition"/> all hand-write
/// <c>Equals</c>, because a record's generated equality compares list members by reference and would
/// call a replayed state unequal to the state it replayed. Hand-written means hand-maintained, and
/// the failure mode is silent: add a field, forget to compare it, and the replay tests keep passing
/// because <em>both sides ignore it</em>. A false green on the project's prime directive.
/// </para>
/// <para>
/// So coverage is enforced by reflection and correctness by hand. Every property is enumerated; each
/// needs a registered mutation that changes it. A new property with no mutation fails here with its
/// own name, which is the prompt to decide whether <c>Equals</c> should see it. Registering a
/// mutation then proves it does.
/// </para>
/// </remarks>
public class StateEqualityCoverageTests
{
    [Fact]
    public void GameState_ComparesEveryPropertyItHas()
    {
        AssertEveryPropertyIsCompared(Fixtures.Game(), Mutations.Game, "GameState");
    }

    [Fact]
    public void RunState_ComparesEveryPropertyItHas()
    {
        AssertEveryPropertyIsCompared(Fixtures.Run(), Mutations.Run, "RunState");
    }

    [Fact]
    public void FightDefinition_ComparesEveryPropertyItHas()
    {
        AssertEveryPropertyIsCompared(Fixtures.Fight(), Mutations.Fight, "FightDefinition");
    }

    [Fact]
    public void GameState_HashChangesWithTheState()
    {
        // Equality is what replay asserts; the hash is what the assertion is cheap to do with. A
        // hash that ignores a field is a weaker failure than an Equals that does, but still a real
        // one, so it is checked over the same mutations rather than trusted.
        AssertHashMoves(Fixtures.Game(), Mutations.Game, "GameState");
    }

    [Fact]
    public void RunState_HashChangesWithTheState()
    {
        AssertHashMoves(Fixtures.Run(), Mutations.Run, "RunState");
    }

    /// <summary>
    /// Every readable public property of the fixture must have a mutation registered, and applying
    /// that mutation must make the object compare unequal to the original.
    /// </summary>
    private static void AssertEveryPropertyIsCompared<T>(
        T original,
        IReadOnlyDictionary<string, Func<T, T>> mutations,
        string name)
        where T : class
    {
        var properties = Settable(typeof(T));

        var missing = properties
            .Where(p => !mutations.ContainsKey(p.Name))
            .Select(p => p.Name)
            .ToList();

        Assert.True(
            missing.Count == 0,
            name + " gained " + string.Join(", ", missing) + " with no mutation registered in this "
            + "test. Add one, then check " + name + ".Equals compares it — a field Equals ignores is "
            + "a field replay cannot see changing.");

        foreach (var property in properties)
        {
            var mutated = mutations[property.Name](original);

            Assert.False(
                original.Equals(mutated),
                name + "." + property.Name + " can change without " + name
                + ".Equals noticing. Replay would compare two different states as identical.");
        }
    }

    private static void AssertHashMoves<T>(
        T original,
        IReadOnlyDictionary<string, Func<T, T>> mutations,
        string name)
        where T : class
    {
        foreach (var property in Settable(typeof(T)))
        {
            var mutated = mutations[property.Name](original);

            Assert.True(
                original.GetHashCode() != mutated.GetHashCode(),
                name + "." + property.Name + " does not move the hash.");
        }
    }

    /// <summary>
    /// The properties that carry state: public, readable, and settable through a <c>with</c>
    /// expression. Computed properties are excluded — they are derived from the ones that are.
    /// </summary>
    private static IReadOnlyList<PropertyInfo> Settable(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.SetMethod is not null)
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .ToList();

    // --- fixtures ---------------------------------------------------------------------------------

    private static class Fixtures
    {
        internal static GameState Game()
        {
            var fight = FightLibrary.ById("first-contact");
            var state = Faultline.Core.Game.Start(fight, seed: 7).NewState;

            // A fixture with something in every collection, so a mutation to any of them is visible.
            // Intents are taken from Core's own planner rather than hand-built, so this fixture
            // cannot drift out of shape when EnemyIntent gains a field.
            return Ai.DeclareAll(state, new List<GameEvent>());
        }

        internal static RunState Run() =>
            Campaign.Start(CampaignLibrary.Faultline, seed: 7).NewState;

        internal static FightDefinition Fight() => FightLibrary.ById("hold-the-gate");
    }

    // --- mutations: one per property, each changing exactly that property -------------------------

    private static class Mutations
    {
        internal static readonly IReadOnlyDictionary<string, Func<GameState, GameState>> Game =
            new Dictionary<string, Func<GameState, GameState>>
            {
                ["Seed"] = s => s with { Seed = s.Seed + 1 },
                ["RngState"] = s => s with { RngState = s.RngState + 1 },
                ["Fight"] = s => s with { Fight = FightLibrary.ById("the-teeth") },
                ["Board"] = s => s with { Board = Board.Filled(3, 3) },
                ["Units"] = s => s with { Units = s.Units.Take(s.Units.Count - 1).ToList() },
                ["Structures"] = s => s with
                {
                    Structures = new[] { new Structure { At = new Coord(0, 0), Hp = 4, MaxHp = 4 } },
                },
                ["Reinforcements"] = s => s with
                {
                    Reinforcements = new[] { new PendingReinforcement(new UnitId(9), 3, new Coord(0, 0)) },
                },
                ["Round"] = s => s with { Round = s.Round + 1 },
                ["Phase"] = s => s with { Phase = Phase.Complete },
                ["ActiveTeam"] = s => s with { ActiveTeam = Team.Enemy },
                ["NextPlayerTeam"] = s => s with { NextPlayerTeam = Team.PlayerB },
                ["ActiveUnitId"] = s => s with { ActiveUnitId = new UnitId(3) },
                ["Momentum"] = s => s with { Momentum = s.Momentum + 1 },
                ["Intents"] = s => s with { Intents = Array.Empty<EnemyIntent>() },
                ["Outcome"] = s => s with { Outcome = FightOutcome.Won },
            };

        internal static readonly IReadOnlyDictionary<string, Func<RunState, RunState>> Run =
            new Dictionary<string, Func<RunState, RunState>>
            {
                ["Seed"] = s => s with { Seed = s.Seed + 1 },
                ["Campaign"] = s => s with
                {
                    Campaign = s.Campaign with { Id = s.Campaign.Id + "-other" },
                },
                ["NodeIndex"] = s => s with { NodeIndex = s.NodeIndex + 1 },
                ["Squad"] = s => s with { Squad = s.Squad.Take(s.Squad.Count - 1).ToList() },
                ["Phase"] = s => s with { Phase = RunPhase.Complete },
                ["Outcome"] = s => s with { Outcome = RunOutcome.Won },
                ["Fight"] = s => s with { Fight = Fixtures.Game() },
                ["Bindings"] = s => s with
                {
                    Bindings = new[] { new RunBinding(new RunUnitId(0), new UnitId(0), Team.PlayerA) },
                },
                ["FightsWon"] = s => s with { FightsWon = s.FightsWon + 1 },
            };

        internal static readonly IReadOnlyDictionary<string, Func<FightDefinition, FightDefinition>> Fight =
            new Dictionary<string, Func<FightDefinition, FightDefinition>>
            {
                ["Id"] = f => f with { Id = f.Id + "-other" },
                ["Number"] = f => f with { Number = f.Number + 1 },
                ["Name"] = f => f with { Name = f.Name + "!" },
                ["Description"] = f => f with { Description = f.Description + "!" },
                ["DesignNotes"] = f => f with { DesignNotes = new[] { "a different note" } },
                ["RetiredReason"] = f => f with { RetiredReason = "retired for a reason" },
                ["Board"] = f => f with { Board = Board.Filled(3, 3) },
                ["DeploymentZoneA"] = f => f with { DeploymentZoneA = new[] { new Coord(5, 5) } },
                ["DeploymentZoneB"] = f => f with { DeploymentZoneB = new[] { new Coord(4, 4) } },
                ["RosterA"] = f => f with { RosterA = new[] { UnitKind.Archer } },
                ["RosterB"] = f => f with { RosterB = new[] { UnitKind.Vanguard } },
                ["Enemies"] = f => f with
                {
                    Enemies = new[] { new EnemySpawn(UnitKind.Husk, new Coord(2, 2)) },
                },
                ["ProtectedZone"] = f => f with { ProtectedZone = new[] { new Coord(1, 1) } },
                ["Objective"] = f => f with { Objective = Objective.KillAll },
                ["TurnLimit"] = f => f with { TurnLimit = f.TurnLimit + 1 },
                ["Waves"] = f => f with { Waves = Array.Empty<ReinforcementWave>() },
                ["FootingGrants"] = f => f with
                {
                    FootingGrants = new[] { FootingGrant.ForSide(Team.Enemy, 2) },
                },
            };
    }
}

/// <summary>
/// The writer must be able to say everything the model holds.
/// </summary>
/// <remarks>
/// <para>
/// The creator screen builds a <see cref="FightDefinition"/>, <see cref="FightWriter"/> turns it into
/// <c>.fight</c> text and <see cref="FightParser"/> reads it straight back — so anything the writer
/// cannot express is data the creator silently drops when you save.
/// </para>
/// <para>
/// Round-trip coverage used to be a hand-listed checklist per test: objective here, footing there,
/// design notes somewhere else. Add a field, forget the writer, and nothing failed. This asserts the
/// whole definition at once, over every board that ships, so a field the writer cannot carry is a red
/// suite rather than a quiet loss.
/// </para>
/// </remarks>
public class FightRoundTripTests
{
    public static TheoryData<string> EveryFightId
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (var result in FightLibrary.LoadAll())
            {
                data.Add(result.Fight!.Id);
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(EveryFightId))]
    public void EveryShippedFight_SurvivesTheWriterWhole(string id)
    {
        // Retired boards included: LoadAll reads those too, and a retired file is still embedded,
        // still parsed, and still has to round-trip.
        var original = FightLibrary.ById(id);

        var reparsed = FightParser.Parse(FightWriter.Write(original));

        Assert.True(reparsed.Ok, id + ": " + string.Join(" | ", reparsed.Issues));
        Assert.Equal(original, reparsed.Fight);
    }

    [Fact]
    public void AFightBuiltInMemory_SurvivesTheWriterWhole()
    {
        // The creator's path: a definition assembled in code rather than parsed from a file. The
        // shipped boards do not exercise every field at once — this one does.
        var built = FightLibrary.ById("first-contact") with
        {
            Description = "Built in memory, not parsed from a file.",
            DesignNotes = new[] { "One idea.", "A second, separate idea." },
            TurnLimit = 9,
            Objective = new Objective
            {
                Kind = ObjectiveKind.Protect,
                Tiles = new[] { new Coord(3, 3) },
                Hp = 5,
            },
            FootingGrants = new[] { FootingGrant.ForSide(Team.Enemy, 1), FootingGrant.ForKind(UnitKind.Husk, 2) },
            Waves = new[]
            {
                new ReinforcementWave(2, new[] { new EnemySpawn(UnitKind.Husk, new Coord(0, 0)) }),
            },
        };

        var reparsed = FightParser.Parse(FightWriter.Write(built));

        Assert.True(reparsed.Ok, string.Join(" | ", reparsed.Issues));
        Assert.Equal(built, reparsed.Fight);
    }

    [Fact]
    public void TheRoundTripIsStable_WritingTwiceGivesTheSameText()
    {
        // A writer that is not stable makes every save a spurious diff, which is how a generated
        // file stops being reviewable.
        foreach (var fight in FightLibrary.All())
        {
            string once = FightWriter.Write(fight);
            string twice = FightWriter.Write(FightParser.Parse(once).Fight!);

            Assert.Equal(once, twice);
        }
    }
}
