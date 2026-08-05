using Faultline.Core;

namespace Faultline.Web.Shell.RunMap;

/// <summary>
/// How a squad member reads on a run screen: its health, its state and the class the two are drawn
/// under.
/// </summary>
/// <remarks>
/// One place, because the front door lists the squad as one-liners and the map draws it as a strip
/// of portraits, and the two must never disagree about whether a duck is out of the run. Every
/// string goes through <see cref="Naming"/> (§15) — a run screen has never spelled a
/// <see cref="UnitKind"/> and is not going to start (D-135).
/// </remarks>
public static class SquadView
{
    /// <summary>The duck's display name.</summary>
    /// <param name="unit">The squad member.</param>
    /// <returns>Its name on screen.</returns>
    public static string NameOf(RunUnit unit) => Naming.Of(unit.Kind);

    /// <summary>
    /// Its health, said the way its state means it.
    /// </summary>
    /// <remarks>
    /// A voided duck has no number worth printing — it is not on 0, it is gone. A downed one reads
    /// 0 of its ceiling, because that is what being down is; what it comes back on is a separate
    /// sentence and belongs on the badge.
    /// </remarks>
    /// <param name="unit">The squad member.</param>
    /// <returns>One short string.</returns>
    public static string HpText(RunUnit unit) => unit.Status switch
    {
        RunUnitStatus.Voided => "—",
        RunUnitStatus.Downed => "0/" + unit.MaxHp,
        _ => unit.Hp + "/" + unit.MaxHp,
    };

    /// <summary>The class its line or portrait is drawn under.</summary>
    /// <param name="unit">The squad member.</param>
    /// <returns>A lower-case class, or an empty string when there is nothing to say.</returns>
    public static string CssClass(RunUnit unit) => unit.Status switch
    {
        RunUnitStatus.Voided => "lost",
        RunUnitStatus.Downed => "hurt",
        _ => unit.Hp < unit.MaxHp ? "hurt" : string.Empty,
    };

    /// <summary>The badge it wears, when it wears one.</summary>
    /// <param name="unit">The squad member.</param>
    /// <returns>A short badge, or an empty string.</returns>
    public static string Badge(RunUnit unit) => unit.Status switch
    {
        RunUnitStatus.Voided => "voided",
        RunUnitStatus.Downed => "bedraggled",
        _ => string.Empty,
    };

    /// <summary>What the badge means, in full, for the hover.</summary>
    /// <param name="unit">The squad member.</param>
    /// <returns>One sentence, or an empty string when there is no badge.</returns>
    public static string BadgeTitle(RunUnit unit) => unit.Status switch
    {
        RunUnitStatus.Voided => "Swept down a drain. Gone for the run — nothing brings it back.",
        RunUnitStatus.Downed =>
            "Bedraggled: it returns on " + unit.FieldingHp + "/" + unit.MaxHp + " and takes no "
            + "activation slot in round 1. Everything else about it is normal, and its "
            + Naming.Meter + " and abilities are intact.",
        _ => string.Empty,
    };
}
