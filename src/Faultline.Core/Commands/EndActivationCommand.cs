namespace Faultline.Core
{
    /// <summary>
    /// Ends the current unit's activation, forfeiting anything unspent. This is also Focus: Brief §2
    /// reserves Focus as a hook and specifies it does nothing in the MVP beyond passing.
    /// </summary>
    /// <param name="UnitId">Unit whose activation ends.</param>
    public sealed record EndActivationCommand(UnitId UnitId) : Command;
}
