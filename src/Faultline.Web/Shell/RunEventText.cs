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
        // Display names, never the identifier: the Fisher is UnitKind.Threadcaster in the code and
        // has never been one on a screen (MASTER_DESIGN §15). These four lines used to print the
        // enum, which is exactly the bypass Naming exists to close.
        UnitFielded u => $"{Naming.Of(u.Kind)} fields as {u.UnitId} [{EventText.Side(u.Team)}] on "
            + $"{Num(u.Hp)}/{Num(u.MaxHp)}"
            + (u.Returning ? " — bedraggled: no activation slot in round 1." : "."),
        FightResolved r => $"{r.FightId} {Outcome(r.Outcome)} on round {Num(r.Round)}.",
        UnitCarried c => c.Status switch
        {
            RunUnitStatus.Voided => $"{Naming.Of(c.Kind)} is gone for the run.",
            RunUnitStatus.Downed => $"{Naming.Of(c.Kind)} went down — back next fight bedraggled on "
                + $"{Num(c.FieldingHp)}/{Num(c.MaxHp)}, missing its first activation.",
            _ => $"{Naming.Of(c.Kind)} carries {Num(c.Hp)}/{Num(c.MaxHp)} out.",
        },
        UnitRested u => $"{Naming.Of(u.Kind)} restored {Num(u.From)} → {Num(u.To)}"
            + (u.WasDowned ? ", and is standing again." : "."),
        MapMoved m => m.FromNodeId.Length == 0
            ? $"The act opens at {m.ToNodeId} — column {Num(m.Column + 1)}, {Lane(m.Lane)}."
            : $"Moved to {m.ToNodeId} — {m.Type.ToString().ToLowerInvariant()}, column "
              + $"{Num(m.Column + 1)}, {Lane(m.Lane)}"
              + (m.Voted ? ", by the vote." : ". The column had one door, so nobody voted."),
        VoteResolved v => v.ByCoin
            ? $"Split — A picked {v.ChoiceA}, B picked {v.ChoiceB}. The coin came up "
              + $"{(v.Coin == 0 ? "A" : "B")} and the run takes {v.ChosenNodeId}."
            : $"Agreed — both picked {v.ChosenNodeId}. No coin.",
        EventOffered o => $"{o.Name} — {o.Prompt}",

        // The camp, in the log. What was on the table and what came off it, in the catalogue's own
        // words — a line that reworded the rule would be the second copy of the pool that
        // CampCatalogue exists to prevent.
        CampOffered c => c.Table.IsEmpty
            ? "Camp — nothing left to deal, so the run walks straight through it."
            : $"Camp after {c.FightId} — {c.Table}.",
        CampTaken t => $"Player {EventText.Side(t.Player)} takes {t.Name} for the "
            + $"{Naming.Of(t.Kind)} — {t.Summary}",

        EventDeclined d => d.WalkAwayLine,
        MaxHpRaised r => $"{Naming.Of(r.Kind)} paid {Num(r.HpFrom - r.HpTo)} and came away bigger: "
            + $"{Num(r.HpFrom)}/{Num(r.MaxFrom)} → {Num(r.HpTo)}/{Num(r.MaxTo)}.",

        // The promise rule in the log, exactly as on the map: while nothing can pay the mark, the
        // line names the gap and not the prize. Reading Kind here to print "a legendary" would be
        // the same broken promise the gilt edge is withheld to avoid.
        RewardPromised p => p.Payable
            ? $"{p.NodeId} promises {p.MarkId} for clearing {p.FightId}."
            : $"{p.NodeId} carries a reward mark this build cannot pay, so nothing is promised for it.",
        ActCleared a => $"{a.ActId} cleared — {Num(a.FightsWon)} fights over {Num(a.NodesVisited)} "
            + $"nodes, route {Num(a.RouteHash)}. {a.Tally}",
        RunWon w => $"Run complete — {Num(w.FightsWon)} fights won.",
        RunLost l => $"Run over on node {Num(l.Index + 1)} after {Num(l.FightsWon)} fights. {l.Reason}",
        _ => e.GetType().Name,
    };

    /// <summary>Which side of the comfort gradient a node stands on, in words.</summary>
    /// <param name="lane">The lane.</param>
    /// <returns>A short phrase.</returns>
    public static string Lane(MapLane lane) => lane switch
    {
        MapLane.Safe => "the safe lane",
        MapLane.Hungry => "the hungry lane",
        _ => "neither lane",
    };

    private static string Outcome(FightOutcome outcome) => outcome switch
    {
        FightOutcome.Won => "won",
        FightOutcome.Lost => "lost",
        _ => "unresolved",
    };

    private static string Num(int value) => value.ToString(CultureInfo.InvariantCulture);
}
