using System;
using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// Drives runs for the run tests. Fights are settled by rigging the board rather than by playing
/// them out — a test about carrying damage between fights should say what damage and what carried,
/// not spend four hundred commands getting there.
/// </summary>
internal static class RunFixture
{
    internal const int Seed = 4242;

    /// <summary>A run standing on its first node.</summary>
    internal static RunState Start(int seed = Seed) =>
        Campaign.Start(CampaignLibrary.Faultline, seed).NewState;

    /// <summary>Enters the current node and returns the state after it.</summary>
    internal static RunState Enter(RunState run) =>
        Campaign.ApplyRun(run, new EnterNodeCommand()).NewState;

    /// <summary>A run inside its first fight, with the Vanguard's run id handed back.</summary>
    internal static RunState StartedInFirstFight(out RunUnitId vanguard) =>
        StartedInFirstFight(UnitKind.Vanguard, out vanguard);

    /// <summary>A run inside its first fight, with one squad member's run id handed back.</summary>
    internal static RunState StartedInFirstFight(UnitKind kind, out RunUnitId id)
    {
        var run = Enter(Start());
        id = run.Squad.Single(u => u.Kind == kind).Id;
        return run;
    }

    /// <summary>The board unit a squad member is currently fielding as.</summary>
    internal static Unit OnBoard(RunState run, RunUnitId id)
    {
        var binding = run.Bindings.Single(b => b.RunUnitId.Equals(id));
        return run.Fight!.UnitById(binding.UnitId);
    }

    /// <summary>Sets a fielded unit's hit points directly, without pretending a fight happened.</summary>
    internal static RunState HurtTo(RunState run, RunUnitId id, int hp)
    {
        var unit = OnBoard(run, id);
        return run with { Fight = run.Fight!.WithUnit(unit with { Hp = hp }) };
    }

    /// <summary>Marks a fielded unit as gone down a pit, the way the pit rules would.</summary>
    internal static RunState Void(RunState run, RunUnitId id)
    {
        var unit = OnBoard(run, id);
        return run with { Fight = run.Fight!.WithUnit(unit with { Hp = 0, Voided = true, IsDeployed = false }) };
    }

    /// <summary>
    /// Wins the fight in progress by clearing the board of everything hostile and applying one
    /// command so the run notices. That is the game's own universal win condition rather than a flag
    /// set behind the rules' back.
    ///
    /// There is deliberately no matching "lose" helper. Emptying a board of players between commands
    /// produces a state nothing can leave — the outcome is only checked when a command is applied —
    /// so a loss has to be played into, and <see cref="PlayWholeRun"/> is how the tests get one.
    /// </summary>
    internal static RunStepResult EndFightInAWin(RunState run)
    {
        if (run.Fight is null)
        {
            throw new InvalidOperationException("No fight in progress.");
        }

        // The outcome is only checked once a unit acts, never during deployment — so deployment is
        // played out first, for real. It is only the emptying of the board that is arranged.
        var state = Deploy(run);

        var units = new List<Unit>();
        foreach (var unit in state.Fight!.Units)
        {
            units.Add(unit.Team == Team.Enemy ? unit with { Hp = 0, IsDeployed = false } : unit);
        }

        state = state with
        {
            Fight = state.Fight with
            {
                Units = units,
                Reinforcements = Array.Empty<PendingReinforcement>(),
            },
        };

        var legal = Campaign.LegalRunCommands(state);
        if (legal.Count == 0)
        {
            throw new InvalidOperationException(
                "The rigged board left no legal command to settle it with.");
        }

        return Campaign.ApplyRun(state, legal[0]);
    }

    /// <summary>Plays deployment out with the first legal command each time.</summary>
    internal static RunState Deploy(RunState run)
    {
        int guard = 0;
        while (run.Phase == RunPhase.InFight
               && run.Fight!.Phase == Phase.Deployment
               && guard++ < 200)
        {
            var legal = Campaign.LegalRunCommands(run);
            if (legal.Count == 0)
            {
                break;
            }

            run = Campaign.ApplyRun(run, legal[0]).NewState;
        }

        return run;
    }

    /// <summary>Wins the fight in progress and returns the run standing on the next node.</summary>
    internal static RunState WinTheFight(RunState run)
    {
        var step = EndFightInAWin(run);

        if (step.NewState.Phase == RunPhase.InFight)
        {
            throw new InvalidOperationException(
                "The rigged fight did not settle; it is still in progress on round "
                + step.NewState.Fight!.Round + ".");
        }

        return step.NewState;
    }

    /// <summary>Plays forward, winning every fight, until the run is standing on a rest.</summary>
    internal static RunState PlayForwardToRest(RunState run)
    {
        while (run.Phase != RunPhase.Complete && run.CurrentNode is not RestNode)
        {
            run = Enter(run);
            if (run.Phase == RunPhase.InFight)
            {
                run = WinTheFight(run);
            }
        }

        return run;
    }

    /// <summary>
    /// A run standing on its first rest with a genuinely battered squad: every member is taken to 1
    /// HP during the fourth fight, so the rest has something to restore and the test can say how much.
    /// </summary>
    internal static RunState AtTheFirstRest(out IReadOnlyList<RunUnitId> hurt)
    {
        var run = Start();
        var damaged = new List<RunUnitId>();

        while (run.CurrentNode is not RestNode)
        {
            run = Enter(run);
            if (run.Phase != RunPhase.InFight)
            {
                continue;
            }

            if (run.NodeIndex == 3)
            {
                run = Deploy(run);
                foreach (var binding in run.Bindings)
                {
                    run = HurtTo(run, binding.RunUnitId, 1);
                    damaged.Add(binding.RunUnitId);
                }
            }

            run = WinTheFight(run);
        }

        hurt = damaged;
        return run;
    }

    /// <summary>
    /// Plays a run to its end with commands and nothing else, and returns the state plus the log that
    /// produced it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Nothing here rigs a board. The rigging the other helpers do is out-of-band mutation, and a log
    /// that leaves it out would not replay — a determinism test built on it would pass by testing
    /// nothing. So this drives for real: Core plans the enemy, the players take their first legal
    /// command, and the run ends where that takes it, win or lose.
    /// </para>
    /// <para>
    /// A first-legal driver is not trying to win, so most seeds end the run somewhere in the spine.
    /// That is the correct thing to replay — a run that ended in a loss on fight 3 has to replay to
    /// exactly that loss on exactly that fight.
    /// </para>
    /// </remarks>
    internal static (RunState State, List<RunCommand> Log) PlayWholeRun(int seed, int maxCommands = 40000) =>
        PlayForward(Start(seed), maxCommands);

    /// <summary>
    /// A run standing in its first fight with every fielded unit on one hit point, so that playing
    /// forward reaches a loss the engine decided on rather than one a test arranged.
    /// </summary>
    /// <remarks>
    /// The loss tests used to rely on the first-legal driver dying somewhere in the spine at a known
    /// seed. That is a fact about board tuning, not about the run layer, and it broke the moment
    /// fight 1 stopped being able to kill anybody (D-080). Which fight the run loses is now
    /// irrelevant to what those tests assert.
    /// </remarks>
    internal static RunState CrippledInFirstFight(int seed = Seed)
    {
        var run = Enter(Start(seed));

        foreach (var binding in run.Bindings)
        {
            run = HurtTo(run, binding.RunUnitId, 1);
        }

        return run;
    }

    /// <summary>Plays a run on from wherever it stands, first legal command each time.</summary>
    internal static (RunState State, List<RunCommand> Log) PlayForward(RunState run, int maxCommands = 40000)
    {
        var log = new List<RunCommand>();

        while (run.Phase != RunPhase.Complete && log.Count < maxCommands)
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
                    var legal = Campaign.LegalRunCommands(run);
                    if (legal.Count == 0)
                    {
                        break;
                    }

                    command = FirstAction(legal) ?? legal[0];
                }
            }

            log.Add(command);
            run = Campaign.ApplyRun(run, command).NewState;
        }

        return (run, log);
    }

    /// <summary>
    /// The first legal command that actually does something to somebody, or null when there is none.
    /// </summary>
    /// <remarks>
    /// "First legal command each time" used to reach an attack on its own: moves are enumerated
    /// first, so the driver walked its budget out and then swung with what was left. Under the AP
    /// turn acting costs legs, so a unit that walks its whole pool is offered nothing but
    /// EndActivation and the driver never lands a blow — a run that could not clear a node no matter
    /// how it was tuned. Preferring the action is the same naive policy the harness uses: afford the
    /// action first, close with whatever is left over.
    /// </remarks>
    private static RunCommand? FirstAction(IReadOnlyList<RunCommand> legal)
    {
        foreach (var candidate in legal)
        {
            if (candidate is PlayCommand play
                && play.Command is AttackCommand or AbilityCommand)
            {
                return candidate;
            }
        }

        return null;
    }
}
