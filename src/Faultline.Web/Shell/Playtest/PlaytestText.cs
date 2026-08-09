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
    public static string GuardName => AbilityDefinition.For(Ability.GuardStance).Name;

    /// <summary>The stance's rules text, for a tooltip.</summary>
    public static string GuardSummary => AbilityDefinition.For(Ability.GuardStance).Summary;

    /// <summary>
    /// What a terrain kind is called on screen.
    /// </summary>
    /// <remarks>
    /// <b>The one place the shell names terrain.</b> Core has no display-name layer for
    /// <see cref="TileType"/> the way <see cref="Naming"/> has one for archetypes and spends — the
    /// enum names are still Pit and Spikes, and MASTER_DESIGN's tone pass (Pit→Drain,
    /// Spikes→Brambles) has not reached the code. So this stands in for the missing
    /// <c>Naming.Of(TileType)</c>: when that arrives, this method becomes a one-line delegate rather
    /// than a sweep through the markup.
    /// </remarks>
    /// <param name="tile">Terrain to name.</param>
    /// <returns>Its display name.</returns>
    public static string Terrain(TileType tile) => tile switch
    {
        TileType.Wall => "Wall",
        TileType.Pit => "Drain",
        TileType.Spikes => "Brambles",
        TileType.HighGround => "High ground",
        TileType.Cracked => "Cracked",
        _ => "Open",
    };

    /// <summary>What walking onto this terrain does, in one line, with Core's own numbers.</summary>
    /// <param name="tile">Terrain to describe.</param>
    /// <returns>One line.</returns>
    public static string TerrainWalk(TileType tile) => tile switch
    {
        TileType.Wall => "Cannot be entered.",
        TileType.Pit => "Cannot be entered on purpose.",
        TileType.Spikes => Activation.BrambleCost + " " + ActionPoints.Label + " to enter · "
            + Displacement.SpikeWalkDamage + " damage.",
        TileType.HighGround => Activation.StepCost + " " + ActionPoints.Label
            + " to climb · +" + Combat.HighGroundBonus + " on a ranged shot from here.",
        TileType.Cracked => Activation.StepCost + " " + ActionPoints.Label + " to enter · it is going to give way.",
        _ => Activation.StepCost + " " + ActionPoints.Label + " to enter.",
    };

    /// <summary>What being shoved onto this terrain does, in one line, with Core's own numbers.</summary>
    /// <param name="tile">Terrain to describe.</param>
    /// <returns>One line.</returns>
    public static string TerrainShoved(TileType tile) => tile switch
    {
        TileType.Wall => Displacement.CollisionDamage + " damage and a stagger · the shove stops here.",
        TileType.Pit => "Left clinging on the lip · the shove stops here.",
        TileType.Spikes => Displacement.SpikeDamage + " damage and a stagger · the shove stops here.",
        TileType.HighGround => "Cannot be shoved up onto it.",
        _ => "No effect · the shove carries on.",
    };

    /// <summary>Damage this terrain deals to whoever is shoved onto it, or zero.</summary>
    /// <param name="tile">Terrain to price.</param>
    /// <returns>The damage.</returns>
    public static int TerrainDamage(TileType tile) => tile switch
    {
        TileType.Wall => Displacement.CollisionDamage,
        TileType.Spikes => Displacement.SpikeDamage,
        _ => 0,
    };

    /// <summary>Whether being shoved onto this terrain staggers.</summary>
    /// <param name="tile">Terrain to ask about.</param>
    /// <returns>Whether it staggers.</returns>
    public static bool TerrainStaggers(TileType tile) =>
        tile is TileType.Wall or TileType.Spikes;

    /// <summary>Whether a shove carries on across this terrain rather than stopping on it.</summary>
    /// <param name="tile">Terrain to ask about.</param>
    /// <returns>Whether travel continues.</returns>
    public static bool TerrainContinues(TileType tile) =>
        tile is TileType.Open or TileType.Cracked;

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
    /// <b>Everything any surface needs to draw one duck's meter</b>, from the duck's own Pluck slots
    /// — the token's dots, the strip's pips, the inspector's block and the tooltip behind all three.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>One reading, because the question is one question.</b> Three razors each asked
    /// <c>Verve.SpendFor(unit.Kind)</c> and priced readiness off the unmodded cost, which was the
    /// same answer as the duck's only while a class's spender and its archetype's spender were the
    /// same card. G4 ended that: a Vanguard holding Retort had his meter labelled Wrecking Weight,
    /// a duck that traded its Pluck slot away still drew a meter, and a Fisher wearing Light Line lit
    /// "ready" one point late (D-242).
    /// </para>
    /// <para>
    /// <see cref="Verve.ConditionFor(UnitKind)"/> stays archetype-derived on purpose: §5's
    /// charge-condition table is class-bound and an alternate spender changes the spend, never the
    /// income (D-241).
    /// </para>
    /// </remarks>
    /// <param name="unit">Unit to read, or <c>null</c>.</param>
    /// <returns>The reading, or <c>null</c> when this duck holds no spender and draws no meter.</returns>
    public static MeterReading? MeterOf(Unit? unit)
    {
        if (unit is null || Verve.SpendFor(unit) is not { } spend)
        {
            return null;
        }

        int cost = Verve.CostOf(spend, unit);
        string state = unit.Verve >= cost
            ? $"{Verve.NameOf(spend)} ready"
            : $"{cost - unit.Verve} more for {Verve.NameOf(spend)}";

        return new MeterReading(
            spend,
            Verve.NameOf(spend),
            cost,
            unit.Verve,
            unit.Verve >= cost,
            Verve.ConditionFor(unit.Kind),
            Verve.ChargeLineFor(unit.Kind),
            $"{Naming.Meter} {unit.Verve}/{Verve.Cap} — {state}. {Verve.ChargeLineFor(unit.Kind)}.");
    }

    /// <summary>
    /// The tooltip behind a unit's Verve dots: what it holds, what earns more, and what it is saving
    /// for. Every word of it comes from Core, so the meter on the board and the rule behind it cannot
    /// drift apart.
    /// </summary>
    /// <param name="unit">Unit to describe.</param>
    /// <returns>A one-line tooltip, empty for a duck with no spender.</returns>
    public static string VerveTitle(Unit unit) => MeterOf(unit)?.Title ?? string.Empty;

    /// <summary>
    /// The loud label a Bedraggled duck carries on its deployment card and its board token.
    /// </summary>
    /// <remarks>
    /// Said at placement, not after it: a duck that costs its side an activation is a duck you place
    /// differently, and agency before injury (D-080) means the cost has to be on screen at the moment
    /// of the choice rather than discovered in round 1. The number is Core's own return formula and
    /// not the unit's current HP, so it still reads true after something has hit it.
    /// </remarks>
    /// <param name="unit">Unit to describe.</param>
    /// <returns>The label, or empty when the unit is not Bedraggled.</returns>
    public static string BedraggledLabel(Unit unit) =>
        unit is null || !unit.Bedraggled
            ? string.Empty
            : "Bedraggled — returns at " + Faultline.Core.Bedraggled.ReturningHp(unit.MaxHp)
                + " HP, misses round 1's first activation";

    /// <summary>
    /// One line per Bedraggled player unit, for the round-1 turn summary.
    /// </summary>
    /// <remarks>
    /// Once, and only in round 1, because the state does not survive into round 2 — the missing slot
    /// is a fact about this round and nothing else. The strip carries the same fact as a gap; this is
    /// the sentence version, for the reader who counts activations in words.
    /// </remarks>
    /// <param name="state">Current state.</param>
    /// <returns>The lines, empty when nobody is recovering.</returns>
    public static IReadOnlyList<string> BedraggledLines(GameState state)
    {
        var lines = new List<string>();
        if (state is null || state.Phase != Faultline.Core.Phase.Battle)
        {
            return lines;
        }

        foreach (var unit in state.Units)
        {
            if (unit.Bedraggled && unit.IsOnBoard && unit.Team.IsPlayer())
            {
                lines.Add(unit.Name + " is bedraggled — first activation skipped.");
            }
        }

        return lines;
    }

    /// <summary>
    /// One line per objective-linked structure: what it is called, where it stands and how much of
    /// it is left.
    /// </summary>
    /// <remarks>
    /// The objective panel draws exactly these strings, one per line. A board with two structures
    /// gets two lines rather than one summed number, and a breakable blocker gets none — Core's
    /// <see cref="ObjectiveStatus.Structures"/> has already made both of those decisions, and this
    /// only formats them (D-163).
    /// </remarks>
    /// <param name="state">Current state.</param>
    /// <returns>The lines, empty when the fight has no objective structure.</returns>
    public static IReadOnlyList<string> StructureLines(GameState state)
    {
        var lines = new List<string>();
        if (state is null)
        {
            return lines;
        }

        foreach (var structure in ObjectiveStatus.For(state).Structures)
        {
            lines.Add(StructureLine(structure));
        }

        return lines;
    }

    /// <summary>One structure's line: its name, its hit points, its tile, and whether it is rubble.</summary>
    /// <param name="structure">Structure to describe.</param>
    /// <returns>The line the panel prints.</returns>
    public static string StructureLine(StructureStatus structure) =>
        structure is null
            ? string.Empty
            : structure.Label + " · " + BoardCoords.Of(structure.At)
                + (structure.IsStanding ? string.Empty : " · rubble");

    /// <summary>
    /// One line per clinging player unit, naming the deadline instead of implying one.
    /// </summary>
    /// <remarks>
    /// A unit on a ledge is the only thing in this game on a private clock, and until now the board
    /// said so with a two-pixel dot. "Hanging on" is not information; "gone at the end of this
    /// round" is (D-083).
    /// </remarks>
    /// <param name="state">Current state.</param>
    /// <returns>The lines, empty when nobody is hanging.</returns>
    public static IReadOnlyList<string> ClingingLines(GameState state)
    {
        var lines = new List<string>();
        if (state is null)
        {
            return lines;
        }

        foreach (var unit in state.Units)
        {
            if (!unit.Clinging || !unit.IsAlive || !unit.Team.IsPlayer())
            {
                continue;
            }

            // D-016: it loses its grip at the end of the round after the one it fell in.
            bool lastChance = state.Round > unit.ClingingSinceRound;
            lines.Add(lastChance
                ? unit.Name + " is over the edge — gone at the end of this round."
                : unit.Name + " is over the edge — gone at the end of round "
                    + (unit.ClingingSinceRound + 1) + ".");
        }

        return lines;
    }

    /// <summary>
    /// Why a rescue is not available to this unit right now, or empty when it is.
    /// </summary>
    /// <param name="state">Current state.</param>
    /// <param name="rescuer">Unit that might go.</param>
    /// <param name="clinging">Unit on the ledge.</param>
    /// <returns>A short reason for a disabled button.</returns>
    public static string RescueBlockedReason(GameState state, Unit rescuer, Unit clinging)
    {
        if (rescuer.HasActed)
        {
            return "action already spent";
        }

        int? needed = Pits.MoveNeededToReach(state, rescuer, clinging);
        if (needed is null)
        {
            return "cannot reach this activation";
        }

        if (needed.Value > 0)
        {
            return "needs " + needed.Value + " more move";
        }

        return Pits.RescueDestinations(state, rescuer).Count == 0
            ? "nowhere to set them down"
            : string.Empty;
    }

    /// <summary>
    /// What a cast landing does to whoever is put on it — the per-tile preview (D-091).
    /// </summary>
    /// <param name="state">Current state.</param>
    /// <param name="tile">Landing tile.</param>
    /// <returns>A short outcome, e.g. "spikes 3" or "drain".</returns>
    public static string CastOutcome(GameState state, Coord tile) => state.Board.At(tile) switch
    {
        TileType.Spikes => Terrain(TileType.Spikes).ToLowerInvariant() + " " + Displacement.SpikeDamage,
        TileType.Pit => Terrain(TileType.Pit).ToLowerInvariant(),
        TileType.HighGround => Terrain(TileType.HighGround).ToLowerInvariant(),
        _ => Terrain(TileType.Open).ToLowerInvariant(),
    };

    /// <summary>The word a board chip uses for ending a displacement Staggered.</summary>
    public const string StaggerNote = "stagger";

    /// <summary>The word a board chip uses for ending a displacement in a drain.</summary>
    /// <remarks>
    /// Core's flag is <c>WouldCling</c>; "paddling" is what a player is shown (MASTER_DESIGN §15 —
    /// display names are decoupled from code identifiers, and Drain is never Pit on screen).
    /// </remarks>
    public const string PaddlingNote = "paddling";

    /// <summary>
    /// What a projected displacement leaves behind on the tile it stops on, beyond the damage.
    /// </summary>
    /// <param name="preview">A Core displacement preview.</param>
    /// <returns>"paddling", "stagger", or an empty string.</returns>
    public static string Aftermath(DisplacementPreview preview) =>
        preview.WouldCling ? PaddlingNote
        : preview.WouldStagger ? StaggerNote
        : string.Empty;

    /// <summary>
    /// Why a displacement moves nobody, named. A shove reduced to nothing is still an outcome and
    /// still has to be drawn: CLAUDE.md's earned practice is that a silent no-op is a bug and every
    /// refusal names its reason.
    /// </summary>
    /// <param name="preview">A Core displacement preview that reports itself a no-op.</param>
    /// <returns>e.g. "no movement (resist 2)".</returns>
    /// <remarks>
    /// The number is <see cref="DisplacementPreview.Resistance"/>, which is Core's own subtraction
    /// from the distance arithmetic. Nothing here works out what a stat block would have done, and
    /// nothing here decides that the shove came to nothing — that is the preview's own verdict.
    /// </remarks>
    public static string NoMovement(DisplacementPreview preview) =>
        preview.Resistance > 0
            ? "no movement (resist " + preview.Resistance + ")"
            : "no movement";

    /// <summary>
    /// The verb a ghosted candidate's sentence opens with.
    /// </summary>
    /// <remarks>
    /// A haul long enough to have turned a corner is a Reel and says so; anything else is the plain
    /// verb. Read off the projection rather than off the ability, because the sentence describes what
    /// this candidate does and not which button produced it.
    /// </remarks>
    /// <param name="preview">A Core displacement candidate.</param>
    /// <returns>"push", "pull" or "reel in".</returns>
    public static string AimVerb(DisplacementPreview preview) =>
        preview.Kind == DisplacementKind.Push ? "push"
        : preview.RequestedDistance > 1 ? "reel in"
        : "pull";

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

        if (unit.Bedraggled)
        {
            flags.Add("bedraggled");
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

    /// <summary>What a structure is and how it can be hurt.</summary>
    /// <remarks>
    /// The damage figures interpolate Core's constants rather than retyping them (D-112). This text
    /// used to claim a Destroy structure was "immune to attacks", which D-060 had already stopped
    /// being true: every structure takes <see cref="Objectives.AttackDamageToStructure"/> from a
    /// swing and <see cref="Displacement.CollisionDamage"/> from a slam.
    /// </remarks>
    /// <param name="structure">Structure to describe.</param>
    /// <returns>A tooltip sentence.</returns>
    public static string Structure(Structure structure)
    {
        string hurt = $"An attack takes {Objectives.AttackDamageToStructure}; a collision takes "
            + $"{Displacement.CollisionDamage}.";

        if (structure.IsBlocker)
        {
            return $"Breakable blocker — {structure.Hp}/{structure.MaxHp} HP. Break it to open the tile. "
                + hurt;
        }

        return structure.Role == ObjectiveKind.Protect
            ? $"Protect this — {structure.Hp}/{structure.MaxHp} HP. Enemies claw at it from adjacent tiles. {hurt}"
            : $"Destroy this — {structure.Hp}/{structure.MaxHp} HP. {hurt}";
    }

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

        // Since D-097 the move half is a budget rather than a latch, and acting shuts what is left
        // of it — so "action spent, move still to use" is no longer a state a unit can be in.
        if (unit.HasActed)
        {
            return "Move and action both spent.";
        }

        if (unit.MoveRemaining <= 0)
        {
            return "Move spent — action still to use.";
        }

        return unit.MoveSpent > 0
            ? "Move part-spent (" + unit.MoveRemaining + " MP left) — action still to use."
            : "Move and action both unspent.";
    }

}
