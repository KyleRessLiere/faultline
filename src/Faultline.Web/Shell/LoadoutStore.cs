using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Faultline.Web.Shell;

/// <summary>
/// Saved test loadouts, kept in browser localStorage so a build survives a reload and can be dropped
/// onto a different board.
/// </summary>
/// <remarks>
/// Same shape and same storage as <see cref="CustomFightStore"/> — an index key listing the slugs and
/// one key per item — because there is no reason for two persistence idioms in one shell. A stored
/// preset is a <b>dev convenience</b> and nothing reads it but the picker's bench.
/// </remarks>
public sealed class LoadoutStore
{
    private const string IndexKey = "pluck.loadouts";
    private const string ItemPrefix = "pluck.loadout.";

    private readonly FightFiles _files;
    private readonly List<SavedLoadout> _items = new();

    /// <summary>Creates the store over the browser's storage.</summary>
    /// <param name="files">Storage access.</param>
    public LoadoutStore(FightFiles files) => _files = files;

    /// <summary>Everything saved, in the order it was stored.</summary>
    public IReadOnlyList<SavedLoadout> Items => _items;

    /// <summary>True once storage has been read at least once.</summary>
    public bool Loaded { get; private set; }

    /// <summary>Reads every stored preset back.</summary>
    /// <returns>A task that completes when the list is current.</returns>
    public async Task LoadAsync()
    {
        _items.Clear();

        var index = await _files.GetAsync(IndexKey) ?? string.Empty;
        foreach (var id in index.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var text = await _files.GetAsync(ItemPrefix + id);
            if (!string.IsNullOrWhiteSpace(text))
            {
                _items.Add(new SavedLoadout(id, LoadoutPreset.FromText(text)));
            }
        }

        Loaded = true;
    }

    /// <summary>Stores a preset under its slug, replacing any earlier save with the same name.</summary>
    /// <param name="preset">The preset; its <see cref="LoadoutPreset.Name"/> becomes the slug.</param>
    /// <returns>The slug it was stored under.</returns>
    public async Task<string> SaveAsync(LoadoutPreset preset)
    {
        var slug = CustomFightStore.Slug(preset.Name);
        if (string.IsNullOrEmpty(slug))
        {
            slug = "loadout";
        }

        await _files.SetAsync(ItemPrefix + slug, preset.ToText());

        var ids = new List<string>();
        foreach (var item in _items)
        {
            if (!string.Equals(item.Id, slug, StringComparison.Ordinal))
            {
                ids.Add(item.Id);
            }
        }

        ids.Add(slug);
        await _files.SetAsync(IndexKey, string.Join(",", ids));
        await LoadAsync();
        return slug;
    }

    /// <summary>Forgets one stored preset.</summary>
    /// <param name="id">Its slug.</param>
    /// <returns>A task that completes when the list is current.</returns>
    public async Task DeleteAsync(string id)
    {
        await _files.RemoveAsync(ItemPrefix + id);

        var ids = new List<string>();
        foreach (var item in _items)
        {
            if (!string.Equals(item.Id, id, StringComparison.Ordinal))
            {
                ids.Add(item.Id);
            }
        }

        await _files.SetAsync(IndexKey, string.Join(",", ids));
        await LoadAsync();
    }
}

/// <summary>One stored preset and the slug it lives under.</summary>
/// <param name="Id">Storage slug.</param>
/// <param name="Preset">The build.</param>
public sealed record SavedLoadout(string Id, LoadoutPreset Preset);
