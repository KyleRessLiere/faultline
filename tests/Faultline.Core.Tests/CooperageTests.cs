using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// <c>the-cooperage</c> — the artillery race (MASTER_DESIGN §6). Three barrels, three answers:
/// the lane you lose, the lane you steal, and the junction trap.
/// </summary>
/// <remarks>
/// The board's numbers are asserted here rather than in prose, because a barrel that stopped being
/// two tiles from the Cooper would quietly stop teaching the thing the lane is for.
/// </remarks>
public class CooperageTests
{
    private static FightDefinition Fight() => FightLibrary.ById("the-cooperage");

    private static Coord B1 => new Coord(1, 0);

    private static Coord B2 => new Coord(6, 2);

    private static Coord B3 => new Coord(3, 2);

    private static Coord Junction => new Coord(3, 3);

    /// <summary>It loads, it is banded, and it fields what §6 says it fields.</summary>
    [Fact]
    public void ItLoads_AndFieldsTheRosterTheBriefNames()
    {
        var fight = Fight();

        Assert.Equal(FightPool.Ordinary, fight.Pool);
        Assert.Equal(ObjectiveKind.KillAll, fight.Objective.Kind);
        Assert.Equal(7, fight.Board.Width);
        Assert.Equal(7, fight.Board.Height);

        var byKind = fight.Enemies.GroupBy(e => e.Kind).ToDictionary(g => g.Key, g => g.Count());

        Assert.Equal(1, byKind[UnitKind.Cooper]);
        Assert.Equal(2, byKind[UnitKind.Husk]);
        Assert.Equal(1, byKind[UnitKind.Grappler]);
        Assert.Equal(3, byKind[UnitKind.Barrel]);

        // The fighting roster is 26; the barrels are objects and are not part of it.
        int fighters = fight.Enemies
            .Where(e => !UnitTemplate.For(e.Kind).IsObject)
            .Sum(e => UnitTemplate.For(e.Kind).MaxHp);

        Assert.Equal(26, fighters);
    }

    /// <summary>Seven drafted spots, and two of them stand in a lane a barrel fires down.</summary>
    [Fact]
    public void SevenSpots_AndTwoOfThemVolunteerAsThePlug()
    {
        var fight = Fight();

        Assert.Equal(7, fight.DeploymentSpots.Count);
        Assert.Equal(7, fight.DeploymentSpots.Distinct().Count());

        // b1 fires down column 1 and b3 down column 3; a spot in each is a draft decision about
        // whether to volunteer as the plug before anything has moved.
        Assert.Contains(new Coord(B1.X, 6), fight.DeploymentSpots);
        Assert.Contains(new Coord(B3.X, 6), fight.DeploymentSpots);
    }

    /// <summary>Agency before injury (D-080): nothing can hurt a duck before it has had a turn.</summary>
    [Fact]
    public void ItCertifies_NobodyIsHitBeforeTheyHaveActed()
    {
        var fight = Fight();

        Assert.Empty(Threat.UnsafeSides(fight));
        Assert.True(Threat.HasSafeDeployment(fight));
    }

    /// <summary>
    /// <b>b1 is the lane you lose.</b> The Cooper reaches it long before any duck on foot can — that
    /// is what makes it teach don't-draft-there, plug, or vacate rather than "run faster".
    /// </summary>
    [Fact]
    public void B1_IsCloserToTheCooperThanToAnySpot()
    {
        var fight = Fight();
        var cooper = fight.Enemies.Single(e => e.Kind == UnitKind.Cooper).At;

        int his = cooper.DistanceTo(B1);
        int ours = fight.DeploymentSpots.Min(s => s.DistanceTo(B1));

        Assert.True(his < ours, "b1 is no longer his to reach first: " + his + " vs " + ours);
        Assert.True(
            ours - his >= 3,
            "b1 is close enough to race for on foot, which is the geometry being wrong rather than "
            + "the barrel being strong (see the board's design notes).");
    }

    /// <summary>
    /// <b>b2 is the lane you steal.</b> The eastern spots win this race, and the shove points back
    /// at his own side of the board.
    /// </summary>
    [Fact]
    public void B2_IsCloserToASpotThanToTheCooper()
    {
        var fight = Fight();
        var cooper = fight.Enemies.Single(e => e.Kind == UnitKind.Cooper).At;

        int his = cooper.DistanceTo(B2);
        int ours = fight.DeploymentSpots.Min(s => s.DistanceTo(B2));

        Assert.True(ours < his, "b2 stopped being ours to take first: " + ours + " vs " + his);
    }

    /// <summary>
    /// <b>b3 is the trap.</b> It stands over the junction, and the Grappler's existing pull is what
    /// makes reaching for it dangerous — no new rule, which is the point.
    /// </summary>
    [Fact]
    public void B3_StandsOverTheJunction_WithTheGrapplerBesideIt()
    {
        var fight = Fight();

        Assert.Equal(1, B3.DistanceTo(Junction));
        Assert.Equal(TileType.Open, fight.Board.At(Junction));

        var grappler = fight.Enemies.Single(e => e.Kind == UnitKind.Grappler).At;
        Assert.True(
            grappler.DistanceTo(Junction) <= UnitTemplate.For(UnitKind.Grappler).Range,
            "the Grappler can no longer pull anybody into the junction, so the trap is gone.");
    }

    /// <summary>
    /// Every enemy can be reached: nothing is walled into a pocket no duck can ever finish.
    /// </summary>
    /// <remarks>
    /// Asked as CONNECTIVITY rather than as one activation's reach - a duck crosses this board over
    /// several turns, and a test that only looked one move ahead would call a perfectly ordinary far
    /// corner unreachable.
    /// </remarks>
    [Fact]
    public void EveryEnemy_CanBeReachedFromADeploymentSpot()
    {
        var fight = Fight();
        var open = Flood(fight, fight.DeploymentSpots[0]);

        foreach (var enemy in fight.Enemies.Where(e => e.Kind != UnitKind.Barrel))
        {
            Assert.True(
                Neighbours(enemy.At).Any(open.Contains),
                enemy.Kind + " at " + enemy.At + " sits in a pocket no duck can walk to.");
        }

        // And every spot is in the same pocket, so no part of the draft strands a duck.
        foreach (var spot in fight.DeploymentSpots)
        {
            Assert.Contains(spot, open);
        }
    }

    /// <summary>
    /// <b>A base kit wins it.</b> No cards, no spends, no pocket - deploy, close on the nearest
    /// enemy, and swing when something is in reach. A board has to be beatable by the squad it ships
    /// with before it is allowed to be interesting.
    /// </summary>
    [Fact]
    public void ABaseKitPolicy_WinsIt()
    {
        var (played, log) = Greedy(Deployed(), 900);

        Assert.NotEmpty(log);
        Assert.Equal(FightOutcome.Won, played.Outcome);
    }

    /// <summary>Seed plus command log replays to the identical board, barrels and Cooper included.</summary>
    [Fact]
    public void ItReplaysExactly()
    {
        var (played, log) = Greedy(Deployed(), 900);

        Assert.Equal(played, TestPlay.Replay(Deployed(), log));
    }

    /// <summary>
    /// The simplest policy that deserves the name: take a kill if one is offered, else any attack,
    /// else close on somebody, else pass. Deliberately blind to barrels - if it wins without ever
    /// meaning to use one, the board is beatable on foot alone.
    /// </summary>
    private static (GameState State, List<Command> Log) Greedy(GameState state, int maxSteps)
    {
        var log = new List<Command>();

        for (int i = 0; i < maxSteps; i++)
        {
            if (state.Outcome != FightOutcome.InProgress)
            {
                break;
            }

            var legal = Game.LegalCommands(state);
            if (legal.Count == 0)
            {
                break;
            }

            var command =
                legal.FirstOrDefault(c => c is DeployCommand)
                ?? Lethal(state, legal)
                ?? legal.FirstOrDefault(c => c is AttackCommand)
                ?? Closing(state, legal)
                ?? legal[0];

            log.Add(command);
            state = Game.Apply(state, command).NewState;
        }

        return (state, log);
    }

    /// <summary>An attack that would put its target down this swing, if one is on offer.</summary>
    private static Command? Lethal(GameState state, IReadOnlyList<Command> legal)
    {
        foreach (var command in legal)
        {
            if (command is AttackCommand attack
                && state.FindUnit(attack.TargetId) is { } target
                && Combat.CanAttack(state, state.UnitById(attack.UnitId), target, out int damage)
                && damage >= target.Hp)
            {
                return command;
            }
        }

        return null;
    }

    /// <summary>The step that ends nearest a living enemy - the whole of "close on somebody".</summary>
    private static Command? Closing(GameState state, IReadOnlyList<Command> legal)
    {
        var foes = state.Units
            .Where(u => u.Team == Team.Enemy && u.IsOnBoard && !u.Template.IsObject)
            .Select(u => u.Position)
            .ToList();

        if (foes.Count == 0)
        {
            return null;
        }

        Command? best = null;
        int bestDistance = int.MaxValue;

        foreach (var command in legal)
        {
            if (command is not MoveCommand move)
            {
                continue;
            }

            int distance = foes.Min(f => move.To.DistanceTo(f));
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = command;
            }
        }

        return best;
    }

    /// <summary>Every non-wall tile connected to a starting tile, walked orthogonally.</summary>
    private static HashSet<Coord> Flood(FightDefinition fight, Coord from)
    {
        var seen = new HashSet<Coord> { from };
        var frontier = new Queue<Coord>();
        frontier.Enqueue(from);

        while (frontier.Count > 0)
        {
            foreach (var next in Neighbours(frontier.Dequeue()))
            {
                if (!fight.Board.InBounds(next)
                    || fight.Board.At(next) == TileType.Wall
                    || !seen.Add(next))
                {
                    continue;
                }

                frontier.Enqueue(next);
            }
        }

        return seen;
    }

    private static IEnumerable<Coord> Neighbours(Coord at)
    {
        yield return new Coord(at.X + 1, at.Y);
        yield return new Coord(at.X - 1, at.Y);
        yield return new Coord(at.X, at.Y + 1);
        yield return new Coord(at.X, at.Y - 1);
    }

    /// <summary>
    /// The objective counts fighters, not barrels. A panel that said 0/7 on a board won at 4 would be
    /// lying about the one thing it exists to state.
    /// </summary>
    [Fact]
    public void TheObjectiveCounter_CountsFightersAndNotBarrels()
    {
        var state = Deployed();
        var status = ObjectiveStatus.For(state);

        Assert.Equal(4, status.Target);
        Assert.Equal(0, status.Progress);
    }

    /// <summary>Step 1 answered, so the board is placeable — the same start every other board's tests use.</summary>
    private static GameState Deployed() =>
        Game.Start(Fight(), seed: 12345).NewState.DraftOrder(Team.PlayerA);
}
