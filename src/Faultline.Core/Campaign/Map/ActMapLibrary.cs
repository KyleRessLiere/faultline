using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// The act maps the game ships. One, for now: Act 1, the Warrens — hand-authored, exactly the
    /// graph MASTER_DESIGN §8 prints.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Authored, not generated.</b> §8.5 asks for a seeded, constraint-driven generator that emits
    /// a proof log saying which constraint bound where. That is acts-2-and-3 work. Act 1 is the
    /// teaching zone and always first, so it is the one act whose exact sequence the whole game is
    /// tuned against; generating it would have made "the fight after the shrine" a thing nobody can
    /// point at. <see cref="ActMap.Validate"/> holds the constraints the generator will have to
    /// satisfy, so the authored map is also the generator's acceptance test.
    /// </para>
    /// <para>
    /// <b>The lanes.</b> The safe lane is plainer fights and the act's only mid-lane campfire; the
    /// hungry lane has no campfire at all and carries the act's one marked destination, the high road.
    /// The <c>?</c> in column 3 belongs to neither and is the act's <em>single crossing</em>: it is
    /// the only node with a door into both lanes, so committing to a lane at column 2 is a real
    /// commitment and there is exactly one place to change your mind. §8.5 wants 1–2 crossings per
    /// act; this is one.
    /// </para>
    /// <para>
    /// <b>The floors.</b> The pre-boss column holds a campfire reachable from every lane. The
    /// HP-priced event is not on a zero-Rest lane: from the pool a campfire is always one door away.
    /// Both are pinned by tests, not by care.
    /// </para>
    /// <para>
    /// <b>The dependency.</b> <c>broken-bridge</c> sits on the hungry lane at column 3 and is, at the
    /// time this map was authored, structurally severed — two islands with no crossing — and being
    /// repaired elsewhere. The map references it by id and nothing here depends on its terrain; when
    /// the repair lands, the hungry lane's column 3 becomes playable with no change to this file.
    /// </para>
    /// </remarks>
    public static class ActMapLibrary
    {
        /// <summary>Id of Act 1's map.</summary>
        public const string Act1Id = "act-1-warrens";

        private static readonly ActMap Act1Map = BuildAct1();

        /// <summary>Act 1 — the Warrens, the teaching zone, always first (MASTER_DESIGN §10).</summary>
        public static ActMap Act1 => Act1Map;

        /// <summary>Every act map, in order.</summary>
        /// <returns>The maps.</returns>
        public static IReadOnlyList<ActMap> All() => new[] { Act1Map };

        /// <summary>Finds a map by id.</summary>
        /// <param name="id">Map id.</param>
        /// <returns>The map.</returns>
        /// <exception cref="ArgumentException">No map has that id.</exception>
        public static ActMap ById(string id)
        {
            foreach (var map in All())
            {
                if (string.Equals(map.Id, id, StringComparison.Ordinal))
                {
                    return map;
                }
            }

            throw new ArgumentException("No act map with id '" + id + "'.", nameof(id));
        }

        private static ActMap BuildAct1() => new ActMap
        {
            Id = Act1Id,
            Name = "The Warrens",
            StartNodeId = "c1-first-contact",

            Nodes = new[]
            {
                // Column 1 — the control group. No lane yet; the vote at its exit is the commitment.
                Fight("c1-first-contact", 0, MapLane.Neutral, "first-contact", "First Contact"),

                // Column 2 — the fork. Bait and Break teaches the shove; the Teeth is the hungrier
                // opening, and the only way onto the lane the high road is on.
                Fight("c2-bait-and-break", 1, MapLane.Safe, "cb-06-bait-and-break", "Bait and Break"),
                Fight("c2-the-teeth", 1, MapLane.Hungry, "the-teeth", "The Teeth"),

                // Column 3 — two fights and the act's single crossing between them.
                Fight("c3-the-shrine", 2, MapLane.Safe, "the-shrine", "The Shrine"),
                new MapNode
                {
                    Id = "c3-molting-pool",
                    Column = 2,
                    Type = MapNodeType.Event,
                    Lane = MapLane.Neutral,
                    EventId = EventLibrary.MoltingPoolId,
                    Label = "?",
                },
                Fight("c3-broken-bridge", 2, MapLane.Hungry, "broken-bridge", "Broken Bridge"),

                // Column 4 — the gradient made literal: a campfire on one side, the act's only marked
                // destination on the other. §8.6 pays the high road a permanent legendary, 1 of 2.
                new MapNode
                {
                    Id = "c4-rest",
                    Column = 3,
                    Type = MapNodeType.Rest,
                    Lane = MapLane.Safe,
                    Label = "Camp",
                },
                new MapNode
                {
                    Id = "c4-high-road",
                    Column = 3,
                    Type = MapNodeType.Elite,
                    Lane = MapLane.Hungry,
                    FightId = "high-road",
                    Reward = RewardMark.LegendaryPickOneOfTwo,
                    Label = "High Road",
                },

                // Column 5 — the last fight of each lane.
                Fight("c5-break-the-gate", 4, MapLane.Safe, "break-the-gate", "Break the Gate"),
                Fight("c5-the-trench", 4, MapLane.Hungry, "hz-09-the-trench", "The Trench"),

                // Column 6 — the floor. Reachable from every lane, by law (§8.5).
                new MapNode
                {
                    Id = "c6-rest",
                    Column = 5,
                    Type = MapNodeType.Rest,
                    Lane = MapLane.Neutral,
                    Label = "Camp",
                },

                // Column 7 — the boss, at the end of every lane.
                new MapNode
                {
                    Id = "c7-quarry-king",
                    Column = 6,
                    Type = MapNodeType.Boss,
                    Lane = MapLane.Neutral,
                    FightId = "quarry-king",
                    Label = "The Quarry King",
                },
            },

            Edges = new[]
            {
                new MapEdge("c1-first-contact", "c2-bait-and-break"),
                new MapEdge("c1-first-contact", "c2-the-teeth"),

                new MapEdge("c2-bait-and-break", "c3-the-shrine"),
                new MapEdge("c2-bait-and-break", "c3-molting-pool"),

                new MapEdge("c2-the-teeth", "c3-molting-pool"),
                new MapEdge("c2-the-teeth", "c3-broken-bridge"),

                new MapEdge("c3-the-shrine", "c4-rest"),

                // The crossing, both ways out of it: the pool is where a hungry run can buy a campfire
                // and a safe run can buy the elite. It is also why the HP-priced event is never on a
                // zero-Rest lane — a campfire is always one door from it.
                new MapEdge("c3-molting-pool", "c4-rest"),
                new MapEdge("c3-molting-pool", "c4-high-road"),

                new MapEdge("c3-broken-bridge", "c4-high-road"),

                new MapEdge("c4-rest", "c5-break-the-gate"),
                new MapEdge("c4-high-road", "c5-the-trench"),

                new MapEdge("c5-break-the-gate", "c6-rest"),
                new MapEdge("c5-the-trench", "c6-rest"),

                new MapEdge("c6-rest", "c7-quarry-king"),
            },
        };

        private static MapNode Fight(string id, int column, MapLane lane, string fightId, string label) =>
            new MapNode
            {
                Id = id,
                Column = column,
                Type = MapNodeType.Fight,
                Lane = lane,
                FightId = fightId,
                Label = label,
            };
    }
}
