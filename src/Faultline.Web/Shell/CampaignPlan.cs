using System;
using System.Collections.Generic;
using Faultline.Core;

namespace Faultline.Web.Shell;

/// <summary>
/// The curated set's three groups, as lists of fight ids: the ordered campaign spine
/// (docs/CURATED_SET.md §1), the trials menu (§2) and the co-op gauntlet (§3).
/// </summary>
/// <remarks>
/// <para>
/// Ids, never indexes. A fight's <see cref="FightDefinition.Number"/> is its authoring number, not
/// its position in the campaign — cb-06 is number 506 and sits at campaign slot 2 — so anything
/// that walked the library in order would produce a different game. Resolving by id also means a
/// campaign fight that has not been authored yet is simply absent, and lights up on its own the
/// moment the <c>.fight</c> file lands: no code change, no registration.
/// </para>
/// <para>
/// This list is the one piece of curated-set knowledge the shell holds, because Core has no
/// campaign key to ask. It is membership and order, not rules.
/// </para>
/// </remarks>
public static class CampaignPlan
{
    /// <summary>The ten campaign fights, in the order they are played (CURATED_SET §1).</summary>
    public static IReadOnlyList<string> Order { get; } = new[]
    {
        "first-contact",
        "cb-06-bait-and-break",
        "the-teeth",
        "broken-bridge",
        "the-shrine",
        "break-the-gate",
        "high-road",
        "hz-09-the-trench",
        "hold-the-gate",
        "quarry-king",
    };

    /// <summary>The trials library — pick any board, no assumed order (CURATED_SET §2).</summary>
    public static IReadOnlyList<string> Trials { get; } = new[]
    {
        "hz-01-dig-in",
        "hz-02-the-short-way",
        "hz-04-causeway",
        "hz-06-the-second-shove",
        "hz-08-free-kick",
        "the-maw",
        "ec-02-pincer",
        "ec-03-handoff",
        "ec-05-perch-war",
        "ec-08-triage",
        "ec-09-undertow",
        "cb-04-dead-weight",
        "cb-09-crossfire",
        "as-07-the-terraces",
        "tp-07-three-lanes",
    };

    /// <summary>The four boards about the partnership itself (CURATED_SET §3).</summary>
    public static IReadOnlyList<string> Gauntlet { get; } = new[]
    {
        "as-02-both-sides-of-the-chasm",
        "as-08-two-fires",
        "as-04-rope-and-shield",
        "as-05-the-door",
    };

    /// <summary>How many fights the campaign has, authored or not.</summary>
    public static int Length => Order.Count;

    /// <summary>
    /// Every active fight Core will hand out, keyed by id. Read live from
    /// <see cref="FightLibrary.All()"/> so a fight that lands, or a fight that gains a
    /// <c>retired:</c> key, changes this without the shell knowing any names.
    /// </summary>
    /// <returns>Playable fights by id.</returns>
    public static IReadOnlyDictionary<string, FightDefinition> Active()
    {
        var byId = new Dictionary<string, FightDefinition>(StringComparer.Ordinal);
        foreach (var fight in FightLibrary.All())
        {
            byId[fight.Id] = fight;
        }

        return byId;
    }

    /// <summary>Which curated group a fight id belongs to.</summary>
    /// <param name="id">Fight id.</param>
    /// <returns>The group.</returns>
    public static FightGroup GroupOf(string id)
    {
        if (Contains(Order, id))
        {
            return FightGroup.Campaign;
        }

        if (Contains(Trials, id))
        {
            return FightGroup.Trials;
        }

        return Contains(Gauntlet, id) ? FightGroup.Gauntlet : FightGroup.Other;
    }

    /// <summary>Position of a fight in the campaign spine, or -1 when it is not in it.</summary>
    /// <param name="id">Fight id.</param>
    /// <returns>Zero-based slot.</returns>
    public static int SlotOf(string id)
    {
        for (int i = 0; i < Order.Count; i++)
        {
            if (string.Equals(Order[i], id, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool Contains(IReadOnlyList<string> ids, string id)
    {
        foreach (var candidate in ids)
        {
            if (string.Equals(candidate, id, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>Which section of the curated set a battle belongs to.</summary>
public enum FightGroup
{
    /// <summary>The ordered ten (CURATED_SET §1).</summary>
    Campaign,

    /// <summary>The pick-any trials library (CURATED_SET §2).</summary>
    Trials,

    /// <summary>The four co-op boards (CURATED_SET §3).</summary>
    Gauntlet,

    /// <summary>Authored, active, but not in any curated group.</summary>
    Other,
}
