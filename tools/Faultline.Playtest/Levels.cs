using System.Globalization;
using System.Text;
using Faultline.Core;

namespace Faultline.Playtest;

/// <summary>
/// Plays every campaign fight on its own, by every policy, at full health.
/// </summary>
/// <remarks>
/// <para>
/// The campaign sweep can only measure a level if every level before it was survivable, so one
/// unfinishable board hides the seven behind it. This asks a different question — "what is this
/// board like, faced fresh?" — and can therefore say something about all twelve.
/// </para>
/// <para>
/// It is deliberately not a substitute for the run. A fight met at full health with a full squad is
/// an easier fight than the same board met fourth, and the difference between the two numbers is
/// itself the interesting part.
/// </para>
/// </remarks>
public static class Levels
{
    /// <summary>Plays every campaign fight with every policy and writes the per-level table.</summary>
    /// <param name="seed">Seed every fight is played at.</param>
    /// <param name="outDir">Directory for the report.</param>
    /// <param name="only">
    /// Boards to sweep instead of the campaign spine. Named boards are how a ruling about one
    /// objective gets evidence: <c>as-05-the-door</c> is not on the spine and the campaign sweep can
    /// therefore say nothing at all about it.
    /// </param>
    public static void Report(int seed, string outDir, IReadOnlyList<string>? only = null)
    {
        var policies = Policies.All();
        var fightIds = only is { Count: > 0 }
            ? only.ToList()
            : CampaignLibrary.Faultline.FightIds().ToList();

        Directory.CreateDirectory(outDir);
        Console.WriteLine($"Per-level sweep — {fightIds.Count} campaign fights x {policies.Length} policies, seed {seed}");
        Console.WriteLine();

        var rows = new List<LevelRow>();

        foreach (var id in fightIds)
        {
            foreach (var policy in policies)
            {
                rows.Add(PlayOne(FightLibrary.ById(id), policy, seed));
            }

            var forFight = rows.Where(r => r.FightId == id).ToList();
            Console.WriteLine(
                $"  {id,-24} won {forFight.Count(r => r.Outcome == FightOutcome.Won),2}/{policies.Length}"
                + $"  stalled {forFight.Count(r => r.Stalled),2}"
                + $"  median rounds {Median(forFight.Where(r => r.Outcome == FightOutcome.Won).Select(r => r.Rounds).ToList())}");
        }

        var text = new StringBuilder();
        text.AppendLine("# Per-level sweep — every campaign fight, faced fresh");
        text.AppendLine();
        text.AppendLine("Each board played standalone at full health by every policy, seed " + seed + ".");
        text.AppendLine("This is not how the campaign plays them — a board met fourth is met hurt — but it is");
        text.AppendLine("the only way to say anything about a board sitting behind an unfinishable one.");
        text.AppendLine();
        text.AppendLine("| Fight | Won | Lost | Stalled | Median rounds (won) | Dmg taken (median) | Board share of damage dealt |");
        text.AppendLine("|---|---|---|---|---|---|---|");

        foreach (var id in fightIds)
        {
            var forFight = rows.Where(r => r.FightId == id).ToList();
            var won = forFight.Where(r => r.Outcome == FightOutcome.Won).ToList();

            int boardDealt = forFight.Sum(r => r.BoardDealt);
            int totalDealt = forFight.Sum(r => r.TotalDealt);
            string share = totalDealt == 0
                ? "—"
                : (100.0 * boardDealt / totalDealt).ToString("0", CultureInfo.InvariantCulture) + "%";

            text.AppendLine(
                $"| `{id}` | {won.Count} | {forFight.Count(r => r.Outcome == FightOutcome.Lost)} "
                + $"| {forFight.Count(r => r.Stalled)} | {Median(won.Select(r => r.Rounds).ToList())} "
                + $"| {Median(forFight.Select(r => r.Taken).ToList())} | {share} |");
        }

        text.AppendLine();
        text.AppendLine("## Round-1 high-ground occupancy — the (u) watch flag");
        text.AppendLine();
        text.AppendLine("Player bodies standing on a ledge when round 1 ends, summed and at worst across");
        text.AppendLine("every policy. Absolute counts, not a fraction: MASTER_DESIGN Design Log (u) deleted the");
        text.AppendLine("climb surcharge and flagged \"fights opening as scripted hill races\" as the thing to");
        text.AppendLine("watch. If this rises, the brake is board design, never the surcharge returning.");
        text.AppendLine();
        text.AppendLine("| Fight | Ledge tiles | Perched after round 1 (total) | Worst single policy |");
        text.AppendLine("|---|---|---|---|");

        foreach (var id in fightIds)
        {
            var forFight = rows.Where(r => r.FightId == id).ToList();
            text.AppendLine(
                $"| `{id}` | {forFight.Select(r => r.Ledges).DefaultIfEmpty(0).Max()} "
                + $"| {forFight.Sum(r => r.PerchedAfterRoundOne)} "
                + $"| {forFight.Select(r => r.PerchedAfterRoundOne).DefaultIfEmpty(0).Max()} |");
        }

        text.AppendLine();
        text.AppendLine("## Which policies clear which board");
        text.AppendLine();
        text.Append("| Fight |");
        foreach (var p in policies)
        {
            text.Append(" `").Append(p.Name).Append("` |");
        }

        text.AppendLine();
        text.Append("|---|");
        foreach (var _ in policies)
        {
            text.Append("---|");
        }

        text.AppendLine();

        foreach (var id in fightIds)
        {
            text.Append("| `").Append(id).Append("` |");
            foreach (var p in policies)
            {
                var row = rows.First(r => r.FightId == id && r.Policy == p.Name);
                text.Append(' ').Append(row.Stalled
                    ? "stall"
                    : row.Outcome == FightOutcome.Won ? "won " + row.Rounds : "LOST").Append(" |");
            }

            text.AppendLine();
        }

        var path = Path.Combine(outDir, "levels.md");
        File.WriteAllText(path, text.ToString());
        Console.WriteLine();
        Console.WriteLine("wrote " + path);
    }

    private static LevelRow PlayOne(FightDefinition fight, Policy policy, int seed)
    {
        var rng = new DeterministicRng(seed ^ policy.Name.GetHashCode());
        var state = Game.Start(fight, seed).NewState;

        int taken = 0;
        int boardDealt = 0;
        int totalDealt = 0;

        // The (u) watch flag, counted as an absolute: player bodies standing on a ledge at the moment
        // round 1 ends. The climb surcharge is deleted, and the thing to watch for is fights opening
        // as scripted hill races — a ratio would hide it, because a board with two ledges and a board
        // with six would report the same fraction.
        int perchedAtRoundOneEnd = -1;
        int ledges = 0;
        foreach (var tile in state.Board.AllCoords())
        {
            if (state.Board.At(tile) == TileType.HighGround)
            {
                ledges++;
            }
        }

        for (int i = 0; i < 20000 && state.Outcome == FightOutcome.InProgress; i++)
        {
            if (state.Round > RunHarness.StallRound)
            {
                return new LevelRow(
                    fight.Id, policy.Name, FightOutcome.InProgress, state.Round, taken, boardDealt,
                    totalDealt, true, perchedAtRoundOneEnd < 0 ? 0 : perchedAtRoundOneEnd, ledges);
            }

            var enemy = Game.NextEnemyCommand(state);
            var legal = enemy is null ? Game.LegalCommands(state) : Array.Empty<Command>();

            if (enemy is null && legal.Count == 0)
            {
                break;
            }

            var command = enemy ?? policy.Choose(state, legal, rng);
            var step = Game.Apply(state, command);

            foreach (var e in step.Events)
            {
                if (e is not UnitDamaged damaged)
                {
                    continue;
                }

                var unit = state.FindUnit(damaged.UnitId);
                if (unit is null)
                {
                    continue;
                }

                if (unit.Team.IsPlayer())
                {
                    taken += damaged.Amount;
                }
                else
                {
                    totalDealt += damaged.Amount;
                    if (damaged.Source != DamageSource.Attack)
                    {
                        boardDealt += damaged.Amount;
                    }
                }
            }

            if (perchedAtRoundOneEnd < 0 && step.NewState.Round > 1)
            {
                perchedAtRoundOneEnd = Perched(state);
            }

            state = step.NewState;
        }

        return new LevelRow(
            fight.Id, policy.Name, state.Outcome, state.Round, taken, boardDealt, totalDealt, false,
            perchedAtRoundOneEnd < 0 ? Perched(state) : perchedAtRoundOneEnd,
            ledges);
    }

    // Player bodies standing on a ledge, right now.
    private static int Perched(GameState state)
    {
        int count = 0;
        foreach (var unit in state.Units)
        {
            if (unit.IsOnBoard && unit.Team.IsPlayer() && state.Board.At(unit.Position) == TileType.HighGround)
            {
                count++;
            }
        }

        return count;
    }

    private static string Median(IReadOnlyList<int> values)
    {
        if (values.Count == 0)
        {
            return "—";
        }

        var sorted = values.OrderBy(v => v).ToList();
        return sorted[sorted.Count / 2].ToString(CultureInfo.InvariantCulture);
    }

    private sealed record LevelRow(
        string FightId,
        string Policy,
        FightOutcome Outcome,
        int Rounds,
        int Taken,
        int BoardDealt,
        int TotalDealt,
        bool Stalled,
        int PerchedAfterRoundOne = 0,
        int Ledges = 0);
}
