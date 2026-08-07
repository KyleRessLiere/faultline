using System.Text;
using Faultline.Core;

namespace Faultline.Playtest;

/// <summary>
/// Renders a fight as text, for a reader who has to decide something from it.
/// </summary>
/// <remarks>
/// <para>
/// This exists because of the limitation named in docs/PLAYTEST_FINDINGS.md: the scripted policies
/// choose by command *type*, never by target quality, so they measure how the systems behave rather
/// than how a good player performs. Closing that gap means putting a reader in the seat, and a
/// reader needs to see what a shove would actually do before choosing it.
/// </para>
/// <para>
/// So every displacing option is annotated from Core's own preview — the same simulation the
/// resolution runs, never a recomputation — and every attack carries the damage Core says it deals.
/// A decision made from this text is made on the same information the shell shows a human.
/// </para>
/// </remarks>
public static class View
{
    /// <summary>Renders the whole decision brief: board, units, intents, and the legal options.</summary>
    /// <param name="run">Run being played.</param>
    /// <param name="legal">Commands the player may choose.</param>
    /// <returns>The brief.</returns>
    public static string Brief(RunState run, IReadOnlyList<Command> legal)
    {
        var text = new StringBuilder();
        var state = run.Fight;

        text.Append(RunHeader(run));

        if (state is null)
        {
            return text.ToString();
        }

        text.Append('\n').Append(Board(state));
        text.Append('\n').Append(Units(state));
        text.Append('\n').Append(Intents(state));
        text.Append('\n').Append(Options(state, legal));

        return text.ToString();
    }

    /// <summary>One line per campaign-level fact: which node, the squad's carried state.</summary>
    /// <param name="run">Run being played.</param>
    /// <returns>The header block.</returns>
    public static string RunHeader(RunState run)
    {
        var text = new StringBuilder();
        var node = run.CurrentNode;
        string where = node switch
        {
            FightNode f => "fight " + f.FightId,
            RestNode => "rest",
            _ => "—",
        };

        text.Append("=== node ").Append(run.NodeIndex)
            .Append(" of ").Append(run.Campaign.Nodes.Count - 1)
            .Append("  (").Append(where).Append(")  cleared ").Append(run.FightsWon)
            .Append("  seed ").Append(run.Seed).Append(" ===\n");

        text.Append("squad: ");
        text.Append(string.Join(
            "  ",
            run.Squad.Select(u =>
                $"{Naming.Of(u.Kind)} {u.Hp}/{u.MaxHp} pluck {u.Verve}"
                + (u.Status == RunUnitStatus.Ready ? string.Empty : " [" + u.Status + "]"))));
        text.Append('\n');

        return text.ToString();
    }

    /// <summary>
    /// The board as a grid. Each cell is a unit token where one stands, and a tile glyph otherwise.
    /// </summary>
    /// <param name="state">Fight state.</param>
    /// <returns>The grid, with axis labels and a legend.</returns>
    public static string Board(GameState state)
    {
        var text = new StringBuilder();
        var board = state.Board;

        text.Append("     ");
        for (int x = 0; x < board.Width; x++)
        {
            text.Append(Pad(x.ToString(), 4));
        }

        text.Append('\n');

        for (int y = 0; y < board.Height; y++)
        {
            text.Append(Pad(y.ToString(), 4)).Append(' ');

            for (int x = 0; x < board.Width; x++)
            {
                var at = new Coord(x, y);
                var unit = state.UnitAt(at);
                var structure = state.StructureAt(at);

                string cell = unit is not null && unit.IsOnBoard
                    ? Token(unit)
                    : structure is not null
                        ? "[]"
                        : Glyph(board.At(at));

                text.Append(Pad(cell, 4));
            }

            text.Append('\n');
        }

        text.Append("legend: . open   # wall   O drain   ^ spikes   + high ground   ~ cracked   [] structure\n");
        text.Append("        UPPERCASE = your squad, lowercase = enemy, number = unit id\n");

        var objective = state.Fight.Objective;
        text.Append("objective: ").Append(objective.Kind);
        if (objective.Tiles.Count > 0)
        {
            text.Append(" at ").Append(string.Join(" ", objective.Tiles));
        }

        if (objective.Deadline > 0)
        {
            text.Append(", by round ").Append(objective.Deadline);
        }

        text.Append("\nround ").Append(state.Round)
            .Append("   phase ").Append(state.Phase)
            .Append("   active ").Append(state.ActiveTeam);

        if (state.ActiveUnitId.HasValue)
        {
            text.Append(" (").Append(Name(state, state.ActiveUnitId.Value)).Append(')');
        }

        text.Append('\n');

        return text.ToString();
    }

    /// <summary>Every unit on the board, with the state a decision turns on.</summary>
    /// <param name="state">Fight state.</param>
    /// <returns>The unit table.</returns>
    public static string Units(GameState state)
    {
        var text = new StringBuilder();
        text.Append("units:\n");

        foreach (var unit in state.UnitsOnBoard().OrderBy(u => u.Team.IsPlayer() ? 0 : 1).ThenBy(u => u.Id.Value))
        {
            var flags = new List<string>();
            if (unit.Staggered)
            {
                flags.Add("staggered");
            }

            if (unit.Guarding)
            {
                flags.Add("guarding");
            }

            if (unit.Clinging)
            {
                flags.Add("CLINGING");
            }

            if (unit.Footing > 0)
            {
                flags.Add("footing " + unit.Footing);
            }

            if (unit.HasActivated)
            {
                flags.Add("done");
            }
            else
            {
                if (unit.HasMoved)
                {
                    flags.Add("moved");
                }

                if (unit.HasActed)
                {
                    flags.Add("acted");
                }
            }

            if (unit.Enraged)
            {
                flags.Add("enraged");
            }

            if (unit.WreckingWeightArmed)
            {
                flags.Add("wrecking-weight");
            }

            text.Append("  ").Append(Pad(Token(unit), 4))
                .Append(Pad(Naming.Of(unit.Kind), 14))
                .Append(Pad(unit.Team.IsPlayer() ? "[" + unit.Team + "]" : "[enemy]", 10))
                .Append(Pad(unit.Position.ToString(), 8))
                .Append(Pad("hp " + unit.Hp + "/" + unit.MaxHp, 9))
                .Append(Pad("move " + unit.Move, 8))
                .Append(Pad(state.Board.At(unit.Position) == TileType.HighGround ? "HIGH" : string.Empty, 6));

            if (unit.Team.IsPlayer())
            {
                text.Append(Pad("pluck " + unit.Verve + "/" + Verve.Cap, 10));
            }

            text.Append(string.Join(" ", flags)).Append('\n');
        }

        return text.ToString();
    }

    /// <summary>What every enemy has told you it is about to do.</summary>
    /// <param name="state">Fight state.</param>
    /// <returns>The telegraph block.</returns>
    public static string Intents(GameState state)
    {
        if (state.Intents.Count == 0)
        {
            return "intents: none declared\n";
        }

        var text = new StringBuilder();
        text.Append("intents (what the enemy will do next):\n");

        foreach (var intent in state.Intents)
        {
            text.Append("  ").Append(Pad(Name(state, intent.UnitId), 20))
                .Append(Pad(intent.Action.ToString(), 9));

            if (intent.TargetId.HasValue)
            {
                text.Append("-> ").Append(Pad(Name(state, intent.TargetId.Value), 20));
            }

            if (intent.Damage > 0)
            {
                text.Append("for ").Append(intent.Damage).Append(" ");
            }

            if (intent.RedirectedTo.HasValue)
            {
                text.Append("(guarded by ").Append(Name(state, intent.RedirectedTo.Value)).Append(") ");
            }

            if (intent.Displacement.HasValue)
            {
                text.Append(intent.Displacement.Value).Append(' ')
                    .Append(intent.DisplacementDirection).Append(' ')
                    .Append(intent.DisplacementDistance);

                if (intent.DisplacementTo.HasValue)
                {
                    text.Append(" to ").Append(intent.DisplacementTo.Value);
                }
            }

            if (intent.MoveTo.HasValue)
            {
                text.Append("moves to ").Append(intent.MoveTo.Value);
            }

            text.Append('\n');
        }

        return text.ToString();
    }

    /// <summary>
    /// The legal commands, numbered, each annotated with what Core says it would actually do.
    /// </summary>
    /// <param name="state">Fight state.</param>
    /// <param name="legal">Commands to list.</param>
    /// <returns>The numbered options.</returns>
    public static string Options(GameState state, IReadOnlyList<Command> legal)
    {
        var text = new StringBuilder();
        text.Append("options (").Append(legal.Count).Append("):\n");

        for (int i = 0; i < legal.Count; i++)
        {
            text.Append("  ").Append(Pad(i.ToString(), 5)).Append(Describe(state, legal[i])).Append('\n');
        }

        return text.ToString();
    }

    /// <summary>One command, in words, with its projected outcome.</summary>
    /// <param name="state">Fight state.</param>
    /// <param name="command">Command to describe.</param>
    /// <returns>The description.</returns>
    public static string Describe(GameState state, Command command)
    {
        switch (command)
        {
            case DeployCommand c:
                return $"Deploy {Name(state, c.UnitId)} at {c.At} [{Glyph(state.Board.At(c.At))} {state.Board.At(c.At)}]";

            case MoveCommand c:
            {
                var tile = state.Board.At(c.To);
                string note = tile == TileType.HighGround ? "  HIGH GROUND (+1 ranged)"
                    : tile == TileType.Cracked ? "  CRACKED (may collapse)"
                    : tile == TileType.Spikes ? "  SPIKES"
                    : string.Empty;
                return $"Move {Name(state, c.UnitId)} to {c.To}{note}";
            }

            case AttackCommand c:
            {
                var attacker = state.FindUnit(c.UnitId);
                string elevated = attacker is not null && Combat.IsElevatedShot(state, attacker)
                    ? "  from high ground"
                    : string.Empty;

                return $"Attack[{c.Mode}] {Name(state, c.UnitId)} -> {Name(state, c.TargetId)}"
                    + $"{elevated}{Outcome(state, command)}";
            }

            case AbilityCommand c:
            {
                string aim = c.TargetId.HasValue
                    ? " -> " + Name(state, c.TargetId.Value)
                    : c.Direction.HasValue ? " " + c.Direction.Value : string.Empty;

                return $"Ability {c.Ability} by {Name(state, c.UnitId)}{aim}{Outcome(state, command)}";
            }

            case SpendVerveCommand c:
            {
                string aim = c.TargetId.HasValue ? " -> " + Name(state, c.TargetId.Value) : string.Empty;
                string to = c.To.HasValue
                    ? $" onto {c.To.Value} [{state.Board.At(c.To.Value)}]"
                    : string.Empty;

                return $"PLUCK {Verve.NameOf(c.Spend)} ({Verve.CostOf(c.Spend)}) by "
                    + $"{Name(state, c.UnitId)}{aim}{to}";
            }

            case RescueCommand c:
                return $"Rescue {Name(state, c.ClingingId)} by {Name(state, c.UnitId)} onto {c.To}";

            case FinishClingingCommand c:
                return $"Finish off clinging {Name(state, c.ClingingId)} with {Name(state, c.UnitId)}";

            case EndActivationCommand c:
                return $"End activation of {Name(state, c.UnitId)}";

            default:
                return command.ToString() ?? "?";
        }
    }

    /// <summary>
    /// What an action would do, straight out of Core's projection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every question about which half of an action applies is Core's (<see
    /// cref="Abilities.Outlook"/>), because this renderer answering them itself is exactly how three
    /// contradictions reached the field: a Line ability read as a charge and annotated "nothing that
    /// way" before it hit, a pull annotated with the damage of the shot it was chosen instead of, and
    /// a shove whose destination was drawn without knowing who was shoving.
    /// </para>
    /// <para>
    /// MASTER_DESIGN §3 (locked v) also makes the empty result a sentence rather than a silence, so
    /// there is no branch here that returns nothing for an action that does something.
    /// </para>
    /// </remarks>
    private static string Outcome(GameState state, Command command)
    {
        var outlook = Abilities.Outlook(state, command);
        if (outlook is null)
        {
            return string.Empty;
        }

        var parts = new List<string>();

        // Whether it kills is Core's answer (ActionOutlook.Finishes), never this renderer's. Deciding
        // it here by subtracting the damage from the target's hit points was a second copy of the
        // rules, and it had the wrong answer for a body on a ledge: any damage at all voids a
        // Clinging unit, so a 2 into a ten-point Grappler read "leaves 8" and took all ten.
        if (outlook.Damage > 0 && outlook.TargetId is { } hurtId && state.FindUnit(hurtId) is { } hurt)
        {
            parts.Add($"for {outlook.Damage}"
                + (outlook.Finishes ? "  KILLS" : $"  (leaves {hurt.Hp - outlook.Damage})"));
        }
        else if (outlook.Finishes && outlook.TargetId is { } takenId)
        {
            // A shove that finishes a body it deals no direct damage to still has to say so.
            parts.Add($"KILLS {Name(state, takenId)}");
        }

        foreach (var hit in outlook.LineHits)
        {
            string who = hit.UnitId is { } id ? Name(state, id)
                : hit.HitsStructure ? "the structure"
                : hit.At.ToString();
            parts.Add($"{hit.Damage} to {who}");
        }

        if (outlook.Charge is { } charge)
        {
            parts.AddRange(ChargeParts(state, charge));
        }

        if (outlook.Displacement is { } shove)
        {
            parts.Add(Shove(state, shove));
        }

        if (parts.Count == 0)
        {
            // Named rather than blank. An option that renders as bare text is an option a reader
            // cannot choose between, which is what "a silent no-op is a bug" means here.
            return outlook.LineHits.Count > 0 || outlook.Charge is not null
                ? "  => nothing that way"
                : "  => no effect on the board";
        }

        return "  => " + string.Join(", ", parts);
    }

    /// <summary>What a Bull Rush down a line would do: the run, and the shove at the end of it.</summary>
    private static List<string> ChargeParts(GameState state, ChargePreview preview)
    {
        var parts = new List<string>();

        if (preview.Path.Count > 0)
        {
            parts.Add($"charges {preview.Path.Count} to {preview.Destination}");
        }

        if (preview.SelfDamage > 0)
        {
            parts.Add($"TAKES {preview.SelfDamage} on the way");
        }

        parts.Add(preview.Contact is null
            ? "connects with nothing"
            : "shoves " + Name(state, preview.Contact.UnitId) + ": " + Shove(state, preview.Contact));

        return parts;
    }

    /// <summary>One displacement, in words, straight out of Core's preview.</summary>
    private static string Shove(GameState state, DisplacementPreview preview)
    {
        if (preview.IsNoOp)
        {
            // The reason is Core's own subtraction, never a number worked out here: a refusal that
            // does not name itself is the silent no-op CLAUDE.md calls a bug.
            return preview.Resistance > 0
                ? $"does not move it (resist {preview.Resistance})"
                : "does not move it";
        }

        var parts = new List<string>
        {
            $"{preview.Kind} {preview.Direction} {preview.EffectiveDistance} to {preview.Destination}",
        };

        switch (preview.Stop)
        {
            case DisplacementStop.Collision:
            {
                // Whether the unit on the *other* end of the collision dies is half the decision and
                // the preview does not carry it — WouldDown speaks only for the unit being shoved.
                var obstacle = preview.ObstacleId.HasValue ? state.FindUnit(preview.ObstacleId.Value) : null;
                string fatal = obstacle is not null && preview.DamageToObstacle >= obstacle.Hp
                    ? " AND KILLS IT"
                    : string.Empty;

                parts.Add(obstacle is null
                    ? $"INTO THE WALL for {preview.DamageToUnit}"
                    : $"COLLIDES with {Name(state, obstacle.Id)} "
                        + $"({preview.DamageToUnit} to it, {preview.DamageToObstacle} to them{fatal})");
                break;
            }

            case DisplacementStop.Pit:
                parts.Add("INTO THE DRAIN");
                break;
            case DisplacementStop.Spikes:
                parts.Add($"ONTO SPIKES for {preview.DamageToUnit}");
                break;
            case DisplacementStop.Immovable:
                parts.Add("braced, barely shifts");
                break;
            default:
                if (preview.DamageToUnit > 0)
                {
                    parts.Add(preview.DamageToUnit + " damage");
                }

                break;
        }

        if (preview.StructureAt.HasValue)
        {
            parts.Add($"into the structure at {preview.StructureAt.Value} for {preview.DamageToStructure}");
        }

        if (preview.WouldDown)
        {
            parts.Add("KILLS IT");
        }

        if (preview.WouldCling)
        {
            parts.Add("leaves it clinging");
        }

        if (preview.WouldStagger)
        {
            parts.Add("staggers");
        }

        return string.Join(", ", parts);
    }

    /// <summary>A unit's board token: class letter, cased by side, plus its id.</summary>
    private static string Token(Unit unit)
    {
        char letter = unit.Kind switch
        {
            UnitKind.Vanguard => 'V',
            UnitKind.Archer => 'A',
            UnitKind.Threadcaster => 'F',
            UnitKind.Wardbearer => 'W',
            _ => Naming.Of(unit.Kind)[0],
        };

        return (unit.Team.IsPlayer() ? char.ToUpperInvariant(letter) : char.ToLowerInvariant(letter))
            + unit.Id.Value.ToString();
    }

    private static string Name(GameState state, UnitId id)
    {
        var unit = state.FindUnit(id);
        return unit is null ? id.ToString() : $"{Naming.Of(unit.Kind)} {Token(unit)}";
    }

    private static string Glyph(TileType tile) => tile switch
    {
        TileType.Open => ".",
        TileType.Wall => "#",
        TileType.Pit => "O",
        TileType.Spikes => "^",
        TileType.HighGround => "+",
        TileType.Cracked => "~",
        _ => "?",
    };

    private static string Pad(string value, int width) =>
        value.Length >= width ? value + " " : value + new string(' ', width - value.Length);
}
