using System;
using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// The six <c>nv-*.fight</c> proof battles for the enemy variants, and the whole-library soak that
/// makes a Move 0 unit prove it cannot hang the activation loop. Every fight in the library is
/// driven with the enemy side on Core's planner and the players passing, which is the harshest
/// version of "does the round always end".
/// </summary>
public class EnemyVariantFightTests
{
    private const int Rounds = 6;
    private const int CommandLimit = 4000;

    private static readonly string[] Proofs =
    {
        "nv-01-the-toll",
        "nv-02-contested-ledges",
        "nv-03-formation",
        "nv-04-open-order",
        "nv-05-numbers",
        "nv-06-dead-weight",
    };

    [Fact]
    public void EveryProofBattle_IsEmbeddedAndParsesWithoutErrors()
    {
        var results = FightLibrary.LoadAll();

        foreach (var id in Proofs)
        {
            var match = results.SingleOrDefault(r => r.Fight is not null && r.Fight.Id == id);

            Assert.True(match is not null, id + ".fight is not embedded in the library.");
            Assert.True(
                match!.Errors.Count == 0,
                id + " — " + string.Join(" | ", match.Errors));
        }
    }

    [Fact]
    public void TheProofBattles_AreNumbered701To706()
    {
        var numbers = Proofs.Select(id => FightLibrary.ById(id).Number).ToList();

        Assert.Equal(new[] { 701, 702, 703, 704, 705, 706 }, numbers);
    }

    /// <summary>
    /// Deviations from the layout guidelines are allowed but never silent: each proof battle pins
    /// the exact lints it accepts, so a new one shows up as a failure rather than as noise.
    /// </summary>
    [Theory]
    [InlineData("nv-01-the-toll")]
    [InlineData("nv-03-formation")]
    [InlineData("nv-05-numbers")]
    [InlineData("nv-06-dead-weight")]
    public void TheSevenBySevenProofBattles_LintClean(string id)
    {
        var result = FightLibrary.LoadAll().Single(r => r.Fight is not null && r.Fight.Id == id);

        Assert.True(result.Lints.Count == 0, id + " — " + string.Join(" | ", result.Lints));
    }

    [Fact]
    public void ContestedLedges_DeviatesOnlyOnBoardSize()
    {
        var result = FightLibrary.LoadAll()
            .Single(r => r.Fight is not null && r.Fight.Id == "nv-02-contested-ledges");

        // 7x9, not 7x7: both deploy zones have to start outside range 3 of the Perch's spawn or it
        // shoots on round one and never climbs, which is the whole point of the battle.
        Assert.Equal(
            new[] { FightIssueCode.BoardNotSevenBySeven },
            result.Lints.Select(l => l.Code).Distinct().ToArray());
    }

    [Fact]
    public void OpenOrder_DeviatesOnlyOnHavingNoHazardsAtAll()
    {
        var result = FightLibrary.LoadAll()
            .Single(r => r.Fight is not null && r.Fight.Id == "nv-04-open-order");

        // Zero spikes and zero pits is the premise: the Harrier is displacement without hazards, and
        // the Blunted Stalker's whole point is that it has nothing to do here.
        Assert.Equal(
            new[] { FightIssueCode.SpikeCountOutOfRange },
            result.Lints.Select(l => l.Code).Distinct().ToArray());
    }

    [Fact]
    public void EveryProofBattle_FieldsTheVariantItWasWrittenFor()
    {
        Assert.Contains(UnitKind.Warden, KindsIn("nv-01-the-toll"));
        Assert.Contains(UnitKind.Perch, KindsIn("nv-02-contested-ledges"));
        Assert.Contains(UnitKind.Bulwark, KindsIn("nv-03-formation"));
        Assert.Contains(UnitKind.Harrier, KindsIn("nv-04-open-order"));
        Assert.Contains(UnitKind.Runt, KindsIn("nv-05-numbers"));
        Assert.Contains(UnitKind.Colossus, KindsIn("nv-06-dead-weight"));
    }

    [Fact]
    public void TheProofBattles_BetweenThem_FieldEveryVariant()
    {
        var fielded = new HashSet<UnitKind>();
        foreach (var id in Proofs)
        {
            foreach (var kind in KindsIn(id))
            {
                fielded.Add(kind);
            }
        }

        foreach (var kind in new[]
                 {
                     UnitKind.Warden, UnitKind.Perch, UnitKind.Bulwark, UnitKind.Harrier,
                     UnitKind.Runt, UnitKind.Colossus, UnitKind.LesserGrappler,
                     UnitKind.BluntedStalker, UnitKind.HeavyHusk, UnitKind.MobileAnchor,
                 })
        {
            Assert.True(fielded.Contains(kind), kind + " appears in no nv- proof battle.");
        }
    }

    // ---- the soak ------------------------------------------------------------------------------

    /// <summary>
    /// Drives every fight in the library through <see cref="Game.Start"/> and
    /// <see cref="Game.NextEnemyCommand"/> with passive players. No exception, no fight that fails to
    /// terminate, and — the reason this exists — no enemy that hangs the loop. A Move 0 unit that
    /// declared a move it could never make would spin here forever.
    /// </summary>
    [Fact]
    public void EveryFightInTheLibrary_RunsSixRoundsWithPassivePlayers()
    {
        foreach (var fight in FightLibrary.All())
        {
            Drive(fight);
        }
    }

    [Fact]
    public void TheToll_SixRoundsLater_TheWardenIsStillStandingInTheDoorway()
    {
        var fight = FightLibrary.ById("nv-01-the-toll");
        var spawn = fight.Enemies.Single(e => e.Kind == UnitKind.Warden).At;

        var state = Drive(fight);
        var warden = state.Units.Single(u => u.Kind == UnitKind.Warden);

        Assert.True(warden.IsAlive);
        Assert.Equal(spawn, warden.Position);
    }

    [Fact]
    public void ContestedLedges_ThePerchClimbsAndEndsUpOnHighGround()
    {
        var fight = FightLibrary.ById("nv-02-contested-ledges");
        var state = Drive(fight);
        var perch = state.Units.Single(u => u.Kind == UnitKind.Perch);

        Assert.True(perch.IsAlive);
        Assert.Equal(TileType.HighGround, state.Board.At(perch.Position));
    }

    private static IReadOnlyList<UnitKind> KindsIn(string id) =>
        FightLibrary.ById(id).Enemies.Select(e => e.Kind).ToList();

    private static GameState Drive(FightDefinition fight)
    {
        var state = Game.Start(fight, 1).NewState;
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
               && state.Round <= Rounds
               && commands < CommandLimit)
        {
            var command = Game.NextEnemyCommand(state);

            if (command is null)
            {
                // Passive players: every player activation is passed without moving or acting.
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
            fight.Id + " did not reach round " + (Rounds + 1) + " within " + CommandLimit + " commands.");

        return state;
    }
}
