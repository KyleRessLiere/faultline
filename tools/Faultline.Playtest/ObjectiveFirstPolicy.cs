using Faultline.Core;

namespace Faultline.Playtest;

/// <summary>
/// Sees every outcome and plays the objective: the structure's hit points are what it is counting.
/// </summary>
/// <remarks>
/// <para>
/// MASTER_DESIGN §8.8 names four evaluator policies — baseline, collision-seeking, objective-first,
/// random-legal — and the registry held no <c>objective-first</c>. This is it, built rather than
/// substituted: <c>board-first</c> maps onto baseline and <c>shover</c> onto collision-seeking, but
/// nothing in the registry weighted <em>objective progress</em>, so certifying "the objective always
/// resolves" would have been certified by a player that was not trying to resolve it.
/// </para>
/// <para>
/// It values the same outcomes <c>board-first</c> does and then adds one axis: the objective's hit
/// points, and the distance between a body and the objective. On a <b>Destroy</b> board that means
/// hitting the structure and dragging enemies toward it — bodies are ammunition — and on a
/// <b>Protect</b> board it means the opposite vector, pushing besiegers off the structure and paying
/// a premium for killing one already standing next to it.
/// </para>
/// <para>
/// <b>What it cannot see.</b> It prices one command at a time out of Core's previews, exactly as its
/// parent does, so it cannot plan a two-command shove chain into the gate, and it has no model of the
/// clock — a Protect board is only lost to it when the last chip lands. It is a certification
/// instrument, not a good player.
/// </para>
/// </remarks>
public sealed class ObjectiveFirstPolicy : EvaluatorPolicy
{
    /// <summary>Points per tile of approach to the objective, for a move.</summary>
    private const int Approach = 45;

    /// <summary>Points per tile a shove moves an enemy along the objective's axis.</summary>
    private const int Shove = 90;

    /// <summary>Bonus for removing an enemy that is already standing next to a Protect structure.</summary>
    private const int BesiegerPremium = 900;

    /// <inheritdoc/>
    public override string Name => "objective-first";

    /// <inheritdoc/>
    public override string Intent =>
        "Prices every option from Core's previews and then pays for objective progress: structure hit points, and the distance between a body and the structure. §8.8's fourth evaluator.";

    /// <inheritdoc/>
    protected override Weights Taste => new()
    {
        // A point off the gate is worth three points off a Husk. The gate is the win condition; the
        // Husk is only in the way of it.
        ObjectiveDamage = 120,
    };

    /// <inheritdoc/>
    protected override int Score(GameState state, Command command)
    {
        int score = base.Score(state, command);

        var objective = Target(state);
        if (objective is null)
        {
            return score;
        }

        return score + ObjectiveTerm(state, command, objective);
    }

    /// <summary>The standing structure this policy is playing toward, or null on a board with none.</summary>
    private static Structure? Target(GameState state)
    {
        Structure? best = null;

        foreach (var structure in state.Structures)
        {
            // Blockers are scenery, not anybody's objective (D-114/D-163) — a policy that chased
            // them would have measured barricade-punching and called it objective play.
            if (!structure.IsStanding || structure.IsBlocker)
            {
                continue;
            }

            if (best is null || structure.Hp < best.Hp)
            {
                best = structure;
            }
        }

        return best;
    }

    private static int ObjectiveTerm(GameState state, Command command, Structure objective)
    {
        // Destroy: get close to it and bring bodies with you. Protect: get close to it and drive
        // bodies away. The sign is the whole difference between the two roles.
        int sign = objective.Role == ObjectiveKind.Destroy ? 1 : -1;

        switch (command)
        {
            case MoveCommand move:
            {
                var unit = state.FindUnit(move.UnitId);
                if (unit is null)
                {
                    return 0;
                }

                // Both roles want a duck near the structure: one to break it, one to stand over it.
                return (unit.Position.DistanceTo(objective.At) - move.To.DistanceTo(objective.At))
                    * Approach;
            }

            case DeployCommand deploy:
                return (7 - deploy.At.DistanceTo(objective.At)) * (Approach / 3);

            case AttackCommand attack:
                return Vector(state, objective, sign, attack.TargetId, Preview(state, attack));

            case AbilityCommand ability when ability.TargetId.HasValue:
            {
                var actor = state.FindUnit(ability.UnitId);
                return actor is null
                    ? 0
                    : Vector(
                        state,
                        objective,
                        sign,
                        ability.TargetId.Value,
                        Abilities.PreviewTarget(state, actor, ability.TargetId.Value));
            }

            default:
                return 0;
        }
    }

    /// <summary>
    /// What moving one enemy is worth along the objective's axis, plus the premium for removing a
    /// body that is already clawing at a Protect structure.
    /// </summary>
    private static int Vector(
        GameState state, Structure objective, int sign, UnitId targetId, DisplacementPreview? preview)
    {
        var target = state.FindUnit(targetId);
        if (target is null || target.Team.IsPlayer())
        {
            return 0;
        }

        int score = 0;

        bool besieging = objective.Role == ObjectiveKind.Protect
            && target.Position.DistanceTo(objective.At) <= 1;

        if (preview is not null && !preview.IsNoOp)
        {
            int before = target.Position.DistanceTo(objective.At);
            int after = preview.Destination.DistanceTo(objective.At);
            score += (before - after) * sign * Shove;

            if (besieging && (preview.WouldDown || preview.WouldCling
                || preview.Stop == DisplacementStop.Pit))
            {
                score += BesiegerPremium;
            }
        }
        else if (besieging)
        {
            // A plain swing at a besieger is still objective play — it is the only thing on this
            // board whose death buys the structure a round.
            score += BesiegerPremium / 3;
        }

        return score;
    }

    private static DisplacementPreview? Preview(GameState state, AttackCommand attack)
    {
        var attacker = state.FindUnit(attack.UnitId);
        var target = state.FindUnit(attack.TargetId);

        if (attacker is null || target is null
            || attack.Mode == AttackMode.Damage && attacker.Template.AttackPush <= 0)
        {
            return null;
        }

        var kind = attack.Mode == AttackMode.Pull ? DisplacementKind.Pull : DisplacementKind.Push;

        return Displacement.Preview(
            state, target.Id, attacker.Position, kind, attacker.Template.AttackPush);
    }
}
