using System.Globalization;
using Faultline.Core;

namespace Faultline.Web.Shell;

/// <summary>
/// One line of prose per <see cref="RunEvent"/>.
/// </summary>
/// <remarks>
/// Every payload is read straight off the event and nothing is looked up in
/// <see cref="RunState"/> — a renderer that had to query state to draw an event would be reading a
/// world that has already moved on (CLAUDE.md).
/// </remarks>
public static class RunEventText
{
    /// <summary>Describes one run event.</summary>
    /// <param name="e">The event.</param>
    /// <returns>A single line.</returns>
    public static string Describe(RunEvent e) => e switch
    {
        RunStarted s => $"Run started — {s.CampaignName}, {Num(s.Nodes)} nodes, seed {Num(s.Seed)}.",
        NodeEntered n => $"Node {Num(n.Index + 1)}: {n.Description}.",
        FightBegan f => $"Fight #{Num(f.Number)} {f.Name} begins with {Num(f.Fielded)} of the squad.",
        UnitFielded u => $"{u.Kind} fields as {u.UnitId} [{EventText.Side(u.Team)}] on {Num(u.Hp)}/{Num(u.MaxHp)}"
            + (u.Returning ? " — bedraggled: no activation slot in round 1." : "."),
        FightResolved r => $"{r.FightId} {Outcome(r.Outcome)} on round {Num(r.Round)}.",
        UnitCarried c => c.Status switch
        {
            RunUnitStatus.Voided => $"{c.Kind} is gone for the run.",
            RunUnitStatus.Downed => $"{c.Kind} went down — back next fight bedraggled on "
                + $"{Num(c.FieldingHp)}/{Num(c.MaxHp)}, missing its first activation.",
            _ => $"{c.Kind} carries {Num(c.Hp)}/{Num(c.MaxHp)} out.",
        },
        UnitRested u => $"{u.Kind} restored {Num(u.From)} → {Num(u.To)}"
            + (u.WasDowned ? ", and is standing again." : "."),
        RunWon w => $"Run complete — {Num(w.FightsWon)} fights won.",
        RunLost l => $"Run over on node {Num(l.Index + 1)} after {Num(l.FightsWon)} fights. {l.Reason}",
        _ => e.GetType().Name,
    };

    private static string Outcome(FightOutcome outcome) => outcome switch
    {
        FightOutcome.Won => "won",
        FightOutcome.Lost => "lost",
        _ => "unresolved",
    };

    private static string Num(int value) => value.ToString(CultureInfo.InvariantCulture);
}
