using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Faultline.Web.Shell;

/// <summary>
/// Acts built in the UI, kept in browser localStorage so a shape survives a reload and can be
/// iterated on rather than rebuilt.
/// </summary>
/// <remarks>
/// Same shape and same storage idiom as <see cref="CustomFightStore"/> and <see cref="LoadoutStore"/>
/// — an index key listing the slugs and one key per item. The DRAFT persists; a run walking one does
/// not (see <c>RunSession.StartCustomAsync</c>).
/// </remarks>
public sealed class ActStore
{
    private const string IndexKey = "pluck.acts";
    private const string ItemPrefix = "pluck.act.";

    private readonly FightFiles _files;
    private readonly List<SavedAct> _items = new();

    /// <summary>Creates the store over the browser's storage.</summary>
    /// <param name="files">Storage access.</param>
    public ActStore(FightFiles files) => _files = files;

    /// <summary>Everything saved, in the order it was stored.</summary>
    public IReadOnlyList<SavedAct> Items => _items;

    /// <summary>True once storage has been read at least once.</summary>
    public bool Loaded { get; private set; }

    /// <summary>Reads every stored act back.</summary>
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
                _items.Add(new SavedAct(id, ActDraft.FromText(text)));
            }
        }

        Loaded = true;
    }

    /// <summary>Stores an act under its slug, replacing any earlier save with the same name.</summary>
    /// <param name="draft">The act; its name becomes the slug.</param>
    /// <returns>The slug it was stored under.</returns>
    public async Task<string> SaveAsync(ActDraft draft)
    {
        var slug = CustomFightStore.Slug(draft.Name);
        if (string.IsNullOrEmpty(slug))
        {
            slug = "act";
        }

        await _files.SetAsync(ItemPrefix + slug, draft.ToText());

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

    /// <summary>Forgets one stored act.</summary>
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

/// <summary>One stored act and the slug it lives under.</summary>
/// <param name="Id">Storage slug.</param>
/// <param name="Draft">The act.</param>
public sealed record SavedAct(string Id, ActDraft Draft);
