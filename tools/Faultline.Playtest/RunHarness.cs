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
/// <param name="HpOut">Squad HP carried out, as "Kind=hp/max" entries.</param>
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
    List<string> HpOut);

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

        // Per-fight accumulators, reset when a fight begins.
        var playerDamage = new Dictionary<DamageSource, int>();
        var enemyDamage = new Dictionary<DamageSource, int>();
        int downed = 0, voided = 0, killed = 0, collisions = 0, pushes = 0;
        string fightId = string.Empty;
        int nodeIndex = 0;

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
                    playerDamage = new Dictionary<DamageSource, int>();
                    enemyDamage = new Dictionary<DamageSource, int>();
                    downed = voided = killed = collisions = pushes = 0;
                    fightId = began.FightId;
                    nodeIndex = began.Index;
                }
            }

            var board = step.FinalBoard ?? run.Fight;
            foreach (var e in step.FightEvents)
            {
                Absorb(e, board, playerDamage, enemyDamage, ref downed, ref voided, ref killed, ref collisions, ref pushes);
            }

            foreach (var e in step.Events)
            {
                if (e is FightResolved resolved)
                {
                    fights.Add(new FightReport(
                        nodeIndex,
                        resolved.FightId,
                        resolved.Outcome,
                        resolved.Round,
                        new Dictionary<DamageSource, int>(playerDamage),
                        new Dictionary<DamageSource, int>(enemyDamage),
                        downed,
                        voided,
                        killed,
                        collisions,
                        pushes,
                        step.NewState.Squad.Select(u => u.Kind + "=" + u.Hp + "/" + u.MaxHp).ToList()));
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
    private static void Absorb(
        GameEvent e,
        GameState? board,
        Dictionary<DamageSource, int> playerDamage,
        Dictionary<DamageSource, int> enemyDamage,
        ref int downed,
        ref int voided,
        ref int killed,
        ref int collisions,
        ref int pushes)
    {
        switch (e)
        {
            case UnitDamaged d:
            {
                var sink = IsPlayer(board, d.UnitId) ? playerDamage : enemyDamage;
                sink.TryGetValue(d.Source, out int had);
                sink[d.Source] = had + d.Amount;
                break;
            }

            case UnitDowned down:
                if (IsPlayer(board, down.UnitId))
                {
                    downed++;
                }
                else
                {
                    killed++;
                }

                break;

            case Voided v:
                if (IsPlayer(board, v.UnitId))
                {
                    voided++;
                }
                else
                {
                    killed++;
                }

                break;

            case Collision:
                collisions++;
                break;

            case UnitPushed:
                pushes++;
                break;
        }
    }

    private static bool IsPlayer(GameState? board, UnitId id)
    {
        var unit = board?.FindUnit(id);
        return unit is not null && unit.Team.IsPlayer();
    }
}
