using System.Globalization;
using System.Text;
using Faultline.Core;

namespace Faultline.Playtest;

/// <summary>
/// The Warrens edition-A certification gate: MASTER_DESIGN §8.8's five criteria, measured by playing
/// each board with each of the four evaluator policies §8.8 names.
/// </summary>
/// <remarks>
/// <para>
/// <b>Certification is a claim about what a player can reach, so it is reached by playing.</b>
/// <see cref="WarrensEditionATests"/> asserts the shapes §8.8 fixes — 7×7, safe deployment, the
/// bramble opener. This asks the other half: started, fought to its end four different ways, does the
/// board hold. A board that parses is not a board that plays.
/// </para>
/// <para>
/// <b>What it cannot measure.</b> <c>--seed</c> is inert: nothing in <c>Faultline.Core</c> constructs
/// or consumes an <see cref="DeterministicRng"/> inside a fight, so the three deterministic policies
/// are byte-identical at every seed and only <c>random-legal</c> varies at all — and it varies per
/// process, not per seed. Every row here is therefore <b>one sample</b>, and the "won 3/4" columns
/// count policies, never trials. Rounds, hit points and drains are absolutes; there is no distribution
/// behind them to take a median of.
/// </para>
/// </remarks>
public static class Certification
{
    /// <summary>The eight combat nodes of act 1, in the order §8 draws them.</summary>
    public static readonly string[] Boards =
    {
        "first-contact", "cb-06-bait-and-break", "the-teeth", "the-shrine",
        "broken-bridge", "high-road", "break-the-gate", "hz-09-the-trench",
    };

    /// <summary>The hungry lane's boards — §8.8 requires a base-kit policy win on each.</summary>
    public static readonly string[] HungryLane =
    {
        "the-teeth", "broken-bridge", "high-road", "hz-09-the-trench",
    };

    /// <summary>§8.8's four evaluator policies, in the order it names them.</summary>
    /// <returns>Baseline, collision-seeking, objective-first, random-legal.</returns>
    public static Policy[] Four() => new Policy[]
    {
        new BoardFirstPolicy(),
        new ShoverPolicy(),
        new ObjectiveFirstPolicy(),
        new RandomPolicy("a"),
    };

    /// <summary>Plays every board with every policy and writes the certification report.</summary>
    /// <param name="seed">Seed the fights start at. Inert; recorded for the log's own sake.</param>
    /// <param name="outDir">Directory for the report.</param>
    public static void Report(int seed, string outDir)
    {
        var policies = Four();
        var rows = new List<BoardRun>();

        Directory.CreateDirectory(outDir);
        Console.WriteLine("Warrens edition-A certification — " + Boards.Length + " boards x "
            + policies.Length + " policies, seed " + seed);
        Console.WriteLine();

        foreach (string id in Boards)
        {
            foreach (var policy in policies)
            {
                var row = Play(FightLibrary.ById(id)!, policy, seed);
                rows.Add(row);
                Console.WriteLine(
                    "  " + id.PadRight(22) + policy.Name.PadRight(16)
                    + row.Verdict.PadRight(10)
                    + " r" + row.Rounds
                    + "  hp " + row.HpOut + "/" + row.HpIn
                    + "  downs " + row.Downs
                    + "  drained " + row.PlayerDrains + "/" + row.EnemyDrains
                    + (row.PreviewLies.Count > 0 ? "  LIES " + row.PreviewLies.Count : string.Empty)
                    + (row.ClingingAtEnd > 0 ? "  CLINGING " + row.ClingingAtEnd : string.Empty));
            }
        }

        var text = new StringBuilder();
        Write(text, rows, policies, seed);

        string path = Path.Combine(outDir, "warrens-certification.md");
        File.WriteAllText(path, text.ToString());
        Console.WriteLine();
        Console.WriteLine("wrote " + path);
    }

    // ---- one board, one policy -------------------------------------------------------------------

    private static BoardRun Play(FightDefinition fight, Policy policy, int seed)
    {
        var rng = new DeterministicRng(seed ^ policy.Name.GetHashCode());
        var state = Game.Start(fight, seed).NewState;

        var run = new BoardRun(fight.Id, policy.Name)
        {
            Reach = Reachability(state),
        };

        var hpIn = new Dictionary<UnitId, int>();
        foreach (var unit in state.Units)
        {
            if (unit.Team.IsPlayer())
            {
                hpIn[unit.Id] = unit.MaxHp;
                run.HpIn += unit.MaxHp;
            }
        }

        int lastRoundSampled = 0;
        bool stalled = false;

        for (int i = 0; i < 40000 && state.Outcome == FightOutcome.InProgress; i++)
        {
            if (state.Round > RunHarness.StallRound)
            {
                stalled = true;
                break;
            }

            var enemy = Game.NextEnemyCommand(state);
            var legal = enemy is null ? Game.LegalCommands(state) : Array.Empty<Command>();

            if (enemy is null && legal.Count == 0)
            {
                run.NoLegalCommand = true;
                break;
            }

            var command = enemy ?? policy.Choose(state, legal, rng);

            // "No false preview", the Stage A shape, applied to the boards rather than to fixtures:
            // the projection the shell would draw for THIS command on THIS board, checked against the
            // board the same command produces. Only the players' commands — an enemy plan is a
            // telegraph, and the telegraph is a different claim with a different test.
            var outlook = enemy is null ? Abilities.Outlook(state, command) : null;

            var step = Game.Apply(state, command);

            if (outlook is not null)
            {
                Audit(run, state, step.NewState, command, outlook, step.Events);
            }

            foreach (var e in step.Events)
            {
                Absorb(run, state, e);
            }

            state = step.NewState;

            // The objective's hit points, sampled once a round: the clock a Protect board is played
            // against, and the progress bar a Destroy board is played against.
            if (state.Round != lastRoundSampled)
            {
                lastRoundSampled = state.Round;
                run.ObjectiveByRound.Add((state.Round, ObjectiveHp(state)));
            }
        }

        run.Rounds = state.Round;
        run.Outcome = state.Outcome;
        run.Stalled = stalled;
        run.ObjectiveHpOut = ObjectiveHp(state);
        run.ObjectiveHpIn = fight.Objective.Hp;
        run.ObjectiveRole = fight.Objective.Kind;

        foreach (var unit in state.Units)
        {
            if (!unit.Team.IsPlayer())
            {
                if (unit.Clinging)
                {
                    run.ClingingAtEnd++;
                }

                continue;
            }

            run.HpOut += Math.Max(0, unit.Hp);
            run.Ducks.Add((unit.Kind, hpIn.TryGetValue(unit.Id, out int had) ? had : unit.MaxHp,
                Math.Max(0, unit.Hp), !unit.IsAlive || unit.Voided));

            if (unit.Clinging)
            {
                run.ClingingAtEnd++;
            }
        }

        return run;
    }

    /// <summary>Checks one projection against the board the same command produced.</summary>
    /// <remarks>
    /// <para>
    /// Three things the audit deliberately does <b>not</b> call a lie, because none of them is one:
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>Overkill.</b> A promised 4 into a body holding 2 removes 2. The preview
    /// said what the blow is worth; the comparison is against the hit points there were to lose.</description></item>
    /// <item><description><b>A round boundary inside the step.</b> Ending an activation can end the
    /// round, and a bramble tick, a drain strip or the doomed-cling sweep then changes hit points the
    /// action never claimed. Those steps are skipped and counted, not scored.</description></item>
    /// <item><description><b>A kill the displacement did not claim but the action delivered.</b>
    /// <c>WouldDown</c> is the displacement's own claim; when the action's direct damage is what tips
    /// it over, that is reported separately as an under-claimed kill rather than folded into the
    /// destination lies — the two are different defects with different fixes.</description></item>
    /// </list>
    /// </remarks>
    private static void Audit(
        BoardRun run, GameState before, GameState after, Command command, ActionOutlook outlook,
        IReadOnlyList<GameEvent> events)
    {
        var displacement = outlook.Displacement ?? outlook.Charge?.Contact;
        if (displacement is null)
        {
            return;
        }

        foreach (var e in events)
        {
            if (e is RoundEnded)
            {
                run.PreviewsSkippedAtRoundEnd++;
                return;
            }
        }

        run.PreviewsChecked++;

        var moved = before.FindUnit(displacement.UnitId);
        var settled = after.FindUnit(displacement.UnitId);
        if (moved is null || settled is null)
        {
            return;
        }

        // Two named rules can make the board differ from a truthful projection, and neither is the
        // projection being wrong. A Footing refusal is the OTHER SIDE's decision, taken after the
        // preview was read (§3: "the destination is an intent, not a promise"); the doomed-cling
        // sweep (D-081) is a separate rule that fires once the shove has already resolved.
        bool refused = false;
        bool swept = false;
        foreach (var e in events)
        {
            if (e is DisplacementRefused)
            {
                refused = true;
            }

            if (e is Voided v && v.Reason == Pits.SweptReason)
            {
                swept = true;
            }
        }

        var sink = refused || swept ? run.PreviewCaveats : run.PreviewLies;
        string tag = refused ? " [Footing refusal]" : swept ? " [doomed-cling sweep]" : string.Empty;

        // The one case where the projection is wrong on its own account: damage to a body that is
        // already clinging voids it where it hangs, so the shove the preview drew never happens.
        if (moved.Clinging)
        {
            sink = run.PreviewLies;
            tag = " [target was already clinging]";
        }

        string what = command.GetType().Name + " on " + moved.Kind + " at " + moved.Position + tag;

        if (settled.Position != displacement.Destination)
        {
            sink.Add(what + ": said it would land on " + displacement.Destination
                + ", landed on " + settled.Position);
        }

        int lost = moved.Hp - settled.Hp;
        int promised = outlook.Damage + displacement.DamageToUnit;
        int deliverable = Math.Min(promised, moved.Hp);
        if (lost != deliverable)
        {
            sink.Add(what + ": promised " + promised + " damage into " + moved.Hp
                + " hit points, took " + lost);
        }

        if (settled.Clinging != displacement.WouldCling)
        {
            sink.Add(what + ": cling promised " + displacement.WouldCling
                + ", was " + settled.Clinging);
        }

        if (settled.IsAlive == displacement.WouldDown)
        {
            // The action killed it and the displacement alone would not have: the projection is
            // silent about a kill its own direct damage delivers.
            if (!settled.IsAlive && promised >= moved.Hp && !refused && !swept && !moved.Clinging)
            {
                run.UnclaimedKills.Add(what + ": " + promised + " into " + moved.Hp
                    + " hit points, and WouldDown is false");
            }
            else
            {
                sink.Add(what + ": down promised " + displacement.WouldDown
                    + ", alive " + settled.IsAlive);
            }
        }
    }

    private static void Absorb(BoardRun run, GameState before, GameEvent e)
    {
        switch (e)
        {
            case UnitDowned down when down.Team.IsPlayer():
                run.Downs++;
                break;

            case Voided v:
                if (v.Team.IsPlayer())
                {
                    run.PlayerDrains++;
                }
                else
                {
                    run.EnemyDrains++;
                }

                break;

            case Clinging cling:
                run.ClingEvents++;
                break;

            case ReinforcementScheduled:
                run.WavesScheduled++;
                break;

            case ReinforcementArrived:
                run.WavesArrived++;
                break;

            case ReinforcementDelayed:
                run.WavesDelayed++;
                break;

            case StructureAttacked attacked:
            {
                var actor = before.FindUnit(attacked.AttackerId);
                if (actor is not null && actor.Team.IsPlayer())
                {
                    Bump(run.StructureChipsBy, actor.Kind);
                }

                break;
            }

            case StructureDamaged damaged when damaged.Source == DamageSource.Collision:
                run.StructureCollisions++;
                break;

            case StructureDestroyed:
                run.StructuresDestroyed++;
                break;
        }
    }

    private static int ObjectiveHp(GameState state)
    {
        int hp = 0;
        foreach (var structure in state.Structures)
        {
            if (!structure.IsBlocker)
            {
                hp += structure.Hp;
            }
        }

        return hp;
    }

    // ---- reachability ----------------------------------------------------------------------------

    /// <summary>
    /// Whether every enemy and every objective structure can be got at, walking from the player
    /// deployment zones over walkable ground with the bodies taken off the board.
    /// </summary>
    /// <remarks>
    /// Bodies are ignored on purpose: a unit standing in a corridor is a tactical problem, and a wall
    /// across it is a board defect. What this asks is the second question.
    /// </remarks>
    private static ReachReport Reachability(GameState state)
    {
        var start = new List<Coord>();
        foreach (var unit in state.Units)
        {
            if (unit.Team.IsPlayer() && unit.IsOnBoard)
            {
                start.Add(unit.Position);
            }
        }

        if (start.Count == 0)
        {
            foreach (var zone in new[] { state.Fight.DeploymentZoneA, state.Fight.DeploymentZoneB })
            {
                foreach (var tile in zone)
                {
                    start.Add(tile);
                }
            }
        }

        var seen = new HashSet<Coord>();
        var queue = new Queue<Coord>();
        foreach (var tile in start)
        {
            if (seen.Add(tile))
            {
                queue.Enqueue(tile);
            }
        }

        while (queue.Count > 0)
        {
            var at = queue.Dequeue();
            foreach (var direction in Directions.All)
            {
                var next = at.Step(direction);
                if (!state.Board.InBounds(next) || seen.Contains(next))
                {
                    continue;
                }

                // A standing structure blocks its own tile, and a drain is not a route — but both are
                // things you can stand NEXT to, which is all reaching them requires.
                if (!Movement.IsWalkable(state.Board.At(next)) || state.StructureAt(next) is not null)
                {
                    continue;
                }

                seen.Add(next);
                queue.Enqueue(next);
            }
        }

        var unreachableEnemies = new List<string>();
        foreach (var unit in state.Units)
        {
            if (unit.Team.IsPlayer() || !unit.IsOnBoard)
            {
                continue;
            }

            if (!Adjacent(seen, unit.Position, state))
            {
                unreachableEnemies.Add(unit.Kind + " at " + unit.Position);
            }
        }

        var unreachableStructures = new List<string>();
        foreach (var structure in state.Structures)
        {
            if (!structure.IsStanding)
            {
                continue;
            }

            if (!Adjacent(seen, structure.At, state))
            {
                unreachableStructures.Add(structure.Role + " at " + structure.At);
            }
        }

        return new ReachReport(unreachableEnemies, unreachableStructures);
    }

    private static bool Adjacent(HashSet<Coord> reached, Coord at, GameState state)
    {
        if (reached.Contains(at))
        {
            return true;
        }

        foreach (var direction in Directions.All)
        {
            if (reached.Contains(at.Step(direction)))
            {
                return true;
            }
        }

        return false;
    }

    // ---- the report ------------------------------------------------------------------------------

    private static void Write(StringBuilder text, List<BoardRun> rows, Policy[] policies, int seed)
    {
        text.AppendLine("# Warrens edition A — certification (MASTER_DESIGN §8.8)");
        text.AppendLine();
        text.AppendLine("Generated by `tools/Faultline.Playtest -- --certify`. Every board played");
        text.AppendLine("standalone at full health, base kit, no camp cards, by each of §8.8's four");
        text.AppendLine("evaluator policies: `board-first` (baseline), `shover` (collision-seeking),");
        text.AppendLine("`objective-first`, `random-a` (random-legal).");
        text.AppendLine();
        text.AppendLine("**One sample per cell, not three.** `--seed` is inert — nothing in");
        text.AppendLine("`Faultline.Core` consumes an RNG inside a fight — so the three deterministic");
        text.AppendLine("policies are byte-identical at every seed, and `random-a` reseeds per process");
        text.AppendLine("rather than per seed. Seed " + seed + " is a filename, not a trial.");
        text.AppendLine();

        text.AppendLine("## Per-node attrition");
        text.AppendLine();
        text.AppendLine("HP in/out is the whole squad's absolute hit points, not a fraction. `drains`");
        text.AppendLine("is players/enemies taken by a Drain.");
        text.AppendLine();
        text.AppendLine("| Board | Policy | Result | Rounds | Squad HP in | out | lost | Downs | Drains | Objective HP in→out |");
        text.AppendLine("|---|---|---|---|---|---|---|---|---|---|");

        foreach (var row in rows)
        {
            text.AppendLine("| `" + row.FightId + "` | `" + row.Policy + "` | " + row.Verdict
                + " | " + row.Rounds
                + " | " + row.HpIn + " | " + row.HpOut + " | " + (row.HpIn - row.HpOut)
                + " | " + row.Downs
                + " | " + row.PlayerDrains + "/" + row.EnemyDrains
                + " | " + (row.ObjectiveHpIn == 0
                    ? "—"
                    : row.ObjectiveHpIn + "→" + row.ObjectiveHpOut + " (" + row.ObjectiveRole + ")")
                + " |");
        }

        text.AppendLine();
        text.AppendLine("## Per-duck hit points, in and out");
        text.AppendLine();
        text.AppendLine("| Board | Policy | " + string.Join(" | ", DuckOrder.Select(k => Naming.Of(k))) + " |");
        text.AppendLine("|---|---|" + string.Concat(DuckOrder.Select(_ => "---|")));

        foreach (var row in rows)
        {
            text.Append("| `" + row.FightId + "` | `" + row.Policy + "` |");
            foreach (var kind in DuckOrder)
            {
                var duck = row.Ducks.FirstOrDefault(d => d.Kind == kind);
                text.Append(duck.Kind == kind
                    ? " " + duck.HpIn + "→" + duck.HpOut + (duck.Down ? " DOWN" : string.Empty) + " |"
                    : " — |");
            }

            text.AppendLine();
        }

        text.AppendLine();
        text.AppendLine("## Objective hit points, by round");
        text.AppendLine();

        foreach (var row in rows.Where(r => r.ObjectiveHpIn > 0))
        {
            text.AppendLine("- `" + row.FightId + "` / `" + row.Policy + "` ("
                + row.ObjectiveRole + " " + row.ObjectiveHpIn + "): "
                + (row.ObjectiveByRound.Count == 0
                    ? "no round completed"
                    : string.Join(", ", row.ObjectiveByRound.Select(p => "r" + p.Round + "=" + p.Hp))));
        }

        text.AppendLine();
        text.AppendLine("## Certification, criterion by criterion");
        text.AppendLine();
        text.AppendLine("| Board | Winnable deployment | Enemies & structures reachable | No reinforcement deadlock | No false preview | Objective & Clinging resolve | Base-kit win |");
        text.AppendLine("|---|---|---|---|---|---|---|");

        foreach (string id in Boards)
        {
            var forBoard = rows.Where(r => r.FightId == id).ToList();
            var reach = forBoard[0].Reach;

            bool anyWin = forBoard.Any(r => r.Outcome == FightOutcome.Won);
            bool lies = forBoard.Any(r => r.PreviewLies.Count > 0);
            bool clinging = forBoard.Any(r => r.ClingingAtEnd > 0);
            bool stalls = forBoard.Any(r => r.Stalled || r.NoLegalCommand);
            bool wavesLanded = forBoard.All(r => r.WavesScheduled == 0 || r.WavesArrived > 0);

            text.AppendLine("| `" + id + "` "
                + "| " + (anyWin ? "pass" : "**FAIL**")
                + " | " + (reach.Enemies.Count == 0 && reach.Structures.Count == 0
                    ? "pass"
                    : "**FAIL** — " + string.Join("; ", reach.Enemies.Concat(reach.Structures)))
                + " | " + (!stalls && wavesLanded ? "pass" : "**FAIL**")
                + " | " + (lies ? "**FAIL**" : "pass") + " (" + forBoard.Sum(r => r.PreviewsChecked)
                    + " checked, " + forBoard.Sum(r => r.PreviewsSkippedAtRoundEnd) + " skipped at a round end)"
                + " | " + (clinging ? "**FAIL**" : "pass")
                + " | " + (anyWin
                    ? forBoard.Count(r => r.Outcome == FightOutcome.Won) + "/" + policies.Length
                    : "**FAIL** 0/" + policies.Length)
                + " |");
        }

        text.AppendLine();
        text.AppendLine("## Waves, structure chips and collisions");
        text.AppendLine();
        text.AppendLine("`chips` counts player attacks that took hit points off a structure, by class —");
        text.AppendLine("the D-060 \"any attack chips masonry\" baseline, counted rather than assumed.");
        text.AppendLine();
        text.AppendLine("| Board | Policy | Waves sched/arrived/delayed | Structure collisions | Player chips (by class) | Structures destroyed |");
        text.AppendLine("|---|---|---|---|---|---|");

        foreach (var row in rows)
        {
            text.AppendLine("| `" + row.FightId + "` | `" + row.Policy + "` | "
                + row.WavesScheduled + "/" + row.WavesArrived + "/" + row.WavesDelayed
                + " | " + row.StructureCollisions
                + " | " + (row.StructureChipsBy.Count == 0
                    ? "none"
                    : string.Join(", ", row.StructureChipsBy.Select(p => Naming.Of(p.Key) + " " + p.Value)))
                + " | " + row.StructuresDestroyed + " |");
        }

        var caveats = rows
            .SelectMany(r => r.PreviewCaveats.Select(l => r.FightId + " / " + r.Policy + ": " + l))
            .ToList();
        text.AppendLine();
        text.AppendLine("## Where the board differed from a truthful projection");
        text.AppendLine();
        text.AppendLine("Neither of these is the preview being wrong. A **Footing refusal** is the other");
        text.AppendLine("side spending Footing to refuse the whole displacement AFTER the preview was read");
        text.AppendLine("— §3's \"the destination is an intent, not a promise\". The **doomed-cling sweep**");
        text.AppendLine("(D-081) is a separate rule firing once the shove has already resolved.");
        text.AppendLine();
        text.AppendLine(caveats.Count == 0
            ? "None."
            : string.Join(Environment.NewLine, caveats.Select(l => "- " + l)));

        var unclaimed = rows
            .SelectMany(r => r.UnclaimedKills.Select(l => r.FightId + " / " + r.Policy + ": " + l))
            .ToList();
        text.AppendLine();
        text.AppendLine("## Kills the projection does not claim");
        text.AppendLine();
        text.AppendLine("Not a contradiction of the destination or the damage — the body died and the");
        text.AppendLine("displacement's own `WouldDown` was false because the action's DIRECT damage is");
        text.AppendLine("what tipped it. §3 wants the outcome out loud, and `ActionOutlook` carries no");
        text.AppendLine("composite lethality flag for a renderer to read.");
        text.AppendLine();
        text.AppendLine(unclaimed.Count == 0
            ? "None."
            : string.Join(Environment.NewLine, unclaimed.Select(l => "- " + l)));

        var allLies = rows.SelectMany(r => r.PreviewLies.Select(l => r.FightId + " / " + r.Policy + ": " + l)).ToList();
        text.AppendLine();
        text.AppendLine("## Preview contradictions");
        text.AppendLine();
        text.AppendLine(allLies.Count == 0
            ? "None, across " + rows.Sum(r => r.PreviewsChecked)
                + " projections checked against the board the same command produced."
            : string.Join("\n", allLies.Select(l => "- " + l)));
    }

    private static readonly UnitKind[] DuckOrder =
    {
        UnitKind.Vanguard, UnitKind.Archer, UnitKind.Threadcaster, UnitKind.Wardbearer,
    };

    private static void Bump(Dictionary<UnitKind, int> counts, UnitKind key)
    {
        counts.TryGetValue(key, out int had);
        counts[key] = had + 1;
    }

    private sealed record ReachReport(IReadOnlyList<string> Enemies, IReadOnlyList<string> Structures);

    private sealed class BoardRun
    {
        public BoardRun(string fightId, string policy)
        {
            FightId = fightId;
            Policy = policy;
        }

        public string FightId { get; }

        public string Policy { get; }

        public FightOutcome Outcome { get; set; }

        public bool Stalled { get; set; }

        public bool NoLegalCommand { get; set; }

        public int Rounds { get; set; }

        public int HpIn { get; set; }

        public int HpOut { get; set; }

        public int Downs { get; set; }

        public int PlayerDrains { get; set; }

        public int EnemyDrains { get; set; }

        public int ClingEvents { get; set; }

        public int ClingingAtEnd { get; set; }

        public int WavesScheduled { get; set; }

        public int WavesArrived { get; set; }

        public int WavesDelayed { get; set; }

        public int StructureCollisions { get; set; }

        public int StructuresDestroyed { get; set; }

        public int ObjectiveHpIn { get; set; }

        public int ObjectiveHpOut { get; set; }

        public ObjectiveKind ObjectiveRole { get; set; }

        public int PreviewsChecked { get; set; }

        public int PreviewsSkippedAtRoundEnd { get; set; }

        public ReachReport Reach { get; set; } = new(Array.Empty<string>(), Array.Empty<string>());

        public List<string> PreviewLies { get; } = new();

        public List<string> UnclaimedKills { get; } = new();

        public List<string> PreviewCaveats { get; } = new();

        public Dictionary<UnitKind, int> StructureChipsBy { get; } = new();

        public List<(int Round, int Hp)> ObjectiveByRound { get; } = new();

        public List<(UnitKind Kind, int HpIn, int HpOut, bool Down)> Ducks { get; } = new();

        public string Verdict => Stalled ? "STALL"
            : NoLegalCommand ? "NOLEGAL"
            : Outcome == FightOutcome.Won ? "won"
            : Outcome == FightOutcome.Lost ? "LOST"
            : "unfinished";
    }
}
