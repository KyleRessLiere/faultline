using System;
using System.Collections.Generic;
using System.Globalization;
using Faultline.Core;

namespace Faultline.Web.Shell;

/// <summary>How a campaign run stands.</summary>
public enum CampaignStatus
{
    /// <summary>A fight is still ahead.</summary>
    Playing,

    /// <summary>Every authored fight in the spine was cleared.</summary>
    Won,

    /// <summary>A fight was lost. A run ends on its first defeat.</summary>
    Lost,

    /// <summary>Voids left the squad unable to field both players.</summary>
    Wiped,
}

/// <summary>
/// One playthrough of the ten-fight spine: which seed, how far along, which fights were cleared or
/// passed over, and — the only thing carried between fights — which classes have been voided.
/// </summary>
/// <remarks>
/// <para>
/// This is bookkeeping, not rules. Core decides what <see cref="Unit.Voided"/> means and when it is
/// set; the run only reads the flag off the finished state and refuses to hand that class a slot in
/// the next fight's roster. A unit that was merely downed carries nothing forward — it is rebuilt
/// from its template by <see cref="Game.Start(FightDefinition, int)"/> like every other fight.
/// </para>
/// <para>
/// The squad's identity across fights is the <see cref="UnitKind"/>, because that is what the
/// <c>.fight</c> files name: every campaign board rosters the same four classes, split between the
/// two players in whatever way that board wants. Losing the Archer therefore means the Archer's
/// slot disappears from whichever roster the next board put her in.
/// </para>
/// </remarks>
public sealed record CampaignRun
{
    /// <summary>Storage id; a zero-padded tick count, so runs sort chronologically.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Run seed. Every fight in the run starts from it.</summary>
    public int Seed { get; init; }

    /// <summary>
    /// Zero-based slot in <see cref="CampaignPlan.Order"/> the run is sitting on. Always a slot with
    /// an authored fight, or <see cref="CampaignPlan.Length"/> when the spine is finished.
    /// </summary>
    public int Index { get; init; }

    /// <summary>Ids of fights won, in the order they were won.</summary>
    public IReadOnlyList<string> Cleared { get; init; } = Array.Empty<string>();

    /// <summary>Ids passed over because no <c>.fight</c> file exists for them yet.</summary>
    public IReadOnlyList<string> Skipped { get; init; } = Array.Empty<string>();

    /// <summary>Classes voided so far. These never appear again in this run.</summary>
    public IReadOnlyList<UnitKind> Lost { get; init; } = Array.Empty<UnitKind>();

    /// <summary>Where the run stands.</summary>
    public CampaignStatus Status { get; init; } = CampaignStatus.Playing;

    /// <summary>True while the run still has a fight to play.</summary>
    public bool InProgress => Status == CampaignStatus.Playing && Index < CampaignPlan.Length;

    /// <summary>The id of the fight being played, or <c>null</c> when the run is over.</summary>
    public string? CurrentId =>
        Index >= 0 && Index < CampaignPlan.Length ? CampaignPlan.Order[Index] : null;

    /// <summary>How many of the ten have been cleared.</summary>
    public int ClearedCount => Cleared.Count;

    /// <summary>Starts a run at the top of the spine.</summary>
    /// <param name="id">Storage id.</param>
    /// <param name="seed">Run seed.</param>
    /// <returns>A run sitting on slot 0.</returns>
    public static CampaignRun Begin(string id, int seed) =>
        new CampaignRun { Id = id, Seed = seed, Index = 0 };

    /// <summary>
    /// The fight as this run must play it: the authored board, with every voided class's roster slot
    /// removed.
    /// </summary>
    /// <param name="fight">The authored fight.</param>
    /// <returns>The fight with the surviving squad, or the original when nothing has been lost.</returns>
    public FightDefinition Adapt(FightDefinition fight)
    {
        if (fight is null)
        {
            throw new ArgumentNullException(nameof(fight));
        }

        if (Lost.Count == 0)
        {
            return fight;
        }

        // One pool consumed across both rosters, so losing one Archer removes exactly one Archer
        // slot no matter which player's list the board put her in.
        var pool = new List<UnitKind>(Lost);
        var a = Survivors(fight.RosterA, pool);
        var b = Survivors(fight.RosterB, pool);

        return fight with { RosterA = a, RosterB = b };
    }

    /// <summary>
    /// Whether the surviving squad can still field this board. Both players need at least one unit:
    /// a side with an empty roster has nothing to deploy and nothing to activate.
    /// </summary>
    /// <param name="fight">The authored fight.</param>
    /// <returns>Whether the run can play it.</returns>
    public bool CanField(FightDefinition fight)
    {
        var adapted = Adapt(fight);
        return adapted.RosterA.Count > 0 && adapted.RosterB.Count > 0;
    }

    /// <summary>Records a win on the current fight and moves to the next playable slot.</summary>
    /// <param name="state">The finished state, read for voided units.</param>
    /// <param name="active">The fights Core currently hands out, by id.</param>
    /// <returns>The advanced run.</returns>
    public CampaignRun Advance(GameState state, IReadOnlyDictionary<string, FightDefinition> active)
    {
        var id = CurrentId;
        if (id is null)
        {
            return this;
        }

        var cleared = new List<string>(Cleared);
        if (!cleared.Contains(id))
        {
            cleared.Add(id);
        }

        var run = this with
        {
            Cleared = cleared,
            Lost = Bury(state),
            Index = Index + 1,
        };

        return run.Settle(active);
    }

    /// <summary>Records a defeat. A run ends on its first loss.</summary>
    /// <param name="state">The finished state, read for voided units.</param>
    /// <returns>The finished run.</returns>
    public CampaignRun Fail(GameState state) =>
        this with { Lost = Bury(state), Status = CampaignStatus.Lost };

    /// <summary>
    /// Walks the index forward over campaign fights that have no authored file, then decides whether
    /// the run can carry on. Run on every advance <em>and</em> every load, so a spine that gains a
    /// file plays it without anything being migrated.
    /// </summary>
    /// <param name="active">The fights Core currently hands out, by id.</param>
    /// <returns>The settled run.</returns>
    public CampaignRun Settle(IReadOnlyDictionary<string, FightDefinition> active)
    {
        if (Status == CampaignStatus.Lost)
        {
            return this;
        }

        var skipped = new List<string>(Skipped);
        int index = Index < 0 ? 0 : Index;

        while (index < CampaignPlan.Length && !active.ContainsKey(CampaignPlan.Order[index]))
        {
            var id = CampaignPlan.Order[index];
            if (!skipped.Contains(id))
            {
                skipped.Add(id);
            }

            index++;
        }

        if (index >= CampaignPlan.Length)
        {
            return this with { Index = CampaignPlan.Length, Skipped = skipped, Status = CampaignStatus.Won };
        }

        var next = active[CampaignPlan.Order[index]];
        return this with
        {
            Index = index,
            Skipped = skipped,
            Status = CanField(next) ? CampaignStatus.Playing : CampaignStatus.Wiped,
        };
    }

    /// <summary>
    /// The classes that will never fight again: everything already lost, plus every player unit Core
    /// marked <see cref="Unit.Voided"/> in the state just finished.
    /// </summary>
    /// <param name="state">A finished — or in-progress — fight state.</param>
    /// <returns>The lost classes, in the order they were lost.</returns>
    public IReadOnlyList<UnitKind> Bury(GameState? state)
    {
        var lost = new List<UnitKind>(Lost);
        if (state is null)
        {
            return lost;
        }

        foreach (var unit in state.Units)
        {
            // Core owns the meaning of Voided; the run only reads it. A downed unit is not voided
            // and comes back next fight at full health.
            if (unit.Team.IsPlayer() && unit.Voided)
            {
                lost.Add(unit.Kind);
            }
        }

        return lost;
    }

    /// <summary>Renders the run as one <c>key: value</c> line per field.</summary>
    /// <returns>The stored text.</returns>
    /// <remarks>
    /// Hand-written for the same reason <see cref="PlaytestNotes"/> is: nothing here should depend on
    /// a serialiser surviving trimming, and the stored shape is visible in one place.
    /// </remarks>
    public string Render()
    {
        var text = new System.Text.StringBuilder();
        text.Append("id: ").Append(Id).Append('\n');
        text.Append("seed: ").Append(Number(Seed)).Append('\n');
        text.Append("index: ").Append(Number(Index)).Append('\n');
        text.Append("status: ").Append(Status.ToString()).Append('\n');
        text.Append("cleared: ").Append(string.Join(",", Cleared)).Append('\n');
        text.Append("skipped: ").Append(string.Join(",", Skipped)).Append('\n');
        text.Append("lost: ").Append(string.Join(",", Lost)).Append('\n');
        return text.ToString();
    }

    /// <summary>Reads a run back out of storage.</summary>
    /// <param name="stored">Text produced by <see cref="Render"/>.</param>
    /// <returns>The run, or <c>null</c> when the record is unreadable.</returns>
    public static CampaignRun? Parse(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored))
        {
            return null;
        }

        var fields = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in stored!.Split('\n'))
        {
            int split = line.IndexOf(':');
            if (split < 0)
            {
                continue;
            }

            fields[line.Substring(0, split).Trim()] = line.Substring(split + 1).Trim();
        }

        if (!fields.TryGetValue("id", out var id) || id.Length == 0)
        {
            return null;
        }

        var lost = new List<UnitKind>();
        foreach (var name in Split(fields, "lost"))
        {
            // An unreadable class name is dropped rather than resurrecting a unit the player lost —
            // erring towards "still dead" is the safer of the two mistakes.
            if (Enum.TryParse(name, out UnitKind kind))
            {
                lost.Add(kind);
            }
        }

        var status = fields.TryGetValue("status", out var raw)
            && Enum.TryParse(raw, out CampaignStatus parsed)
            ? parsed
            : CampaignStatus.Playing;

        return new CampaignRun
        {
            Id = id,
            Seed = Int(fields, "seed"),
            Index = Int(fields, "index"),
            Cleared = Split(fields, "cleared"),
            Skipped = Split(fields, "skipped"),
            Lost = lost,
            Status = status,
        };
    }

    private static IReadOnlyList<UnitKind> Survivors(IReadOnlyList<UnitKind> roster, List<UnitKind> pool)
    {
        var kept = new List<UnitKind>();
        foreach (var kind in roster)
        {
            if (pool.Remove(kind))
            {
                continue;
            }

            kept.Add(kind);
        }

        return kept;
    }

    private static List<string> Split(Dictionary<string, string> fields, string key)
    {
        var items = new List<string>();
        if (!fields.TryGetValue(key, out var raw))
        {
            return items;
        }

        foreach (var item in raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = item.Trim();
            if (trimmed.Length > 0)
            {
                items.Add(trimmed);
            }
        }

        return items;
    }

    private static int Int(Dictionary<string, string> fields, string key) =>
        fields.TryGetValue(key, out var raw)
        && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
            ? value
            : 0;

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
}
