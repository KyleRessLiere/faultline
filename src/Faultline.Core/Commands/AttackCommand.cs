namespace Faultline.Core
{
    /// <summary>Spends the action half of an activation on a basic attack.</summary>
    /// <param name="UnitId">Attacking unit.</param>
    /// <param name="TargetId">Unit to attack.</param>
    /// <param name="Mode">Which half of the attack profile to use; only the Threadcaster has a choice.</param>
    /// <param name="Aim">
    /// Which of two tiles a diagonal shove or haul sends the target to. Part of the aim, not a
    /// reaction to it: unlike a Footing refusal — which is the <em>other</em> side answering mid
    /// resolution, and therefore its own command — the acting side knows this before anything
    /// resolves, so it rides the command that already carries the rest of the aim, exactly as Bull
    /// Rush's charge direction does. That is what puts the choice in the log and makes a replay
    /// resolve the ambiguity the way the played fight did.
    /// </param>
    /// <param name="Technique">
    /// Technique halves the attacker elects for this attack — Follow-In's step, Hand-Off's granted
    /// push. See <see cref="TechniqueOption"/> for why the election rides the command.
    /// </param>
    public sealed record AttackCommand(
        UnitId UnitId,
        UnitId TargetId,
        AttackMode Mode = AttackMode.Damage,
        DisplacementAim Aim = DisplacementAim.Default,
        TechniqueOption Technique = TechniqueOption.None) : Command;
}
