using System.Globalization;
using System.Text;
using Faultline.Core;
using Faultline.Playtest;

// Ten campaign runs at one seed, one per policy, written as TSV plus a human summary.
//
//   dotnet run --project tools/Faultline.Playtest -- --seed 1 --out playtest
//
// Headless: no browser, no dev server, no port. It can be run while someone is playing the app.

if (args.Length > 0 && args[0] == "--stranded")
{
    Faultline.Playtest.Reach.Stranded();
    return;
}

if (args.Length > 0 && args[0] == "--losing-seeds")
{
    Faultline.Playtest.Agency.LosingSeeds(args.Length > 1 && int.TryParse(args[1], out int lt) ? lt : 12);
    return;
}

if (args.Length > 0 && args[0] == "--sweep" && args.Length > 2)
{
    Faultline.Playtest.Agency.Sweep(args[1], Enum.Parse<UnitKind>(args[2]));
    return;
}

if (args.Length > 0 && args[0] == "--agency")
{
    if (args.Length > 1)
    {
        Faultline.Playtest.Agency.Placements(args[1]);
    }
    else
    {
        Faultline.Playtest.Agency.Report();
    }

    return;
}

if (args.Length > 0 && args[0] == "--levels")
{
    int levelSeed = 1;
    string levelOut = Path.Combine("docs", "playtest");
    for (int i = 1; i < args.Length; i++)
    {
        if (args[i] == "--seed" && i + 1 < args.Length && int.TryParse(args[i + 1], out int ls))
        {
            levelSeed = ls;
        }
        else if (args[i] == "--out" && i + 1 < args.Length)
        {
            levelOut = args[i + 1];
        }
    }

    Faultline.Playtest.Levels.Report(levelSeed, levelOut);
    return;
}

if (args.Length > 0 && args[0] == "--connectivity")
{
    Faultline.Playtest.Connectivity.Report();
    return;
}

if (args.Length > 0 && args[0] == "--session")
{
    Session.Run(args.Skip(1).ToArray());
    return;
}

if (args.Length > 0 && args[0] == "--replay")
{
    Session.Replay(args.Skip(1).ToArray());
    return;
}

if (args.Length > 0 && args[0] == "--probe")
{
    Faultline.Playtest.Probe.SoftLock(args.Length > 1 && int.TryParse(args[1], out int ps) ? ps : 1);
    return;
}

int seed = 1;
string outDir = Path.Combine("docs", "playtest");

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--seed" && i + 1 < args.Length && int.TryParse(args[i + 1], out int s))
    {
        seed = s;
    }
    else if (args[i] == "--out" && i + 1 < args.Length)
    {
        outDir = args[i + 1];
    }
}

// Ten runs. The seed is the same for every one of them on purpose — the campaign, the boards and
// every enemy plan are therefore identical, and the only thing that differs is how the players
// decide. Anything the runs disagree about is caused by play, not by luck.
var policies = Policies.All();

Directory.CreateDirectory(outDir);
Console.WriteLine($"Faultline playtest — campaign '{CampaignLibrary.Faultline.Id}', seed {seed}, {policies.Length} runs");
Console.WriteLine();

// Verve is a player resource, so the per-class table walks the roster rather than every archetype.
var playerClasses = new[]
{
    UnitKind.Vanguard, UnitKind.Archer, UnitKind.Threadcaster, UnitKind.Wardbearer,
};

var allSpends = Enum.GetValues<VerveSpend>();

var reports = new List<RunReport>();
foreach (var policy in policies)
{
    // Every run is recorded, so any of them can be watched back with
    //   dotnet run --project tools/Faultline.Playtest -- --replay <log> --boards
    var log = new RunLog();
    var report = RunHarness.Play(policy, seed, log);
    log.Save(Path.Combine(outDir, "logs", policy.Name + ".log"));
    reports.Add(report);
    Console.WriteLine(
        $"  {policy.Name,-12} {report.Outcome,-10} cleared {report.FightsWon,2}/10  " +
        $"stopped at node {report.EndedAtNode,2}  ({report.Reason})");
}

Console.WriteLine();

// --- runs.tsv: one row per run -----------------------------------------------------------------
var runs = new StringBuilder();
runs.AppendLine("policy\tseed\toutcome\tfights_won\tended_at_node\tcommands\treason");
foreach (var r in reports)
{
    runs.AppendLine(string.Join('\t', r.Policy, r.Seed, r.Outcome, r.FightsWon, r.EndedAtNode, r.Commands, r.Reason));
}

File.WriteAllText(Path.Combine(outDir, "runs.tsv"), runs.ToString());

// --- fights.tsv: one row per fight played ------------------------------------------------------
var fights = new StringBuilder();
fights.AppendLine(
    "policy\tnode\tfight\toutcome\trounds\tdmg_taken\tdmg_taken_attack\tdmg_taken_collision\t" +
    "dmg_taken_spikes\tdmg_taken_fall\tdmg_dealt\tdmg_dealt_attack\tdmg_dealt_collision\t" +
    "dmg_dealt_spikes\tdmg_dealt_fall\tdowned\tvoided\tenemies_killed\tcollisions\tpushes\t" +
    "verve_earned\tverve_wasted\tverve_spent\tspends");

foreach (var r in reports)
{
    foreach (var f in r.Fights)
    {
        fights.AppendLine(string.Join(
            '\t',
            r.Policy,
            f.NodeIndex,
            f.FightId,
            f.Outcome,
            f.Rounds,
            Total(f.PlayerDamage),
            Get(f.PlayerDamage, DamageSource.Attack),
            Get(f.PlayerDamage, DamageSource.Collision),
            Get(f.PlayerDamage, DamageSource.Spikes),
            Get(f.PlayerDamage, DamageSource.Fall),
            Total(f.EnemyDamage),
            Get(f.EnemyDamage, DamageSource.Attack),
            Get(f.EnemyDamage, DamageSource.Collision),
            Get(f.EnemyDamage, DamageSource.Spikes),
            Get(f.EnemyDamage, DamageSource.Fall),
            f.Downed,
            f.Voided,
            f.EnemiesKilled,
            f.Collisions,
            f.Pushes,
            f.VerveEarned.Values.Sum(),
            f.VerveWasted.Values.Sum(),
            f.VerveSpent.Values.Sum(),
            f.Spends.Values.Sum()));
    }
}

File.WriteAllText(Path.Combine(outDir, "fights.tsv"), fights.ToString());

// --- summary.md: where the runs died, and what killed them -------------------------------------
var summary = new StringBuilder();
summary.AppendLine("# Playtest — " + policies.Length + " campaign runs, seed " + seed);
summary.AppendLine();
summary.AppendLine("Generated by `tools/Faultline.Playtest`. Every run played the same campaign at the");
summary.AppendLine("same seed, so the boards, the enemy plans and every draw were identical. The only");
summary.AppendLine("thing that differed is how the players chose — anything below is caused by play.");
summary.AppendLine();

summary.AppendLine("## Runs");
summary.AppendLine();
summary.AppendLine("| Policy | What it does | Outcome | Cleared | Stopped at | Why |");
summary.AppendLine("|---|---|---|---|---|---|");
for (int i = 0; i < reports.Count; i++)
{
    var r = reports[i];
    summary.AppendLine($"| `{r.Policy}` | {policies[i].Intent} | {r.Outcome} | {r.FightsWon}/10 | node {r.EndedAtNode} | {r.Reason} |");
}

summary.AppendLine();
summary.AppendLine("## Where runs stop");
summary.AppendLine();
summary.AppendLine("| Node | Fight | Runs that reached it | Runs that died here |");
summary.AppendLine("|---|---|---|---|");

var order = CampaignLibrary.Faultline.Nodes;
for (int n = 0; n < order.Count; n++)
{
    if (order[n] is not FightNode fn)
    {
        summary.AppendLine($"| {n} | *rest* | {reports.Count(r => r.EndedAtNode >= n)} | — |");
        continue;
    }

    int reached = reports.Count(r => r.Fights.Any(f => f.NodeIndex == n));
    int diedHere = reports.Count(r => r.Outcome == RunOutcome.Lost && r.EndedAtNode == n);
    summary.AppendLine($"| {n} | `{fn.FightId}` | {reached} | {diedHere} |");
}

summary.AppendLine();
summary.AppendLine("## Damage the squad took, by source");
summary.AppendLine();
summary.AppendLine("The thesis check. A game whose primary weapon is the board should not be settled by");
summary.AppendLine("ordinary attacks alone.");
summary.AppendLine();
summary.AppendLine("| Policy | Attack | Collision | Spikes | Fall | Collisions caused | Pushes |");
summary.AppendLine("|---|---|---|---|---|---|---|");
foreach (var r in reports)
{
    summary.AppendLine(string.Format(
        CultureInfo.InvariantCulture,
        "| `{0}` | {1} | {2} | {3} | {4} | {5} | {6} |",
        r.Policy,
        r.Fights.Sum(f => Get(f.PlayerDamage, DamageSource.Attack)),
        r.Fights.Sum(f => Get(f.PlayerDamage, DamageSource.Collision)),
        r.Fights.Sum(f => Get(f.PlayerDamage, DamageSource.Spikes)),
        r.Fights.Sum(f => Get(f.PlayerDamage, DamageSource.Fall)),
        r.Fights.Sum(f => f.Collisions),
        r.Fights.Sum(f => f.Pushes)));
}

summary.AppendLine();
summary.AppendLine("## " + Naming.Meter + ", by class");
summary.AppendLine();
summary.AppendLine("The other end of the thesis check. Every charge condition is a displacement, a hazard,");
summary.AppendLine("high ground or absorption, so a squad earning Verve is a squad using the board — and this");
summary.AppendLine("number and the attack share above should move in opposite directions.");
summary.AppendLine();
summary.AppendLine("**Wasted** is a charge that arrived at a full meter. A large wasted column against a small");
summary.AppendLine("spent one means the game is paying out faster than a player can find a use for it.");
summary.AppendLine();
summary.AppendLine("| Policy | Class | Earned | Wasted | Spent |");
summary.AppendLine("|---|---|---|---|---|");
foreach (var r in reports)
{
    foreach (var kind in playerClasses)
    {
        int earned = r.Fights.Sum(f => Get(f.VerveEarned, kind));
        int wasted = r.Fights.Sum(f => Get(f.VerveWasted, kind));
        int spent = r.Fights.Sum(f => Get(f.VerveSpent, kind));

        if (earned == 0 && wasted == 0 && spent == 0)
        {
            continue;
        }

        summary.AppendLine($"| `{r.Policy}` | {Naming.Of(kind)} | {earned} | {wasted} | {spent} |");
    }
}

summary.AppendLine();
summary.AppendLine("## Preen against what it soaked");
summary.AppendLine();
summary.AppendLine("The negative-sum check. Preen is a flat " + Verve.PreenHeal + " heal — it is not scaled to anything —");
summary.AppendLine("and the only thing that fills a Wardbearer's meter is absorbing hits meant for somebody");
summary.AppendLine("else, so what he heals should not exceed what the stance took for the squad.");
summary.AppendLine();
summary.AppendLine("**Absorbed is the blow the ally was spared, before the guard's own halving.** He pays");
summary.AppendLine("`Guard.Halve` of it; the squad is spared all of it. Counting what he paid instead");
summary.AppendLine("understates every absorb by half.");
summary.AppendLine();
summary.AppendLine("| Policy | Absorbed via stance | Healed by Preen | Net |");
summary.AppendLine("|---|---|---|---|");
var breaches = new List<string>();
var carried = new List<string>();
foreach (var r in reports)
{
    int absorbed = r.Fights.Sum(f => f.Absorbed);
    int healed = r.Fights.Sum(f => f.Healed);
    summary.AppendLine($"| `{r.Policy}` | {absorbed} | {healed} | {absorbed - healed} |");

    // Per run, which is the only unit that can balance: the meter carries between fights by
    // design (D-074), so a Wardbearer can soak in one fight and spend the charge in the next.
    if (healed > absorbed)
    {
        breaches.Add($"`{r.Policy}`: healed {healed} against {absorbed} absorbed across the run");
    }

    // Per fight is reported because it is where the carry-over shows up, not because it is
    // expected to hold.
    foreach (var f in r.Fights.Where(f => f.Healed > f.Absorbed))
    {
        carried.Add($"`{r.Policy}` on `{f.FightId}`: healed {f.Healed} against {f.Absorbed} absorbed in that fight");
    }
}

summary.AppendLine();
if (breaches.Count == 0)
{
    summary.AppendLine("**Held in every run measured.**");
}
else
{
    summary.AppendLine("**BREACHED** — Preen healed more than the stance soaked:");
    foreach (var breach in breaches)
    {
        summary.AppendLine("- " + breach);
    }
}

if (carried.Count > 0)
{
    summary.AppendLine();
    summary.AppendLine("Fights where the heal was paid for out of an earlier fight's soaking. Expected —");
    summary.AppendLine("this is the meter carrying between fights, not a leak:");
    foreach (var line in carried)
    {
        summary.AppendLine("- " + line);
    }
}

summary.AppendLine();
summary.AppendLine("**One hole remains, and it is not visible in these numbers.** A charge does not require");
summary.AppendLine("damage: an absorb that only moved the guard a tile earns a point while adding nothing to");
summary.AppendLine("the absorbed column, so three shoves that push a Wardbearer around without hurting him");
summary.AppendLine("would buy a Preen off zero soaking. No policy has managed it yet (D-084).");

summary.AppendLine();
summary.AppendLine("## Cast — where she put them, and how close the hazards were");
summary.AppendLine();
summary.AppendLine("Cast lands within one tile of the Fisher, so the landing distribution is bounded by");
summary.AppendLine("where she is standing rather than by how well she aims. The last column is the whole");
summary.AppendLine("story: her step distance to the nearest drain or spikes at the moment she cast.");
summary.AppendLine();
summary.AppendLine("| Policy | Casts | → open | → spikes | → drain | Avg. hazard distance at cast |");
summary.AppendLine("|---|---|---|---|---|---|");
foreach (var r in reports)
{
    int casts = r.Fights.Sum(f => Get(f.Spends, VerveSpend.Cast));
    if (casts == 0)
    {
        continue;
    }

    int open = r.Fights.Sum(f => Get(f.CastLandings, TileType.Open) + Get(f.CastLandings, TileType.HighGround));
    int spikes = r.Fights.Sum(f => Get(f.CastLandings, TileType.Spikes));
    int drain = r.Fights.Sum(f => Get(f.CastLandings, TileType.Pit));

    var distances = r.Fights.SelectMany(f => f.HazardDistanceAtCast).Where(d => d >= 0).ToList();
    string average = distances.Count == 0
        ? "—"
        : (distances.Sum() / (double)distances.Count).ToString("0.0", CultureInfo.InvariantCulture);

    summary.AppendLine($"| `{r.Policy}` | {casts} | {open} | {spikes} | {drain} | {average} |");
}

summary.AppendLine();
summary.AppendLine("## What the " + Naming.Meter + " went on");
summary.AppendLine();
summary.AppendLine("| Policy | " + string.Join(" | ", allSpends.Select(Verve.NameOf)) + " |");
summary.AppendLine("|---|" + string.Concat(allSpends.Select(_ => "---|")));
foreach (var r in reports)
{
    summary.AppendLine(
        $"| `{r.Policy}` | "
        + string.Join(" | ", allSpends.Select(sp => r.Fights.Sum(f => Get(f.Spends, sp))))
        + " |");
}

summary.AppendLine();
summary.AppendLine("## Damage the squad dealt, by source");
summary.AppendLine();
summary.AppendLine("| Policy | Attack | Collision | Spikes | Fall | Enemies killed |");
summary.AppendLine("|---|---|---|---|---|---|");
foreach (var r in reports)
{
    summary.AppendLine(string.Format(
        CultureInfo.InvariantCulture,
        "| `{0}` | {1} | {2} | {3} | {4} | {5} |",
        r.Policy,
        r.Fights.Sum(f => Get(f.EnemyDamage, DamageSource.Attack)),
        r.Fights.Sum(f => Get(f.EnemyDamage, DamageSource.Collision)),
        r.Fights.Sum(f => Get(f.EnemyDamage, DamageSource.Spikes)),
        r.Fights.Sum(f => Get(f.EnemyDamage, DamageSource.Fall)),
        r.Fights.Sum(f => f.EnemiesKilled)));
}

summary.AppendLine();
summary.AppendLine("## Per-fight length, by policy");
summary.AppendLine();
summary.AppendLine("Rounds each fight took. A blank means the run never got there.");
summary.AppendLine();
summary.Append("| Fight |");
foreach (var r in reports)
{
    summary.Append(" `" + r.Policy + "` |");
}

summary.AppendLine();
summary.Append("|---|");
foreach (var _ in reports)
{
    summary.Append("---|");
}

summary.AppendLine();

foreach (var id in CampaignLibrary.Faultline.FightIds())
{
    summary.Append("| `" + id + "` |");
    foreach (var r in reports)
    {
        var f = r.Fights.FirstOrDefault(x => x.FightId == id);
        summary.Append(' ').Append(f is null ? "—" : f.Rounds + (f.Outcome == FightOutcome.Won ? "" : " ✗")).Append(" |");
    }

    summary.AppendLine();
}

File.WriteAllText(Path.Combine(outDir, "summary.md"), summary.ToString());

Console.WriteLine($"wrote {Path.Combine(outDir, "runs.tsv")}");
Console.WriteLine($"wrote {Path.Combine(outDir, "fights.tsv")}");
Console.WriteLine($"wrote {Path.Combine(outDir, "summary.md")}");

static int Get<T>(Dictionary<T, int> d, T key)
    where T : notnull => d.TryGetValue(key, out int v) ? v : 0;

static int Total(Dictionary<DamageSource, int> d) => d.Values.Sum();
