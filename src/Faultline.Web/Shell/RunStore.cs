using System;
using System.Globalization;
using System.Threading.Tasks;
using Faultline.Core;

namespace Faultline.Web.Shell;

/// <summary>
/// The run record in browser localStorage, so a reload does not throw away the seed, the progress
/// or the damage the squad is carrying.
/// </summary>
/// <remarks>
/// One key per run record plus an index key, the same shape as <see cref="CustomFightStore"/> and
/// <see cref="PlaytestNotes"/>. A single JSON blob would put the whole run at the mercy of one quota
/// failure, and the record format is hand-written so nothing depends on a serialiser surviving
/// trimming.
/// </remarks>
public sealed class RunStore
{
    private const string IndexKey = "faultline.runs";
    private const string ItemPrefix = "faultline.run.";

    private readonly FightFiles _files;

    /// <summary>Creates the store.</summary>
    /// <param name="files">Browser storage access.</param>
    public RunStore(FightFiles files) => _files = files;

    /// <summary>Storage id of the run currently written, or <c>null</c> when there is none.</summary>
    public string? Id { get; private set; }

    /// <summary>Reads the stored run back, if this browser holds one.</summary>
    /// <returns>The record, or <c>null</c>.</returns>
    public async Task<RunSave?> ReadAsync()
    {
        Id = null;

        var index = await _files.GetAsync(IndexKey) ?? string.Empty;
        RunSave? found = null;

        foreach (var id in index.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var save = RunSave.Parse(await _files.GetAsync(ItemPrefix + id.Trim()));
            if (save is not null)
            {
                found = save;
                Id = save.Id;
            }
        }

        return found;
    }

    /// <summary>Writes a run, replacing whatever was there.</summary>
    /// <param name="state">The run to save.</param>
    /// <returns>The record written.</returns>
    public async Task<RunSave> WriteAsync(RunState state)
    {
        Id ??= NextId();

        var save = RunSave.Of(Id, state);
        await _files.SetAsync(ItemPrefix + save.Id, save.Render());
        await _files.SetAsync(IndexKey, save.Id);
        return save;
    }

    /// <summary>Starts a fresh record, so the next write does not land on the old run's key.</summary>
    public void Rotate() => Id = NextId();

    /// <summary>Forgets the run entirely.</summary>
    /// <returns>A task that completes when storage is clear.</returns>
    public async Task ClearAsync()
    {
        if (Id is not null)
        {
            await _files.RemoveAsync(ItemPrefix + Id);
        }

        await _files.SetAsync(IndexKey, string.Empty);
        Id = null;
    }

    private static string NextId() =>
        DateTime.UtcNow.Ticks.ToString("D19", CultureInfo.InvariantCulture);
}
