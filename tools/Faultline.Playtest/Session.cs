using Faultline.Core;

namespace Faultline.Playtest;

/// <summary>
/// A campaign played one decision at a time, by whoever is reading the output.
/// </summary>
/// <remarks>
/// <para>
/// Stateless between invocations, on purpose. A run is a seed plus a command log and it replays
/// byte-identically, so the log *is* the save file: every invocation loads it, replays it, acts, and
/// writes it back. Nothing is held in memory between calls and there is no daemon to leave running.
/// </para>
/// <para>
/// That also means the artefact a session leaves behind is the same artefact a policy run leaves
/// behind, and <see cref="Replay"/> watches either of them without knowing which it has.
/// </para>
/// </remarks>
public static class Session
{
    /// <summary>Runs the session sub-command.</summary>
    /// <param name="args">Arguments after <c>--session</c>.</param>
    public static void Run(string[] args)
    {
        string path = args.Length > 0 && !args[0].StartsWith("--", StringComparison.Ordinal)
            ? args[0]
            : Path.Combine("docs", "playtest", "logs", "session.log");

        int seed = IntArg(args, "--seed", 1);
        string label = StringArg(args, "--label", "claude");
        bool fresh = Has(args, "--new");

        var log = fresh ? new RunLog() : RunLog.Load(path);
        if (fresh)
        {
            log.Seed = seed;
            log.Label = label;
        }

        var driver = RunDriver.Start(CampaignLibrary.ById(log.CampaignId), log.Seed);
        int consumed = driver.Replay(log.Decisions);

        if (consumed < log.Decisions.Count)
        {
            Console.WriteLine(
                $"warning: the log has {log.Decisions.Count} decisions but the run only accepted "
                + $"{consumed}. The run ended early, or the log is out of sync with the rules.");
        }

        // --pick takes a comma-separated list so a run of obvious moves costs one invocation
        // rather than one per decision.
        var picks = StringArg(args, "--pick", string.Empty);
        if (picks.Length > 0)
        {
            foreach (var raw in picks.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (!driver.AtDecision)
                {
                    Console.WriteLine("run is over — the remaining picks were ignored.");
                    break;
                }

                if (!int.TryParse(raw.Trim(), out int index) || index < 0 || index >= driver.Legal.Count)
                {
                    Console.WriteLine($"'{raw.Trim()}' is not one of the {driver.Legal.Count} options. Stopping here.");
                    break;
                }

                var chosen = driver.Legal[index];
                log.Decisions.Add(new RunLog.Decision(
                    driver.Run.NodeIndex, KindOf(driver, chosen), chosen));

                // Described against the board as it was *before* the command. Describing it after
                // re-previews the shove from the destination and prints a different move from the
                // one that was made.
                var before = driver.Run.Fight!;
                driver.ClearEvents();
                driver.Decide(chosen);

                Console.WriteLine("> " + View.Describe(before, chosen));
                foreach (var line in Narrate(driver))
                {
                    Console.WriteLine("    " + line);
                }
            }
        }

        // --auto hands the next N decisions to a scripted policy, for the parts of a fight that do
        // not deserve a reader's attention — deployment, mopping up a last Husk.
        var auto = StringArg(args, "--auto", string.Empty);
        if (auto.Length > 0)
        {
            var parts = auto.Split(':');
            var policy = Policies.ByName(parts[0]);
            int count = parts.Length > 1 && int.TryParse(parts[1], out int n) ? n : 1;
            var rng = new DeterministicRng(log.Seed ^ policy.Name.GetHashCode());

            for (int i = 0; i < count && driver.AtDecision; i++)
            {
                var chosen = policy.Choose(driver.Run.Fight!, driver.Legal, rng);
                log.Decisions.Add(new RunLog.Decision(
                    driver.Run.NodeIndex, KindOf(driver, chosen), chosen));
                driver.Decide(chosen);
            }

            Console.WriteLine($"auto: {policy.Name} played up to {count} decisions.");
        }

        log.Save(path);

        Console.WriteLine();
        if (driver.Complete)
        {
            Console.WriteLine($"=== run over: {driver.Run.Outcome} — {driver.Reason} ===");
            Console.WriteLine($"cleared {driver.Run.FightsWon} fights, stopped at node {driver.Run.NodeIndex}");
            Console.WriteLine(View.RunHeader(driver.Run));
        }
        else if (driver.AtDecision)
        {
            Console.WriteLine(View.Brief(driver.Run, driver.Legal));
        }
        else
        {
            Console.WriteLine("stuck: " + driver.Reason);
        }

        Console.WriteLine($"log: {path}  ({log.Decisions.Count} decisions)");
    }

    /// <summary>Watches a recorded run back, frame by frame.</summary>
    /// <param name="args">Arguments after <c>--replay</c>.</param>
    public static void Replay(string[] args)
    {
        string path = args.Length > 0 && !args[0].StartsWith("--", StringComparison.Ordinal)
            ? args[0]
            : Path.Combine("docs", "playtest", "logs", "session.log");

        var log = RunLog.Load(path);
        bool boards = Has(args, "--boards");
        int pause = IntArg(args, "--pause", 0);

        Console.WriteLine($"=== replay: {log.Label} on '{log.CampaignId}', seed {log.Seed}, "
            + $"{log.Decisions.Count} decisions ===\n");

        var driver = RunDriver.Start(CampaignLibrary.ById(log.CampaignId), log.Seed);
        int node = -1;

        foreach (var decision in log.Decisions)
        {
            if (!driver.AtDecision)
            {
                Console.WriteLine("the run ended before the log did.");
                break;
            }

            if (driver.Run.NodeIndex != node)
            {
                node = driver.Run.NodeIndex;
                Console.WriteLine();
                Console.WriteLine(View.RunHeader(driver.Run));
            }

            var board = driver.Run.Fight!;
            driver.ClearEvents();
            driver.Decide(decision.Command);

            Console.WriteLine("> " + View.Describe(board, decision.Command));
            foreach (var line in Narrate(driver))
            {
                Console.WriteLine("    " + line);
            }

            if (boards && driver.Run.Fight is not null)
            {
                Console.WriteLine();
                Console.WriteLine(View.Board(driver.Run.Fight));
            }

            if (pause > 0)
            {
                Thread.Sleep(pause);
            }
        }

        Console.WriteLine();
        Console.WriteLine($"=== {driver.Run.Outcome} — {driver.Reason} ===");
        Console.WriteLine($"cleared {driver.Run.FightsWon} fights, stopped at node {driver.Run.NodeIndex}");
    }

    /// <summary>
    /// Turns the events of one step into readable lines, using Core's own log formatter so a replay
    /// and an exported combat log say the same thing about the same event.
    /// </summary>
    private static IEnumerable<string> Narrate(RunDriver driver)
    {
        var board = driver.LastBoard;

        foreach (var e in driver.FightEvents)
        {
            if (board is null || !CombatLog.IsHandled(e))
            {
                continue;
            }

            // The noisy bookkeeping events say nothing a reader of a replay wants.
            if (e is ActivationStarted or ActivationEnded or RoundStarted or RoundEnded or IntentDeclared)
            {
                continue;
            }

            yield return CombatLog.EventName(e) + ": " + CombatLog.Detail(e, board);
        }

        foreach (var e in driver.RunEvents)
        {
            switch (e)
            {
                case FightBegan began:
                    yield return $"** fight {began.Index}: {began.FightId} begins **";
                    break;
                case FightResolved resolved:
                    yield return $"** {resolved.FightId}: {resolved.Outcome} on round {resolved.Round} **";
                    break;
                case RunLost lost:
                    yield return "** run lost: " + lost.Reason + " **";
                    break;
                case RunWon:
                    yield return "** the campaign is cleared **";
                    break;
            }
        }
    }

    /// <summary>The class of the unit a command acts as, for the log's desync guard.</summary>
    private static UnitKind KindOf(RunDriver driver, Command command) =>
        driver.Run.Fight?.FindUnit(RunLog.ActorOf(command))?.Kind ?? UnitKind.Husk;

    private static bool Has(string[] args, string flag) =>
        args.Any(a => string.Equals(a, flag, StringComparison.Ordinal));

    private static string StringArg(string[] args, string flag, string fallback)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], flag, StringComparison.Ordinal))
            {
                return args[i + 1];
            }
        }

        return fallback;
    }

    private static int IntArg(string[] args, string flag, int fallback) =>
        int.TryParse(StringArg(args, flag, string.Empty), out int value) ? value : fallback;
}
