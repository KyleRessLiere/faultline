using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Faultline.Core;

namespace Faultline.Web.Shell;

/// <summary>
/// An act being built in the UI: an ordered list of nodes, turned into a
/// <see cref="CampaignDefinition"/> the run layer walks like any other.
/// </summary>
/// <remarks>
/// <para>
/// <b>Linear, and that is not a simplification of the real thing — it is one of the two real
/// things.</b> `CampaignLibrary.Faultline` is a linear list of nodes and `Act1` is a graph; the run
/// layer walks both, and the node handlers are shared. So an act sequenced here is played by exactly
/// the code a shipped act is played by, which is the whole reason testing an event this way proves
/// anything about the event.
/// </para>
/// <para>
/// <b>Why this exists:</b> an event is a run node, so the only way to reach one was to play a
/// campaign until the map offered it. That made the one shipped event effectively untestable and
/// made iterating on act SHAPE — what follows what — a code change. Both are now a list you can
/// reorder.
/// </para>
/// </remarks>
public sealed class ActDraft
{
    /// <summary>What one node in the draft is.</summary>
    public enum NodeKind
    {
        /// <summary>A board.</summary>
        Fight = 0,

        /// <summary>An event scene.</summary>
        Event = 1,

        /// <summary>A rest: heal or forge.</summary>
        Rest = 2,
    }

    /// <summary>One step of the act.</summary>
    public sealed class Step
    {
        /// <summary>What kind of node it is.</summary>
        public NodeKind Kind { get; set; } = NodeKind.Fight;

        /// <summary>The board id for a fight, or the event id for an event. Unused for a rest.</summary>
        public string Id { get; set; } = string.Empty;
    }

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The act's steps, in order.</summary>
    public List<Step> Steps { get; } = new();

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

    /// <summary>True when the act has at least one node and so can be played.</summary>
    public bool IsPlayable => Steps.Count > 0 && Steps.All(IsResolvable);

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
                        NodeKind.Fight => "One of the battles has no board chosen.",
                        NodeKind.Event => "One of the events has no scene chosen.",
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
    /// Whether a step names something that exists.
    /// </summary>
    /// <remarks>
    /// Asked of the LISTS rather than of <c>FightLibrary.ById</c>, which throws on an id it does not
    /// know — including the empty one a freshly added node carries. A draft is half-built by
    /// definition, so its validity check cannot be one that explodes on half-built input.
    /// </remarks>
    private static bool IsResolvable(Step step) => step.Kind switch
    {
        NodeKind.Fight => step.Id.Length > 0 && FightLibrary.All().Any(f => f.Id == step.Id),
        NodeKind.Event => EventLibrary.All().Any(e => e.Id == step.Id),
        _ => true,
    };

    /// <summary>Turns the draft into something the run layer can walk.</summary>
    /// <remarks>
    /// <b>A MAPPED act, one node per column, chained.</b> A campaign can be a bare node list or a
    /// graph, and the run layer walks both — but the shell's run screens are built for the graph:
    /// the map screen draws <c>CurrentMapNode</c>, and a list has none, so a linear act's event node
    /// was entered by Core and then drawn as a rest by a screen with nothing to look up. Emitting a
    /// map means an act built here uses the shipped path end to end rather than a second one that
    /// only mostly works.
    /// <para>
    /// One node per column and an edge to the next is a graph with no forks. The shape is a line
    /// because that is what sequencing gives you; branching is a bigger editor and a later one.
    /// </para>
    /// </remarks>
    /// <param name="id">Storage slug, used as the campaign id.</param>
    /// <returns>The definition.</returns>
    public CampaignDefinition ToCampaign(string id)
    {
        var mapNodes = new List<MapNode>(Steps.Count);
        var edges = new List<MapEdge>();

        for (int i = 0; i < Steps.Count; i++)
        {
            var step = Steps[i];
            var nodeId = "n" + i.ToString(CultureInfo.InvariantCulture);

            mapNodes.Add(new MapNode
            {
                Id = nodeId,
                Column = i,
                Lane = MapLane.Neutral,
                Type = step.Kind switch
                {
                    NodeKind.Fight => MapNodeType.Fight,
                    NodeKind.Event => MapNodeType.Event,
                    _ => MapNodeType.Rest,
                },
                FightId = step.Kind == NodeKind.Fight ? step.Id : string.Empty,
                EventId = step.Kind == NodeKind.Event ? step.Id : string.Empty,
                Label = LabelFor(step),
            });

            if (i > 0)
            {
                edges.Add(new MapEdge("n" + (i - 1).ToString(CultureInfo.InvariantCulture), nodeId));
            }
        }

        var map = new ActMap
        {
            Id = id,
            Name = Name.Length > 0 ? Name : "Untitled act",
            StartNodeId = mapNodes.Count > 0 ? mapNodes[0].Id : string.Empty,
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
    private static string LabelFor(Step step)
    {
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

        return step.Id;
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
        var lines = new List<string> { "name=" + Name };

        foreach (var kind in Squad)
        {
            lines.Add("squad=" + kind);
        }

        foreach (var step in Steps)
        {
            lines.Add("step=" + step.Kind + ":" + step.Id);
        }

        return string.Join("\n", lines);
    }

    /// <summary>Reads a draft back. Unknown lines are skipped rather than fatal.</summary>
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
                    if (Enum.TryParse<UnitKind>(value, out var kind))
                    {
                        if (!squadRead)
                        {
                            draft.Squad.Clear();
                            squadRead = true;
                        }

                        draft.Squad.Add(kind);
                    }

                    break;
                case "step":
                    int colon = value.IndexOf(':');
                    if (colon > 0 && Enum.TryParse<NodeKind>(value.Substring(0, colon), out var stepKind))
                    {
                        draft.Steps.Add(new Step
                        {
                            Kind = stepKind,
                            Id = value.Substring(colon + 1),
                        });
                    }

                    break;
            }
        }

        return draft;
    }

    /// <summary>A one-line summary of the shape, for a list.</summary>
    /// <returns>Something of the form <c>fight · event · rest · fight</c>.</returns>
    public string Shape() =>
        Steps.Count == 0
            ? "empty"
            : string.Join(" · ", Steps.Select(s => s.Kind.ToString().ToLowerInvariant()));

    /// <summary>How many nodes of one kind the act has.</summary>
    /// <param name="kind">Kind to count.</param>
    /// <returns>The count.</returns>
    public int CountOf(NodeKind kind) => Steps.Count(s => s.Kind == kind);

    /// <summary>The act's length, as a sentence.</summary>
    /// <returns>Something of the form <c>4 nodes — 2 battles, 1 event, 1 rest</c>.</returns>
    public string Length()
    {
        int fights = CountOf(NodeKind.Fight);
        int scenes = CountOf(NodeKind.Event);
        int rests = CountOf(NodeKind.Rest);

        return Steps.Count.ToString(CultureInfo.InvariantCulture) + " node(s) — "
            + fights + " battle(s), " + scenes + " event(s), " + rests + " rest(s)";
    }
}
