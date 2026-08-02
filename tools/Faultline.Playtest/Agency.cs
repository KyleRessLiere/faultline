using Faultline.Core;

namespace Faultline.Playtest;

/// <summary>
/// Reports how every shipped board stands against the agency-before-injury law (D-080): can each
/// side deploy somewhere nothing can damage it before it has had a turn?
/// </summary>
internal static class Agency
{
    /// <summary>Prints a per-board verdict for the campaign, then for everything else.</summary>
    internal static void Report()
    {
        var campaign = new HashSet<string>();
        foreach (var node in CampaignLibrary.Faultline.Nodes)
        {
            if (node is FightNode fight)
            {
                campaign.Add(fight.FightId);
            }
        }

        Console.WriteLine("Agency before injury — safe round-1 deployment per board");
        Console.WriteLine();

        Section("Campaign", FightLibrary.All().Where(f => campaign.Contains(f.Id)));
        Console.WriteLine();
        Section("Everything else", FightLibrary.All().Where(f => !campaign.Contains(f.Id)));
    }

    /// <summary>
    /// For one board, prints where each enemy archetype could stand without threatening any
    /// deployment tile. Answers "can this board obey the law at all" with tiles rather than argument.
    /// </summary>
    internal static void Placements(string id)
    {
        var fight = FightLibrary.ById(id);
        var start = Game.Start(fight, seed: 0).NewState;

        var deploy = new List<Coord>();
        deploy.AddRange(fight.DeploymentZoneA);
        deploy.AddRange(fight.DeploymentZoneB);

        Console.WriteLine($"{id}: {fight.Board.Width}x{fight.Board.Height}, "
            + $"deploy tiles {string.Join(" ", deploy)}");
        Console.WriteLine();

        foreach (var kind in fight.Enemies.Select(e => e.Kind).Distinct())
        {
            var template = UnitTemplate.For(kind);
            var safe = new List<Coord>();

            foreach (var tile in fight.Board.AllCoords())
            {
                if (fight.Board.At(tile) is not TileType.Open and not TileType.HighGround)
                {
                    continue;
                }

                if (deploy.Contains(tile))
                {
                    continue;
                }

                // One lone enemy of this kind on an otherwise empty board: the most room it will ever
                // have, so a tile that fails here fails in every arrangement.
                var probe = start with
                {
                    Units = new[]
                    {
                        Unit.FromTemplate(new UnitId(0), kind, Team.Enemy) with
                        {
                            Position = tile,
                            IsDeployed = true,
                        },
                    },
                };

                var threat = new HashSet<Coord>(Threat.ForUnit(probe, probe.Units[0]));
                if (!deploy.Any(threat.Contains))
                {
                    safe.Add(tile);
                }
            }

            Console.WriteLine($"  {kind,-14} move {template.Move}, reach {template.BasicReach} "
                + $"→ {safe.Count} placement(s) threatening no deploy tile");
            if (safe.Count > 0)
            {
                Console.WriteLine("      " + string.Join(" ", safe));
                continue;
            }

            // Nowhere clean. Then the useful number is how close the board can get, because that is
            // the difference between "reposition it" and "this board cannot hold this archetype".
            var best = int.MaxValue;
            var bestTiles = new List<Coord>();

            foreach (var tile in fight.Board.AllCoords())
            {
                if (fight.Board.At(tile) is not TileType.Open and not TileType.HighGround
                    || deploy.Contains(tile))
                {
                    continue;
                }

                var probe = start with
                {
                    Units = new[]
                    {
                        Unit.FromTemplate(new UnitId(0), kind, Team.Enemy) with
                        {
                            Position = tile,
                            IsDeployed = true,
                        },
                    },
                };

                var threat = new HashSet<Coord>(Threat.ForUnit(probe, probe.Units[0]));
                int hit = deploy.Count(threat.Contains);

                if (hit < best)
                {
                    best = hit;
                    bestTiles.Clear();
                }

                if (hit == best)
                {
                    bestTiles.Add(tile);
                }
            }

            Console.WriteLine($"      best possible: threatens {best} of {deploy.Count} deploy tiles");
            foreach (var tile in bestTiles)
            {
                var probe = start with
                {
                    Units = new[]
                    {
                        Unit.FromTemplate(new UnitId(0), kind, Team.Enemy) with
                        {
                            Position = tile,
                            IsDeployed = true,
                        },
                    },
                };

                var threat = new HashSet<Coord>(Threat.ForUnit(probe, probe.Units[0]));
                Console.WriteLine($"        from {tile} → hits {string.Join(" ", deploy.Where(threat.Contains))}");
            }
        }
    }

    /// <summary>
    /// Sweeps an archetype's Move on one board and reports how many clean placements each value
    /// buys. Answers "how much mobility has to come off" with a number instead of a guess.
    /// </summary>
    internal static void Sweep(string id, UnitKind kind)
    {
        var fight = FightLibrary.ById(id);
        var start = Game.Start(fight, seed: 0).NewState;

        var deploy = new List<Coord>();
        deploy.AddRange(fight.DeploymentZoneA);
        deploy.AddRange(fight.DeploymentZoneB);

        var template = UnitTemplate.For(kind);
        Console.WriteLine($"{id}: {kind} reach {template.BasicReach}, deploy {string.Join(" ", deploy)}");
        Console.WriteLine();

        for (int move = template.Move; move >= 0; move--)
        {
            var safe = new List<Coord>();

            foreach (var tile in fight.Board.AllCoords())
            {
                if (fight.Board.At(tile) is not TileType.Open and not TileType.HighGround
                    || deploy.Contains(tile))
                {
                    continue;
                }

                // Move is read off the template, so the sweep fakes it by capping the reachable set
                // at the value under test rather than by mutating a shared stat block.
                var probe = start with
                {
                    Units = new[]
                    {
                        Unit.FromTemplate(new UnitId(0), kind, Team.Enemy) with
                        {
                            Position = tile,
                            IsDeployed = true,
                        },
                    },
                };

                var enemy = probe.Units[0];
                var stands = new List<Coord> { tile };
                foreach (var pair in Movement.Reachable(probe, enemy))
                {
                    if (pair.Value.Cost <= move)
                    {
                        stands.Add(pair.Key);
                    }
                }

                var threat = new HashSet<Coord>();
                foreach (var stand in stands)
                {
                    foreach (var t in Combat.RangeTiles(probe, enemy with { Position = stand }))
                    {
                        threat.Add(t);
                    }
                }

                if (!deploy.Any(threat.Contains))
                {
                    safe.Add(tile);
                }
            }

            Console.WriteLine($"  move {move} → {safe.Count,2} clean placement(s)"
                + (safe.Count > 0 ? "   " + string.Join(" ", safe.Take(14)) : string.Empty));
        }
    }

    /// <summary>
    /// Finds seeds where the first-legal policy plays a whole run into a loss. The run tests need
    /// one: a loss the engine reaches on its own is the only kind worth asserting on, and which
    /// seeds produce one moves whenever a board changes.
    /// </summary>
    internal static void LosingSeeds(int tries)
    {
        Console.WriteLine("seed  outcome     cleared  stopped  why");
        for (int seed = 1; seed <= tries; seed++)
        {
            var report = RunHarness.Play(new FirstLegalPolicy(), seed, maxCommands: 40000);
            Console.WriteLine(
                $"{seed,4}  {report.Outcome,-10}  {report.FightsWon,7}  {report.EndedAtNode,7}  {report.Reason}");
        }
    }

    private static void Section(string title, IEnumerable<FightDefinition> fights)
    {
        Console.WriteLine("## " + title);
        Console.WriteLine();

        int ok = 0, bad = 0;
        foreach (var fight in fights.OrderBy(f => f.Number).ThenBy(f => f.Id, StringComparer.Ordinal))
        {
            var failures = Threat.UnsafeSides(fight);
            var state = Game.Start(fight, seed: 0).NewState;
            int threatened = Threat.DamageRound1(state).Count;
            int shoved = Threat.DisplacementRound1(state).Count;

            if (failures.Count == 0)
            {
                ok++;
                int safeA = Threat.SafeDeploymentTiles(state, Team.PlayerA).Count;
                int safeB = Threat.SafeDeploymentTiles(state, Team.PlayerB).Count;
                bool strict = safeA == fight.DeploymentZoneA.Count
                    && safeB == fight.DeploymentZoneB.Count;

                Console.WriteLine($"  ok    {fight.Id,-28} {threatened,3} threatened"
                    + $"  A {safeA}/{fight.DeploymentZoneA.Count} B {safeB}/{fight.DeploymentZoneB.Count}"
                    + (strict ? "  STRICT" : string.Empty)
                    + (shoved > 0 ? $"  ({shoved} shove-only)" : string.Empty));
                continue;
            }

            bad++;
            Console.WriteLine($"  FAIL  {fight.Id,-28} {threatened,3} tiles threatened — "
                + string.Join("; ", failures));
        }

        Console.WriteLine();
        Console.WriteLine($"  {ok} ok, {bad} failing");
    }
}
