using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// Agency before injury (D-080): a player should never lose hit points to a decision they were not
/// allowed to make.
/// </summary>
public class AgencyTests
{
    // ---- the query ------------------------------------------------------------------------

    [Fact]
    public void AMeleeEnemy_ThreatensEverythingWithinItsWalkPlusItsReach()
    {
        // Husk: move 3, melee 1. On an open line that is every tile from 1 to 4 away.
        var state = BoardBuilder.Open(9, 1)
            .Enemy(UnitKind.Husk, 0, 0)
            .Build();

        var threat = new HashSet<Coord>(Threat.ForUnit(state, state.Find(UnitKind.Husk)));

        for (int x = 1; x <= 4; x++)
        {
            Assert.Contains(new Coord(x, 0), threat);
        }

        Assert.DoesNotContain(new Coord(5, 0), threat);
    }

    [Fact]
    public void ARangedEnemy_ThreatensThroughWalls_BecauseThereIsNoLineOfSight()
    {
        // Not an oversight being pinned as behaviour: Combat.RangeTiles is pure step distance, and
        // every threat number in this file follows from that. A wall stops a Lobber walking and does
        // nothing whatever to stop it shooting.
        var state = BoardBuilder.Rows(".#....")
            .Enemy(UnitKind.Lobber, 0, 0)
            .Build();

        var threat = new HashSet<Coord>(Threat.ForUnit(state, state.Find(UnitKind.Lobber)));

        Assert.Contains(new Coord(2, 0), threat);
        Assert.Contains(new Coord(3, 0), threat);
    }

    [Fact]
    public void AWalledInEnemy_ThreatensOnlyItsRange_BecauseItCannotGetAnywhere()
    {
        var state = BoardBuilder.Rows("#.#....", "#######")
            .Enemy(UnitKind.Lobber, 1, 0)
            .Build();

        var threat = new HashSet<Coord>(Threat.ForUnit(state, state.Find(UnitKind.Lobber)));

        // Range 3 from (1,0) and not a tile more, because there is nowhere else it can stand.
        Assert.Contains(new Coord(4, 0), threat);
        Assert.DoesNotContain(new Coord(5, 0), threat);
    }

    [Fact]
    public void AnEnemyThatDealsNoDamage_IsReportedAsAShoveThreatRatherThanADamageThreat()
    {
        // The Stalker's whole action is a push. It cannot take a hit point off anybody, so the law
        // as worded does not see it — but a shove into a pit is worse than any damage in the game,
        // so it is counted somewhere rather than nowhere.
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Vanguard, 5, 0)
            .Enemy(UnitKind.Stalker, 0, 0)
            .Build();

        Assert.Empty(Threat.DamageRound1(state));
        Assert.NotEmpty(Threat.DisplacementRound1(state));
    }

    [Fact]
    public void AnUndeployedReinforcement_ThreatensNothingOnRoundOne()
    {
        // It is on the unit list from the first command so ids never shift (D-035), and it is not on
        // the board, so it cannot reach anybody in the round the player deploys into.
        var fight = FightLibrary.All().First(f => f.Waves.Count > 0);
        var state = Game.Start(fight, seed: 0).NewState;

        var pending = state.Reinforcements.Select(r => r.UnitId).ToHashSet();
        Assert.NotEmpty(pending);

        foreach (var id in pending)
        {
            Assert.Empty(Threat.ForUnit(state, state.UnitById(id)));
        }
    }

    // ---- the law --------------------------------------------------------------------------

    [Fact]
    public void FirstContact_IsStrict_EveryDeploymentTileIsSafeOnRoundOne()
    {
        // The strict form, and the only board held to it: fight 1 is where a player learns what the
        // game does to them, so nothing on it may hit before they have moved.
        var fight = FightLibrary.Fight1();
        var state = Game.Start(fight, seed: 0).NewState;

        Assert.Equal(
            fight.DeploymentZoneA.Count,
            Threat.SafeDeploymentTiles(state, Team.PlayerA).Count);

        Assert.Equal(
            fight.DeploymentZoneB.Count,
            Threat.SafeDeploymentTiles(state, Team.PlayerB).Count);
    }

    [Fact]
    public void FirstContact_StillTeachesTheLineShove()
    {
        // The law moved two enemies. It must not have moved the pair the fight exists to teach:
        // two Husks adjacent on the west edge, so one Push puts the front one into the back one.
        var west = FightLibrary.Fight1().Enemies
            .Where(e => e.Kind == UnitKind.Husk && e.At.X == 0)
            .OrderBy(e => e.At.Y)
            .ToList();

        Assert.Equal(2, west.Count);
        Assert.Equal(1, west[0].At.DistanceTo(west[1].At));
    }

    /// <summary>
    /// The campaign boards that still break the law, pinned exactly.
    /// </summary>
    /// <remarks>
    /// An allow-list rather than a plain assertion, because the law is new and most of the set
    /// predates it. Pinned in both directions on purpose: a board that starts failing fails this
    /// test, and a board that is fixed without being struck off fails it too. When this list empties,
    /// <see cref="FightIssueCode.UnsafeRound1Deployment"/> moves into the error range and this test
    /// becomes a plain "no campaign board breaks the law".
    /// </remarks>
    private static readonly string[] KnownUnsafe =
    {
        "cb-06-bait-and-break",
        "the-teeth",
        "broken-bridge",
        "the-shrine",
        "high-road",
        "hz-09-the-trench",
    };

    [Fact]
    public void EveryCampaignBoardExceptTheKnownOnes_OffersADamageFreeRoundOne()
    {
        var failing = new List<string>();

        foreach (var node in CampaignLibrary.Faultline.Nodes.OfType<FightNode>())
        {
            if (!Threat.HasSafeDeployment(FightLibrary.ById(node.FightId)))
            {
                failing.Add(node.FightId);
            }
        }

        var unexpected = failing.Except(KnownUnsafe).ToList();
        var fixedSince = KnownUnsafe.Except(failing).ToList();

        Assert.True(
            unexpected.Count == 0,
            "These campaign boards newly break agency before injury (D-080): "
            + string.Join(", ", unexpected)
            + ". Every side must be able to field its roster on tiles no enemy can damage before the "
            + "players have had a turn.");

        Assert.True(
            fixedSince.Count == 0,
            "These campaign boards now obey D-080 and are still listed as known-unsafe: "
            + string.Join(", ", fixedSince)
            + ". Strike them from KnownUnsafe — and if the list is now empty, move "
            + "UnsafeRound1Deployment into the error range, which is the whole point of it.");
    }

    [Fact]
    public void ASafeDeployment_IsAlsoADamageFreeRoundOne()
    {
        // What makes the counting argument sound: a unit on an unthreatened tile that spends its
        // activation doing nothing cannot be reached, so no activation ordering has to be searched
        // for. Played rather than asserted from the query, so the two cannot agree by construction.
        var fight = FightLibrary.Fight1();
        var start = Game.Start(fight, seed: 0).NewState;

        var safeA = Threat.SafeDeploymentTiles(start, Team.PlayerA);
        var safeB = Threat.SafeDeploymentTiles(start, Team.PlayerB);
        Assert.True(safeA.Count >= fight.RosterA.Count && safeB.Count >= fight.RosterB.Count);

        var state = start;
        int slotA = 0, slotB = 0;

        while (state.Phase == Phase.Deployment)
        {
            var legal = Game.LegalCommands(state);
            var undeployed = state.Units.First(u => u.Team == state.ActiveTeam && !u.IsDeployed);
            var tile = state.ActiveTeam == Team.PlayerA ? safeA[slotA++] : safeB[slotB++];

            state = state.Then(legal.OfType<DeployCommand>()
                .First(d => d.UnitId == undeployed.Id && d.At == tile));
        }

        // Round 1, everybody passes. Then the enemies do their worst.
        int round = state.Round;
        while (state.Round == round && state.Outcome == FightOutcome.InProgress)
        {
            var command = Game.NextEnemyCommand(state) ?? Game.LegalCommands(state)
                .OfType<EndActivationCommand>()
                .FirstOrDefault();

            if (command is null)
            {
                break;
            }

            state = state.Then(command);
        }

        foreach (var unit in state.Units.Where(u => u.Team.IsPlayer()))
        {
            Assert.Equal(unit.MaxHp, unit.Hp);
        }
    }

    [Fact]
    public void TheLint_NamesTheSideAndHowShortItIs()
    {
        var result = FightParser.Parse(FightWriter.Write(FightLibrary.ById("the-teeth")));

        var lint = result.Lints.FirstOrDefault(i => i.Code == FightIssueCode.UnsafeRound1Deployment);

        Assert.NotNull(lint);
        Assert.Contains("PlayerB", lint!.Message);
    }

    [Fact]
    public void TheLint_DoesNotFireOutsideTheCampaign()
    {
        // The law is scoped to the run. A trial board is picked from a menu that shows what is on it.
        foreach (var fight in FightLibrary.All().Where(f => !CampaignLibrary.IsCampaignFight(f.Id)))
        {
            var result = FightParser.Parse(FightWriter.Write(fight));

            Assert.DoesNotContain(
                result.Lints,
                i => i.Code == FightIssueCode.UnsafeRound1Deployment);
        }
    }
}
