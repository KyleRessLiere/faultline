using System.Linq;
using System;
using System.Collections.Generic;
using Faultline.Core;

namespace Faultline.Web.Shell.Playtest;

/// <summary>
/// The words and class names the playtest panels share. One copy so the header, the board and the
/// unit table never disagree about what a phase or a side is called.
/// </summary>
public static class PlaytestText
{
    /// <summary>
    /// What a guarding unit's stance is called, straight off Core's descriptor so the board, the
    /// unit panel and the reference never disagree about the name.
    /// </summary>
    public static string GuardName => AbilityDescriptor.For(Ability.GuardStance).Name;

    /// <summary>The stance's rules text, for a tooltip.</summary>
    public static string GuardSummary => AbilityDescriptor.For(Ability.GuardStance).Summary;

    /// <summary>The phase, as a person reads it.</summary>
    /// <param name="phase">Phase to name.</param>
    /// <returns>A display label.</returns>
    public static string Phase(Faultline.Core.Phase phase) => phase switch
    {
        Faultline.Core.Phase.Deployment => "Deployment",
        Faultline.Core.Phase.Battle => "Battle",
        _ => "Complete",
    };

    /// <summary>The side, as a person reads it.</summary>
    /// <param name="team">Team to name.</param>
    /// <returns>A display label.</returns>
    public static string Team(Faultline.Core.Team team) => team switch
    {
        Faultline.Core.Team.PlayerA => "Player A",
        Faultline.Core.Team.PlayerB => "Player B",
        _ => "Enemy",
    };

    /// <summary>The one-letter class suffix a side's colour is keyed on.</summary>
    /// <param name="team">Team to classify.</param>
    /// <returns><c>a</c>, <c>b</c> or <c>e</c>.</returns>
    public static string TeamClass(Faultline.Core.Team team) => team switch
    {
        Faultline.Core.Team.PlayerA => "a",
        Faultline.Core.Team.PlayerB => "b",
        _ => "e",
    };

    /// <summary>Hit points, or what replaced them.</summary>
    /// <param name="unit">Unit to describe.</param>
    /// <returns>A short label.</returns>
    public static string Hp(Unit unit) =>
        unit.Voided ? "voided" : unit.IsAlive ? $"{unit.Hp}/{unit.MaxHp}" : "down";

    /// <summary>
    /// The tooltip behind a unit's Verve dots: what it holds, what earns more, and what it is saving
    /// for. Every word of it comes from Core, so the meter on the board and the rule behind it cannot
    /// drift apart.
    /// </summary>
    /// <param name="unit">Unit to describe.</param>
    /// <returns>A one-line tooltip, empty for a class with no meter.</returns>
    public static string VerveTitle(Unit unit)
    {
        var spender = Verve.SpendFor(unit.Kind);
        if (spender is null)
        {
            return string.Empty;
        }

        int cost = Verve.CostOf(spender.Value);
        string state = unit.Verve >= cost
            ? $"{Verve.NameOf(spender.Value)} ready"
            : $"{cost - unit.Verve} more for {Verve.NameOf(spender.Value)}";

        return $"Verve {unit.Verve}/{Verve.Cap} — {state}. Earns from {Verve.ConditionFor(unit.Kind)}.";
    }

    /// <summary>The status flags worth showing beside a unit.</summary>
    /// <param name="unit">Unit to describe.</param>
    /// <returns>A comma-separated list, possibly empty.</returns>
    public static string Flags(Unit unit)
    {
        var flags = new List<string>();

        if (unit.Clinging)
        {
            flags.Add("clinging");
        }

        if (unit.Staggered)
        {
            flags.Add("staggered");
        }

        // A stance is invisible on the board unless something says so, and it is the whole reason the
        // unit spent its action. The word is Core's ability name, lower-cased to sit in the list.
        if (unit.Guarding)
        {
            flags.Add(GuardName.ToLowerInvariant());
        }

        // Footing is not listed: several archetypes start on zero, so a "no footing" on every row
        // says nothing. The board draws a pip for it and the selected-unit panel gives the number.
        if (unit.HasActivated && unit.IsOnBoard)
        {
            flags.Add("done");
        }

        return string.Join(", ", flags);
    }

    /// <summary>How far a unit's basic action reaches, for the unit panels.</summary>
    /// <param name="unit">Unit to describe.</param>
    /// <returns>A short label.</returns>
    public static string Reach(Unit unit)
    {
        var template = unit.Template;

        if (template.Attack == AttackKind.Melee)
        {
            return "melee";
        }

        if (template.Attack == AttackKind.Ranged)
        {
            return template.Range.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        return template.BasicReach > 0
            ? template.BasicReach.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : "—";
    }

    /// <summary>What an objective structure is and how it can be hurt.</summary>
    /// <param name="structure">Structure to describe.</param>
    /// <returns>A tooltip sentence.</returns>
    public static string Structure(Structure structure) =>
        structure.Role == ObjectiveKind.Protect
            ? $"Protect this — {structure.Hp}/{structure.MaxHp} HP. Enemies claw at it from adjacent tiles."
            : $"Destroy this — {structure.Hp}/{structure.MaxHp} HP. Immune to attacks; only collision damage counts.";

    /// <summary>
    /// Names as a person would say them: "A", "A or B", "A, B or C".
    /// </summary>
    /// <remarks>
    /// The turn summary lists every unit that can still activate rather than naming one, because
    /// within a side's slot the player may choose any un-activated unit. Naming a single one would
    /// invent an activation order the rules do not have.
    /// </remarks>
    /// <param name="names">Names in board order.</param>
    /// <returns>The joined phrase, or empty for no names.</returns>
    public static string Names(IReadOnlyList<string> names)
    {
        if (names is null || names.Count == 0)
        {
            return string.Empty;
        }

        return names.Count switch
        {
            1 => names[0],
            2 => names[0] + " or " + names[1],
            _ => string.Join(", ", names.Take(names.Count - 1)) + " or " + names[names.Count - 1],
        };
    }

    /// <summary>
    /// Which halves of its activation the acting unit still has, read straight off the unit.
    /// </summary>
    /// <param name="unit">The unit that is acting.</param>
    /// <returns>One sentence.</returns>
    public static string Halves(Unit unit)
    {
        if (unit is null)
        {
            throw new ArgumentNullException(nameof(unit));
        }

        return (unit.HasMoved, unit.HasActed) switch
        {
            (false, false) => "Move and action both unspent.",
            (true, false) => "Move spent — action still to use.",
            (false, true) => "Action spent — move still to use.",
            _ => "Move and action both spent.",
        };
    }

}
