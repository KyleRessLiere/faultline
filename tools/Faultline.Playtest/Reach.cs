using Faultline.Core;

namespace Faultline.Playtest;

/// <summary>
/// Answers two questions about every active board, ignoring the move budget: can this enemy ever
/// engage anybody at all, and how long does it spend walking before it can.
/// </summary>
/// <remarks>
/// Written to check a playtest note — "the husk just walks into a wall and does nothing". Nothing is
/// stranded, as it turns out, but the walking is real and measurable.
/// </remarks>
public static class Reach
{
    /// <summary>Reports stranded and slow-to-engage enemies across the active library.</summary>
    public static void Stranded()
    {
        int boards = 0, enemies = 0, stranded = 0, slowest = 0;
        string slowestWhere = string.Empty;

        foreach (var fight in FightLibrary.All())
        {
            var state = Game.Start(fight, 4242).NewState;

            int guard = 0;
            while (state.Phase == Phase.Deployment && guard++ < 200)
            {
                var legal = Game.LegalCommands(state);
                if (legal.Count == 0)
                {
                    break;
                }

                state = Game.Apply(state, legal[0]).NewState;
            }

            var players = state.Units.Where(u => u.Team.IsPlayer() && u.IsOnBoard).ToList();
            if (players.Count == 0)
            {
                continue;
            }

            boards++;

            foreach (var enemy in state.Units.Where(u => u.Team == Team.Enemy && u.IsOnBoard))
            {
                enemies++;

                // "Can act" is not "is adjacent". A Lobber with range 3 is doing its job from three
                // tiles away, so measuring every enemy against adjacency would call all of them slow.
                int reach = enemy.Template.Attack == AttackKind.Ranged
                    ? enemy.Template.Range
                    : Math.Max(1, enemy.Template.BasicReach);

                var field = PathField.To(state, enemy, enemy.Position);

                var firingTiles = AllTiles(state)
                    .Where(t => field.Reaches(t))
                    .Where(t => players.Any(p => t.DistanceTo(p.Position) <= reach))
                    .ToList();

                if (firingTiles.Count == 0)
                {
                    stranded++;
                    Console.WriteLine($"STRANDED  {fight.Id,-28} {enemy.Kind,-13} at {enemy.Position}");
                    continue;
                }

                int cost = firingTiles.Select(field.At).Min();
                int rounds = enemy.Move > 0 ? (cost + enemy.Move - 1) / enemy.Move : 0;

                if (rounds >= 3)
                {
                    Console.WriteLine(
                        $"SLOW      {fight.Id,-28} {enemy.Kind,-13} at {enemy.Position,-8} " +
                        $"{cost,2} MP / move {enemy.Move} = {rounds} rounds before it can act");
                }

                if (cost > slowest)
                {
                    slowest = cost;
                    slowestWhere = fight.Id + " " + enemy.Kind + " at " + enemy.Position;
                }
            }
        }

        Console.WriteLine();
        Console.WriteLine($"checked {enemies} enemies across {boards} active boards — {stranded} stranded");
        Console.WriteLine($"longest approach: {slowest} MP — {slowestWhere}");
    }

    private static IEnumerable<Coord> AllTiles(GameState state)
    {
        for (int y = 0; y < state.Board.Height; y++)
        {
            for (int x = 0; x < state.Board.Width; x++)
            {
                yield return new Coord(x, y);
            }
        }
    }
}
