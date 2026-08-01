using System;
using System.Collections.Generic;
using Faultline.Core;

namespace Faultline.Web.Shell;

/// <summary>
/// The curated set as the battle picker needs it: which section a board belongs to, and where a
/// campaign board sits in the spine.
/// </summary>
/// <remarks>
/// <para>
/// <strong>The spine is not stored here.</strong> Its order is
/// <see cref="CampaignLibrary.Faultline"/>'s, read live through
/// <see cref="CampaignDefinition.FightIds"/>, because the run engine walks that list and a second
/// copy in a renderer would drift the first time someone reordered it. What the shell still owns is
/// the two lists Core has no opinion about — the pick-any trials menu (<c>docs/CURATED_SET.md</c>
/// §2) and the co-op gauntlet (§3) — which are picker sections rather than a campaign.
/// </para>
/// <para>
/// Ids, never indexes. A fight's <see cref="FightDefinition.Number"/> is its authoring number, not
/// its position in the campaign — cb-06 is number 506 and sits at campaign slot 2.
/// </para>
/// </remarks>
public static class CuratedSet
{
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

    /// <summary>The campaign's fights, in the order the run plays them. Core's list, not a copy.</summary>
    public static IReadOnlyList<string> Spine => CampaignLibrary.Faultline.FightIds();

    /// <summary>How many fights the campaign has.</summary>
    public static int SpineLength => Spine.Count;

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
        if (Contains(Spine, id))
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
    /// <returns>Zero-based slot among the campaign's fights.</returns>
    public static int SlotOf(string id)
    {
        var spine = Spine;
        for (int i = 0; i < spine.Count; i++)
        {
            if (string.Equals(spine[i], id, StringComparison.Ordinal))
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
    /// <summary>The campaign spine (CURATED_SET §1), whose order lives in Core.</summary>
    Campaign,

    /// <summary>The pick-any trials library (CURATED_SET §2).</summary>
    Trials,

    /// <summary>The four co-op boards (CURATED_SET §3).</summary>
    Gauntlet,

    /// <summary>Authored, active, but not in any curated group.</summary>
    Other,
}
