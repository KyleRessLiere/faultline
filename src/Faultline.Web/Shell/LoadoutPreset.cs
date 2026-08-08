using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Faultline.Core;

namespace Faultline.Web.Shell;

/// <summary>
/// A named test loadout, <b>keyed by class rather than by roster slot</b>, so one saved setup can be
/// dropped onto any board.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="TestLoadout"/> is positional because <see cref="SquadLoadout"/> is: a board may roster
/// the same class twice and two Vanguards on different hit points would be indistinguishable under a
/// class-keyed map. A <em>saved</em> loadout has the opposite problem — slot 0 is a Vanguard on one
/// board and a Fisher on the next, so a positional preset applied to a second board would hand a
/// Vanguard's hit points to whatever happened to be rostered first.
/// </para>
/// <para>
/// So the preset is keyed by <see cref="UnitKind"/> and <b>applying it is a per-slot lookup</b>:
/// every slot whose class the preset knows takes that class's entry, and a board rostering two
/// Vanguards gives both the same build. That is the right answer for a test bench, where "my tanky
/// Vanguard" is the thing being reused.
/// </para>
/// </remarks>
public sealed class LoadoutPreset
{
    /// <summary>What one class starts holding.</summary>
    public sealed class ClassSetup
    {
        /// <summary>Hit point ceiling, or <c>null</c> for the archetype's own.</summary>
        public int? MaxHp { get; set; }

        /// <summary>Hit points to open on, or <c>null</c> for full.</summary>
        public int? Hp { get; set; }

        /// <summary>The one pocket item, or <c>null</c>.</summary>
        public Consumable? Item { get; set; }

        /// <summary>Technique modifiers this class carries.</summary>
        public List<TechniqueModifier> Techniques { get; } = new();

        /// <summary>Mods this class carries.</summary>
        public List<Mod> Mods { get; } = new();

        /// <summary>True while nothing has been set.</summary>
        public bool IsDefault =>
            MaxHp is null && Hp is null && Item is null && Techniques.Count == 0 && Mods.Count == 0;
    }

    /// <summary>Display name, as typed.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Per-class setups, by archetype.</summary>
    public Dictionary<UnitKind, ClassSetup> Classes { get; } = new();

    /// <summary>That class's setup, created on first ask.</summary>
    /// <param name="kind">The archetype.</param>
    /// <returns>Its setup.</returns>
    public ClassSetup For(UnitKind kind)
    {
        if (!Classes.TryGetValue(kind, out var setup))
        {
            setup = new ClassSetup();
            Classes[kind] = setup;
        }

        return setup;
    }

    /// <summary>True when any class has something set.</summary>
    public bool IsCustom => Classes.Values.Any(c => !c.IsDefault);

    /// <summary>
    /// Reads this preset out of a board's bench, so "save what I just built" needs no second editor.
    /// </summary>
    /// <param name="bench">The board's positional bench.</param>
    /// <param name="fight">The board, for the class in each slot.</param>
    /// <param name="name">What to call it.</param>
    /// <returns>The preset.</returns>
    public static LoadoutPreset From(TestLoadout bench, FightDefinition fight, string name)
    {
        var preset = new LoadoutPreset { Name = name };
        if (bench is null || fight is null)
        {
            return preset;
        }

        foreach (var (team, kind, slot) in Slots(fight))
        {
            var duck = bench.For(team, slot);
            if (duck.IsDefault)
            {
                continue;
            }

            // Last slot of a repeated class wins. A preset is one build per class by construction;
            // a board that rosters two Vanguards differently cannot be a class-keyed preset, and
            // saying so by collapsing is better than saving something that cannot be reapplied.
            var entry = preset.For(kind);
            entry.MaxHp = duck.MaxHp;
            entry.Hp = duck.Hp;
            entry.Item = duck.Item;
            entry.Techniques.Clear();
            entry.Techniques.AddRange(duck.Techniques);
            entry.Mods.Clear();
            entry.Mods.AddRange(duck.Mods);
        }

        return preset;
    }

    /// <summary>Writes this preset onto a board's bench, matching by class.</summary>
    /// <param name="bench">The board's positional bench, cleared first.</param>
    /// <param name="fight">The board, for the class in each slot.</param>
    public void ApplyTo(TestLoadout bench, FightDefinition fight)
    {
        if (bench is null || fight is null)
        {
            return;
        }

        bench.Reset();

        foreach (var (team, kind, slot) in Slots(fight))
        {
            if (!Classes.TryGetValue(kind, out var entry))
            {
                continue;
            }

            var duck = bench.For(team, slot);
            duck.MaxHp = entry.MaxHp;
            duck.Hp = entry.Hp;
            duck.Item = entry.Item;
            duck.Techniques.Clear();
            duck.Techniques.AddRange(entry.Techniques);
            duck.Mods.Clear();
            duck.Mods.AddRange(entry.Mods);
        }
    }

    /// <summary>How many of a board's slots this preset would touch.</summary>
    /// <param name="fight">The board.</param>
    /// <returns>The slot count, so a preset that fits nothing can say so.</returns>
    public int MatchingSlots(FightDefinition fight) =>
        fight is null ? 0 : Slots(fight).Count(s => Classes.ContainsKey(s.Kind));

    /// <summary>Both rosters as (side, archetype, slot), in the order the board fields them.</summary>
    /// <param name="fight">The board.</param>
    /// <returns>Player A's slots then Player B's, each with the class rostered there.</returns>
    public static IEnumerable<(Team Team, UnitKind Kind, int Slot)> Slots(FightDefinition fight)
    {
        for (int i = 0; i < fight.RosterA.Count; i++)
        {
            yield return (Team.PlayerA, fight.RosterA[i], i);
        }

        for (int i = 0; i < fight.RosterB.Count; i++)
        {
            yield return (Team.PlayerB, fight.RosterB[i], i);
        }
    }

    // ---- Storage -------------------------------------------------------------------------------
    //
    // A flat line-oriented text format rather than JSON: Core targets netstandard2.1 and the shell
    // has no serializer dependency to hand, the values are all enums and small integers, and a
    // preset a person can read in localStorage is a preset a person can fix when it goes wrong.

    /// <summary>Renders the preset as storable text.</summary>
    /// <returns>One line per field.</returns>
    public string ToText()
    {
        var lines = new List<string> { "name=" + Name };

        foreach (var pair in Classes.OrderBy(p => (int)p.Key))
        {
            var setup = pair.Value;
            if (setup.IsDefault)
            {
                continue;
            }

            lines.Add("class=" + pair.Key);

            if (setup.MaxHp is { } max)
            {
                lines.Add("maxhp=" + max.ToString(CultureInfo.InvariantCulture));
            }

            if (setup.Hp is { } hp)
            {
                lines.Add("hp=" + hp.ToString(CultureInfo.InvariantCulture));
            }

            if (setup.Item is { } item)
            {
                lines.Add("item=" + item);
            }

            foreach (var technique in setup.Techniques)
            {
                lines.Add("technique=" + technique);
            }

            foreach (var mod in setup.Mods)
            {
                lines.Add("mod=" + mod);
            }
        }

        return string.Join("\n", lines);
    }

    /// <summary>Reads a preset back. Unknown names are skipped rather than fatal.</summary>
    /// <param name="text">Stored text.</param>
    /// <returns>The preset.</returns>
    /// <remarks>
    /// Forgiving on purpose: a stored preset outlives the enum it names, and a renamed card should
    /// cost a tester one checkbox rather than the whole saved build.
    /// </remarks>
    public static LoadoutPreset FromText(string? text)
    {
        var preset = new LoadoutPreset();
        if (string.IsNullOrWhiteSpace(text))
        {
            return preset;
        }

        ClassSetup? current = null;

        foreach (var raw in text!.Split('\n'))
        {
            var line = raw.Trim();
            int split = line.IndexOf('=');
            if (split <= 0)
            {
                continue;
            }

            var key = line.Substring(0, split);
            var value = line.Substring(split + 1);

            switch (key)
            {
                case "name":
                    preset.Name = value;
                    break;
                case "class":
                    current = Enum.TryParse<UnitKind>(value, out var kind) ? preset.For(kind) : null;
                    break;
                case "maxhp":
                    if (current is not null && int.TryParse(value, out int max)) { current.MaxHp = max; }

                    break;
                case "hp":
                    if (current is not null && int.TryParse(value, out int hp)) { current.Hp = hp; }

                    break;
                case "item":
                    if (current is not null && Enum.TryParse<Consumable>(value, out var item))
                    {
                        current.Item = item;
                    }

                    break;
                case "technique":
                    if (current is not null && Enum.TryParse<TechniqueModifier>(value, out var tech))
                    {
                        current.Techniques.Add(tech);
                    }

                    break;
                case "mod":
                    if (current is not null && Enum.TryParse<Mod>(value, out var mod))
                    {
                        current.Mods.Add(mod);
                    }

                    break;
            }
        }

        return preset;
    }
}
