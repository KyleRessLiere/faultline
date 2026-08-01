using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Faultline.Core;

namespace Faultline.Web.Shell;

/// <summary>
/// Scenarios the player built in the creator, kept in browser localStorage so they survive a
/// refresh and show up in the battle picker straight away.
/// </summary>
/// <remarks>
/// <para>
/// Only the <c>.fight</c> text is stored — never a serialised object. Reloading therefore goes back
/// through <see cref="FightParser"/>, so a stored scenario is validated by exactly the same code as
/// an embedded one and cannot drift into a shape the parser would reject.
/// </para>
/// <para>
/// One key per scenario plus a comma-separated index key, rather than a JSON blob, so nothing here
/// depends on a serialiser surviving trimming.
/// </para>
/// </remarks>
public sealed class CustomFightStore
{
    private const string IndexKey = "faultline.customFights";
    private const string ItemPrefix = "faultline.fight.";

    private readonly FightFiles _files;
    private readonly List<CustomFight> _items = new();

    /// <summary>Creates the store.</summary>
    /// <param name="files">Browser storage access.</param>
    public CustomFightStore(FightFiles files) => _files = files;

    /// <summary>Custom scenarios, newest save last, each with its parse result.</summary>
    public IReadOnlyList<CustomFight> Items => _items;

    /// <summary>True once <see cref="LoadAsync"/> has run at least once.</summary>
    public bool Loaded { get; private set; }

    /// <summary>Turns free text into a filename- and key-safe slug.</summary>
    /// <param name="text">Raw id typed by the designer.</param>
    /// <returns>A lower-case slug, or <c>"untitled"</c> when nothing usable is left.</returns>
    public static string Slug(string? text)
    {
        var slug = new StringBuilder();
        foreach (char c in text ?? string.Empty)
        {
            if (char.IsLetterOrDigit(c))
            {
                slug.Append(char.ToLowerInvariant(c));
            }
            else if (slug.Length > 0 && slug[slug.Length - 1] != '-')
            {
                slug.Append('-');
            }
        }

        var result = slug.ToString().Trim('-');
        return result.Length == 0 ? "untitled" : result;
    }

    /// <summary>Finds a stored scenario by slug.</summary>
    /// <param name="id">Scenario slug; slugged again so a raw id works too.</param>
    /// <returns>The stored scenario, or <c>null</c> when this browser has never seen it.</returns>
    public CustomFight? Find(string? id)
    {
        var slug = Slug(id);
        foreach (var item in _items)
        {
            if (string.Equals(item.Id, slug, StringComparison.Ordinal))
            {
                return item;
            }
        }

        return null;
    }

    /// <summary>Whether saving under this id would replace an existing scenario.</summary>
    /// <param name="id">Scenario id; slugged before the check.</param>
    /// <returns>True when something is already stored there.</returns>
    public bool Exists(string? id) => Find(id) is not null;

    /// <summary>
    /// The first id near <paramref name="desired"/> that nothing is stored under, so "save a copy"
    /// can never quietly eat the scenario it was copied from.
    /// </summary>
    /// <param name="desired">Preferred slug.</param>
    /// <returns><paramref name="desired"/> when it is free, otherwise it with <c>-2</c>, <c>-3</c>… appended.</returns>
    public string FreeId(string? desired)
    {
        var slug = Slug(desired);
        if (!Exists(slug))
        {
            return slug;
        }

        for (int n = 2; n < 1000; n++)
        {
            var candidate = slug + "-" + n.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (!Exists(candidate))
            {
                return candidate;
            }
        }

        return slug;
    }

    /// <summary>Reads every stored scenario back and re-parses it.</summary>
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
                _items.Add(new CustomFight(id, text!, FightParser.Parse(text!)));
            }
        }

        Loaded = true;
    }

    /// <summary>Stores a scenario under its slug, replacing any earlier save with the same id.</summary>
    /// <param name="id">Scenario id; slugged before use.</param>
    /// <param name="text">The <c>.fight</c> contents.</param>
    /// <returns>The slug it was stored under.</returns>
    public async Task<string> SaveAsync(string id, string text)
    {
        var slug = Slug(id);
        await _files.SetAsync(ItemPrefix + slug, text);

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

    /// <summary>Forgets one stored scenario.</summary>
    /// <param name="id">Scenario slug.</param>
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

/// <summary>One stored scenario and what the parser makes of it.</summary>
/// <param name="Id">Storage slug.</param>
/// <param name="Text">The <c>.fight</c> file contents.</param>
/// <param name="Result">Parse result, carrying errors and lints.</param>
public sealed record CustomFight(string Id, string Text, FightParseResult Result);
