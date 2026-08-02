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
    int Absorbed);

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

    /// <summary>
    /// The guard an interception just named, until the redirected effect lands on it.
    /// </summary>
    /// <remarks>
    /// GuardIntercepted is emitted before the effect resolves, so "how much did the stance soak" is
    /// the damage on the very next event about that unit. Reading it any other way would count every
    /// hit the Wardbearer ever took as absorbed, including the ones aimed at him.
    /// </remarks>
    internal UnitId? PendingGuard { get; set; }

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
    /// <summary>Plays one campaign to its end.</summary>
    /// <param name="policy">How the players decide.</param>
    /// <param name="seed">Run seed.</param>
    /// <param name="maxCommands">Safety stop.</param>
    /// <returns>The report.</returns>
    public static RunReport Play(Policy policy, int seed, int maxCommands = 200000)
    {
        var rng = new DeterministicRng(seed ^ policy.Name.GetHashCode());
        var run = Campaign.Start(CampaignLibrary.Faultline, seed).NewState;

        var fights = new List<FightReport>();
        int commands = 0;
        string reason = "completed";

        // Per-fight accumulators, replaced wholesale when a fight begins.
        var tally = new FightTally();

        while (run.Phase != RunPhase.Complete && commands < maxCommands)
        {
            RunCommand command;

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

                    command = new PlayCommand(policy.Choose(run.Fight!, legal, rng));
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
                        tally.Absorbed));
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

                if (tally.PendingGuard == d.UnitId)
                {
                    tally.Absorbed += d.Amount;
                    tally.PendingGuard = null;
                }

                break;

            case GuardIntercepted intercepted:
                tally.PendingGuard = intercepted.UnitId;
                break;

            case UnitDowned down:
                if (IsPlayer(board, down.UnitId))
                {
                    tally.Downed++;
                }
                else
                {
                    tally.Killed++;
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
                }

                break;

            case Collision:
                tally.Collisions++;
                break;

            case UnitPushed:
                tally.Pushes++;
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
                break;

            case UnitHealed healed:
                tally.Healed += healed.Amount;
                break;
        }
    }

    private static UnitKind KindOf(GameState? board, UnitId id) =>
        board?.FindUnit(id)?.Kind ?? UnitKind.Husk;

    private static bool IsPlayer(GameState? board, UnitId id)
    {
        var unit = board?.FindUnit(id);
        return unit is not null && unit.Team.IsPlayer();
    }
}
