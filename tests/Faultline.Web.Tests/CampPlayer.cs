using System;
using System.Collections.Generic;
using System.Linq;
using Faultline.Core;
using Faultline.Web.Shell;

namespace Faultline.Web.Tests;

/// <summary>
/// Plays a run's fight through <see cref="RunSession"/> until it stops being a fight — deployment
/// and all, one legal command at a time.
/// </summary>
/// <remarks>
/// <para>
/// <b>It exists because restoring is not playing.</b> D-125 was reached by a whole suite of tests
/// that got to a later node by writing a save and reading it back, which is exactly why nobody
/// noticed that the phase the save dropped was one the run could not leave. A test that wants to
/// assert something about what follows a won fight has to <em>win one</em>, and this is the cheapest
/// honest way to do that from the shell's own command surface.
/// </para>
/// <para>
/// <b>It decides nothing about the rules.</b> Every command it submits comes off
/// <see cref="RunSession.Legal"/>, which is <see cref="Campaign.LegalRunCommands"/>. The
/// preference order below is a playing style, not a rule: hit if you can, otherwise close the
/// distance, otherwise pass.
/// </para>
/// </remarks>
internal static class CampPlayer
{
    /// <summary>Enters the node the run is standing on and plays its fight out.</summary>
    /// <param name="session">The run.</param>
    /// <param name="steps">Safety valve; a fight that needs more than this has gone wrong.</param>
    /// <returns>How the fight ended.</returns>
    /// <exception cref="InvalidOperationException">The fight never settled.</exception>
    public static FightOutcome PlayCurrentFight(RunSession session, int steps = 4000)
    {
        session.Enter();

        if (!session.InFight)
        {
            throw new InvalidOperationException(
                "The run did not enter a fight; it is " + (session.State?.Phase.ToString() ?? "gone") + ".");
        }

        for (int i = 0; i < steps && session.InFight; i++)
        {
            if (!Step(session))
            {
                throw new InvalidOperationException(
                    "The board offered nothing legal on round "
                    + (session.State?.Fight?.Round.ToString() ?? "?") + ".");
            }
        }

        if (session.InFight)
        {
            throw new InvalidOperationException("The fight never settled.");
        }

        return session.LastResolution?.Outcome ?? FightOutcome.InProgress;
    }

    /// <summary>
    /// Enters the node the run is standing on and loses its fight — by deploying and then doing
    /// nothing.
    /// </summary>
    /// <remarks>
    /// <b>Losing is a state a screen has to draw</b>, and the only honest way to reach it is to
    /// arrive at it the way a player would. Restoring a save that says <c>lost</c> would prove the
    /// front door can render a field, not that the game can put a run into that field. Passing every
    /// activation is a legal way to play and the enemy does the rest.
    /// </remarks>
    /// <param name="session">The run.</param>
    /// <param name="steps">Safety valve.</param>
    /// <returns>How the fight ended.</returns>
    /// <exception cref="InvalidOperationException">The fight never settled.</exception>
    public static FightOutcome LoseCurrentFight(RunSession session, int steps = 4000)
    {
        session.Enter();

        if (!session.InFight)
        {
            throw new InvalidOperationException(
                "The run did not enter a fight; it is " + (session.State?.Phase.ToString() ?? "gone") + ".");
        }

        for (int i = 0; i < steps && session.InFight; i++)
        {
            var state = session.State?.Fight;
            var commands = session.Legal.OfType<PlayCommand>().Select(p => p.Command).ToList();
            if (state is null || commands.Count == 0)
            {
                throw new InvalidOperationException("The board offered nothing legal.");
            }

            // The enemy plays its own game. Passing on its behalf too would be a fight in which
            // nobody ever swings, which settles nothing and is not a defeat.
            if (state.ActiveTeam == Team.Enemy)
            {
                session.Play(commands[0]);
                continue;
            }

            // Deployment still has to happen — a squad that never takes the field is not a squad
            // that lost, it is a fight that never started. After that: walk towards them and never
            // swing. Standing still does not lose this board — the Husks run out of reachable ducks
            // and the fight stalls at round 284 with everyone alive, which is a stalemate rather
            // than a defeat. Walking in is a legal way to play, and a bad one.
            var deploy = commands.OfType<DeployCommand>().FirstOrDefault();
            if (deploy is not null)
            {
                session.Play(deploy);
                continue;
            }

            var closing = Closing(state, commands.OfType<MoveCommand>().ToList());
            var pass = commands.OfType<EndActivationCommand>().FirstOrDefault();

            session.Play(closing ?? (Command?)pass ?? commands[0]);
        }

        if (session.InFight)
        {
            throw new InvalidOperationException("The fight never settled.");
        }

        return session.LastResolution?.Outcome ?? FightOutcome.InProgress;
    }

    /// <summary>Submits one command, chosen greedily.</summary>
    private static bool Step(RunSession session)
    {
        var state = session.State?.Fight;
        if (state is null)
        {
            return false;
        }

        var commands = session.Legal.OfType<PlayCommand>().Select(p => p.Command).ToList();
        if (commands.Count == 0)
        {
            return false;
        }

        // The enemy side is Core's own to decide; the shell resolves it the same way the screen's
        // timer does, by taking what the rules hand back.
        if (state.ActiveTeam == Team.Enemy)
        {
            session.Play(commands[0]);
            return true;
        }

        var chosen = Choose(state, commands);
        session.Play(chosen);
        return true;
    }

    private static Command Choose(GameState state, IReadOnlyList<Command> commands)
    {
        // Deployment first, and in the order Core offers it: where a duck starts is not what this
        // driver is for.
        var deploy = commands.OfType<DeployCommand>().FirstOrDefault();
        if (deploy is not null)
        {
            return deploy;
        }

        // A killing blow, then any blow. The whole point is to end the fight, and a driver that
        // never finished one would be a slow way of asserting nothing.
        var attacks = commands.OfType<AttackCommand>().ToList();
        var killer = attacks.FirstOrDefault(a => Kills(state, a));
        if (killer is not null)
        {
            return killer;
        }

        if (attacks.Count > 0)
        {
            return attacks[0];
        }

        var ability = commands.OfType<AbilityCommand>().FirstOrDefault(a => HitsAnEnemy(state, a));
        if (ability is not null)
        {
            return ability;
        }

        var closing = Closing(state, commands.OfType<MoveCommand>().ToList());
        if (closing is not null)
        {
            return closing;
        }

        var end = commands.OfType<EndActivationCommand>().FirstOrDefault();
        return end ?? commands[0];
    }

    private static bool Kills(GameState state, AttackCommand attack)
    {
        var target = state.FindUnit(attack.TargetId);
        var actor = state.FindUnit(attack.UnitId);
        return target is not null && actor is not null && target.Hp <= actor.Template.Damage;
    }

    private static bool HitsAnEnemy(GameState state, AbilityCommand ability) =>
        ability.TargetId is { } target
        && state.FindUnit(target) is { IsAlive: true, Team: Team.Enemy };

    /// <summary>The move that ends up nearest a living enemy, or nothing when none exists.</summary>
    private static Command? Closing(GameState state, IReadOnlyList<MoveCommand> moves)
    {
        if (moves.Count == 0)
        {
            return null;
        }

        var enemies = state.Units.Where(u => u.IsOnBoard && u.IsAlive && u.Team == Team.Enemy).ToList();
        if (enemies.Count == 0)
        {
            return null;
        }

        MoveCommand? best = null;
        int bestDistance = int.MaxValue;

        foreach (var move in moves)
        {
            int distance = enemies.Min(e => Chebyshev(move.To, e.Position));
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = move;
            }
        }

        return best;
    }

    private static int Chebyshev(Coord a, Coord b) =>
        Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
}
