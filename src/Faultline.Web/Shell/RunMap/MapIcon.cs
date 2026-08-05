namespace Faultline.Web.Shell.RunMap;

/// <summary>
/// The glyphs an act map draws on its nodes (MASTER_DESIGN §8.5: "every node wears its type").
/// </summary>
/// <remarks>
/// Eight, exactly the eight the design doc lists. A node's type alone does not pick one — four of
/// them separate <em>combat</em> nodes by what winning means, which is the fight's
/// <see cref="Faultline.Core.Objective"/> and therefore a fact read out of Core rather than a
/// judgement made here. <see cref="MapCards.IconFor"/> is the one place the mapping lives.
/// </remarks>
public enum MapIcon
{
    /// <summary>Swords — kill everything.</summary>
    Swords = 0,

    /// <summary>Shield — defend: a Hold or a Protect.</summary>
    Shield = 1,

    /// <summary>Broken gate — a raid: break the thing, or get through to it.</summary>
    Gate = 2,

    /// <summary>Hourglass — survive to the bell.</summary>
    Hourglass = 3,

    /// <summary>Skull — an elite.</summary>
    Skull = 4,

    /// <summary>A question mark — an event.</summary>
    Question = 5,

    /// <summary>
    /// A still pond — a Rest.
    /// </summary>
    /// <remarks>
    /// Not a campfire. Ducks rest on still water, and MASTER_DESIGN's (r) tone lock says so: "more
    /// campfires on the safe lane" has to read "more ponds". Display and fiction only — the node
    /// type identifier stays <see cref="Faultline.Core.MapNodeType.Rest"/> per §15's decoupling, and
    /// the <em>camp</em> is a different thing entirely (a run-seam phase, D-127) that is not on the
    /// map at all.
    /// </remarks>
    Pond = 6,

    /// <summary>The boss sigil, at the end of every lane.</summary>
    Boss = 7,
}
