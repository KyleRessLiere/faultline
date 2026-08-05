namespace Faultline.Core
{
    /// <summary>
    /// Answers an outstanding <see cref="FootingPrompt"/>: spend Footing and refuse the whole
    /// displacement, or decline and let it land.
    /// </summary>
    /// <remarks>
    /// Both answers are commands, and both go in the log. A decline that left no command behind would
    /// make a replayed fight diverge from the one that was played, because "nobody answered yet" and
    /// "the owner said no" are different states.
    /// </remarks>
    /// <param name="TargetId">Unit being displaced; must match the outstanding prompt.</param>
    /// <param name="Refuse">True to spend the Footing, false to let the displacement resolve.</param>
    public sealed record FootingRefuseCommand(UnitId TargetId, bool Refuse) : Command;
}
