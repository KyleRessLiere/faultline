using Faultline.Core;

namespace Faultline.Playtest;

/// <summary>What one fight inside a run cost.</summary>
/// <param name="NodeIndex">Position in the campaign.</param>
/// <param name="FightId">Which board.</param>
/// <param name="Outcome">How it ended.</param>
/// <param name="Rounds">Round it ended on.</param>
/// <param name="PlayerDamage">Damage the squad took, by source.</param>
/// <param name="EnemyDamage">Damage the squad dealt, by source.</param>
/// <param name="Downed">Squad members that hit zero.</param>
/// <param name="Voided">Squad members lost down a pit.</param>
/// <param name="EnemiesKilled">Enemies put down.</param>
/// <param name="Collisions">Collision events, whoever they hit.</param>
/// <param name="Pushes">Displacements the players caused.</param>
/// <param name="HpOut">Squad HP and Verve carried out, as "Kind=hp/max vN" entries.</param>
/// <param name="VerveEarned">Verve banked, by class. The thesis-compliance metric.</param>
/// <param name="VerveWasted">Charges that arrived at a full meter and were discarded, by class.</param>
/// <param name="VerveSpent">Verve spent, by class.</param>
/// <param name="Spends">How many times each spender was used.</param>
/// <param name="Healed">Hit points Preen put back.</param>
/// <param name="Absorbed">Hit points Guard Stance redirected onto a guard.</param>
/// <param name="CastLandings">Where cast units came down, by tile type.</param>
/// <param name="HazardDistanceAtCast">
/// The Fisher's step distance to the nearest hazard each time she cast, one entry per cast. She can
/// only post somebody into a drain she is standing beside, so this is what bounds the whole ability.
/// </param>
/// <param name="AttackDamageBy">Attack damage the squad dealt, by the class that dealt it.</param>
/// <param name="AttackDistance">Step distance of every player attack, by class.</param>
/// <param name="AbilityUses">Abilities the squad used, by ability.</param>
/// <param name="RescueAttempts">Rescue commands issued.</param>
/// <param name="RescueSuccesses">Rescues that got somebody back on the board.</param>
/// <param name="AttackKills">Enemies whose killing damage was a weapon.</param>
/// <param name="BoardKills">Enemies the board killed, drains included.</param>
public sealed record FightReport(
    int NodeIndex,
    string FightId,
    FightOutcome Outcome,
    int Rounds,
    Dictionary<DamageSource, int> PlayerDamage,
    Dictionary<DamageSource, int> EnemyDamage,
    int Downed,
    int Voided,
    int EnemiesKilled,
    int Collisions,
    int Pushes,
    List<string> HpOut,
    Dictionary<UnitKind, int> VerveEarned,
    Dictionary<UnitKind, int> VerveWasted,
    Dictionary<UnitKind, int> VerveSpent,
    Dictionary<VerveSpend, int> Spends,
    int Healed,
    int Absorbed,
    Dictionary<TileType, int> CastLandings,
    List<int> HazardDistanceAtCast,
    Dictionary<UnitKind, int> AttackDamageBy,
    Dictionary<UnitKind, List<int>> AttackDistance,
    Dictionary<Ability, int> AbilityUses,
    int RescueAttempts,
    int RescueSuccesses,
    int AttackKills,
    int BoardKills);

/// <summary>
/// Running totals for the fight in progress.
/// </summary>
/// <remarks>
/// One object rather than nine <c>ref</c> parameters. The list was already at the edge of readable
/// before Verve wanted four more on it, and a tally that is awkward to extend is a tally nobody
/// extends.
/// </remarks>
internal sealed class FightTally
{
    internal Dictionary<DamageSource, int> PlayerDamage { get; } = new();

    internal Dictionary<DamageSource, int> EnemyDamage { get; } = new();

    internal Dictionary<UnitKind, int> VerveEarned { get; } = new();

    internal Dictionary<UnitKind, int> VerveWasted { get; } = new();

    internal Dictionary<UnitKind, int> VerveSpent { get; } = new();

    internal Dictionary<VerveSpend, int> Spends { get; } = new();

    /// <summary>Hit points Preen put back this fight.</summary>
    internal int Healed { get; set; }

    /// <summary>Hit points redirected onto a guard by Guard Stance this fight.</summary>
    internal int Absorbed { get; set; }

    internal Dictionary<TileType, int> CastLandings { get; } = new();

    internal List<int> HazardDistanceAtCast { get; } = new();

    /// <summary>Attack damage the squad dealt, by the class that dealt it.</summary>
    internal Dictionary<UnitKind, int> AttackDamageBy { get; } = new();

    /// <summary>
    /// Step distance of every attack a player made, one entry each. The kiting metric: an Archer
    /// that can no longer walk and shoot in the same activation should be shooting from closer.
    /// </summary>
    internal Dictionary<UnitKind, List<int>> AttackDistance { get; } = new();

    /// <summary>Abilities the squad used, by ability. Reel at two points lives or dies here.</summary>
    internal Dictionary<Ability, int> AbilityUses { get; } = new();

    /// <summary>Rescue commands issued, whether or not the haul landed.</summary>
    internal int RescueAttempts { get; set; }

    /// <summary>Rescues that actually got somebody back on the board.</summary>
    internal int RescueSuccesses { get; set; }

    /// <summary>Enemies whose last damage was a weapon.</summary>
    internal int AttackKills { get; set; }

    /// <summary>Enemies whose last damage was the board, plus everything that went down a drain.</summary>
    internal int BoardKills { get; set; }

    /// <summary>What last hurt each unit, so a down can be attributed to a source it does not carry.</summary>
    internal Dictionary<UnitId, DamageSource> LastHurtBy { get; } = new();



    internal int Downed { get; set; }

    internal int Voided { get; set; }

    internal int Killed { get; set; }

    internal int Collisions { get; set; }

    internal int Pushes { get; set; }

    internal string FightId { get; set; } = string.Empty;

    internal int NodeIndex { get; set; }

    internal static void Bump<T>(Dictionary<T, int> counts, T key, int by)
        where T : notnull
    {
        counts.TryGetValue(key, out int had);
        counts[key] = had + by;
    }
}

/// <summary>What one whole campaign run did.</summary>
/// <param name="Policy">Who played it.</param>
/// <param name="Seed">Run seed.</param>
/// <param name="Outcome">Won, lost, or still going when the command budget ran out.</param>
/// <param name="FightsWon">How many nodes were cleared.</param>
/// <param name="EndedAtNode">Where it stopped.</param>
/// <param name="Reason">Why it stopped.</param>
/// <param name="Commands">How many commands it took.</param>
/// <param name="Fights">Per-fight detail.</param>
/// <param name="FinalSquad">The squad at the end.</param>
public sealed record RunReport(
    string Policy,
    int Seed,
    RunOutcome Outcome,
    int FightsWon,
    int EndedAtNode,
    string Reason,
    int Commands,
    List<FightReport> Fights,
    List<string> FinalSquad);

/// <summary>
/// Plays whole campaigns headlessly, straight through Core.
/// </summary>
/// <remarks>
/// No browser, no dev server, no port. That is deliberate — a playtest harness that needed the app
/// running could not be used while someone was playing the app, and the whole point is to gather
/// evidence without interrupting anything.
/// </remarks>
public static class RunHarness
{
    /// <summary>
    /// Rounds a single fight may run before it is called stalled.
    /// </summary>
    /// <remarks>
    /// A KillAll fight with no turn limit on a board whose halves cannot reach each other never ends.
    /// Left to the command budget it costs 200,000 commands and reports "budget exhausted", which
    /// reads like a harness limit rather than what it is — a fight nobody can finish. Every campaign
    /// board is decided inside 20 rounds when it is decidable at all.
    /// </remarks>
    public const int StallRound = 60;

    /// <summary>Plays one campaign to its end.</summary>
    /// <param name="policy">How the players decide.</param>
    /// <param name="seed">Run seed.</param>
    /// <param name="log">
    /// Filled with every player decision, when supplied. The seed plus this log replays the run, so
    /// a policy's run can be watched back exactly like a hand-played one.
    /// </param>
    /// <param name="maxCommands">Safety stop.</param>
    /// <returns>The report.</returns>
    public static RunReport Play(Policy policy, int seed, RunLog? log = null, int maxCommands = 200000)
    {
        var rng = new DeterministicRng(seed ^ policy.Name.GetHashCode());
        var run = Campaign.Start(CampaignLibrary.Faultline, seed).NewState;

        if (log is not null)
        {
            log.Seed = seed;
            log.Label = policy.Name;
        }

        var fights = new List<FightReport>();
        int commands = 0;
        string reason = "completed";

        // Per-fight accumulators, replaced wholesale when a fight begins.
        var tally = new FightTally();

        while (run.Phase != RunPhase.Complete && commands < maxCommands)
        {
            RunCommand command;

            if (run.Fight is { Round: > StallRound })
            {
                reason = $"stalled: {run.Fight.Fight.Id} reached round {run.Fight.Round} "
                    + "with neither side able to finish it";
                break;
            }

            if (run.Phase == RunPhase.AtNode)
            {
                command = new EnterNodeCommand();
            }
            else
            {
                // The enemy is Core's own planner. The policy only plays the players.
                var enemy = Game.NextEnemyCommand(run.Fight!);
                if (enemy is not null)
                {
                    command = new PlayCommand(enemy);
                }
                else
                {
                    var legal = Game.LegalCommands(run.Fight!);
                    if (legal.Count == 0)
                    {
                        reason = "no legal command on node " + run.NodeIndex;
                        break;
                    }

                    var chosen = policy.Choose(run.Fight!, legal, rng);
                    log?.Decisions.Add(new RunLog.Decision(
                        run.NodeIndex,
                        run.Fight!.FindUnit(RunLog.ActorOf(chosen))?.Kind ?? UnitKind.Husk,
                        chosen));

                    // Counted from the command rather than an event, because the interesting half
                    // of the full-pool price is the rescues that were tried and did not land.
                    if (chosen is RescueCommand)
                    {
                        tally.RescueAttempts++;
                    }

                    command = new PlayCommand(chosen);
                }
            }

            var step = Campaign.ApplyRun(run, command);
            commands++;

            foreach (var e in step.Events)
            {
                if (e is FightBegan began)
                {
                    tally = new FightTally { FightId = began.FightId, NodeIndex = began.Index };
                }
            }

            var board = step.FinalBoard ?? run.Fight;
            foreach (var e in step.FightEvents)
            {
                Absorb(e, board, tally);
            }

            foreach (var e in step.Events)
            {
                if (e is FightResolved resolved)
                {
                    fights.Add(new FightReport(
                        tally.NodeIndex,
                        resolved.FightId,
                        resolved.Outcome,
                        resolved.Round,
                        new Dictionary<DamageSource, int>(tally.PlayerDamage),
                        new Dictionary<DamageSource, int>(tally.EnemyDamage),
                        tally.Downed,
                        tally.Voided,
                        tally.Killed,
                        tally.Collisions,
                        tally.Pushes,
                        step.NewState.Squad
                            .Select(u => u.Kind + "=" + u.Hp + "/" + u.MaxHp + " v" + u.Verve)
                            .ToList(),
                        new Dictionary<UnitKind, int>(tally.VerveEarned),
                        new Dictionary<UnitKind, int>(tally.VerveWasted),
                        new Dictionary<UnitKind, int>(tally.VerveSpent),
                        new Dictionary<VerveSpend, int>(tally.Spends),
                        tally.Healed,
                        tally.Absorbed,
                        new Dictionary<TileType, int>(tally.CastLandings),
                        new List<int>(tally.HazardDistanceAtCast),
                        new Dictionary<UnitKind, int>(tally.AttackDamageBy),
                        tally.AttackDistance.ToDictionary(p => p.Key, p => new List<int>(p.Value)),
                        new Dictionary<Ability, int>(tally.AbilityUses),
                        tally.RescueAttempts,
                        tally.RescueSuccesses,
                        tally.AttackKills,
                        tally.BoardKills));
                }
                else if (e is RunLost lost)
                {
                    reason = lost.Reason;
                }
            }

            run = step.NewState;
        }

        if (commands >= maxCommands)
        {
            reason = "command budget exhausted";
        }

        return new RunReport(
            policy.Name,
            seed,
            run.Outcome,
            run.FightsWon,
            run.NodeIndex,
            reason,
            commands,
            fights,
            run.Squad.Select(u => u.ToString()).ToList());
    }

    /// <summary>
    /// Folds one combat event into the accumulators. Sides are read off the board rather than
    /// guessed, so a collision that hurts both parties is counted for both.
    /// </summary>
    private static void Absorb(GameEvent e, GameState? board, FightTally tally)
    {
        switch (e)
        {
            case UnitDamaged d:
                FightTally.Bump(
                    IsPlayer(board, d.UnitId) ? tally.PlayerDamage : tally.EnemyDamage,
                    d.Source,
                    d.Amount);

                // A down event carries no cause, so the last thing that hurt the unit is what
                // attributes the kill. Events arrive in resolution order, so this is the blow.
                tally.LastHurtBy[d.UnitId] = d.Source;
                break;

            // Only the squad's own swings: the same event fires for the enemy round and would
            // otherwise fold their attacks into the Archer's share of the damage.
            case UnitAttacked attacked when IsPlayer(board, attacked.AttackerId):
            {
                var kind = KindOf(board, attacked.AttackerId);
                FightTally.Bump(tally.AttackDamageBy, kind, attacked.Damage);

                if (!tally.AttackDistance.TryGetValue(kind, out var distances))
                {
                    distances = new List<int>();
                    tally.AttackDistance[kind] = distances;
                }

                distances.Add(attacked.From.DistanceTo(attacked.To));
                break;
            }

            case AbilityUsed used when IsPlayer(board, used.UnitId):
                FightTally.Bump(tally.AbilityUses, used.Ability, 1);
                break;

            // The rescuer, not the rescued: enemies pull their own out too, and counting those
            // against the squad's column made the full-pool price look cheaper than it is.
            case Rescued rescued when IsPlayer(board, rescued.RescuerId):
                tally.RescueSuccesses++;
                break;

            // What the stance took on, which is the blow the ally was spared — not the halved
            // figure the guard ends up paying. Counting the latter understates the absorb by half
            // and made the Preen invariant look breached when it was not.
            case GuardIntercepted intercepted:
                tally.Absorbed += intercepted.Redirected;
                break;

            case UnitDowned down:
                if (IsPlayer(board, down.UnitId))
                {
                    tally.Downed++;
                }
                else
                {
                    tally.Killed++;

                    // The thesis in one number: did the sword finish it, or did the board?
                    if (tally.LastHurtBy.TryGetValue(down.UnitId, out var cause)
                        && cause == DamageSource.Attack)
                    {
                        tally.AttackKills++;
                    }
                    else
                    {
                        tally.BoardKills++;
                    }
                }

                break;

            case Voided v:
                if (IsPlayer(board, v.UnitId))
                {
                    tally.Voided++;
                }
                else
                {
                    tally.Killed++;

                    // A drain is the board at its most complete - nothing was ever damaged.
                    tally.BoardKills++;
                }

                break;

            case Collision:
                tally.Collisions++;
                break;

            case UnitPushed pushed:
                tally.Pushes++;

                // A throw is the only displacement whose landing tile is a choice, so it is the only
                // one worth classifying.
                if (pushed.Kind == DisplacementKind.Throw && board is not null)
                {
                    FightTally.Bump(tally.CastLandings, board.Board.At(pushed.To), 1);
                }

                break;

            // Earned and wasted are counted apart on purpose. A squad charging hard into a meter
            // that is already full is playing the board and getting nothing for it, which reads as a
            // healthy earn rate right up until you notice none of it is being banked.
            case VerveCharged charged:
            {
                var kind = KindOf(board, charged.UnitId);
                FightTally.Bump(charged.Wasted ? tally.VerveWasted : tally.VerveEarned, kind, 1);
                break;
            }

            case VerveSpent spent:
                FightTally.Bump(tally.VerveSpent, KindOf(board, spent.UnitId), spent.Cost);
                FightTally.Bump(tally.Spends, spent.Spend, 1);

                // How far she was from anything worth throwing somebody into, at the moment she
                // threw. Cast lands within one tile of her, so this is the ceiling on the ability.
                if (spent.Spend == VerveSpend.Cast && board is not null)
                {
                    tally.HazardDistanceAtCast.Add(NearestHazard(board, spent.At));
                }

                break;



            case UnitHealed healed:
                tally.Healed += healed.Amount;
                break;
        }
    }

    private static UnitKind KindOf(GameState? board, UnitId id) =>
        board?.FindUnit(id)?.Kind ?? UnitKind.Husk;

    /// <summary>Step distance from a tile to the nearest drain or spikes, or -1 when there are none.</summary>
    private static int NearestHazard(GameState board, Coord from)
    {
        int best = -1;

        foreach (var tile in board.Board.AllCoords())
        {
            var type = board.Board.At(tile);
            if (type != TileType.Pit && type != TileType.Spikes)
            {
                continue;
            }

            int distance = from.DistanceTo(tile);
            if (best < 0 || distance < best)
            {
                best = distance;
            }
        }

        return best;
    }

    private static bool IsPlayer(GameState? board, UnitId id)
    {
        var unit = board?.FindUnit(id);
        return unit is not null && unit.Team.IsPlayer();
    }
}
