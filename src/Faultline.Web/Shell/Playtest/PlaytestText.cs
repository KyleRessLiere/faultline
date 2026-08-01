using System.Collections.Generic;
using Faultline.Core;

namespace Faultline.Web.Shell.Playtest;

/// <summary>
/// The words and class names the playtest panels share. One copy so the header, the board and the
/// unit table never disagree about what a phase or a side is called.
/// </summary>
public static class PlaytestText
{
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
}
