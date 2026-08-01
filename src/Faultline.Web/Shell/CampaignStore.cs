using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Faultline.Core;

namespace Faultline.Web.Shell;

/// <summary>
/// The campaign run, kept in browser localStorage so a reload does not throw away the seed, the
/// progress or the dead.
/// </summary>
/// <remarks>
/// <para>
/// One key per run record plus a comma-separated index key, the same shape as
/// <see cref="CustomFightStore"/> and <see cref="PlaytestNotes"/>. A single JSON blob would put the
/// whole run at the mercy of one quota failure, and the record format is hand-written so nothing
/// depends on a serialiser surviving trimming.
/// </para>
/// <para>
/// What survives a reload is the run — seed, slot, cleared list, dead list — not the half-played
/// board. Restoring a board mid-fight would mean persisting the command log, which is a bigger
/// promise than "a reload does not cost you the run"; the current fight simply starts again from
/// its first deployment, on the same seed, with the same survivors.
/// </para>
/// </remarks>
public sealed class CampaignStore
{
    private const string IndexKey = "faultline.campaigns";
    private const string ItemPrefix = "faultline.campaign.";

    private readonly FightFiles _files;

    /// <summary>Creates the store.</summary>
    /// <param name="files">Browser storage access.</param>
    public CampaignStore(FightFiles files) => _files = files;

    /// <summary>The run in this browser, or <c>null</c> when nobody has started one.</summary>
    public CampaignRun? Run { get; private set; }

    /// <summary>True once <see cref="LoadAsync"/> has run at least once.</summary>
    public bool Loaded { get; private set; }

    /// <summary>The fights Core currently hands out, by id. Re-read on every load.</summary>
    public IReadOnlyDictionary<string, FightDefinition> Active { get; private set; } =
        new Dictionary<string, FightDefinition>(StringComparer.Ordinal);

    /// <summary>True when there is a run with a fight still to play.</summary>
    public bool HasRunInProgress => Run is { } run && run.InProgress;

    /// <summary>The authored fight the run is sitting on, or <c>null</c> when the run is over.</summary>
    public FightDefinition? CurrentFight
    {
        get
        {
            var id = Run?.CurrentId;
            return id is not null && Active.TryGetValue(id, out var fight) ? fight : null;
        }
    }

    /// <summary>
    /// The current fight as the run must play it — the authored board minus the voided classes'
    /// roster slots.
    /// </summary>
    public FightDefinition? CurrentAdapted
    {
        get
        {
            var fight = CurrentFight;
            return fight is null || Run is null ? null : Run.Adapt(fight);
        }
    }

    /// <summary>Reads the run back and re-settles it against the fights that exist right now.</summary>
    /// <returns>A task that completes when <see cref="Run"/> is current.</returns>
    public async Task LoadAsync()
    {
        Active = CampaignPlan.Active();
        Run = null;

        var index = await _files.GetAsync(IndexKey) ?? string.Empty;
        foreach (var id in index.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var run = CampaignRun.Parse(await _files.GetAsync(ItemPrefix + id));
            if (run is not null)
            {
                Run = run;
            }
        }

        if (Run is not null)
        {
            // Settling on load is what makes a newly authored fight light up without a migration,
            // and what stops a run pointing at a fight that has since been retired.
            var settled = Run.Settle(Active);

            // Compared as stored text: the record holds lists, so `with` always produces a new
            // instance and record equality would call every load a change.
            if (!string.Equals(settled.Render(), Run.Render(), StringComparison.Ordinal))
            {
                await WriteAsync(settled);
            }

            Run = settled;
        }

        Loaded = true;
    }

    /// <summary>Throws away the current run and starts a new one from the top of the spine.</summary>
    /// <param name="seed">Run seed.</param>
    /// <returns>The new run.</returns>
    public async Task<CampaignRun> StartAsync(int seed)
    {
        Active = CampaignPlan.Active();

        if (Run is not null)
        {
            await _files.RemoveAsync(ItemPrefix + Run.Id);
        }

        var run = CampaignRun.Begin(NextId(), seed).Settle(Active);
        await WriteAsync(run);
        Run = run;
        Loaded = true;
        return run;
    }

    /// <summary>Forgets the run entirely.</summary>
    /// <returns>A task that completes when storage is clear.</returns>
    public async Task AbandonAsync()
    {
        if (Run is not null)
        {
            await _files.RemoveAsync(ItemPrefix + Run.Id);
        }

        await _files.SetAsync(IndexKey, string.Empty);
        Run = null;
    }

    /// <summary>
    /// Files the result of the fight that just ended: a win advances to the next authored slot, a
    /// loss ends the run. Either way the voided are read off the final state first.
    /// </summary>
    /// <param name="state">The finished fight state.</param>
    /// <returns>The updated run, or <c>null</c> when there was none.</returns>
    public async Task<CampaignRun?> RecordOutcomeAsync(GameState state)
    {
        if (Run is null || state is null || state.Outcome == FightOutcome.InProgress)
        {
            return Run;
        }

        Active = CampaignPlan.Active();

        var run = state.Outcome == FightOutcome.Won
            ? Run.Advance(state, Active)
            : Run.Fail(state);

        await WriteAsync(run);
        Run = run;
        return run;
    }

    /// <summary>
    /// Loads the run's current fight into the session, with the surviving squad.
    /// </summary>
    /// <param name="session">Session to start the fight in.</param>
    /// <returns>True when a fight was started.</returns>
    public bool Resume(GameSession session)
    {
        var fight = CurrentAdapted;
        if (session is null || fight is null || Run is null || !Run.InProgress)
        {
            return false;
        }

        session.StartCampaignFight(fight, Run.Seed);
        return true;
    }

    private async Task WriteAsync(CampaignRun run)
    {
        await _files.SetAsync(ItemPrefix + run.Id, run.Render());
        await _files.SetAsync(IndexKey, run.Id);
    }

    private static string NextId() =>
        DateTime.UtcNow.Ticks.ToString("D19", CultureInfo.InvariantCulture);
}
