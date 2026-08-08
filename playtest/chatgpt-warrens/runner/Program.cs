using System.Globalization;
using Faultline.Core;
using Faultline.Playtest;

const string CampaignId = ActMapLibrary.Act1Id;

if (args.Contains("--catalogue", StringComparer.Ordinal))
{
    Console.WriteLine("kind|tier|class|name|summary|tags");
    foreach (var definition in TechniqueDefinition.All())
    {
        Console.WriteLine(
            $"Technique|{definition.Rarity}|{definition.Kind}|{definition.Name}|{definition.Summary}|{definition.Tags}");
    }

    foreach (var definition in UpgradeDefinition.All())
    {
        var offer = new CampOffer(RunUnitId.None, definition.Category, definition.Value);
        Console.WriteLine(
            $"{definition.Category}|{offer.Rarity}|{definition.Kind?.ToString() ?? "Any"}|"
            + $"{definition.Name}|{definition.Summary}|");
    }

    foreach (var definition in ConsumableDefinition.All())
    {
        var offer = CampOffer.Of(RunUnitId.None, definition.Item);
        Console.WriteLine(
            $"Consumable|{offer.Rarity}|Any|{definition.Name}|{definition.Summary}|");
    }

    foreach (var definition in LegendaryCatalogue.All())
    {
        Console.WriteLine(
            $"Legendary|{definition.Tier}|{definition.Class}|{definition.Name}|{definition.Summary}|");
    }

    return;
}

string sessionPath = ValueAfter(args, "--session")
    ?? Path.Combine("playtest", "chatgpt-warrens", "run-1.session");
int seed = int.TryParse(ValueAfter(args, "--seed"), out int parsedSeed) ? parsedSeed : 47;
bool fresh = args.Contains("--new", StringComparer.Ordinal);
bool compact = args.Contains("--compact", StringComparer.Ordinal);
string? forkFrom = ValueAfter(args, "--fork-from");
int forkThrough = int.TryParse(ValueAfter(args, "--through"), out int parsedThrough)
    ? parsedThrough
    : int.MaxValue;

if (forkFrom is not null)
{
    var source = Load(forkFrom, seed);
    Save(sessionPath, source.Seed, source.Picks.Take(forkThrough).ToList());
}
else if (fresh)
{
    Save(sessionPath, seed, Array.Empty<int>());
}

var saved = Load(sessionPath, seed);
var driver = new MapDriver(Campaign.Start(CampaignLibrary.ById(CampaignId), saved.Seed).NewState);

foreach (int recordedIndex in saved.Picks)
{
    driver.AdvanceToDecision();
    if (!driver.Waiting || recordedIndex < 0 || recordedIndex >= driver.Legal.Count)
    {
        throw new InvalidOperationException(
            $"Session desynchronised at recorded pick {recordedIndex}; "
            + $"the current state has {driver.Legal.Count} choices.");
    }

    driver.Apply(driver.Legal[recordedIndex]);
}

driver.AdvanceToDecision();

if (int.TryParse(ValueAfter(args, "--pick"), out int pick))
{
    if (!driver.Waiting || pick < 0 || pick >= driver.Legal.Count)
    {
        throw new ArgumentOutOfRangeException(nameof(pick), pick, $"Choose 0..{driver.Legal.Count - 1}.");
    }

    string chosen = Describe(driver.Run, driver.Legal[pick]);
    driver.ClearEvents();
    driver.Apply(driver.Legal[pick]);
    saved.Picks.Add(pick);
    Save(sessionPath, saved.Seed, saved.Picks);
    Console.WriteLine("> " + chosen);
    foreach (string line in driver.EventLines())
    {
        Console.WriteLine("    " + line);
    }

    Console.WriteLine();
    driver.AdvanceToDecision();
}

Print(driver, saved.Picks.Count, sessionPath, compact);

static void Print(MapDriver driver, int decisions, string path, bool compact)
{
    Console.WriteLine(Header(driver.Run));

    if (driver.Run.Phase == RunPhase.Complete)
    {
        Console.WriteLine($"RUN OVER: {driver.Run.Outcome}; fights cleared {driver.Run.FightsWon}");
        Console.WriteLine($"session: {path} ({decisions} decisions)");
        return;
    }

    if (driver.Run.Fight is { } fight)
    {
        if (compact)
        {
            Console.WriteLine(View.Board(fight));
            Console.WriteLine(View.Intents(fight));
            Console.WriteLine(View.Options(fight, driver.Legal.OfType<PlayCommand>().Select(p => p.Command).ToList()));
        }
        else
        {
            var combat = driver.Legal.OfType<PlayCommand>().Select(p => p.Command).ToList();
            Console.WriteLine(View.Brief(driver.Run, combat));
        }
    }
    else
    {
        if (driver.Run.CurrentMapNode is { EventId: { Length: > 0 } eventId })
        {
            var definition = EventLibrary.ById(eventId);
            Console.WriteLine($"EVENT: {definition.Name} [{definition.Shape}]");
            Console.WriteLine(definition.Prompt);
            Console.WriteLine($"Printed terms: pay {definition.HpCost} HP now; gain +{definition.MaxHpGain} maximum HP for this run; lethal payment is blocked.");
            Console.WriteLine("Walk-away line: " + definition.WalkAwayLine);
            Console.WriteLine();
        }

        Console.WriteLine("choices:");
        for (int i = 0; i < driver.Legal.Count; i++)
        {
            Console.WriteLine($"  {i,3}  {Describe(driver.Run, driver.Legal[i])}");
        }
    }

    Console.WriteLine($"session: {path} ({decisions} decisions)");
}

static string Header(RunState run)
{
    string node = run.CurrentMapNode is { } mapNode
        ? $"{mapNode.Label} [{mapNode.Type}, {mapNode.Lane}, column {mapNode.Column + 1}]"
        : "none";
    string squad = string.Join(" | ", run.Squad.Select(u =>
        $"{u.Id}:{Naming.Of(u.Kind)} {u.Hp}/{u.MaxHp} Pluck {u.Verve} {u.Status}"));
    return $"=== {node} | phase {run.Phase} | cleared {run.FightsWon} ===\n{squad}";
}

static string Describe(RunState run, RunCommand command) => command switch
{
    PlayCommand play when run.Fight is not null => View.Describe(run.Fight, play.Command),
    VoteCommand vote => vote.IsAgreed
        ? $"Both players vote for {Label(run, vote.ChoiceA)}"
        : $"Player A votes {Label(run, vote.ChoiceA)}; Player B votes {Label(run, vote.ChoiceB)}; flip coin",
    RestHealCommand => "Rest at the Still Pond: heal each available duck by half maximum HP",
    EventWalkAwayCommand => "Walk away from the event without paying",
    EventPayCommand pay => $"Accept event; {SquadName(run, pay.Payer)} pays the printed cost",
    CampPickCommand camp => "Camp pick — " + Offer(run, camp.Chosen)
        + Bound(camp.Drawn.Bound),
    LegendaryPickCommand legendary => "Legendary pick — " + LegendaryOfferText(run, legendary.Chosen)
        + Bound(legendary.Drawn.Bound),
    EnterNodeCommand => "Enter current node",
    _ => command.ToString() ?? command.GetType().Name,
};

static string Label(RunState run, string id) => run.Map?.NodeAt(id)?.Label ?? id;

static string SquadName(RunState run, RunUnitId id)
{
    var duck = run.FindUnit(id);
    return duck is null ? id.ToString() : $"{id}:{Naming.Of(duck.Kind)} ({duck.Hp}/{duck.MaxHp} HP)";
}

static string Offer(RunState run, CampOffer? offer)
{
    if (offer is not { } value)
    {
        return "no offer";
    }

    return $"{SquadName(run, value.Duck)} gets [{value.Rarity} {value.Category}] "
        + $"{value.Name} — {value.Summary}";
}

static string LegendaryOfferText(RunState run, LegendaryOffer? offer)
{
    if (offer is not { } value)
    {
        return "no offer";
    }

    return $"{SquadName(run, value.Duck)} gets [{value.Rarity} Legendary] "
        + $"{value.Name} — {value.Summary}";
}

static string Bound(IReadOnlyList<string> rules) => rules.Count == 0
    ? string.Empty
    : " (director: " + string.Join(", ", rules) + ")";

static string? ValueAfter(string[] values, string flag)
{
    for (int i = 0; i + 1 < values.Length; i++)
    {
        if (string.Equals(values[i], flag, StringComparison.Ordinal))
        {
            return values[i + 1];
        }
    }

    return null;
}

static SessionData Load(string path, int fallbackSeed)
{
    if (!File.Exists(path))
    {
        return new SessionData(fallbackSeed, new List<int>());
    }

    int loadedSeed = fallbackSeed;
    var picks = new List<int>();
    foreach (string raw in File.ReadAllLines(path))
    {
        string line = raw.Trim();
        if (line.StartsWith("seed=", StringComparison.Ordinal)
            && int.TryParse(line[5..], NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
        {
            loadedSeed = value;
        }
        else if (line.StartsWith("pick=", StringComparison.Ordinal)
            && int.TryParse(line[5..], NumberStyles.Integer, CultureInfo.InvariantCulture, out int index))
        {
            picks.Add(index);
        }
    }

    return new SessionData(loadedSeed, picks);
}

static void Save(string path, int seed, IReadOnlyList<int> picks)
{
    Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
    var lines = new List<string>
    {
        "# ChatGPT Core playtest session; decisions are indices into Core's legal-command list",
        "campaign=" + CampaignId,
        "seed=" + seed.ToString(CultureInfo.InvariantCulture),
    };
    lines.AddRange(picks.Select(p => "pick=" + p.ToString(CultureInfo.InvariantCulture)));
    File.WriteAllLines(path, lines);
}

sealed record SessionData(int Seed, List<int> Picks);

sealed class MapDriver
{
    private readonly List<GameEvent> _fightEvents = new();
    private readonly List<RunEvent> _runEvents = new();

    public MapDriver(RunState run) => Run = run;

    public RunState Run { get; private set; }
    public IReadOnlyList<RunCommand> Legal { get; private set; } = Array.Empty<RunCommand>();
    public bool Waiting { get; private set; }
    public GameState? LastBoard { get; private set; }

    public void ClearEvents()
    {
        _fightEvents.Clear();
        _runEvents.Clear();
    }

    public void AdvanceToDecision()
    {
        Waiting = false;
        Legal = Array.Empty<RunCommand>();
        int safety = 0;

        while (Run.Phase != RunPhase.Complete)
        {
            if (++safety > 200000)
            {
                throw new InvalidOperationException("Automatic command budget exhausted.");
            }

            if (Run.Phase == RunPhase.AtNode)
            {
                Apply(new EnterNodeCommand());
                continue;
            }

            if (Run.Phase == RunPhase.InFight)
            {
                var enemy = Game.NextEnemyCommand(Run.Fight!);
                if (enemy is not null)
                {
                    Apply(new PlayCommand(enemy));
                    continue;
                }
            }

            var legal = Campaign.LegalRunCommands(Run);
            if (legal.Count == 0)
            {
                throw new InvalidOperationException($"No legal commands in phase {Run.Phase}.");
            }

            Legal = legal;
            Waiting = true;
            return;
        }
    }

    public void Apply(RunCommand command)
    {
        var step = Campaign.ApplyRun(Run, command);
        LastBoard = step.FinalBoard ?? Run.Fight ?? LastBoard;
        _fightEvents.AddRange(step.FightEvents);
        _runEvents.AddRange(step.Events);
        Run = step.NewState;
        Waiting = false;
        Legal = Array.Empty<RunCommand>();
    }

    public IEnumerable<string> EventLines()
    {
        foreach (var e in _fightEvents)
        {
            if (LastBoard is not null && CombatLog.IsHandled(e)
                && e is not ActivationStarted and not ActivationEnded and not RoundStarted
                && e is not RoundEnded and not IntentDeclared)
            {
                yield return CombatLog.EventName(e) + ": " + CombatLog.Detail(e, LastBoard);
            }
        }

        foreach (var e in _runEvents)
        {
            yield return e switch
            {
                FightBegan began => $"Fight began: {began.FightId}",
                FightResolved resolved => $"Fight resolved: {resolved.FightId} — {resolved.Outcome}, round {resolved.Round}",
                VoteResolved vote => vote.ByCoin
                    ? $"Split vote: coin {vote.Coin}, route moved to {vote.ChosenNodeId}"
                    : $"Agreed vote: route moved to {vote.ChosenNodeId}",
                MapMoved moved => $"Map moved to {moved.ToNodeId} ({moved.Lane}, column {moved.Column + 1})",
                EventOffered offered => $"Event offered: {offered.EventId}",
                EventDeclined declined => $"Event declined: {declined.EventId}",
                MaxHpRaised raised => $"Maximum HP raised: {raised.RunUnitId} {raised.MaxFrom}->{raised.MaxTo}; HP {raised.HpFrom}->{raised.HpTo}",
                UnitRested rested => $"Rested: {rested.RunUnitId} HP {rested.From}->{rested.To}",
                CampOffered offered => $"Camp offered: {offered.Table}",
                CampTaken taken => $"Camp taken: {taken.Player} gave {taken.Kind} {taken.Name} — {taken.Summary}",
                ActCleared cleared => $"Act cleared: {cleared.ActId}, {cleared.FightsWon} fights",
                RunWon => "Run won",
                RunLost lost => "Run lost: " + lost.Reason,
                _ => e.GetType().Name,
            };
        }
    }
}
