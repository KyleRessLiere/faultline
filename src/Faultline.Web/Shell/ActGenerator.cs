using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Faultline.Core;

namespace Faultline.Web.Shell;

/// <summary>
/// Builds an act graph from constraints and a seed, and says which constraint bound where.
/// </summary>
/// <remarks>
/// <para>
/// <b>The seed selects; it never places.</b> MASTER_DESIGN §8.5 and F3 are unchanged on this: nothing
/// here scatters terrain, rolls a stat or hides a die. Every board, every scene and every enemy is
/// authored; the generator only chooses which authored thing fills a node and which doors exist. A
/// generated act is therefore made of exactly the same content a hand-authored one is.
/// </para>
/// <para>
/// <b>The proof log is not optional.</b> §8.5 requires a generated act to emit which constraint bound
/// where, and the reason is practical: a constraint solver that refuses silently costs a week. Every
/// floor below writes a line when it binds, and the summary lines at the end carry the numbers a
/// designer is actually comparing — nodes, doors, crossings and how often a board repeats.
/// </para>
/// <para>
/// <b>Where this lives, and why it is not in Core.</b> §8.5's generator is run-level design and will
/// belong to Core when an act ships generated. Today it fills a draft in a browser — the shipped
/// Warrens is still <see cref="ActMapLibrary"/>'s hand-authored graph and nothing here touches it — so
/// it sits in the shell where a dev tool sits. <see cref="ActMap.Validate"/> is Core's, and is the
/// acceptance test this runs against itself.
/// </para>
/// </remarks>
public static class ActGenerator
{
    /// <summary>The board every act opens on. Fixed, never drawn.</summary>
    /// <remarks>
    /// §10 makes Act 1 the teaching zone and the opener its control group. A drawn opener would make
    /// "the first fight" a different fight per run, which is the one thing a teaching column cannot be.
    /// </remarks>
    public const string OpenerId = "first-contact";

    /// <summary>The board the act terminates on.</summary>
    public const string BossId = "quarry-king";

    /// <summary>The boards an elite node may field.</summary>
    /// <remarks>
    /// One, because one is what is authored. Inventing which other boards are "elite enough" would be
    /// content design, and this file does not get to make that call.
    /// </remarks>
    public static IReadOnlyList<string> ElitePool { get; } = new[] { "high-road" };

    /// <summary>Generates an act to a sizing.</summary>
    /// <param name="shape">The sizing. Clamped to what a graph can actually be.</param>
    /// <param name="seed">Run seed. The same seed and shape always produce the same act.</param>
    /// <param name="name">What to call it.</param>
    /// <returns>The draft, with its proof log filled in.</returns>
    public static ActDraft Generate(ActShape shape, int seed, string name)
    {
        if (shape is null)
        {
            throw new ArgumentNullException(nameof(shape));
        }

        var draft = new ActDraft { Name = name };
        var rng = new SeededRng(seed);
        var proof = draft.Proof;

        int columns = Clamp(shape.Columns, 3, 24);
        int minWidth = Clamp(shape.MinWidth, 1, 6);
        int maxWidth = Clamp(shape.MaxWidth, minWidth, 6);
        int minDoors = Clamp(shape.MinDoors, 1, 4);
        int maxDoors = Clamp(shape.MaxDoors, minDoors, 4);

        int last = columns - 1;
        int preBoss = last - 1;

        proof.Add(
            "seed " + seed.ToString(CultureInfo.InvariantCulture)
            + " · " + columns.ToString(CultureInfo.InvariantCulture) + " columns"
            + " · " + minWidth + "–" + maxWidth + " wide"
            + " · " + minDoors + "–" + maxDoors + " doors per node");

        if (columns != shape.Columns || maxWidth != shape.MaxWidth)
        {
            proof.Add("sizing — clamped: an act needs at least three columns and no column may exceed six.");
        }

        Skeleton(draft, rng, proof, columns, minWidth, maxWidth);
        PlaceRests(draft, rng, proof, shape.Rests, preBoss);
        PlaceEvents(draft, rng, proof, shape.Events, preBoss);
        PlaceElite(draft, rng, proof, preBoss);
        KeepColumnsFightable(draft, proof, preBoss);
        DealBoards(draft, rng, proof, shape.WholeLibrary, last);
        Wire(draft, rng, proof, minDoors, maxDoors, last);
        Summarise(draft, proof);

        return draft;
    }

    // ---- The skeleton --------------------------------------------------------------------------

    /// <summary>Lays the columns out: the fixed opener, the middle, the floor and the boss.</summary>
    private static void Skeleton(
        ActDraft draft, SeededRng rng, List<string> proof, int columns, int minWidth, int maxWidth)
    {
        int last = columns - 1;
        int preBoss = last - 1;

        var opener = draft.Add(0, ActDraft.NodeKind.Fight);
        opener.Id = OpenerId;
        proof.Add("column 1 — bound: the opener is fixed, never drawn (" + OpenerId + ").");

        for (int c = 1; c < preBoss; c++)
        {
            int width = minWidth + rng.Next(maxWidth - minWidth + 1);
            for (int r = 0; r < width; r++)
            {
                draft.Add(c, ActDraft.NodeKind.Fight).Lane = LaneFor(r, width);
            }
        }

        draft.Add(preBoss, ActDraft.NodeKind.Rest);
        proof.Add(
            "column " + Col(preBoss)
            + " — bound: the pre-boss Rest is a column of its own, so every route reaches it.");

        var boss = draft.Add(last, ActDraft.NodeKind.Boss);
        boss.Id = BossId;
        proof.Add("column " + Col(last) + " — bound: the boss is the act's one terminal.");
    }

    /// <summary>
    /// Which lane a row stands in. The top row is the safe side and the bottom the hungry one, which
    /// is how §8.5's comfort gradient reads on a screen that draws columns top to bottom.
    /// </summary>
    private static MapLane LaneFor(int row, int width)
    {
        if (width < 2)
        {
            return MapLane.Neutral;
        }

        if (row == 0)
        {
            return MapLane.Safe;
        }

        return row == width - 1 ? MapLane.Hungry : MapLane.Neutral;
    }

    // ---- The floors ----------------------------------------------------------------------------

    /// <summary>Places the mid-act Still Ponds. The safe lane carries them (§8.5).</summary>
    private static void PlaceRests(
        ActDraft draft, SeededRng rng, List<string> proof, int wanted, int preBoss)
    {
        for (int i = 0; i < wanted; i++)
        {
            var candidates = new List<int>();
            for (int c = 1; c < preBoss; c++)
            {
                var column = draft.ColumnAt(c);

                // Never a whole column of pond, never two non-fights in one column, and never two
                // pond columns in a row — a run that rests twice without a fight between them has
                // stopped being an act.
                if (column.Count < 2 || column.Any(n => n.Kind != ActDraft.NodeKind.Fight))
                {
                    continue;
                }

                if (HasRest(draft, c - 1) || HasRest(draft, c + 1))
                {
                    continue;
                }

                candidates.Add(c);
            }

            if (candidates.Count == 0)
            {
                proof.Add(
                    "rests — bound: only " + i + " of " + wanted
                    + " mid-act Ponds fit; no column was wide enough and far enough from another.");
                return;
            }

            int pick = candidates[rng.Next(candidates.Count)];
            var chosen = draft.ColumnAt(pick);
            var target = chosen.FirstOrDefault(n => n.Lane == MapLane.Safe) ?? chosen[0];
            target.Kind = ActDraft.NodeKind.Rest;

            proof.Add("column " + Col(pick) + " — a mid-act Still Pond, on the safe lane.");
        }
    }

    /// <summary>
    /// Places the event nodes, under §8.5's floor: an HP-priced event never stands where a route has
    /// no Pond behind it.
    /// </summary>
    /// <remarks>
    /// Satisfied the way the authored Warrens satisfies it — a Pond is always one door away — and with
    /// one extra condition: the column BEFORE an event must be all combat. Without it a route could
    /// thread event → pond → event and spend three columns without a fight, which is the "no route is
    /// all-Rest or all-Event across three columns" floor. One condition closes both.
    /// </remarks>
    private static void PlaceEvents(
        ActDraft draft, SeededRng rng, List<string> proof, int wanted, int preBoss)
    {
        var scene = EventLibrary.All().Count > 0 ? EventLibrary.All()[0].Id : string.Empty;

        for (int i = 0; i < wanted; i++)
        {
            var candidates = new List<int>();
            for (int c = 1; c < preBoss; c++)
            {
                var column = draft.ColumnAt(c);
                if (column.Count < 2 || column.Any(n => n.Kind != ActDraft.NodeKind.Fight))
                {
                    continue;
                }

                if (!HasRest(draft, c + 1))
                {
                    continue;
                }

                if (draft.ColumnAt(c - 1).Any(n => !n.IsCombat))
                {
                    continue;
                }

                candidates.Add(c);
            }

            if (candidates.Count == 0)
            {
                proof.Add(
                    "events — bound: only " + i + " of " + wanted
                    + " placed. An HP-priced event needs a Pond one door away and a fight behind it, "
                    + "and no further column offered both.");
                return;
            }

            int pick = candidates[rng.Next(candidates.Count)];
            var chosen = draft.ColumnAt(pick);

            // The crossing belongs to neither lane, which is what makes it the place to change your
            // mind — so it takes a middle row where there is one.
            var target = chosen.FirstOrDefault(n => n.Lane == MapLane.Neutral) ?? chosen[0];
            target.Kind = ActDraft.NodeKind.Event;
            target.Id = scene;
            target.Lane = MapLane.Neutral;

            proof.Add(
                "column " + Col(pick)
                + " — an event, bound by the HP floor: a Pond stands one door away in column "
                + Col(pick + 1) + ".");
        }
    }

    /// <summary>Places the one mid-act node that carries a guaranteed reward.</summary>
    private static void PlaceElite(ActDraft draft, SeededRng rng, List<string> proof, int preBoss)
    {
        var candidates = new List<int>();
        for (int c = 1; c < preBoss; c++)
        {
            if (draft.ColumnAt(c).Any(n => n.Kind == ActDraft.NodeKind.Fight))
            {
                candidates.Add(c);
            }
        }

        if (candidates.Count == 0)
        {
            proof.Add("reward — bound: the act has no mid-act combat column, so nothing carries the mark.");
            return;
        }

        // The middle half, so the promise is not the second node of the act or the last thing before
        // the floor. Falls back to anywhere when the act is too short to have a middle.
        var middle = candidates.Where(c => c * 4 >= preBoss && c * 4 <= preBoss * 3).ToList();
        var from = middle.Count > 0 ? middle : candidates;
        int pick = from[rng.Next(from.Count)];

        var column = draft.ColumnAt(pick);
        var target = column.FirstOrDefault(n => n.Kind == ActDraft.NodeKind.Fight && n.Lane == MapLane.Hungry)
            ?? column.First(n => n.Kind == ActDraft.NodeKind.Fight);

        target.Kind = ActDraft.NodeKind.Elite;
        target.Lane = MapLane.Hungry;
        target.Gilt = true;

        proof.Add(
            "column " + Col(pick)
            + " — the act's guaranteed reward, on the hungry lane. Risk buys rarity as geography.");
    }

    /// <summary>Belt and braces: no middle column may end up with nothing to fight in it.</summary>
    private static void KeepColumnsFightable(ActDraft draft, List<string> proof, int preBoss)
    {
        for (int c = 1; c < preBoss; c++)
        {
            var column = draft.ColumnAt(c);
            if (column.Count > 0 && column.All(n => !n.IsCombat))
            {
                column[0].Kind = ActDraft.NodeKind.Fight;
                proof.Add("column " + Col(c) + " — bound: a column with no combat in it was turned back.");
            }
        }
    }

    // ---- The content ---------------------------------------------------------------------------

    /// <summary>Fills every combat node with an authored board.</summary>
    /// <remarks>
    /// <b>The repetition debt, made visible.</b> An act of this length against a pool of eight boards
    /// will field the same board twice, and that is knowingly accepted rather than an oversight — the
    /// exit condition is board editions plus objective and roster swaps on fixed terrain. Until then
    /// the rule is: exhaust the pool before repeating, and never repeat in adjacent columns. Every time
    /// the pool has to be recycled the proof log says so, so the debt stays measured rather than
    /// assumed.
    /// </remarks>
    private static void DealBoards(
        ActDraft draft, SeededRng rng, List<string> proof, bool wholeLibrary, int last)
    {
        var pool = wholeLibrary ? LibraryPool() : WarrensPool();
        if (pool.Count == 0)
        {
            proof.Add("boards — bound: the pool is empty; combat nodes were left unchosen.");
            return;
        }

        proof.Add(
            "boards — drawing from " + pool.Count.ToString(CultureInfo.InvariantCulture)
            + (wholeLibrary ? " boards (the whole active library)." : " boards (the Warrens' pool)."));

        var elites = ElitePool;
        var used = new List<string>();
        int recycles = 0;

        for (int c = 1; c < last; c++)
        {
            var previous = draft.ColumnAt(c - 1).Select(n => n.Id).Where(id => id.Length > 0).ToList();
            var here = new List<string>();

            foreach (var node in draft.ColumnAt(c))
            {
                if (node.Kind == ActDraft.NodeKind.Elite)
                {
                    node.Id = elites[rng.Next(elites.Count)];
                    here.Add(node.Id);
                    continue;
                }

                if (node.Kind != ActDraft.NodeKind.Fight)
                {
                    continue;
                }

                var open = pool.Where(id => !previous.Contains(id) && !here.Contains(id)).ToList();
                if (open.Count == 0)
                {
                    open = pool.Where(id => !here.Contains(id)).ToList();
                    proof.Add(
                        "column " + Col(c)
                        + " — bound: the pool is too small to avoid an adjacent-column repeat.");
                }

                if (open.Count == 0)
                {
                    open = pool.ToList();
                }

                var fresh = open.Where(id => !used.Contains(id)).ToList();
                if (fresh.Count == 0)
                {
                    used.Clear();
                    recycles++;
                    fresh = open;
                }

                node.Id = fresh[rng.Next(fresh.Count)];
                used.Add(node.Id);
                here.Add(node.Id);
            }
        }

        if (recycles > 0)
        {
            proof.Add(
                "boards — the pool was exhausted " + recycles.ToString(CultureInfo.InvariantCulture)
                + " time(s); from there on the act repeats boards at distance. Accepted debt, not an oversight.");
        }
    }

    /// <summary>The eight-ish boards the authored Warrens fields, read off the authored map.</summary>
    private static IReadOnlyList<string> WarrensPool() =>
        ActMapLibrary.Act1.Nodes
            .Where(n => n.Type == MapNodeType.Fight
                && n.FightId.Length > 0
                && !string.Equals(n.FightId, OpenerId, StringComparison.Ordinal))
            .Select(n => n.FightId)
            .Distinct(StringComparer.Ordinal)
            .ToList();

    /// <summary>Every active board that is not already spoken for by the opener, elite or boss.</summary>
    private static IReadOnlyList<string> LibraryPool() =>
        FightLibrary.All()
            .Select(f => f.Id)
            .Where(id => !string.Equals(id, OpenerId, StringComparison.Ordinal)
                && !string.Equals(id, BossId, StringComparison.Ordinal)
                && !ElitePool.Contains(id, StringComparer.Ordinal))
            .ToList();

    // ---- The doors -----------------------------------------------------------------------------

    /// <summary>Opens the forward doors, then repairs anything the roll left stranded.</summary>
    private static void Wire(
        ActDraft draft, SeededRng rng, List<string> proof, int minDoors, int maxDoors, int last)
    {
        for (int c = 0; c < last; c++)
        {
            var here = draft.ColumnAt(c);
            var next = draft.ColumnAt(c + 1);

            for (int r = 0; r < here.Count; r++)
            {
                int wanted = Math.Min(next.Count, minDoors + rng.Next(maxDoors - minDoors + 1));
                foreach (int target in Spread(Anchor(r, here.Count, next.Count), wanted, next.Count))
                {
                    here[r].Doors.Add(next[target].Key);
                }
            }

            // No unreachable nodes: a node with no door into it is not a hard act, it is a bug.
            for (int r = 0; r < next.Count; r++)
            {
                string key = next[r].Key;
                if (here.Any(n => n.Doors.Contains(key)))
                {
                    continue;
                }

                here[Anchor(r, next.Count, here.Count)].Doors.Add(key);
                proof.Add(
                    "column " + Col(c + 1)
                    + " — bound: node " + (r + 1) + " had no door into it, so one was opened.");
            }
        }

        // The HP floor again, this time as a door rather than as a placement: the Pond one column
        // ahead is only a floor if the event can actually reach it.
        foreach (var node in draft.Steps.Where(n => n.Kind == ActDraft.NodeKind.Event).ToList())
        {
            if (draft.DoorsOf(node).Any(d => d.Kind == ActDraft.NodeKind.Rest))
            {
                continue;
            }

            var pond = draft.ColumnAt(node.Column + 1)
                .FirstOrDefault(n => n.Kind == ActDraft.NodeKind.Rest);

            if (pond is null)
            {
                continue;
            }

            node.Doors.Add(pond.Key);
            proof.Add(
                "column " + Col(node.Column)
                + " — bound: the event's doors missed the Pond, so a door to it was opened.");
        }
    }

    /// <summary>Which row in the next column a row lines up with, so the graph reads as lanes.</summary>
    private static int Anchor(int row, int fromCount, int toCount)
    {
        if (toCount <= 1)
        {
            return 0;
        }

        if (fromCount <= 1)
        {
            return toCount / 2;
        }

        return row * (toCount - 1) / (fromCount - 1);
    }

    /// <summary>Rows outward from an anchor: the anchor, then below, then above, and so on.</summary>
    private static IEnumerable<int> Spread(int anchor, int count, int of)
    {
        var taken = new List<int>();

        for (int step = 0; taken.Count < count && step <= of; step++)
        {
            foreach (int candidate in step == 0 ? new[] { anchor } : new[] { anchor + step, anchor - step })
            {
                if (candidate >= 0 && candidate < of && !taken.Contains(candidate) && taken.Count < count)
                {
                    taken.Add(candidate);
                }
            }
        }

        return taken;
    }

    // ---- The numbers ---------------------------------------------------------------------------

    /// <summary>The summary half of the proof log: what the act came out as.</summary>
    private static void Summarise(ActDraft draft, List<string> proof)
    {
        int crossings = 0;
        foreach (var node in draft.Steps)
        {
            foreach (var door in draft.DoorsOf(node))
            {
                if ((node.Lane == MapLane.Safe && door.Lane == MapLane.Hungry)
                    || (node.Lane == MapLane.Hungry && door.Lane == MapLane.Safe))
                {
                    crossings++;
                }
            }
        }

        proof.Add("shape — " + draft.Shape() + ".");
        proof.Add("contents — " + draft.Length() + ".");

        var repetition = draft.Repetition();
        if (repetition.Length > 0)
        {
            proof.Add("boards — " + repetition);
        }

        proof.Add(
            "crossings — " + crossings.ToString(CultureInfo.InvariantCulture)
            + " door(s) join the safe lane to the hungry one. Every one of them is a fork the vote can split on.");

        var issues = draft.ToCampaign("generated").Map!.Validate();
        proof.Add(issues.Count == 0
            ? "linter — clean against Core's own ActMap.Validate."
            : "linter — " + issues.Count + " issue(s), first: " + issues[0]);
    }

    private static bool HasRest(ActDraft draft, int column) =>
        draft.ColumnAt(column).Any(n => n.Kind == ActDraft.NodeKind.Rest);

    /// <summary>Columns are zero-based in the data and one-based on a screen.</summary>
    private static string Col(int column) =>
        (column + 1).ToString(CultureInfo.InvariantCulture);

    private static int Clamp(int value, int low, int high) =>
        value < low ? low : value > high ? high : value;
}
