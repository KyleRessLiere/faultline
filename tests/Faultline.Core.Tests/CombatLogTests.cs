using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// docs/COMBAT_LOG.md. The headline property is determinism — the same seed and command log must
/// produce a byte-identical event log, because comparing two runs is most of the point. The other
/// property that matters is coverage: an event type nobody taught the formatter about must fail
/// loudly here rather than quietly vanish from every future log.
/// </summary>
public class CombatLogTests
{
    [Fact]
    public void Log_SameSeedAndCommandLog_IsByteIdentical()
    {
        string first = PlayAndExport(seed: 12345);
        string second = PlayAndExport(seed: 12345);

        Assert.Equal(first, second);
        Assert.Equal(Encoding.UTF8.GetBytes(first), Encoding.UTF8.GetBytes(second));
    }

    [Fact]
    public void Log_ReplayingARecordedCommandLog_ProducesTheSameEventLog()
    {
        var fight = FightLibrary.Fight1();
        var recorded = Play(fight, seed: 2024);

        var replay = new CombatRecorder(fight, 2024);
        var start = Game.Start(fight, 2024);
        replay.RecordStart(start);
        var state = start.NewState;

        foreach (var command in recorded.Commands)
        {
            var result = Game.Apply(state, command);
            replay.RecordStep(command, state, result);
            state = result.NewState;
        }

        Assert.Equal(recorded.RenderEventLog(), replay.RenderEventLog());
        Assert.Equal(recorded.LineCount, replay.LineCount);
    }

    [Fact]
    public void Log_DifferentSeeds_StillProduceALogWithTheSameShape()
    {
        var one = Play(FightLibrary.Fight1(), seed: 1);
        var two = Play(FightLibrary.Fight1(), seed: 2);

        Assert.NotEmpty(one.Lines);
        Assert.NotEmpty(two.Lines);
        Assert.All(one.Lines, line => Assert.Equal(CombatLog.ColumnCount, Columns(line).Length));
        Assert.All(two.Lines, line => Assert.Equal(CombatLog.ColumnCount, Columns(line).Length));
    }

    [Fact]
    public void EveryGameEventType_ProducesItsOwnLine()
    {
        var state = SampleState();
        var types = EventTypes();

        Assert.NotEmpty(types);

        foreach (var type in types)
        {
            var evt = Construct(type);

            Assert.True(CombatLog.IsHandled(evt), type.Name + " falls through to the unknown-event branch.");

            string line = CombatLog.Line(3, CombatLog.Slot(Team.PlayerA, new UnitId(0)), evt, state);
            var columns = Columns(line);

            Assert.Equal(CombatLog.ColumnCount, columns.Length);
            Assert.Equal(type.Name, columns[3]);
            Assert.NotEqual(string.Empty, columns[4].Trim());
            Assert.DoesNotContain("unhandled event type", line, StringComparison.Ordinal);
            Assert.DoesNotContain(CombatLog.UnknownEvent, line, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void EveryGameEventType_IsCoveredByTheDetailColumnToo()
    {
        var state = SampleState();

        foreach (var type in EventTypes())
        {
            string detail = CombatLog.Detail(Construct(type), state);

            Assert.False(string.IsNullOrWhiteSpace(detail), type.Name + " has an empty detail column.");
            Assert.DoesNotContain(CombatLog.ColumnSeparator, detail, StringComparison.Ordinal);
            Assert.DoesNotContain("\n", detail, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Actor_NamesTheUnitAndItsIdSoThreeHusksAreTellableApart()
    {
        var state = BoardBuilder.Open(4, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0)
            .Enemy(UnitKind.Husk, 2, 0)
            .Build();

        Assert.Equal("Vanguard [A] u0", CombatLog.Actor(state, new UnitId(0)));
        Assert.Equal("Husk [E] u1", CombatLog.Actor(state, new UnitId(1)));
        Assert.Equal("Husk [E] u2", CombatLog.Actor(state, new UnitId(2)));
        Assert.Equal(CombatLog.NoActor, CombatLog.Actor(state, UnitId.None));
    }

    [Fact]
    public void Push_IntoAnotherUnit_LogsPushCollisionDamageToBothAndStaggerAsSeparateLines()
    {
        // Vanguard at (0,0) shoves the Grappler right; it travels one tile and slams into the Anchor.
        var state = BoardBuilder.Open(4, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Grappler, 1, 0)
            .Enemy(UnitKind.Anchor, 3, 0)
            .Build();

        var events = new List<GameEvent>();
        var after = Displacement.Resolve(
            state, new UnitId(1), new Coord(0, 0), DisplacementKind.Push, 3, false, events);

        var lines = CombatLog.Lines(events, 1, CombatLog.Slot(Team.PlayerA, new UnitId(0)), after);

        Assert.Equal(
            new[] { "UnitPushed", "Collision", "UnitDamaged", "Staggered", "UnitDamaged", "Staggered" },
            lines.Select(l => Columns(l)[3]).ToArray());

        var push = Columns(lines[0]);
        Assert.Equal("Grappler [E] u1", push[2]);
        Assert.Contains("Push 3", push[4], StringComparison.Ordinal);
        Assert.Contains("(1,0) -> (2,0)", push[4], StringComparison.Ordinal);
        Assert.Contains("via (2,0)", push[4], StringComparison.Ordinal);

        var collision = Columns(lines[1]);
        Assert.Equal("Grappler [E] u1", collision[2]);
        Assert.Contains("into Anchor [E] u2", collision[4], StringComparison.Ordinal);
        Assert.Contains("4 damage to both", collision[4], StringComparison.Ordinal);

        // Both parties take their 4 and are staggered, each on its own line.
        Assert.Equal("Grappler [E] u1", Columns(lines[2])[2]);
        Assert.Contains("-4 Collision", Columns(lines[2])[4], StringComparison.Ordinal);
        Assert.Equal("Grappler [E] u1", Columns(lines[3])[2]);
        Assert.Equal("Anchor [E] u2", Columns(lines[4])[2]);
        Assert.Contains("-4 Collision", Columns(lines[4])[4], StringComparison.Ordinal);
        Assert.Equal("Anchor [E] u2", Columns(lines[5])[2]);
    }

    [Fact]
    public void Push_IntoAPit_LogsTheRouteTileByTileAndTheCling()
    {
        var state = BoardBuilder.Rows("..O.")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, footing: 0)
            .Build();

        var events = new List<GameEvent>();
        var after = Displacement.Resolve(
            state, new UnitId(1), new Coord(0, 0), DisplacementKind.Push, 2, false, events);

        var lines = CombatLog.Lines(events, 1, CombatLog.Slot(Team.PlayerA, new UnitId(0)), after);

        Assert.Contains(lines, l => Columns(l)[3] == "UnitPushed" && Columns(l)[4].Contains("via (2,0)"));
        Assert.Contains(lines, l => Columns(l)[3] == "Clinging" && Columns(l)[4].Contains("(2,0)"));
    }

    [Fact]
    public void EveryEventLine_HasAStableColumnCount()
    {
        var recorder = Play(FightLibrary.Fight1(), seed: 3);

        Assert.NotEmpty(recorder.Lines);
        Assert.Equal(CombatLog.ColumnCount, Columns(CombatLog.Header).Length);
        Assert.All(recorder.Lines, line => Assert.Equal(CombatLog.ColumnCount, Columns(line).Length));
        Assert.DoesNotContain(recorder.Lines, line => line.Contains(CombatLog.UnknownEvent, StringComparison.Ordinal));
    }

    [Fact]
    public void CommandLog_CarriesTheSeedTheFightIdAndOneEntryPerCommandInOrder()
    {
        var fight = FightLibrary.Fight1();
        var recorder = Play(fight, seed: 7);
        string text = recorder.RenderCommandLog();

        Assert.Contains(RunRecord.SeedKey + "\t7", text, StringComparison.Ordinal);
        Assert.Contains(RunRecord.FightKey + "\t" + fight.Id, text, StringComparison.Ordinal);
        Assert.NotEmpty(recorder.Commands);

        var entries = text
            .Split('\n')
            .Where(l => l.Length > 0 && char.IsDigit(l[0]))
            .ToList();

        Assert.Equal(recorder.Commands.Count, entries.Count);

        for (int i = 0; i < entries.Count; i++)
        {
            Assert.StartsWith((i + 1) + "\t" + RunRecord.Format(recorder.Commands[i]), entries[i], StringComparison.Ordinal);
        }
    }

    [Fact]
    public void CommandLog_ParsesBackOutOfAFullExportAndReplaysTheSameFight()
    {
        var fight = FightLibrary.Fight1();
        var recorder = Play(fight, seed: 7);

        Assert.True(RunRecord.TryParse(recorder.Export(), out var parsed));

        Assert.Equal(fight.Id, parsed.FightId);
        Assert.Equal(fight.Number, parsed.FightNumber);
        Assert.Equal(7, parsed.Seed);
        Assert.Equal(recorder.Commands, parsed.Commands);

        var original = TestPlay.Replay(Game.Start(fight, 7).NewState, recorder.Commands);
        var replayed = TestPlay.Replay(Game.Start(fight, 7).NewState, parsed.Commands);

        Assert.Equal(original, replayed);
        Assert.Equal(original.GetHashCode(), replayed.GetHashCode());
    }

    [Fact]
    public void CommandLog_RoundTripsEveryCommandShape()
    {
        var commands = new Command[]
        {
            new DeployCommand(new UnitId(0), new Coord(3, 5)),
            new MoveCommand(new UnitId(1), new Coord(0, 0)),
            new AttackCommand(new UnitId(2), new UnitId(5)),
            new AttackCommand(new UnitId(2), new UnitId(5), AttackMode.Pull),
            new AbilityCommand(new UnitId(3), Ability.StaggerShot, new UnitId(6)),
            new AbilityCommand(new UnitId(3), Ability.BullRush, null, Direction.Left),
            new AbilityCommand(new UnitId(3), Ability.GuardStance),
            new RescueCommand(new UnitId(0), new UnitId(1), new Coord(2, 2)),
            new FinishClingingCommand(new UnitId(0), new UnitId(7)),
            new EndActivationCommand(new UnitId(4)),

            // A shove-attack read back as an ordinary swing is a different fight from the one that
            // was recorded, so every mode is round-tripped rather than just Pull.
            new AttackCommand(new UnitId(2), new UnitId(5), AttackMode.Push),

            // Every Pluck spender. Cast is the one that aims at a unit and a tile at once; the
            // other three carry neither, and both halves have to survive the round trip.
            new SpendVerveCommand(new UnitId(0), VerveSpend.WreckingWeight),
            new SpendVerveCommand(new UnitId(1), VerveSpend.Cast, new UnitId(6), new Coord(4, 3)),
            new SpendVerveCommand(new UnitId(2), VerveSpend.DoubleNock),
            new SpendVerveCommand(new UnitId(3), VerveSpend.Preen),

            // A legendary's free step carries the tile it lands on, and the tile is the whole point:
            // a banked step has one landing Core re-derives, while Follow Through picks between
            // every neighbour. Round-tripped with a tile that is not the origin so a dropped column
            // cannot pass as a coincidence (D-204).
            new TakeFreeStepCommand(new UnitId(1), new Coord(6, 2)),
            new TakeBankedStepCommand(new UnitId(1)),
        };

        var record = new RunRecord { FightId = "the-maw", FightNumber = 4, Seed = -19, Commands = commands };

        Assert.True(RunRecord.TryParse(record.Render(), out var parsed));
        Assert.Equal("the-maw", parsed.FightId);
        Assert.Equal(-19, parsed.Seed);
        Assert.Equal(commands, parsed.Commands);
    }

    /// <summary>
    /// The command log is the save format, so a <see cref="Command"/> the formatter has not been
    /// taught about does not degrade — it makes the whole log unreplayable from that line on.
    /// <see cref="SpendVerveCommand"/> was exactly that for the length of M5: Pluck shipped, and
    /// every log containing a spend silently became fiction.
    /// </summary>
    [Fact]
    public void EveryCommandType_IsKnownToTheCommandLog()
    {
        var types = typeof(Command).Assembly
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(Command)) && !t.IsAbstract)
            .OrderBy(t => t.Name, StringComparer.Ordinal)
            .ToList();

        Assert.NotEmpty(types);

        foreach (var type in types)
        {
            // Coverage only. Exact equality is asserted by the explicit list in
            // Record_RoundTripsEveryCommandType, because a reflected sample sets every field at
            // once — an AbilityCommand aimed at a unit *and* a direction is not a command the game
            // can produce, and holding the encoding to it would test a fiction.
            var command = (Command)SampleFor(type)!;
            string rendered = RunRecord.Format(command);

            Assert.False(
                rendered.StartsWith("Unknown", StringComparison.Ordinal),
                type.Name + " has no case in RunRecord.Format, so a log containing one cannot replay.");

            Assert.True(
                RunRecord.ParseCommand(rendered.Split('\t'), 0) is not null,
                type.Name + " renders but does not parse back.");
        }
    }

    [Fact]
    public void Export_PutsTheCommandLogFirstSoTheFileIsReRunnable()
    {
        string text = Play(FightLibrary.Fight1(), seed: 5).Export();

        int commandSection = text.IndexOf("=== command log ===", StringComparison.Ordinal);
        int eventSection = text.IndexOf("=== event log ===", StringComparison.Ordinal);

        Assert.True(commandSection >= 0);
        Assert.True(eventSection > commandSection);
        Assert.Contains(CombatLog.Header, text, StringComparison.Ordinal);
        Assert.DoesNotContain("\r\n", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Recorder_SwitchedOnMidFight_SaysTheCommandLogIsPartial()
    {
        var fight = FightLibrary.Fight1();
        var state = Game.Start(fight, 11).NewState;
        var late = new CombatRecorder(fight, 11);

        var legal = Game.LegalCommands(state);
        var result = Game.Apply(state, legal[0]);
        late.RecordStep(legal[0], state, result);

        Assert.False(late.FromFightStart);
        Assert.Contains("recording started mid-fight", late.Export(), StringComparison.Ordinal);
        Assert.DoesNotContain("recording started mid-fight", Play(fight, 11).Export(), StringComparison.Ordinal);
    }

    [Fact]
    public void Recorder_TracksRoundAndActivationSlotFromTheStreamItself()
    {
        var recorder = Play(FightLibrary.Fight1(), seed: 8);

        var deployment = recorder.Lines.First(l => Columns(l)[3] == "UnitDeployed");
        Assert.Equal("0", Columns(deployment)[0]);

        var activation = recorder.Lines.First(l => Columns(l)[3] == "ActivationStarted");
        Assert.Equal("1", Columns(activation)[0]);

        // The line an activation opens with already carries that activation's slot.
        var slot = Columns(activation)[1];
        Assert.Contains(":u", slot, StringComparison.Ordinal);
        Assert.StartsWith("Player", slot, StringComparison.Ordinal);

        // A round belongs to no activation, and the slot column says so rather than guessing.
        var roundStart = recorder.Lines.First(l => Columns(l)[3] == "RoundStarted");
        Assert.Equal(CombatLog.NoActor, Columns(roundStart)[1]);

        // Every line inside an activation names the same slot until it ends.
        int opened = recorder.Lines.ToList().IndexOf(activation);
        int ended = recorder.Lines.ToList().FindIndex(opened, l => Columns(l)[3] == "ActivationEnded");
        Assert.True(ended > opened);
        for (int i = opened; i <= ended; i++)
        {
            Assert.Equal(slot, Columns(recorder.Lines[i])[1]);
        }
    }

    [Fact]
    public void Recorder_FileNameComesFromTheFightId()
    {
        Assert.Equal(FightLibrary.Fight1().Id + ".log", new CombatRecorder(FightLibrary.Fight1(), 1).FileName);
        Assert.Equal("fight.log", new CombatRecorder(new FightDefinition(), 1).FileName);
    }

    [Fact]
    public void Clean_FlattensAnythingThatWouldBreakTheColumns()
    {
        Assert.Equal("a b c", CombatLog.Clean("a\tb\nc"));
        Assert.Equal(string.Empty, CombatLog.Clean(string.Empty));
    }

    private static string[] Columns(string line) => line.Split('\t');

    private static IReadOnlyList<Type> EventTypes() =>
        typeof(GameEvent).Assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && typeof(GameEvent).IsAssignableFrom(t))
            .OrderBy(t => t.Name, StringComparer.Ordinal)
            .ToList();

    private static GameEvent Construct(Type type)
    {
        var ctor = type.GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length)
            .First();

        var args = ctor.GetParameters().Select(p => SampleFor(p.ParameterType)).ToArray();
        return (GameEvent)ctor.Invoke(args);
    }

    private static object? SampleFor(Type type)
    {
        if (type == typeof(UnitId))
        {
            return new UnitId(0);
        }

        if (type == typeof(UnitId?))
        {
            return new UnitId(1);
        }

        if (type == typeof(Coord))
        {
            return new Coord(1, 2);
        }

        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying is not null)
        {
            return SampleFor(underlying);
        }

        if (type == typeof(int))
        {
            return 2;
        }

        if (type == typeof(bool))
        {
            return true;
        }

        if (type == typeof(string))
        {
            return "a stated reason";
        }

        if (type.IsEnum)
        {
            return Enum.GetValues(type).GetValue(0);
        }

        if (typeof(IReadOnlyList<Coord>).IsAssignableFrom(type))
        {
            return new[] { new Coord(1, 2), new Coord(1, 3) };
        }

        // A queue an event carries — ActivationOrderChanged's re-published enemy order.
        if (typeof(IReadOnlyList<UnitId>).IsAssignableFrom(type))
        {
            return new[] { new UnitId(1), new UnitId(2) };
        }

        // Payload records nested inside an event (an enemy intent, say) build the same way, so a new
        // event type is covered by this test the moment it is added.
        var ctor = type.GetConstructors().OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();
        if (ctor is not null && !type.IsAbstract)
        {
            return ctor.Invoke(ctor.GetParameters().Select(p => SampleFor(p.ParameterType)).ToArray());
        }

        throw new InvalidOperationException(
            "CombatLogTests cannot build a sample " + type.Name + "; teach SampleFor about it.");
    }

    private static GameState SampleState() =>
        BoardBuilder.Open(4, 2)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0)
            .Build();

    private static CombatRecorder Play(FightDefinition fight, int seed)
    {
        var recorder = new CombatRecorder(fight, seed);
        var start = Game.Start(fight, seed);
        recorder.RecordStart(start);

        var state = start.NewState;
        for (int i = 0; i < 400; i++)
        {
            var legal = Game.LegalCommands(state);
            if (legal.Count == 0)
            {
                break;
            }

            var command = legal[0];
            var result = Game.Apply(state, command);
            recorder.RecordStep(command, state, result);
            state = result.NewState;
        }

        return recorder;
    }

    private static string PlayAndExport(int seed) => Play(FightLibrary.Fight1(), seed).Export();
}
