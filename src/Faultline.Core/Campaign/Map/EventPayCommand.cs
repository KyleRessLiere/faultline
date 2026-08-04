namespace Faultline.Core
{
    /// <summary>
    /// Take the offer, with one named duck paying for it.
    /// </summary>
    /// <remarks>
    /// <b>Bodily consent (MASTER_DESIGN §8.5).</b> The vote governs where the run goes; it does not
    /// govern what a duck pays. So there is no command that accepts an event on the party's behalf,
    /// and no command that lets the rules pick who bleeds: a payment names its payer, and the surface
    /// that issues it is responsible for having asked that duck's owner. The engine's half of the
    /// guarantee is that no other command can charge a duck — enumerate
    /// <see cref="Campaign.LegalRunCommands"/> at an event and every payment on the list is one
    /// specific duck's.
    /// </remarks>
    /// <param name="Payer">The duck that pays.</param>
    public sealed record EventPayCommand(RunUnitId Payer) : RunCommand
    {
    }
}
