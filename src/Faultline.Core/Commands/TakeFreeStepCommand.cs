namespace Faultline.Core
{
    /// <summary>
    /// Spends one tile of the free movement a permanent legendary owes this duck — Follow Through
    /// after a collision, Kestrel Step after a shot (MASTER_DESIGN §8.6).
    /// </summary>
    /// <remarks>
    /// <para>
    /// One tile per command rather than a route, because the tiles are taken after the board has
    /// already changed and each one is a fresh decision: the shove that paid for them may have opened
    /// a lane that was not there when the action was aimed.
    /// </para>
    /// <para>
    /// <b>Taking none is a legal answer.</b> The activation holds open while tiles are owed, and
    /// <see cref="EndActivationCommand"/> is on the list beside this one — a duck that has nowhere it
    /// wants to be simply stops. Nothing is spent by declining, because nothing was paid for them.
    /// </para>
    /// </remarks>
    /// <param name="UnitId">Duck taking the step.</param>
    /// <param name="To">The adjacent tile it steps into.</param>
    public sealed record TakeFreeStepCommand(UnitId UnitId, Coord To) : Command;
}
