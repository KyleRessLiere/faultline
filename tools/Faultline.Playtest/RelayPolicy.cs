using Faultline.Core;

namespace Faultline.Playtest;

/// <summary>
/// Scores every command off <see cref="Abilities.Outlook"/> — the single Core projection per action —
/// and takes the best. The only policy that can see a technique modifier at all.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this had to exist to measure anything.</b> Every other policy scores by command
/// <em>type</em>: an attack is worth so much, a shove so much, a move so much. Under that ordering a
/// Follow-In attack and a plain attack are the same command with the same score, and
/// <see cref="Policy.Best"/> breaks the tie by list order, so the elected variant is never chosen —
/// which makes "did the card change what the player attempted?" unanswerable by construction. A card
/// can only change a choice for a player who can see what it does.
/// </para>
/// <para>
/// <b>It reads the preview and nothing else.</b> Stage A made <see cref="ActionOutlook"/> the one
/// projection per action and pinned it against resolution; this scores that projection. It holds no
/// copy of any rule — every number it adds up is one Core computed.
/// </para>
/// </remarks>
public sealed class RelayPolicy : Policy
{
    /// <inheritdoc/>
    public override string Name => "relay";

    /// <inheritdoc/>
    public override string Intent =>
        "Reads Core's own preview of every action and takes the one that does the most — damage, "
        + "displacement, its own footing, and anything it hands the other flock.";

    /// <inheritdoc/>
    public override Command Choose(GameState state, IReadOnlyList<Command> legal, DeterministicRng rng) =>
        Best(legal, command => Score(state, command));

    private static int Score(GameState state, Command command)
    {
        if (command is MoveCommand move)
        {
            return Walk(state, move, 6);
        }

        if (command is TakeBankedStepCommand)
        {
            // A free tile is a free tile, and refusing one is the only way Shelter Step's offer can
            // be declined. Taking it is worth more than standing still and less than acting.
            return 8;
        }

        if (command is EndActivationCommand)
        {
            return 0;
        }

        var outlook = Abilities.Outlook(state, command);
        if (outlook is null)
        {
            return command is RescueCommand ? 40 : 4;
        }

        int score = 10;
        score += outlook.Damage * 4;

        foreach (var hit in outlook.LineHits)
        {
            // Signed, for the reason Masonry.Sign gives: a line that clips the shrine this policy is
            // defending is a cost, and the preview cannot say so on its own — it reports what the
            // action does, not whose side the wall is on.
            score += hit.Damage * 4 * (hit.HitsStructure ? Masonry.Sign(state, hit.At) : 1);
        }

        if (outlook.Displacement is { } shove)
        {
            score += shove.DamageToUnit * 4;
            score += shove.DamageToObstacle * 3;
            score += shove.EffectiveDistance * 2;

            if (shove.WouldDown)
            {
                score += 30;
            }

            if (shove.WouldCling)
            {
                score += 25;
            }
        }

        if (outlook.Charge is { } charge)
        {
            score += charge.Path.Count;
            score -= charge.SelfDamage * 3;

            if (charge.Contact is { } contact)
            {
                score += contact.DamageToUnit * 4;
                score += contact.EffectiveDistance * 2;
            }
        }

        // The two halves the v1 pool had no way to price. A step into the tile the target left is
        // board the flock now holds; a shot the other flock's Archer takes for free, and a push
        // handed to the other flock's duck, are the RELAY category paying out.
        if (outlook.ActorMovesTo is not null)
        {
            score += 7;
        }

        if (outlook.Reaction is { } reaction)
        {
            score += reaction.Damage * 4;
        }

        if (outlook.GrantsTo is not null)
        {
            score += 9;
        }

        return score;
    }
}
