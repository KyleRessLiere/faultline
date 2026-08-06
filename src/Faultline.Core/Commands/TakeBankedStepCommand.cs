namespace Faultline.Core
{
    /// <summary>
    /// Takes the free step Shelter Step banked for this duck (MASTER_DESIGN §8.6): one tile, into the
    /// tile the Wardbearer left, costing nothing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This command is the consent.</b> The step is offered by the other player's card and moves
    /// this player's body, and §8.5 is explicit that bodily consent is separate from anything the
    /// other flock decides. Banking a tile and waiting for its owner is the narrowest way to say that
    /// in a command log: nothing happens until the owner asks for it, and never asking is a legal
    /// answer that costs nothing.
    /// </para>
    /// <para>
    /// It is not an activation and does not spend one. A duck may take it whenever it is its owner's
    /// turn to act — the same freedom a consumable has (§8.5, "free-timing in its own activation").
    /// </para>
    /// </remarks>
    /// <param name="UnitId">Duck taking its banked step.</param>
    public sealed record TakeBankedStepCommand(UnitId UnitId) : Command;
}
