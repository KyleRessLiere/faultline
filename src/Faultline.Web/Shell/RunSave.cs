using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Faultline.Core;

namespace Faultline.Web.Shell;

/// <summary>
/// A run as it is written to browser storage, and read back: the seed, where it stands, and what
/// every squad member is carrying.
/// </summary>
/// <remarks>
/// <para>
/// This is the whole of DECISIONS.md D-050. Seed, node index and the squad's carried hit points
/// survive a reload; the half-played board does not, and the current fight restarts from
/// deployment. The honest way to persist a board would be the seed plus the ordered run command
/// log, which replays everything — and until that is what gets written, a board snapshot would only
/// be a second save format that has to agree with the first.
/// </para>
/// <para>
/// One <c>key: value</c> line per field, hand-written, the same shape as <see cref="CustomFightStore"/>
/// and <see cref="PlaytestNotes"/>: nothing here depends on a serialiser surviving trimming, and the
/// stored shape is visible in one place. Squad members are repeated <c>unit:</c> lines rather than a
/// nested structure, because a flat file of lines is the only thing this format has ever needed.
/// </para>
/// </remarks>
public sealed record RunSave
{
    /// <summary>Storage id; a zero-padded tick count, so runs sort chronologically.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Which campaign was being played.</summary>
    public string CampaignId { get; init; } = CampaignLibrary.FaultlineId;

    /// <summary>Run seed. Every fight in the run is derived from it.</summary>
    public int Seed { get; init; }

    /// <summary>Index of the node the run is standing on.</summary>
    public int NodeIndex { get; init; }

    /// <summary>How many fights had been won when this was written.</summary>
    public int FightsWon { get; init; }

    /// <summary>Won, lost, or still going.</summary>
    public RunOutcome Outcome { get; init; } = RunOutcome.InProgress;

    /// <summary>
    /// True when the run was inside a fight when it was saved. Not restored as such — it is what
    /// lets the screen say that the reload sent the current fight back to deployment.
    /// </summary>
    public bool WasInFight { get; init; }

    /// <summary>The squad, in campaign order: kind, carried hit points and status.</summary>
    public IReadOnlyList<RunUnit> Squad { get; init; } = Array.Empty<RunUnit>();

    /// <summary>
    /// The nodes an act-map run has stood on, oldest first. Empty for a linear campaign, which has
    /// no graph and whose <see cref="NodeIndex"/> is the whole of its position.
    /// </summary>
    /// <remarks>
    /// The route rather than just the current node, because the route <em>is</em> the position on a
    /// graph: two runs standing on the same campfire having come down different lanes are different
    /// runs, and <see cref="MapState.RouteHash"/> — the number a determinism check compares — has to
    /// tell them apart.
    /// </remarks>
    public IReadOnlyList<string> Route { get; init; } = Array.Empty<string>();

    /// <summary>True when an act-map run had cleared its terminal node.</summary>
    public bool ActCleared { get; init; }

    /// <summary>
    /// True when the run was standing at a fork with the vote still to cast.
    /// </summary>
    /// <remarks>
    /// The one phase this record has to carry. Everything else a save drops is re-entered harmlessly
    /// — D-050 sends a half-played fight back to deployment — but the node under a fork has already
    /// been cleared, so a run restored onto it as <see cref="RunPhase.AtNode"/> is offered the fight
    /// it just won, all over again, and the fork never arrives (DECISIONS.md D-125).
    /// </remarks>
    public bool AtVote { get; init; }

    /// <summary>
    /// True when the run was standing at a Camp with its picks still to make.
    /// </summary>
    /// <remarks>
    /// Carried for exactly the reason <see cref="AtVote"/> is: the node under a camp has already been
    /// cleared, so a run restored onto it as <see cref="RunPhase.AtNode"/> would be handed the fight
    /// it just won. The camp's two cards are <em>not</em> stored — they are a pure function of the
    /// run RNG cursor and the squad, so restoring <see cref="RngState"/> deals them again (D-127).
    /// </remarks>
    public bool AtCamp { get; init; }

    /// <summary>
    /// True when the run was standing at a gilt destination with its legendaries still to pick.
    /// </summary>
    /// <remarks>
    /// <b>The third instance of the same defect, and the reason this one is written down.</b> Carried
    /// for exactly the reason <see cref="AtVote"/> and <see cref="AtCamp"/> are: the node under a
    /// destination has already been cleared, so a run restored onto it as
    /// <see cref="RunPhase.AtNode"/> is handed High Road all over again — and the legendary it was
    /// about to take is gone. D-125 was this bug at a fork, D-127's camp was this bug at a camp, and
    /// this is it at a destination (D-222). The pair of cards is <em>not</em> stored: like the camp's,
    /// it is a pure function of the RNG cursor and the squad, so restoring <see cref="RngState"/>
    /// deals the same two again.
    /// </remarks>
    public bool AtDestination { get; init; }

    /// <summary>
    /// The run RNG's cursor as it stood, or <c>null</c> for a record written before votes existed.
    /// Restored so the next split vote does not re-flip a coin this run has already flipped; when it
    /// is absent Core falls back to the seed, which is where an unflipped run starts.
    /// </summary>
    public int? RngState { get; init; }

    /// <summary>
    /// How many camps this run has already resolved. Stored because §8.6's offer director reads it:
    /// a run restored without it would be dealt camp 1's engine starters at its fifth camp.
    /// </summary>
    public int CampsHeld { get; init; }

    /// <summary>Which player took the last camp card, for §8.6's ownership-fairness row.</summary>
    public Team? LastPickOwner { get; init; }

    /// <summary>Which player took the one before that.</summary>
    public Team? PreviousPickOwner { get; init; }

    /// <summary>Takes a snapshot of a live run.</summary>
    /// <param name="id">Storage id.</param>
    /// <param name="state">The run to write down.</param>
    /// <returns>The record.</returns>
    public static RunSave Of(string id, RunState state)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        return new RunSave
        {
            Id = id,
            CampaignId = state.Campaign.Id,
            Seed = state.Seed,
            NodeIndex = state.NodeIndex,
            FightsWon = state.FightsWon,
            Outcome = state.Outcome,
            WasInFight = state.Phase == RunPhase.InFight,
            Squad = state.Squad,
            Route = state.MapState?.Route ?? (IReadOnlyList<string>)Array.Empty<string>(),
            ActCleared = state.MapState?.Completed ?? false,
            AtVote = state.Phase == RunPhase.AtVote,
            AtCamp = state.Phase == RunPhase.AtCamp,
            AtDestination = state.Phase == RunPhase.AtDestination,
            RngState = state.RngState,
            CampsHeld = state.CampsHeld,
            LastPickOwner = state.LastPickOwner,
            PreviousPickOwner = state.PreviousPickOwner,
        };
    }

    /// <summary>
    /// Rebuilds the run this record describes, standing on its node with the fight not yet entered.
    /// </summary>
    /// <returns>The restored run.</returns>
    /// <exception cref="ArgumentException">
    /// The campaign id is not one the game ships, or the stored squad does not match the campaign's.
    /// </exception>
    public RunState Restore() =>
        // Core assembles the state. The storage layer's job stops at three facts and a squad; whether
        // they are a legal run — the squad matching the campaign, a finished run being Complete, a run
        // past its last node having won — is the rules' business.
        Campaign.Restore(
            CampaignLibrary.ById(CampaignId),
            Seed,
            NodeIndex,
            Squad,
            FightsWon,
            Outcome,
            Route.Count == 0
                ? null
                : new MapState
                {
                    CurrentNodeId = Route[Route.Count - 1],
                    Route = Route,
                    Completed = ActCleared,
                },
            RngState,
            AtVote,
            AtCamp,
            CampsHeld,
            LastPickOwner,
            PreviousPickOwner,
            AtDestination);

    /// <summary>Renders the record as one <c>key: value</c> line per field.</summary>
    /// <returns>The stored text.</returns>
    public string Render()
    {
        var text = new StringBuilder();
        text.Append("id: ").Append(Id).Append('\n');
        text.Append("campaign: ").Append(CampaignId).Append('\n');
        text.Append("seed: ").Append(Number(Seed)).Append('\n');
        text.Append("node: ").Append(Number(NodeIndex)).Append('\n');
        text.Append("fights-won: ").Append(Number(FightsWon)).Append('\n');
        text.Append("outcome: ").Append(Outcome.ToString()).Append('\n');
        text.Append("in-fight: ").Append(WasInFight ? "yes" : "no").Append('\n');

        if (RngState is int rng)
        {
            text.Append("rng: ").Append(Number(rng)).Append('\n');
        }

        if (Route.Count > 0)
        {
            // One line, the nodes in order, because the order is the fact. A node id has no spaces
            // and no '>' in it, so the separator needs no escaping.
            text.Append("route: ").Append(string.Join(">", Route)).Append('\n');
            text.Append("act-cleared: ").Append(ActCleared ? "yes" : "no").Append('\n');
        }

        // Outside the route block, and both of them, because neither phase belongs to the graph: the
        // linear ten runs camps after its fights too, and a camp written down only for map runs is a
        // camp that a reload on the other shape silently walks past — back onto the node the run had
        // already cleared, which is the exact trap D-125 named.
        text.Append("at-vote: ").Append(AtVote ? "yes" : "no").Append('\n');
        text.Append("at-camp: ").Append(AtCamp ? "yes" : "no").Append('\n');
        text.Append("at-destination: ").Append(AtDestination ? "yes" : "no").Append('\n');
        text.Append("camps: ").Append(Number(CampsHeld)).Append('\n');

        if (LastPickOwner is Team last)
        {
            text.Append("last-pick: ").Append(last.ToString()).Append('\n');
        }

        if (PreviousPickOwner is Team previous)
        {
            text.Append("previous-pick: ").Append(previous.ToString()).Append('\n');
        }

        foreach (var unit in Squad)
        {
            // id, kind, carried hp, status, then the meter it is carrying, the ceiling this run has
            // bought it, and what the camps have hung on it. Positional and appended, so a record
            // written before any of the three still reads — ParseUnit needs four and takes seven.
            text.Append("unit: ")
                .Append(Number(unit.Id.Value)).Append(' ')
                .Append(unit.Kind).Append(' ')
                .Append(Number(unit.Hp)).Append(' ')
                .Append(unit.Status).Append(' ')
                .Append(Number(unit.Verve)).Append(' ')
                .Append(Number(unit.BonusMaxHp)).Append(' ')
                .Append(LoadoutText(unit.Loadout))
                .Append('\n');
        }

        return text.ToString();
    }

    /// <summary>Reads a record back out of storage.</summary>
    /// <param name="stored">Text produced by <see cref="Render"/>.</param>
    /// <returns>The record, or <c>null</c> when it is unreadable.</returns>
    public static RunSave? Parse(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored))
        {
            return null;
        }

        var fields = new Dictionary<string, string>(StringComparer.Ordinal);
        var squad = new List<RunUnit>();

        foreach (var line in stored!.Split('\n'))
        {
            int split = line.IndexOf(':');
            if (split < 0)
            {
                continue;
            }

            var key = line.Substring(0, split).Trim();
            var value = line.Substring(split + 1).Trim();

            if (string.Equals(key, "unit", StringComparison.Ordinal))
            {
                var unit = ParseUnit(value);
                if (unit is not null)
                {
                    squad.Add(unit);
                }

                continue;
            }

            fields[key] = value;
        }

        if (!fields.TryGetValue("id", out var id) || id.Length == 0)
        {
            return null;
        }

        var campaignId = fields.TryGetValue("campaign", out var raw) && raw.Length > 0
            ? raw
            : CampaignLibrary.FaultlineId;

        // A record naming a campaign this build does not ship is unreadable rather than a crash on
        // the next render.
        if (!Known(campaignId))
        {
            return null;
        }

        return new RunSave
        {
            Id = id,
            CampaignId = campaignId,
            Seed = Int(fields, "seed"),
            NodeIndex = Int(fields, "node"),
            FightsWon = Int(fields, "fights-won"),
            Outcome = fields.TryGetValue("outcome", out var outcome)
                && Enum.TryParse(outcome, out RunOutcome parsed)
                    ? parsed
                    : RunOutcome.InProgress,
            WasInFight = fields.TryGetValue("in-fight", out var inFight)
                && string.Equals(inFight, "yes", StringComparison.Ordinal),
            Squad = squad,
            Route = fields.TryGetValue("route", out var route) && route.Length > 0
                ? route.Split(new[] { '>' }, StringSplitOptions.RemoveEmptyEntries)
                : Array.Empty<string>(),
            ActCleared = fields.TryGetValue("act-cleared", out var cleared)
                && string.Equals(cleared, "yes", StringComparison.Ordinal),
            AtVote = fields.TryGetValue("at-vote", out var voting)
                && string.Equals(voting, "yes", StringComparison.Ordinal),
            AtCamp = fields.TryGetValue("at-camp", out var camping)
                && string.Equals(camping, "yes", StringComparison.Ordinal),

            // Absent in an older save, which is a run written before destinations existed and so a
            // run that cannot have been standing at one.
            AtDestination = fields.TryGetValue("at-destination", out var gilt)
                && string.Equals(gilt, "yes", StringComparison.Ordinal),
            CampsHeld = fields.TryGetValue("camps", out var camps)
                && int.TryParse(camps, NumberStyles.Integer, CultureInfo.InvariantCulture, out int held)
                    ? held
                    : 0,

            // Absent in an older save, which is a run that had no camp history to write down.
            LastPickOwner = fields.TryGetValue("last-pick", out var lastPick)
                && Enum.TryParse(lastPick, out Team lastOwner)
                    ? lastOwner
                    : (Team?)null,
            PreviousPickOwner = fields.TryGetValue("previous-pick", out var previousPick)
                && Enum.TryParse(previousPick, out Team previousOwner)
                    ? previousOwner
                    : (Team?)null,

            RngState = fields.TryGetValue("rng", out var rng)
                && int.TryParse(rng, NumberStyles.Integer, CultureInfo.InvariantCulture, out int cursor)
                    ? cursor
                    : (int?)null,
        };
    }

    private static RunUnit? ParseUnit(string value)
    {
        var parts = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4
            || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)
            || !Enum.TryParse(parts[1], out UnitKind kind)
            || !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int hp)
            || !Enum.TryParse(parts[3], out RunUnitStatus status))
        {
            // A line that cannot be read is dropped rather than guessed at: a squad member restored
            // at the wrong health is worse than one the player can see is missing.
            return null;
        }

        return new RunUnit
        {
            Id = new RunUnitId(id),
            Kind = kind,
            Hp = hp,
            Status = status,
            Verve = Optional(parts, 4),
            BonusMaxHp = Optional(parts, 5),
            Loadout = ParseLoadout(parts.Length > 6 ? parts[6] : string.Empty),
        };
    }

    /// <summary>
    /// What the camps gave one duck, as one space-free token: <c>m0,3|w1|u2|p4|s0,4,9</c>, and a bare
    /// <c>-</c> for a duck carrying nothing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Positional and appended, like every field before it, so a record written before camps existed
    /// still reads — <see cref="ParseUnit"/> needs four and takes seven. Indices rather than names,
    /// because these are enum members and the file is not a document anybody edits by hand.
    /// </para>
    /// <para>
    /// <b><c>s</c> is the duck's ability slots</b>, written only once surgery has changed them: an
    /// absent <c>s</c> reads back as the class's starting kit, which is what an empty
    /// <see cref="DuckLoadout.Slots"/> means everywhere else. Three saves have now shipped that
    /// dropped a field Core had grown (D-125, the camp, D-222), so this one arrives with the state.
    /// </para>
    /// </remarks>
    private static string LoadoutText(DuckLoadout loadout)
    {
        if (loadout is null || loadout.IsEmpty)
        {
            return "-";
        }

        var text = new StringBuilder();
        text.Append('m').Append(Numbers(loadout.Mods));
        text.Append("|w").Append(Numbers(loadout.SecondWinds));
        text.Append("|u").Append(Numbers(loadout.Unlocks));
        text.Append("|t").Append(Numbers(loadout.Techniques));
        text.Append("|p").Append(loadout.Pocket is { } pocket ? Number((int)pocket) : string.Empty);

        if (loadout.Slots.Count > 0)
        {
            text.Append("|s").Append(Numbers(loadout.Slots));
        }

        return text.ToString();
    }

    private static string Numbers<T>(IReadOnlyList<T> values)
        where T : struct
    {
        var parts = new List<string>(values.Count);
        foreach (var value in values)
        {
            parts.Add(Number(Convert.ToInt32(value, CultureInfo.InvariantCulture)));
        }

        return string.Join(",", parts);
    }

    /// <summary>The saved ability slots, or <c>null</c> when any entry is unreadable.</summary>
    private static KitEntry[]? ParseSlots(string body)
    {
        var raw = body.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (raw.Length == 0)
        {
            return null;
        }

        var slots = new KitEntry[raw.Length];
        for (int i = 0; i < raw.Length; i++)
        {
            if (!int.TryParse(raw[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
                || !Enum.IsDefined(typeof(KitEntry), value))
            {
                return null;
            }

            slots[i] = (KitEntry)value;
        }

        return slots;
    }

    private static DuckLoadout ParseLoadout(string token)
    {
        if (token.Length == 0 || string.Equals(token, "-", StringComparison.Ordinal))
        {
            return DuckLoadout.Empty;
        }

        var loadout = DuckLoadout.Empty;

        foreach (var part in token.Split('|'))
        {
            if (part.Length == 0)
            {
                continue;
            }

            char kind = part[0];
            var body = part.Substring(1);

            if (kind == 'p')
            {
                if (int.TryParse(body, NumberStyles.Integer, CultureInfo.InvariantCulture, out int pocket)
                    && Enum.IsDefined(typeof(Consumable), pocket))
                {
                    loadout = loadout.WithPocket((Consumable)pocket);
                }

                continue;
            }

            if (kind == 's')
            {
                // All of the slots or none of them. The others are a bag where dropping one unknown
                // member costs that member; this is an ordered kit of a fixed length, so a dropped
                // entry would silently hand the duck a shorter kit than it saved — a class quietly
                // losing a slot. An unreadable list falls back to the starting kit instead.
                if (ParseSlots(body) is { } slots)
                {
                    loadout = loadout with { Slots = slots };
                }

                continue;
            }

            foreach (var raw in body.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                {
                    continue;
                }

                // A value this build does not know is dropped rather than guessed at, the same way an
                // unreadable unit line is: a duck restored with somebody else's mod is worse than one
                // restored without it.
                loadout = kind switch
                {
                    'm' when Enum.IsDefined(typeof(Mod), value) => loadout.With((Mod)value),
                    'w' when Enum.IsDefined(typeof(SecondWind), value) => loadout.With((SecondWind)value),
                    'u' when Enum.IsDefined(typeof(Unlock), value) => loadout.With((Unlock)value),
                    't' when Enum.IsDefined(typeof(TechniqueModifier), value) =>
                        loadout.With((TechniqueModifier)value),
                    _ => loadout,
                };
            }
        }

        return loadout;
    }

    /// <summary>A trailing positional field, or zero when the record predates it.</summary>
    private static int Optional(string[] parts, int at) =>
        at < parts.Length
        && int.TryParse(parts[at], NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
            ? value
            : 0;

    private static bool Known(string campaignId)
    {
        foreach (var campaign in CampaignLibrary.All())
        {
            if (string.Equals(campaign.Id, campaignId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static int Int(Dictionary<string, string> fields, string key) =>
        fields.TryGetValue(key, out var raw)
        && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
            ? value
            : 0;

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
}
