using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Faultline.Core;
using Faultline.Web.Shell;

namespace Faultline.Web.Tests;

/// <summary>
/// Builds <c>docs/WARRENS_CONTENT.md</c> — what the Warrens fields today, what Warrens v2 asks for,
/// and the shortfall between them — and fails when the file no longer matches.
/// </summary>
/// <remarks>
/// <para>
/// Generated for the same reason the progression review is: every number in it is a question the
/// repo can already answer, and a shortfall typed into markdown is wrong the next time a board lands.
/// Set <c>PLUCK_WRITE_DOCS=1</c> to rewrite it instead of asserting.
/// </para>
/// <para>
/// <b>The difficulty bands are a proxy and the doc says so.</b> No <c>.fight</c> carries a tier, a
/// role or a pool — the library's organising axis is SUBJECT (topology, hazard, composition), not
/// difficulty. Total enemy hit points is the one gradient that is derivable, and it happens to track
/// the authored act's own ramp. It is a starting draft for a marking pass, not the marking.
/// </para>
/// </remarks>
public class WarrensContentDocTests
{
    /// <summary>Seeds the v2 sample is measured over. Fixed, so the doc is reproducible.</summary>
    private static readonly int[] Seeds = { 1, 777, 12345, 20260808, 31337, 4242, 99, 8, 555, 2024 };

    private sealed record Band(string Name, int Low, int High, string Role);

    /// <summary>
    /// The bands, cut where the authored act's own ramp cuts: its opener sits at 16–20, its middle at
    /// 20–32, its last fight at 42 and its boss at 64.
    /// </summary>
    private static readonly Band[] Bands =
    {
        new Band("Opener", 0, 20, "column 1, and the gentlest of the early third"),
        new Band("Ordinary", 21, 30, "the bulk of an act — early and middle columns"),
        new Band("Hard", 31, 46, "the late third, and what an elite is cut from"),
        new Band("Endurance", 47, 56, "objective-shaped rather than harder: survive, hold"),
        new Band("Boss", 57, int.MaxValue, "terminals"),
    };

    /// <summary>The doc on disk still says what the repo says.</summary>
    [Fact]
    public void TheWarrensContentDoc_MatchesTheRepo()
    {
        var b = new List<string>();
        void W(string line) => b.Add(line);

        Header(W);
        Today(W);
        VersionTwo(W);
        Pool(W);
        Gap(W);

        var path = Path.Combine(RepoRoot(), "docs", "WARRENS_CONTENT.md");
        var built = string.Join("\n", b).TrimEnd() + "\n";

        if (Environment.GetEnvironmentVariable("PLUCK_WRITE_DOCS") == "1")
        {
            File.WriteAllText(path, built);
            return;
        }

        Assert.True(File.Exists(path), "docs/WARRENS_CONTENT.md is missing. Regenerate with PLUCK_WRITE_DOCS=1.");

        var onDisk = File.ReadAllText(path).Replace("\r\n", "\n").TrimEnd() + "\n";

        Assert.True(
            string.Equals(onDisk, built, StringComparison.Ordinal),
            "docs/WARRENS_CONTENT.md is stale — the board pool or the generator moved. "
            + "Regenerate with PLUCK_WRITE_DOCS=1 dotnet test --filter WarrensContentDoc.");
    }

    // ---- sections ------------------------------------------------------------------------------

    private static void Header(Action<string> W)
    {
        W("<!-- GENERATED — every count is read out of the repo. Regenerate rather than hand-edit:");
        W("     PLUCK_WRITE_DOCS=1 dotnet test tests/Faultline.Web.Tests --filter WarrensContentDoc -->");
        W("");
        W("# The Warrens — what it fields now, and what v2 needs");
        W("");
        W("Act 1 as authored, Warrens v2 as generated, the board pool as it stands, and the gap");
        W("between them — **how many boards to author, in which band, to fill v2 without repeats**.");
        W("");
    }

    private static void Today(Action<string> W)
    {
        var map = ActMapLibrary.Act1;

        W("## Act 1 today");
        W("");
        W("Hand-authored (`ActMapLibrary`), **"
            + map.Nodes.Count + " nodes over " + map.ColumnCount + " columns**, "
            + map.Edges.Count + " doors.");
        W("");
        W("| Column | Nodes | What stands there |");
        W("|---|---|---|");

        for (int c = 0; c < map.ColumnCount; c++)
        {
            var column = map.ColumnAt(c);
            W("| " + (c + 1) + " | " + column.Count + " | "
                + string.Join(" · ", column.Select(Describe)) + " |");
        }

        W("");

        var counts = Counts(map.Nodes);
        W("**By type:** " + string.Join(" · ", counts.Select(kv => kv.Value + " " + kv.Key)) + ".");
        W("");

        var boards = map.Nodes.Where(n => n.IsCombat && n.FightId.Length > 0)
            .Select(n => n.FightId).Distinct(StringComparer.Ordinal).ToList();

        W("**Boards fielded: " + boards.Count + "**, all distinct — "
            + string.Join(", ", boards.Select(NameOf)) + ".");
        W("");
        W("A run walks one route: **" + RouteRange(map, n => n.IsCombat) + " combat nodes**, "
            + RouteRange(map, n => n.Type == MapNodeType.Rest) + " Rests, and "
            + RouteRange(map, n => n.Type == MapNodeType.Event) + " events.");
        W("");
    }

    private static void VersionTwo(Action<string> W)
    {
        var shape = ActShape.Presets()[1].Copy();

        W("## Warrens v2, as generated");
        W("");
        W("`" + shape.Name + "` — " + shape.Columns + " columns, " + shape.MinWidth + "–"
            + shape.MaxWidth + " wide, " + shape.MinDoors + "–" + shape.MaxDoors + " doors per node, "
            + shape.Events + " events, " + shape.Rests + " mid-act Rests. **The sizing is not in the");
        W("design doc** (D-264) — it is a dial, and these are the numbers it currently produces.");
        W("");
        W("Measured over " + Seeds.Length + " fixed seeds:");
        W("");
        W("| | Low | High | Median |");
        W("|---|---|---|---|");

        var drafts = Seeds.Select(s => ActGenerator.Generate(shape, s, "v2")).ToList();

        Stat(W, "Nodes", drafts.Select(d => d.Steps.Count));
        Stat(W, "Combat nodes", drafts.Select(d => d.Steps.Count(s => s.IsCombat)));
        Stat(W, "Events placed", drafts.Select(d => d.CountOf(ActDraft.NodeKind.Event)));
        Stat(W, "Rests", drafts.Select(d => d.CountOf(ActDraft.NodeKind.Rest)));
        Stat(W, "Elites", drafts.Select(d => d.CountOf(ActDraft.NodeKind.Elite)));
        Stat(W, "Board repeats", drafts.Select(Repeats));
        Stat(W, "Widest column", drafts.Select(d => d.WidestColumn));
        Stat(W, "Adjacent pair, widest", drafts.Select(WidestAdjacentPair));

        W("");
        W("### What each third of the act asks for");
        W("");
        W("Bands rather than a per-column curve: with a pool this size a curve starves immediately.");
        W("Column 1 is the fixed opener and the last two columns are the pre-boss Rest and the boss.");
        W("");
        W("| Third | Columns | Combat nodes (low–high) | Widest adjacent pair |");
        W("|---|---|---|---|");

        foreach (var (name, from, to) in Thirds(shape.Columns))
        {
            var nodes = drafts.Select(d => CombatIn(d, from, to)).ToList();
            var pairs = drafts.Select(d => WidestAdjacentPair(d, from, to)).ToList();

            W("| " + name + " | " + (from + 1) + "–" + (to + 1)
                + " | " + nodes.Min() + "–" + nodes.Max()
                + " | " + pairs.Max() + " |");
        }

        W("");
    }

    private static void Pool(Action<string> W)
    {
        W("## The board pool today");
        W("");
        W("**" + FightLibrary.All().Count + " active boards** (" + FightLibrary.Retired().Count
            + " retired). Nothing marks difficulty: a `.fight` carries `id · name · number · size ·");
        W("description · design · protected · retired · footing · objective` and no tier, role or pool.");
        W("The library's organising axis is **subject** — `tp-` topology, `hz-` hazard, `ec-` enemy");
        W("composition, `as-` asymmetry, `cb-` manoeuvre, `nv-` variant proofs — not difficulty.");
        W("");
        W("So the bands below are **derived from total enemy hit points**, which is the one gradient");
        W("the data actually has, and which tracks the authored act's own ramp. **It is a draft for a");
        W("marking pass, not the marking** — and it provably cannot answer one question: `high-road`,");
        W("the act's elite, sits at 32, the same as two ordinary boards. Elite is a fact about the");
        W("reward and the lane, not about the roster.");
        W("");
        W("| Band | Enemy HP | Active boards | Role |");
        W("|---|---|---|---|");

        foreach (var band in Bands)
        {
            var boards = InBand(band);
            W("| **" + band.Name + "** | " + Range(band) + " | " + boards.Count + " | " + band.Role + " |");
        }

        W("");

        foreach (var band in Bands)
        {
            var boards = InBand(band);
            if (boards.Count == 0)
            {
                continue;
            }

            W("**" + band.Name + " (" + boards.Count + ")** — "
                + string.Join(", ", boards.Select(f => f.Name + " " + Hp(f))) + ".");
            W("");
        }

        W("**Events: " + EventLibrary.All().Count + "** — "
            + string.Join(", ", EventLibrary.All().Select(e => e.Name)) + ".");
        W("");
    }

    private static void Gap(Action<string> W)
    {
        var shape = ActShape.Presets()[1].Copy();
        var drafts = Seeds.Select(s => ActGenerator.Generate(shape, s, "v2")).ToList();

        int combatHigh = drafts.Max(d => d.Steps.Count(s => s.IsCombat));
        int pairHigh = drafts.Max(WidestAdjacentPair);

        W("## The gap");
        W("");
        W("Two targets, because they answer different questions.");
        W("");
        W("**The floor** is what stops a board repeating in adjacent columns: the widest two");
        W("neighbouring columns any seed produces is **" + pairHigh + "**, so the draw needs "
            + pairHigh + " boards available");
        W("at every step. Below that the generator repeats and says so in its proof log (D-264).");
        W("");
        W("**No repeats at all** needs one distinct board per combat node: **" + combatHigh
            + "** at the worst seed.");
        W("");
        // Demand is summed PER BAND, not per third: the early and middle thirds both draw Ordinary,
        // so a table that reported them separately would hide the shortfall by halving it.
        var thirds = Thirds(shape.Columns).ToList();

        int ordinaryNeed = drafts.Max(d => thirds
            .Where(t => BandFor(t.Name).Name == "Ordinary")
            .Sum(t => CombatIn(d, t.From, t.To)));

        int hardNeed = drafts.Max(d => thirds
            .Where(t => BandFor(t.Name).Name == "Hard")
            .Sum(t => CombatIn(d, t.From, t.To)));

        int elites = ActGenerator.ElitePool.Count;
        int events = EventLibrary.All().Count;
        int eventsNeeded = drafts.Max(d => d.CountOf(ActDraft.NodeKind.Event));

        W("| Category | Fills | Have | v2 floor | v2 no-repeats | Shortfall |");
        W("|---|---|---|---|---|---|");
        W("| **Opener** | column 1, fixed | " + InBand(Bands[0]).Count + " | 1 | 1 | — |");
        W("| **Ordinary** | early + middle thirds | " + InBand(Bands[1]).Count + " | " + pairHigh
            + " | " + ordinaryNeed + " | " + Short(InBand(Bands[1]).Count, ordinaryNeed) + " |");
        W("| **Hard** | late third | " + InBand(Bands[2]).Count + " | " + pairHigh + " | " + hardNeed
            + " | " + Short(InBand(Bands[2]).Count, hardNeed) + " |");
        W("| **Elite** | one gilt node | " + elites + " | 1 | 1 | " + Short(elites, 1) + " |");
        W("| **Event** | " + eventsNeeded + " scenes | " + events + " | 1 | " + eventsNeeded + " | "
            + Short(events, eventsNeeded) + " |");
        W("| **Boss** | the terminal | 1 | 1 | 1 | — |");
        W("| **Endurance** | unplaced by the generator | " + InBand(Bands[3]).Count + " | — | — | — |");
        W("| Rest | its own column | n/a | n/a | n/a | not authored content — a node type |");
        W("");
        W("**Whole act:** " + combatHigh + " combat nodes at the worst seed against "
            + (InBand(Bands[0]).Count + InBand(Bands[1]).Count + InBand(Bands[2]).Count)
            + " boards in the three bands the generator draws from"
            + (combatHigh <= InBand(Bands[0]).Count + InBand(Bands[1]).Count + InBand(Bands[2]).Count
                ? " — enough, if every band may fill any column."
                : " — short by "
                    + (combatHigh - (InBand(Bands[0]).Count + InBand(Bands[1]).Count + InBand(Bands[2]).Count))
                    + " even drawing from all three.")); 
        W("");
        W("### What that means in one paragraph");
        W("");
        W("The **floor is met with room to spare** — " + FightLibrary.All().Count + " active boards");
        W("against the " + pairHigh + " a single adjacent pair needs — so v2 walks today and its");
        W("repetition is at distance rather than back to back.");
        W("");
        W("**The band that actually runs out is Ordinary.** The early and middle thirds both draw from");
        W("it and together ask for up to " + ordinaryNeed + " against " + InBand(Bands[1]).Count
            + " boards. Hard is comfortable at " + InBand(Bands[2]).Count + " for "
            + hardNeed + ".");
        W("");
        W("**The one shortfall a player meets inside a single run is events**: v2 places up to "
            + eventsNeeded + " and " + events + " ships, so the same scene appears "
            + eventsNeeded + " times, three columns apart.");
        W("");
        W("### The order I would author in");
        W("");
        W("1. **Events — " + Math.Max(0, eventsNeeded - events) + " more.** The only shortfall a player");
        W("   meets inside a single run: the same scene, twice or three times, columns apart. Everything");
        W("   else on this list is about how two runs differ; this one is about how one run reads.");
        W("2. **Ordinary-band boards — " + Math.Max(0, ordinaryNeed - InBand(Bands[1]).Count)
            + " more.** The only band the arithmetic actually");
        W("   runs out of: two thirds of the act draw from it and together want " + ordinaryNeed);
        W("   against " + InBand(Bands[1]).Count + ".");
        W("3. **The `pool:` marking itself**, before more boards land. Boards authored with no way to");
        W("   say which band they belong to leave the generator drawing from one flat list, which is");
        W("   what makes an early column and a late one feel the same — and it is the marking, not the");
        W("   count, that turns a bigger library into a better act.");
        W("4. **A second elite board.** Not a shortfall for one act — v2 places exactly one — but one");
        W("   board means every gilt node in every run is the same fight, and the elite is the node the");
        W("   hungry route is *for*.");
        W("5. **Hard is comfortable** at " + InBand(Bands[2]).Count + " for " + hardNeed
            + ", and **Endurance (" + InBand(Bands[3]).Count + ") is unreachable** —");
        W("   the generator never places an objective-shaped board, so `the-door` and `hold-the-gate`");
        W("   cannot appear in a generated act at all. That is a generator gap, not a content one.");
        W("");
    }

    // ---- helpers -------------------------------------------------------------------------------

    private static Band BandFor(string third) => third switch
    {
        "Early" => Bands[1],
        "Middle" => Bands[1],
        _ => Bands[2],
    };

    private static IEnumerable<(string Name, int From, int To)> Thirds(int columns)
    {
        int last = columns - 1;
        int preBoss = last - 1;
        int span = Math.Max(1, (preBoss - 1) / 3);

        yield return ("Early", 1, 1 + span - 1);
        yield return ("Middle", 1 + span, 1 + (2 * span) - 1);
        yield return ("Late", 1 + (2 * span), preBoss - 1);
    }

    private static int CombatIn(ActDraft draft, int from, int to) =>
        draft.Steps.Count(s => s.IsCombat && s.Column >= from && s.Column <= to);

    private static int WidestAdjacentPair(ActDraft draft) =>
        WidestAdjacentPair(draft, 0, draft.ColumnCount - 1);

    private static int WidestAdjacentPair(ActDraft draft, int from, int to)
    {
        int widest = 0;
        for (int c = from; c < to; c++)
        {
            int pair = draft.ColumnAt(c).Count(s => s.IsCombat)
                + draft.ColumnAt(c + 1).Count(s => s.IsCombat);

            if (pair > widest)
            {
                widest = pair;
            }
        }

        return widest;
    }

    private static int Repeats(ActDraft draft)
    {
        var used = draft.Steps.Where(s => s.IsCombat && s.Id.Length > 0).Select(s => s.Id).ToList();
        return used.Count - used.Distinct(StringComparer.Ordinal).Count();
    }

    private static void Stat(Action<string> W, string label, IEnumerable<int> values)
    {
        var list = values.OrderBy(v => v).ToList();
        W("| " + label + " | " + list.First() + " | " + list.Last() + " | " + list[list.Count / 2] + " |");
    }

    private static string Short(int have, int need) =>
        have >= need ? "—" : "**+" + (need - have) + "**";

    private static string Range(Band band) =>
        band.High == int.MaxValue ? band.Low + "+" : band.Low + "–" + band.High;

    private static IReadOnlyList<FightDefinition> InBand(Band band) =>
        FightLibrary.All().Where(f => EnemyHp(f) >= band.Low && EnemyHp(f) <= band.High)
            .OrderBy(EnemyHp)
            .ToList();

    private static int EnemyHp(FightDefinition fight) =>
        fight.Enemies.Select(e => e.Kind)
            .Concat(fight.Waves.SelectMany(w => w.Arrivals.Select(a => a.Kind)))
            .Sum(k => UnitTemplate.For(k).MaxHp);

    private static string Hp(FightDefinition fight) =>
        "(" + EnemyHp(fight).ToString(CultureInfo.InvariantCulture) + ")";

    private static string NameOf(string fightId)
    {
        foreach (var fight in FightLibrary.All())
        {
            if (string.Equals(fight.Id, fightId, StringComparison.Ordinal))
            {
                return fight.Name;
            }
        }

        return fightId;
    }

    private static string Describe(MapNode node) => node.Type switch
    {
        MapNodeType.Rest => "Still Pond",
        MapNodeType.Event => "Event — " + node.EventId,
        MapNodeType.Elite => "ELITE — " + NameOf(node.FightId),
        MapNodeType.Boss => "BOSS — " + NameOf(node.FightId),
        _ => NameOf(node.FightId),
    };

    private static Dictionary<string, int> Counts(IReadOnlyList<MapNode> nodes)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var node in nodes)
        {
            var key = node.Type switch
            {
                MapNodeType.Rest => "Rests",
                MapNodeType.Event => "events",
                MapNodeType.Elite => "elite",
                MapNodeType.Boss => "boss",
                _ => "fights",
            };

            counts[key] = counts.TryGetValue(key, out int had) ? had + 1 : 1;
        }

        return counts;
    }

    /// <summary>Fewest and most a single route meets, walked exhaustively.</summary>
    private static string RouteRange(ActMap map, Func<MapNode, bool> counts)
    {
        int min = int.MaxValue;
        int max = 0;
        Walk(map, map.StartNodeId, 0, counts, ref min, ref max);
        return min == max ? min.ToString(CultureInfo.InvariantCulture) : min + "–" + max;
    }

    private static void Walk(
        ActMap map, string id, int sofar, Func<MapNode, bool> counts, ref int min, ref int max)
    {
        var node = map.NodeAt(id)!;
        if (counts(node))
        {
            sofar++;
        }

        var next = map.Successors(id);
        if (next.Count == 0)
        {
            if (sofar < min) { min = sofar; }
            if (sofar > max) { max = sofar; }
            return;
        }

        foreach (var to in next)
        {
            Walk(map, to, sofar, counts, ref min, ref max);
        }
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "CLAUDE.md")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? ".";
    }
}
