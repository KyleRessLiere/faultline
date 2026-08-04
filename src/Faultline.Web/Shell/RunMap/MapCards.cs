using System;
using System.Collections.Generic;
using Faultline.Core;

namespace Faultline.Web.Shell.RunMap;

/// <summary>
/// Turns an <see cref="ActMap"/> and a run's <see cref="MapState"/> into the cards the map screen
/// draws.
/// </summary>
/// <remarks>
/// <para>
/// The whole of the map screen's thinking, in one testable place and out of the markup. Every
/// question it answers is answered by asking Core: which nodes exist and in what column
/// (<see cref="ActMap.Nodes"/>), which are doors from here (<see cref="ActMap.Successors"/>), which
/// have been stood on (<see cref="MapState.Visited"/>), what a fight's objective is
/// (<see cref="FightDefinition.Objective"/>), and whether a mark can be paid
/// (<see cref="RewardMark.Payable"/>).
/// </para>
/// <para>
/// Nothing here decides legality. Whether a door may be taken, and whether taking it is a vote or a
/// walk, is <see cref="Campaign"/>'s and is read off <see cref="RunState.Phase"/>.
/// </para>
/// </remarks>
public static class MapCards
{
    /// <summary>Builds every node of the map, in authored order.</summary>
    /// <param name="map">The act map.</param>
    /// <param name="where">Where the run stands, or <c>null</c> before a run has started.</param>
    /// <returns>One card per node.</returns>
    public static IReadOnlyList<MapCard> Build(ActMap map, MapState? where)
    {
        if (map is null)
        {
            throw new ArgumentNullException(nameof(map));
        }

        var fights = CuratedSet.Active();
        var doors = where is null
            ? Array.Empty<string>()
            : map.Successors(where.CurrentNodeId);

        var cards = new List<MapCard>(map.Nodes.Count);

        foreach (var node in map.Nodes)
        {
            bool isDoor = Contains(doors, node.Id);
            fights.TryGetValue(node.FightId, out var fight);

            cards.Add(new MapCard
            {
                NodeId = node.Id,
                Label = node.Label,
                Column = node.Column,
                Type = node.Type,
                Lane = node.Lane,
                Icon = IconFor(node, fight),
                TypeName = TypeNameFor(node, fight),
                State = StateFor(node, where, isDoor),
                FightId = node.FightId,
                FightName = fight?.Name ?? string.Empty,
                Gilt = GiltFor(node.Reward),
                Promise = PromiseFor(node.Reward),

                // The spoiler rule, enforced by not having the data rather than by remembering not
                // to print it: only a door one step away carries a roster.
                Roster = isDoor ? RosterFor(fight) : Array.Empty<RosterLine>(),
            });
        }

        return cards;
    }

    /// <summary>
    /// Whether a node's mark may be drawn as a gilt edge.
    /// </summary>
    /// <remarks>
    /// <b>The promise rule (MASTER_DESIGN §8.5).</b> A gilt edge means a legendary is literally
    /// there — promise, not probability. The only question asked here is whether the run can hand it
    /// over, which is <see cref="RewardMark.Payable"/>. The mark's
    /// <see cref="RewardMark.Kind"/> is deliberately not consulted: reading the kind and drawing
    /// something anyway is exactly how a screen ends up promising what the game cannot grant. When
    /// the legendary pools ship, <c>Payable</c> turns true and the gilt lights up with the payout,
    /// because they are the same flag.
    /// </remarks>
    /// <param name="mark">The node's mark, or <c>null</c>.</param>
    /// <returns>Whether to gild.</returns>
    public static bool GiltFor(RewardMark? mark) => mark is { Payable: true };

    /// <summary>What a payable mark promises, in words. Empty whenever it is not payable.</summary>
    /// <param name="mark">The node's mark, or <c>null</c>.</param>
    /// <returns>The promise, or an empty string.</returns>
    public static string PromiseFor(RewardMark? mark)
    {
        if (!GiltFor(mark))
        {
            return string.Empty;
        }

        string prize = mark!.Kind switch
        {
            RewardMarkKind.LegendaryPick => "permanent legendaries",
            RewardMarkKind.LegendaryConsumablePick => "legendary consumables",
            _ => "prizes",
        };

        return "Pick " + mark.Pick + " of " + mark.From + " " + prize + ".";
    }

    /// <summary>The glyph a node wears.</summary>
    /// <param name="node">The map node.</param>
    /// <param name="fight">Its fight, when it plays one and the file is loaded.</param>
    /// <returns>The icon.</returns>
    public static MapIcon IconFor(MapNode node, FightDefinition? fight)
    {
        if (node is null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        return node.Type switch
        {
            MapNodeType.Boss => MapIcon.Boss,
            MapNodeType.Elite => MapIcon.Skull,
            MapNodeType.Rest => MapIcon.Campfire,
            MapNodeType.Event => MapIcon.Question,
            _ => IconForObjective(fight?.Objective.Kind ?? ObjectiveKind.KillAll),
        };
    }

    /// <summary>
    /// Which of §8.5's four combat glyphs an objective wears.
    /// </summary>
    /// <remarks>
    /// The design doc names swords, shield, broken gate and hourglass, and Core has six objective
    /// kinds. Hold and Protect are both "defend" and share the shield; Destroy and Reach are both
    /// "raid" — break the thing, or get through to it — and share the gate.
    /// </remarks>
    /// <param name="kind">What winning means.</param>
    /// <returns>The icon.</returns>
    public static MapIcon IconForObjective(ObjectiveKind kind) => kind switch
    {
        ObjectiveKind.Survive => MapIcon.Hourglass,
        ObjectiveKind.Hold => MapIcon.Shield,
        ObjectiveKind.Protect => MapIcon.Shield,
        ObjectiveKind.Destroy => MapIcon.Gate,
        ObjectiveKind.Reach => MapIcon.Gate,
        _ => MapIcon.Swords,
    };

    /// <summary>The character a glyph is drawn with.</summary>
    /// <param name="icon">The icon.</param>
    /// <returns>One glyph.</returns>
    public static string Glyph(MapIcon icon) => icon switch
    {
        MapIcon.Swords => "⚔",
        MapIcon.Shield => "\U0001F6E1",
        MapIcon.Gate => "⛩",
        MapIcon.Hourglass => "⌛",
        MapIcon.Skull => "\U0001F480",
        MapIcon.Question => "?",
        MapIcon.Campfire => "\U0001F525",
        MapIcon.Boss => "♛",
        _ => "⚔",
    };

    /// <summary>The CSS class a glyph is drawn under, and what a test looks for.</summary>
    /// <param name="icon">The icon.</param>
    /// <returns>A lower-case class name.</returns>
    public static string IconClass(MapIcon icon) => icon switch
    {
        MapIcon.Swords => "swords",
        MapIcon.Shield => "shield",
        MapIcon.Gate => "gate",
        MapIcon.Hourglass => "hourglass",
        MapIcon.Skull => "skull",
        MapIcon.Question => "question",
        MapIcon.Campfire => "campfire",
        MapIcon.Boss => "boss-sigil",
        _ => "swords",
    };

    /// <summary>The CSS class a node's state is drawn under.</summary>
    /// <param name="state">The state.</param>
    /// <returns>A lower-case class name.</returns>
    public static string StateClass(MapNodeState state) => state switch
    {
        MapNodeState.Visited => "visited",
        MapNodeState.Current => "current",
        MapNodeState.Reachable => "reachable",
        _ => "ahead",
    };

    /// <summary>What a node is, in words. Shown on every node, always.</summary>
    /// <param name="node">The map node.</param>
    /// <param name="fight">Its fight, when it plays one.</param>
    /// <returns>One short phrase.</returns>
    public static string TypeNameFor(MapNode node, FightDefinition? fight)
    {
        if (node is null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        return node.Type switch
        {
            MapNodeType.Boss => "Boss",
            MapNodeType.Elite => "Elite — " + ObjectiveName(fight?.Objective.Kind ?? ObjectiveKind.KillAll),
            MapNodeType.Rest => "Camp",
            MapNodeType.Event => "Event",
            _ => "Fight — " + ObjectiveName(fight?.Objective.Kind ?? ObjectiveKind.KillAll),
        };
    }

    /// <summary>What winning a board means, in three words or fewer.</summary>
    /// <param name="kind">Objective kind.</param>
    /// <returns>The phrase.</returns>
    public static string ObjectiveName(ObjectiveKind kind) => kind switch
    {
        ObjectiveKind.Survive => "survive",
        ObjectiveKind.Hold => "hold the ground",
        ObjectiveKind.Protect => "protect",
        ObjectiveKind.Destroy => "break it down",
        ObjectiveKind.Reach => "get through",
        _ => "kill all",
    };

    /// <summary>
    /// Who is on a board, grouped by archetype and by when they arrive. Setup first, then each wave
    /// in round order.
    /// </summary>
    /// <param name="fight">The fight, or <c>null</c> when its file is not loaded.</param>
    /// <returns>The roster, empty when there is no fight to read.</returns>
    public static IReadOnlyList<RosterLine> RosterFor(FightDefinition? fight)
    {
        if (fight is null)
        {
            return Array.Empty<RosterLine>();
        }

        var lines = new List<RosterLine>();
        Tally(lines, fight.Enemies, 0);

        foreach (var wave in fight.Waves)
        {
            Tally(lines, wave.Arrivals, wave.Round);
        }

        return lines;
    }

    private static void Tally(List<RosterLine> lines, IReadOnlyList<EnemySpawn> spawns, int round)
    {
        int first = lines.Count;

        foreach (var spawn in spawns)
        {
            bool merged = false;

            for (int i = first; i < lines.Count; i++)
            {
                if (lines[i].Kind == spawn.Kind)
                {
                    lines[i] = lines[i] with { Count = lines[i].Count + 1 };
                    merged = true;
                    break;
                }
            }

            if (!merged)
            {
                lines.Add(new RosterLine(spawn.Kind, 1, round));
            }
        }
    }

    private static MapNodeState StateFor(MapNode node, MapState? where, bool isDoor)
    {
        if (where is null)
        {
            return MapNodeState.Ahead;
        }

        if (string.Equals(where.CurrentNodeId, node.Id, StringComparison.Ordinal))
        {
            return MapNodeState.Current;
        }

        if (where.Visited(node.Id))
        {
            return MapNodeState.Visited;
        }

        return isDoor ? MapNodeState.Reachable : MapNodeState.Ahead;
    }

    private static bool Contains(IReadOnlyList<string> ids, string id)
    {
        foreach (string candidate in ids)
        {
            if (string.Equals(candidate, id, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
