using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// The whole-library soak, run over <see cref="FightLibrary.LoadAll"/> rather than
/// <see cref="FightLibrary.All"/>: a retired battle is still embedded, still parsed and still has to
/// start and run, so retiring one can never be a way of quietly letting it rot.
/// </summary>
public class LibrarySoakTests
{
    private const int CommandLimit = 6000;

    [Fact]
    public void EveryEmbeddedFight_RetiredIncluded_StartsAndRunsSixRoundsWithPassivePlayers()
    {
        foreach (var result in FightLibrary.LoadAll())
        {
            Assert.True(
                result.Fight is not null,
                result.Describe() + " — " + string.Join(" | ", result.Errors));

            Drive(result.Fight!, rounds: 6);
        }
    }

    [Fact]
    public void EveryEmbeddedFight_RetiredIncluded_ParsesWithZeroErrors()
    {
        foreach (var result in FightLibrary.LoadAll())
        {
            Assert.Empty(result.Errors);
        }
    }

    [Fact]
    public void EveryEmbeddedFight_RetiredIncluded_HasAUniqueId()
    {
        var ids = FightLibrary.LoadAll().Where(r => r.Fight is not null).Select(r => r.Fight!.Id).ToList();

        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    /// <summary>
    /// Deploys into the first legal slot, then runs rounds with Core planning the enemy and every
    /// player activation passed. Shared with the curated-set board tests.
    /// </summary>
    /// <param name="fight">Fight to drive.</param>
    /// <param name="rounds">How many rounds to play.</param>
    /// <returns>The state it stopped in.</returns>
    internal static GameState Drive(FightDefinition fight, int rounds)
    {
        var state = Game.Start(fight, 4242).NewState;
        int commands = 0;

        while (state.Phase == Phase.Deployment && commands < CommandLimit)
        {
            var legal = Game.LegalCommands(state);
            Assert.True(legal.Count > 0, fight.Id + " ran out of legal commands during deployment.");
            state = Game.Apply(state, legal[0]).NewState;
            commands++;
        }

        while (state.Phase == Phase.Battle
               && state.Outcome == FightOutcome.InProgress
               && state.Round <= rounds
               && commands < CommandLimit)
        {
            var command = Game.NextEnemyCommand(state);

            if (command is null)
            {
                var pending = state.Units.FirstOrDefault(u =>
                    u.Team == state.ActiveTeam && u.IsOnBoard && !u.Clinging && !u.HasActivated);

                Assert.True(
                    pending is not null,
                    fight.Id + " round " + state.Round + ": " + state.ActiveTeam
                    + " holds the activation slot with nobody able to take it.");

                command = new EndActivationCommand(pending!.Id);
            }

            state = Game.Apply(state, command).NewState;
            commands++;
        }

        Assert.True(
            commands < CommandLimit,
            fight.Id + " did not reach round " + (rounds + 1) + " within " + CommandLimit + " commands.");

        return state;
    }
}
