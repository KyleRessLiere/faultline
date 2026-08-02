using Faultline.Core;

namespace Faultline.Playtest;

/// <summary>
/// Asks whether a board is one place or several.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Reach"/> already asks whether every enemy can reach *a* player, and answers "0
/// stranded" for boards that are demonstrably broken — because reaching one deploy zone is not the
/// same as the board being connected. A fight whose objective is KillAll and whose halves cannot see
/// each other cannot be won and cannot be lost: it runs until something else stops it.
/// </para>
/// <para>
/// Walkability is Core's own <see cref="Movement.IsWalkable"/>, so a pit is a hole rather than a
/// tile — which is the whole point on a board built around a trench.
/// </para>
/// </remarks>
public static class Connectivity
{
    /// <summary>Reports every board whose walkable tiles fall into more than one island.</summary>
    public static void Report()
    {
        int split = 0;
        int checkedBoards = 0;

        foreach (var fight in FightLibrary.All())
        {
            if (fight.IsRetired)
            {
                continue;
            }

            checkedBoards++;
            var components = Components(fight.Board);

            // Which island each interesting tile sits in.
            var zoneA = IslandsOf(components, fight.DeploymentZoneA);
            var zoneB = IslandsOf(components, fight.DeploymentZoneB);
            var enemies = IslandsOf(components, fight.Enemies.Select(e => e.At).ToList());

            var reachable = new HashSet<int>(zoneA);
            reachable.UnionWith(zoneB);

            bool zonesSplit = zoneA.Count > 0 && zoneB.Count > 0 && !zoneA.Overlaps(zoneB);
            var marooned = enemies.Where(i => !reachable.Contains(i)).ToList();

            if (!zonesSplit && marooned.Count == 0)
            {
                continue;
            }

            split++;
            Console.WriteLine($"=== {fight.Id} ({fight.Name}) — objective {fight.Objective.Kind}"
                + (fight.TurnLimit > 0 ? $", {fight.TurnLimit}-turn limit" : ", NO turn limit")
                + (CampaignLibrary.IsCampaignFight(fight.Id) ? "  [CAMPAIGN]" : string.Empty));

            if (zonesSplit)
            {
                Console.WriteLine("    the two deploy zones are on separate islands — the players can never meet,");
                Console.WriteLine("    and neither can reach the enemies facing the other one.");
            }

            foreach (var spawn in fight.Enemies)
            {
                int island = components.TryGetValue(spawn.At, out int i) ? i : -1;
                string side = zoneA.Contains(island) ? "A" : zoneB.Contains(island) ? "B" : "NOBODY";
                if (side == "NOBODY" || zonesSplit)
                {
                    Console.WriteLine($"    {spawn.Kind,-12} at {spawn.At}  island {island}  reachable by: {side}");
                }
            }

            Console.WriteLine();
        }

        Console.WriteLine($"checked {checkedBoards} active boards — {split} with a split the objective cannot cross");
    }

    /// <summary>Flood-fills walkable tiles into numbered islands.</summary>
    /// <param name="board">Board to divide.</param>
    /// <returns>Island index per walkable tile.</returns>
    public static Dictionary<Coord, int> Components(Board board)
    {
        var island = new Dictionary<Coord, int>();
        int next = 0;

        foreach (var start in board.AllCoords())
        {
            if (island.ContainsKey(start) || !Movement.IsWalkable(board.At(start)))
            {
                continue;
            }

            int id = next++;
            var queue = new Queue<Coord>();
            queue.Enqueue(start);
            island[start] = id;

            while (queue.Count > 0)
            {
                var at = queue.Dequeue();

                foreach (var direction in new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right })
                {
                    var next2 = at.Step(direction);
                    if (!board.InBounds(next2)
                        || island.ContainsKey(next2)
                        || !Movement.IsWalkable(board.At(next2)))
                    {
                        continue;
                    }

                    island[next2] = id;
                    queue.Enqueue(next2);
                }
            }
        }

        return island;
    }

    private static HashSet<int> IslandsOf(Dictionary<Coord, int> components, IReadOnlyList<Coord> tiles)
    {
        var found = new HashSet<int>();

        foreach (var tile in tiles)
        {
            if (components.TryGetValue(tile, out int island))
            {
                found.Add(island);
            }
        }

        return found;
    }
}
