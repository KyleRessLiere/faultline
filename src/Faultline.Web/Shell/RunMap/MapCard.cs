using System;
using System.Collections.Generic;
using Faultline.Core;

namespace Faultline.Web.Shell.RunMap;

/// <summary>
/// One node of the act map, as the map screen draws it.
/// </summary>
/// <remarks>
/// <para>
/// A projection of a <see cref="MapNode"/> plus where the run stands, and nothing else. Every field
/// is derived in <see cref="MapCards.Build"/> from a Core query, so the panel that renders this
/// makes no decision about the graph at all.
/// </para>
/// <para>
/// <b><see cref="Roster"/> is empty unless the node is a door out of where the run stands.</b> That
/// is the spoiler rule made structural rather than left to the renderer's discretion: a card for a
/// distant node has no roster on it to leak, so no markup can print one.
/// </para>
/// <para>
/// <b><see cref="Gilt"/> and <see cref="Promise"/> are the promise rule.</b> Both come from
/// <see cref="RewardMark.Payable"/> and never from <see cref="RewardMark.Kind"/> — see
/// <see cref="MapCards.GiltFor"/>.
/// </para>
/// </remarks>
public sealed record MapCard
{
    /// <summary>The node's stable id — what a vote and a save refer to.</summary>
    public string NodeId { get; init; } = string.Empty;

    /// <summary>What the map prints under the icon.</summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>Zero-based column, left to right.</summary>
    public int Column { get; init; }

    /// <summary>What the node is, in the map's own vocabulary.</summary>
    public MapNodeType Type { get; init; } = MapNodeType.Fight;

    /// <summary>Which side of the comfort gradient it stands on.</summary>
    public MapLane Lane { get; init; } = MapLane.Neutral;

    /// <summary>The glyph it wears.</summary>
    public MapIcon Icon { get; init; } = MapIcon.Swords;

    /// <summary>The type in words — always shown, on every node, hovered or not.</summary>
    public string TypeName { get; init; } = string.Empty;

    /// <summary>Where the node stands relative to the run.</summary>
    public MapNodeState State { get; init; } = MapNodeState.Ahead;

    /// <summary>The <c>.fight</c> this node plays, or empty.</summary>
    public string FightId { get; init; } = string.Empty;

    /// <summary>The fight's display name, or empty for a node that plays none.</summary>
    public string FightName { get; init; } = string.Empty;

    /// <summary>True when this is the act's boss, which the map draws largest and last.</summary>
    public bool IsBoss => Type == MapNodeType.Boss;

    /// <summary>
    /// True when the map may draw a gilt edge — i.e. when a mark is on the node <em>and</em> the run
    /// can actually hand it over. False for a mark this build cannot pay.
    /// </summary>
    public bool Gilt { get; init; }

    /// <summary>
    /// What the gilt edge promises, in words. Empty whenever <see cref="Gilt"/> is false, because a
    /// promise the game cannot keep must not reach a screen at all.
    /// </summary>
    public string Promise { get; init; } = string.Empty;

    /// <summary>
    /// What stands between the squad and this node — but only for a door one step away. Empty for
    /// every other node on the map.
    /// </summary>
    public IReadOnlyList<RosterLine> Roster { get; init; } = Array.Empty<RosterLine>();

    /// <summary>True when this card is allowed to show a roster preview at all.</summary>
    public bool ShowsRoster => Roster.Count > 0;
}
