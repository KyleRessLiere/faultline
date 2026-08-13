using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Faultline.Core;

namespace Faultline.Web.Tests;

/// <summary>
/// Builds <c>docs/WARRENS_V2_POOL.md</c> — every board Warrens v2 can draw, band by band, with its
/// grid and the reason it exists — and fails when the file no longer matches the library.
/// </summary>
/// <remarks>
/// Generated, like the other reviews: a board's grid, roster and design notes all already live in its
/// <c>.fight</c> file, and a second hand-written copy of them would be wrong the first time a board
/// was edited. Set <c>PLUCK_WRITE_DOCS=1</c> to rewrite it instead of asserting.
/// </remarks>
public class PoolDocTests
{
    private static readonly (FightPool Pool, string Heading, string Blurb)[] Bands =
    {
        (FightPool.Opener,
            "Opener",
            "Column 1, and the gentlest of an act's early third. A control group: nothing here can "
            + "hurt you before you have had a turn."),
        (FightPool.Ordinary,
            "Ordinary",
            "The bulk of an act — its early and middle columns, and the band Warrens v2 draws most "
            + "heavily from."),
        (FightPool.Hard,
            "Hard",
            "The late third: the fights a squad arrives at already spent."),
        (FightPool.Elite,
            "Elite",
            "A gilt node's fight. It costs more and the map says so before you take it."),
        (FightPool.Endurance,
            "Endurance",
            "Objective-shaped rather than harder — survive, hold. One per generated act, in the late "
            + "third."),
        (FightPool.Boss,
            "Boss",
            "Terminals."),
    };

    /// <summary>The doc on disk still says what the library says.</summary>
    [Fact]
    public void ThePoolDoc_MatchesTheLibrary()
    {
        var b = new List<string>();
        void W(string line) => b.Add(line);

        Header(W);
        Summary(W);

        foreach (var (pool, heading, blurb) in Bands)
        {
            Band(W, pool, heading, blurb);
        }

        var path = Path.Combine(RepoRoot(), "docs", "WARRENS_V2_POOL.md");
        var built = string.Join("\n", b).TrimEnd() + "\n";

        if (Environment.GetEnvironmentVariable("PLUCK_WRITE_DOCS") == "1")
        {
            File.WriteAllText(path, built);
            return;
        }

        Assert.True(File.Exists(path), "docs/WARRENS_V2_POOL.md is missing. Regenerate with PLUCK_WRITE_DOCS=1.");

        var onDisk = File.ReadAllText(path).Replace("\r\n", "\n").TrimEnd() + "\n";

        Assert.True(
            string.Equals(onDisk, built, StringComparison.Ordinal),
            "docs/WARRENS_V2_POOL.md is stale — a board was added, edited or rebanded. "
            + "Regenerate with PLUCK_WRITE_DOCS=1 dotnet test --filter PoolDoc.");
    }

    private static void Header(Action<string> W)
    {
        W("<!-- GENERATED — every board below is read out of its own .fight file. Regenerate rather");
        W("     than hand-edit: PLUCK_WRITE_DOCS=1 dotnet test tests/Faultline.Web.Tests --filter PoolDoc -->");
        W("");
        W("# Warrens v2 — the board pool");
        W("");
        W("Every board a generated act can field, in the band its own `pool:` mark declares, with the");
        W("grid it is played on and the reason it exists.");
        W("");
        W("**How the generator uses this.** A `Warrens v2` act is 12 columns, 2–4 nodes wide. Column 1");
        W("is the fixed opener; the early third draws **Ordinary and Opener**, the middle **Ordinary**,");
        W("the late third **Hard**; one node carries the gilt reward and draws **Elite**; one late node");
        W("draws **Endurance**; the terminal draws **Boss**. A preset may *weight* toward a territory's");
        W("own subjects — Warrens v2 leans 70% toward `hz-` — but never *scopes* to them, so every");
        W("board here stays drawable (D-271).");
        W("");
        W("**Reading a grid:** `.` open · `#` wall · `O` pit · `^` spikes · `H` high ground ·");
        W("`*` deployment spot · any other letter is an enemy, named under the grid.");
        W("");
    }

    private static void Summary(Action<string> W)
    {
        W("## The pool at a glance");
        W("");
        W("| Band | Boards | What it fills |");
        W("|---|---|---|");

        foreach (var (pool, heading, blurb) in Bands)
        {
            W("| **" + heading + "** | " + InBand(pool).Count + " | " + blurb + " |");
        }

        W("");
        W("**" + FightLibrary.All().Count + " boards in total**, and "
            + FightLibrary.Retired().Count + " retired ones the generator never sees.");
        W("");
    }

    private static void Band(Action<string> W, FightPool pool, string heading, string blurb)
    {
        var boards = InBand(pool);

        W("---");
        W("");
        W("# " + heading + " — " + boards.Count + " board(s)");
        W("");
        W(blurb);
        W("");

        foreach (var fight in boards)
        {
            Board(W, fight);
        }
    }

    private static void Board(Action<string> W, FightDefinition fight)
    {
        W("## " + fight.Name + " · `" + fight.Id + "`");
        W("");

        var facts = new List<string>
        {
            fight.Board.Width + "×" + fight.Board.Height,
            "objective **" + Objective(fight) + "**",
            EnemyLine(fight),
            fight.DeploymentSpots.Count + " deployment spots",
        };

        if (fight.TurnLimit > 0)
        {
            facts.Add("turn limit " + fight.TurnLimit);
        }

        if (fight.Waves.Count > 0)
        {
            facts.Add(fight.Waves.Count + " reinforcement wave(s)");
        }

        W(string.Join(" · ", facts));
        W("");

        if (fight.Description.Length > 0)
        {
            W("> " + fight.Description);
            W("");
        }

        W("```");
        foreach (var row in Grid(fight))
        {
            W(row);
        }

        W("```");
        W("");

        var legend = Legend(fight);
        if (legend.Length > 0)
        {
            W("`" + legend + "`");
            W("");
        }

        foreach (var note in fight.DesignNotes)
        {
            W("- " + note);
        }

        if (fight.DesignNotes.Count > 0)
        {
            W("");
        }
    }

    // ---- helpers -------------------------------------------------------------------------------

    private static IReadOnlyList<FightDefinition> InBand(FightPool pool) =>
        FightLibrary.All()
            .Where(f => f.Pool == pool)
            .OrderBy(f => f.Number)
            .ThenBy(f => f.Id, StringComparer.Ordinal)
            .ToList();

    /// <summary>The grid as the writer draws it — the board's own file, not a second rendering.</summary>
    private static IEnumerable<string> Grid(FightDefinition fight)
    {
        var text = FightWriter.Write(fight).Replace("\r\n", "\n");
        int at = text.IndexOf("board:", StringComparison.Ordinal);

        if (at < 0)
        {
            yield break;
        }

        foreach (var line in text.Substring(at + "board:".Length).Split('\n'))
        {
            var row = line.Trim();
            if (row.Length > 0)
            {
                yield return row;
            }
        }
    }

    /// <summary>Which letter on the grid is which enemy.</summary>
    private static string Legend(FightDefinition fight)
    {
        var text = FightWriter.Write(fight).Replace("\r\n", "\n");
        var spawns = text.Split('\n')
            .Where(l => l.StartsWith("spawn ", StringComparison.Ordinal))
            .Select(l => l.Substring("spawn ".Length).Trim())
            .ToList();

        return string.Join(" · ", spawns);
    }

    private static string EnemyLine(FightDefinition fight)
    {
        var all = fight.Enemies.Select(e => e.Kind)
            .Concat(fight.Waves.SelectMany(w => w.Arrivals.Select(a => a.Kind)))
            .ToList();

        if (all.Count == 0)
        {
            return "no enemies";
        }

        var counted = all
            .GroupBy(k => k)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key.ToString(), StringComparer.Ordinal)
            .Select(g => g.Count() + "× " + Naming.Of(g.Key));

        int hp = all.Where(k => !UnitTemplate.For(k).IsObject).Sum(k => UnitTemplate.For(k).MaxHp);

        return string.Join(", ", counted) + " (" + hp + " HP of fighters)";
    }

    private static string Objective(FightDefinition fight) => fight.Objective.Kind switch
    {
        ObjectiveKind.Survive => "survive",
        ObjectiveKind.Hold => "hold the ground",
        ObjectiveKind.Protect => "protect",
        ObjectiveKind.Destroy => "break it down",
        ObjectiveKind.Reach => "get through",
        ObjectiveKind.Boss => "bring the boss down",
        _ => "kill all",
    };

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
