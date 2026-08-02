using Faultline.Core;

namespace Faultline.Playtest;

/// <summary>
/// Reproduces the state the brawler run stopped in, to say what a "no legal command" board is.
/// </summary>
public static class Probe
{
    /// <summary>Replays the brawler to the point it jams and describes the board.</summary>
    /// <param name="seed">Run seed.</param>
    public static void SoftLock(int seed)
    {
        var policy = new BrawlerPolicy();
        var rng = new DeterministicRng(seed ^ policy.Name.GetHashCode());
        var run = Campaign.Start(CampaignLibrary.Faultline, seed).NewState;

        for (int i = 0; i < 200000 && run.Phase != RunPhase.Complete; i++)
        {
            RunCommand command;
            if (run.Phase == RunPhase.AtNode)
            {
                command = new EnterNodeCommand();
            }
            else
            {
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
                        Describe(run);
                        return;
                    }

                    command = new PlayCommand(policy.Choose(run.Fight!, legal, rng));
                }
            }

            run = Campaign.ApplyRun(run, command).NewState;
        }

        Console.WriteLine("no soft-lock reached");
    }

    private static void Describe(RunState run)
    {
        var s = run.Fight!;
        Console.WriteLine("=== jammed ===");
        Console.WriteLine($"node {run.NodeIndex}  fight {s.Fight.Id}  phase {s.Phase}  round {s.Round}");
        Console.WriteLine($"outcome {s.Outcome}  activeTeam {s.ActiveTeam}  nextPlayer {s.NextPlayerTeam}");
        Console.WriteLine($"activeUnit {(s.ActiveUnitId?.ToString() ?? "none")}");
        Console.WriteLine($"objective {s.Fight.Objective.Kind}  structures {s.Structures.Count} standing {s.Structures.Count(x => x.IsStanding)}");
        Console.WriteLine("units:");
        foreach (var u in s.Units)
        {
            Console.WriteLine($"   {u.Id} {u.Team,-8} {u.Kind,-13} hp{u.Hp}/{u.MaxHp} onBoard={u.IsOnBoard} deployed={u.IsDeployed} " +
                              $"clinging={u.Clinging} voided={u.Voided} activated={u.HasActivated} @{u.Position}");
        }

        Console.WriteLine($"NextEnemyCommand: {(Game.NextEnemyCommand(s)?.ToString() ?? "null")}");
        Console.WriteLine($"LegalCommands: {Game.LegalCommands(s).Count}");
    }
}
