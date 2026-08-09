using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Faultline.Core;

namespace Faultline.Web.Shell;

/// <summary>
/// An act being built in the UI: columns of typed nodes and the doors between them, turned into a
/// <see cref="CampaignDefinition"/> the run layer walks like any other.
/// </summary>
/// <remarks>
/// <para>
/// <b>A graph, not a list.</b> v1 of this was an ordered list of steps, which could only ever build a
/// corridor — and a corridor cannot exercise the one thing an act map is for, the vote at a fork. The
/// draft now carries the same three facts <see cref="ActMap"/> carries: which column a node stands in,
/// what it is, and which nodes in the next column it opens onto. A hand-built line is the special case
/// where every column holds one node.
/// </para>
/// <para>
/// <b>Why this exists:</b> an event is a run node, so the only way to reach one was to play a campaign
/// until the map offered it. That made the one shipped event effectively untestable and made iterating
/// on act SHAPE — what follows what, and what forks where — a code change.
/// </para>
/// <para>
/// <b>What plays it is the shipped path.</b> <see cref="ToCampaign"/> emits a real <see cref="ActMap"/>,
/// so an act built here runs through <c>Campaign.Start</c> and the shared node handlers. That is the
/// only reason testing an event this way proves anything about the event.
/// </para>
/// </remarks>
public sealed class ActDraft
{
    /// <summary>What one node in the draft is. Mirrors <see cref="MapNodeType"/>.</summary>
    /// <remarks>
    /// The numbers are pinned because saved acts are read back by name and old saves must keep
    /// parsing; adding to the end is the only safe direction.
    /// </remarks>
    public enum NodeKind
    {
        /// <summary>An ordinary board.</summary>
        Fight = 0,

        /// <summary>An event scene.</summary>
        Event = 1,

        /// <summary>A Still Pond: heal or forge.</summary>
        Rest = 2,

        /// <summary>A board that costs more and pays more.</summary>
        Elite = 3,

        /// <summary>The act's terminal.</summary>
        Boss = 4,
    }

    /// <summary>One node of the act.</summary>
    public sealed class Step
    {
        /// <summary>Stable id within the draft. What a door and the emitted map refer to.</summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>Zero-based column. Doors always step exactly one column forward.</summary>
        public int Column { get; set; }

        /// <summary>What kind of node it is.</summary>
        public NodeKind Kind { get; set; } = NodeKind.Fight;

        /// <summary>The board id for combat, or the event id for an event. Unused for a rest.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Which side of the comfort gradient it stands on.</summary>
        public MapLane Lane { get; set; } = MapLane.Neutral;

        /// <summary>Whether the node carries the act's guaranteed reward mark.</summary>
        public bool Gilt { get; set; }

        /// <summary>
        /// Which band this node draws its board from (MASTER_DESIGN §8, locked ag), or
        /// <see cref="FightPool.None"/> for a node the generator did not band.
        /// </summary>
        /// <remarks>
        /// Recorded on the node rather than recomputed from its column, because the band is what the
        /// generator DECIDED — the proof log names it, and a reader working it back out from the
        /// column would be re-deriving a choice instead of reading it.
        /// </remarks>
        public FightPool Band { get; set; } = FightPool.None;

        /// <summary>
        /// Keys of the nodes in the next column this one opens onto. <b>Empty means every node in the
        /// next column</b> — which is what a hand-built act wants and what a saved v1 act becomes.
        /// </summary>
        public List<string> Doors { get; } = new();

        /// <summary>True when entering this node starts a fight.</summary>
        public bool IsCombat =>
            Kind == NodeKind.Fight || Kind == NodeKind.Elite || Kind == NodeKind.Boss;
    }

    private int _nextKey;

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Every node, in column order.</summary>
    public List<Step> Steps { get; } = new();

    /// <summary>The proof log the generator left, or empty for a hand-built act.</summary>
    /// <remarks>
    /// Kept on the draft rather than beside it because §8.5 requires a generated act to be able to say
    /// which constraint bound where, and an answer that lives somewhere else is one nobody reads.
    /// </remarks>
    public List<string> Proof { get; } = new();

    /// <summary>The four classes the act fields.</summary>
    /// <remarks>
    /// Editable because a squad is part of what an act is testing — an escort shape asks a different
    /// question of a Wardbearer than of an Archer. Defaults to the shipped four.
    /// </remarks>
    public List<UnitKind> Squad { get; } = new()
    {
        UnitKind.Vanguard,
        UnitKind.Threadcaster,
        UnitKind.Wardbearer,
        UnitKind.Archer,
    };

    /// <summary>How many columns the act has.</summary>
    public int ColumnCount
    {
        get
        {
            int highest = -1;
            foreach (var step in Steps)
            {
                if (step.Column > highest)
                {
                    highest = step.Column;
                }
            }

            return highest + 1;
        }
    }

    /// <summary>How many nodes the fattest column holds.</summary>
    public int WidestColumn
    {
        get
        {
            int widest = 1;
            for (int c = 0; c < ColumnCount; c++)
            {
                int here = ColumnAt(c).Count;
                if (here > widest)
                {
                    widest = here;
                }
            }

            return widest;
        }
    }

    /// <summary>Every node in one column, in draft order.</summary>
    /// <param name="column">Zero-based column index.</param>
    /// <returns>The column's nodes.</returns>
    public IReadOnlyList<Step> ColumnAt(int column) =>
        Steps.Where(s => s.Column == column).ToList();

    /// <summary>Finds a node by key.</summary>
    /// <param name="key">Node key.</param>
    /// <returns>The node, or null.</returns>
    public Step? ByKey(string key) =>
        Steps.FirstOrDefault(s => string.Equals(s.Key, key, StringComparison.Ordinal));

    /// <summary>Mints a key nothing else in this draft uses.</summary>
    /// <returns>The key.</returns>
    public string NextKey()
    {
        string key;
        do
        {
            key = "n" + (_nextKey++).ToString(CultureInfo.InvariantCulture);
        }
        while (ByKey(key) is not null);

        return key;
    }

    /// <summary>Adds a node to a column and returns it.</summary>
    /// <param name="column">Which column it stands in.</param>
    /// <param name="kind">What it is.</param>
    /// <returns>The new node.</returns>
    public Step Add(int column, NodeKind kind)
    {
        var step = new Step { Key = NextKey(), Column = column, Kind = kind };
        Steps.Add(step);
        Order();
        return step;
    }

    /// <summary>Removes a node and every door that pointed at it.</summary>
    /// <param name="key">Node key.</param>
    public void Remove(string key)
    {
        Steps.RemoveAll(s => string.Equals(s.Key, key, StringComparison.Ordinal));
        foreach (var step in Steps)
        {
            step.Doors.RemoveAll(d => string.Equals(d, key, StringComparison.Ordinal));
        }

        Compact();
    }

    /// <summary>Removes a whole column and closes the gap.</summary>
    /// <param name="column">Zero-based column index.</param>
    public void RemoveColumn(int column)
    {
        foreach (var step in ColumnAt(column).ToList())
        {
            Steps.RemoveAll(s => ReferenceEquals(s, step));
            foreach (var other in Steps)
            {
                other.Doors.RemoveAll(d => string.Equals(d, step.Key, StringComparison.Ordinal));
            }
        }

        foreach (var step in Steps)
        {
            if (step.Column > column)
            {
                step.Column--;
            }
        }

        // The removed column's predecessors kept doors into nodes that are now two columns away, and
        // a door that does not step exactly one column is a graph error rather than a long edge.
        // Falling back to "every node in the next column" is the honest repair.
        foreach (var step in Steps)
        {
            step.Doors.RemoveAll(d => ByKey(d) is not { } to || to.Column != step.Column + 1);
        }

        Compact();
        Order();
    }

    /// <summary>Puts the nodes back in column order, which is the order everything reads them in.</summary>
    public void Order()
    {
        var ordered = Steps.OrderBy(s => s.Column).ToList();
        Steps.Clear();
        Steps.AddRange(ordered);
    }

    /// <summary>Closes any column left empty, so columns stay contiguous.</summary>
    private void Compact()
    {
        int count = ColumnCount;
        for (int c = count - 1; c >= 0; c--)
        {
            if (ColumnAt(c).Count != 0)
            {
                continue;
            }

            foreach (var step in Steps)
            {
                if (step.Column > c)
                {
                    step.Column--;
                }
            }
        }

        Order();
    }

    /// <summary>The nodes one node opens onto, resolved.</summary>
    /// <param name="step">The node.</param>
    /// <returns>Its doors, in draft order. Empty for a terminal.</returns>
    public IReadOnlyList<Step> DoorsOf(Step step)
    {
        if (step is null)
        {
            throw new ArgumentNullException(nameof(step));
        }

        var next = ColumnAt(step.Column + 1);
        if (step.Doors.Count == 0)
        {
            return next;
        }

        return next.Where(n => step.Doors.Contains(n.Key)).ToList();
    }

    /// <summary>Opens or closes one door.</summary>
    /// <param name="from">The node the door leaves.</param>
    /// <param name="toKey">The node in the next column it would reach.</param>
    public void ToggleDoor(Step from, string toKey)
    {
        if (from is null)
        {
            throw new ArgumentNullException(nameof(from));
        }

        // An empty list means "all", so the first close has to write the set out in full before it
        // can take one away — otherwise closing a door would open every other one.
        if (from.Doors.Count == 0)
        {
            foreach (var node in ColumnAt(from.Column + 1))
            {
                from.Doors.Add(node.Key);
            }
        }

        if (!from.Doors.Remove(toKey))
        {
            from.Doors.Add(toKey);
        }
    }

    /// <summary>True when the act has at least one node and every node names something that exists.</summary>
    public bool IsPlayable => Steps.Count > 0 && Steps.All(IsResolvable) && Squad.Count > 0;

    /// <summary>Why the act cannot be played, or empty when it can.</summary>
    /// <remarks>
    /// Named rather than left to a greyed button: a Play that refuses without saying why is the
    /// failure this codebase keeps writing down.
    /// </remarks>
    public string Refusal
    {
        get
        {
            if (Steps.Count == 0)
            {
                return "An act needs at least one node. Add a battle, an event or a rest.";
            }

            foreach (var step in Steps)
            {
                if (!IsResolvable(step))
                {
                    return step.Kind switch
                    {
                        NodeKind.Event => "One of the events has no scene chosen.",
                        NodeKind.Elite => "One of the elites has no board chosen.",
                        NodeKind.Boss => "The boss has no board chosen.",
                        NodeKind.Fight => "One of the battles has no board chosen.",
                        _ => "A node is incomplete.",
                    };
                }
            }

            if (Squad.Count == 0)
            {
                return "An act needs a squad.";
            }

            return string.Empty;
        }
    }

    /// <summary>
    /// What Core's own map linter says about this shape — structural, and never a refusal.
    /// </summary>
    /// <remarks>
    /// <b>Warnings, not gates.</b> <see cref="ActMap.Validate"/> holds what a SHIPPABLE act must be:
    /// one terminal, and that terminal a boss. A three-node probe that ends on a Still Pond breaks
    /// both and is exactly the thing this tool exists to let someone play. So the linter is surfaced
    /// and not enforced — the designer is told the act is not act-shaped and is left to decide whether
    /// that matters for what they are testing.
    /// </remarks>
    public IReadOnlyList<string> Warnings =>
        Steps.Count == 0 || !IsPlayable ? Array.Empty<string>() : ToCampaign("preview").Map!.Validate();

    /// <summary>Whether a node names something that exists.</summary>
    /// <remarks>
    /// Asked of the LISTS rather than of <c>FightLibrary.ById</c>, which throws on an id it does not
    /// know — including the empty one a freshly added node carries. A draft is half-built by
    /// definition, so its validity check cannot be one that explodes on half-built input.
    /// </remarks>
    private static bool IsResolvable(Step step) => step.Kind switch
    {
        NodeKind.Event => EventLibrary.All().Any(e => e.Id == step.Id),
        NodeKind.Rest => true,
        _ => step.Id.Length > 0 && FightLibrary.All().Any(f => f.Id == step.Id),
    };

    /// <summary>Turns the draft into something the run layer can walk.</summary>
    /// <remarks>
    /// <b>A mapped act.</b> A campaign can be a bare node list or a graph, and the run layer walks
    /// both — but the shell's run screens are built for the graph: the map screen draws
    /// <c>CurrentMapNode</c>, and a list has none, so a linear act's event node was entered by Core and
    /// then drawn as a rest by a screen with nothing to look up. Emitting a map means an act built here
    /// uses the shipped path end to end rather than a second one that only mostly works.
    /// </remarks>
    /// <param name="id">Storage slug, used as the campaign id.</param>
    /// <returns>The definition.</returns>
    public CampaignDefinition ToCampaign(string id)
    {
        Order();

        var mapNodes = new List<MapNode>(Steps.Count);
        var edges = new List<MapEdge>();

        foreach (var step in Steps)
        {
            mapNodes.Add(new MapNode
            {
                Id = step.Key,
                Column = step.Column,
                Lane = step.Lane,
                Type = step.Kind switch
                {
                    NodeKind.Event => MapNodeType.Event,
                    NodeKind.Rest => MapNodeType.Rest,
                    NodeKind.Elite => MapNodeType.Elite,
                    NodeKind.Boss => MapNodeType.Boss,
                    _ => MapNodeType.Fight,
                },
                FightId = step.IsCombat ? step.Id : string.Empty,
                EventId = step.Kind == NodeKind.Event ? step.Id : string.Empty,
                Reward = step.Gilt ? RewardMark.LegendaryPickOneOfTwo : null,
                Label = LabelFor(step),
            });

            foreach (var door in DoorsOf(step))
            {
                edges.Add(new MapEdge(step.Key, door.Key));
            }
        }

        var first = ColumnAt(0);

        var map = new ActMap
        {
            Id = id,
            Name = Name.Length > 0 ? Name : "Untitled act",
            StartNodeId = first.Count > 0 ? first[0].Key : string.Empty,
            Nodes = mapNodes,
            Edges = edges,
        };

        return new CampaignDefinition
        {
            Id = id,
            Name = map.Name,
            Squad = Squad.ToList(),
            Nodes = mapNodes.Select(n => n.ToCampaignNode(map)).ToList(),
            Map = map,
        };
    }

    /// <summary>What a node is called on the map.</summary>
    /// <param name="step">The node.</param>
    /// <returns>Its name.</returns>
    public static string LabelFor(Step step)
    {
        if (step is null)
        {
            throw new ArgumentNullException(nameof(step));
        }

        if (step.Kind == NodeKind.Rest)
        {
            return "Camp";
        }

        if (step.Kind == NodeKind.Event)
        {
            foreach (var scene in EventLibrary.All())
            {
                if (scene.Id == step.Id)
                {
                    return scene.Name;
                }
            }

            return "?";
        }

        foreach (var board in FightLibrary.All())
        {
            if (board.Id == step.Id)
            {
                return board.Name;
            }
        }

        return step.Id.Length > 0 ? step.Id : "— unchosen —";
    }

    // ---- Storage -------------------------------------------------------------------------------
    //
    // The same flat line-oriented text the loadout presets use, and for the same reasons: no
    // serializer to hand, values that are all enums and slugs, and a draft a person can read in
    // localStorage is one they can fix by hand when it goes wrong.

    /// <summary>Renders the draft as storable text.</summary>
    /// <returns>One line per field.</returns>
    public string ToText()
    {
        Order();

        var lines = new List<string> { "name=" + Name };

        foreach (var kind in Squad)
        {
            lines.Add("squad=" + kind);
        }

        foreach (var step in Steps)
        {
            lines.Add(
                "node=" + step.Key
                + "|" + step.Column.ToString(CultureInfo.InvariantCulture)
                + "|" + step.Kind
                + "|" + step.Lane
                + "|" + (step.Gilt ? "1" : "0")
                + "|" + step.Band
                + "|" + step.Id);

            foreach (var door in step.Doors)
            {
                lines.Add("door=" + step.Key + "|" + door);
            }
        }

        foreach (var line in Proof)
        {
            lines.Add("proof=" + line);
        }

        return string.Join("\n", lines);
    }

    /// <summary>Reads a draft back. Unknown lines are skipped rather than fatal.</summary>
    /// <remarks>
    /// <c>step=</c> lines are v1's corridor format and are still read: one node per column, in file
    /// order, with doors left implicit. A saved act does not stop being a saved act because the editor
    /// learned to fork.
    /// </remarks>
    /// <param name="text">Stored text.</param>
    /// <returns>The draft.</returns>
    public static ActDraft FromText(string? text)
    {
        var draft = new ActDraft();
        if (string.IsNullOrWhiteSpace(text))
        {
            return draft;
        }

        bool squadRead = false;
        int legacyColumn = 0;

        foreach (var raw in text!.Split('\n'))
        {
            var line = raw.Trim();
            int split = line.IndexOf('=');
            if (split <= 0)
            {
                continue;
            }

            var key = line.Substring(0, split);
            var value = line.Substring(split + 1);

            switch (key)
            {
                case "name":
                    draft.Name = value;
                    break;

                case "squad":
                    if (Enum.TryParse<UnitKind>(value, out var unit))
                    {
                        if (!squadRead)
                        {
                            draft.Squad.Clear();
                            squadRead = true;
                        }

                        draft.Squad.Add(unit);
                    }

                    break;

                case "node":
                    ReadNode(draft, value);
                    break;

                case "door":
                    ReadDoor(draft, value);
                    break;

                case "proof":
                    draft.Proof.Add(value);
                    break;

                case "step":
                    int colon = value.IndexOf(':');
                    if (colon > 0 && Enum.TryParse<NodeKind>(value.Substring(0, colon), out var stepKind))
                    {
                        var step = draft.Add(legacyColumn++, stepKind);
                        step.Id = value.Substring(colon + 1);
                    }

                    break;
            }
        }

        draft.Order();
        return draft;
    }

    private static void ReadNode(ActDraft draft, string value)
    {
        var parts = value.Split('|');
        if (parts.Length < 6
            || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int column))
        {
            return;
        }

        if (!Enum.TryParse<NodeKind>(parts[2], out var kind))
        {
            return;
        }

        Enum.TryParse<MapLane>(parts[3], out var lane);

        // The band was added after the first saved acts, so an older line has the id where the band
        // now sits. Reading it as a band and falling back keeps those acts loading.
        bool banded = Enum.TryParse<FightPool>(parts[5], out var band);

        draft.Steps.Add(new Step
        {
            Key = parts[0],
            Column = column,
            Kind = kind,
            Lane = lane,
            Gilt = parts[4] == "1",
            Band = banded ? band : FightPool.None,

            // Ids may not contain a bar, but rejoining is cheaper than trusting that forever.
            Id = string.Join("|", parts.Skip(banded ? 6 : 5)),
        });

        // Keys minted later must not collide with keys read back.
        if (parts[0].StartsWith("n", StringComparison.Ordinal)
            && int.TryParse(parts[0].Substring(1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n)
            && n >= draft._nextKey)
        {
            draft._nextKey = n + 1;
        }
    }

    private static void ReadDoor(ActDraft draft, string value)
    {
        int bar = value.IndexOf('|');
        if (bar <= 0)
        {
            return;
        }

        draft.ByKey(value.Substring(0, bar))?.Doors.Add(value.Substring(bar + 1));
    }

    /// <summary>A one-line summary of the shape, for a list.</summary>
    /// <returns>Something of the form <c>7 columns · 12 nodes · 15 doors</c>.</returns>
    public string Shape()
    {
        if (Steps.Count == 0)
        {
            return "empty";
        }

        int doors = Steps.Sum(s => DoorsOf(s).Count);

        return ColumnCount.ToString(CultureInfo.InvariantCulture) + " columns · "
            + Steps.Count.ToString(CultureInfo.InvariantCulture) + " nodes · "
            + doors.ToString(CultureInfo.InvariantCulture) + " doors";
    }

    /// <summary>How many nodes of one kind the act has.</summary>
    /// <param name="kind">Kind to count.</param>
    /// <returns>The count.</returns>
    public int CountOf(NodeKind kind) => Steps.Count(s => s.Kind == kind);

    /// <summary>The act's contents, as a sentence.</summary>
    /// <returns>Something of the form <c>12 nodes — 8 battles, 1 elite, 1 event, 2 rests</c>.</returns>
    public string Length()
    {
        var parts = new List<string>();
        Say(parts, CountOf(NodeKind.Fight), "battle", "battles");
        Say(parts, CountOf(NodeKind.Elite), "elite", "elites");
        Say(parts, CountOf(NodeKind.Event), "event", "events");
        Say(parts, CountOf(NodeKind.Rest), "rest", "rests");
        Say(parts, CountOf(NodeKind.Boss), "boss", "bosses");

        return Steps.Count.ToString(CultureInfo.InvariantCulture) + " node(s)"
            + (parts.Count == 0 ? string.Empty : " — " + string.Join(", ", parts));
    }

    private static void Say(List<string> parts, int count, string one, string many)
    {
        if (count > 0)
        {
            parts.Add(count.ToString(CultureInfo.InvariantCulture) + " " + (count == 1 ? one : many));
        }
    }

    /// <summary>
    /// How often the act fields the same board twice — the repetition debt, measured rather than
    /// assumed.
    /// </summary>
    /// <returns>A sentence, or empty when the act fields no boards.</returns>
    public string Repetition()
    {
        var used = Steps.Where(s => s.IsCombat && s.Id.Length > 0).Select(s => s.Id).ToList();
        if (used.Count == 0)
        {
            return string.Empty;
        }

        int distinct = used.Distinct(StringComparer.Ordinal).Count();
        int repeats = used.Count - distinct;

        return repeats == 0
            ? used.Count + " combat nodes, " + distinct + " distinct boards — no repeats."
            : used.Count + " combat nodes, " + distinct + " distinct boards — "
                + repeats + " repeat(s).";
    }
}
