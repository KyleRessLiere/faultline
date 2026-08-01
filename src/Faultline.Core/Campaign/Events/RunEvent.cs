namespace Faultline.Core
{
    /// <summary>
    /// Base type for everything the run layer reports, mirroring <see cref="GameEvent"/> one level up.
    /// </summary>
    /// <remarks>
    /// Every payload is complete — ids, kinds, hit points, node indexes — so a renderer animating a
    /// run never has to query <see cref="RunState"/> to draw what happened (CLAUDE.md).
    /// </remarks>
    public abstract record RunEvent
    {
    }
}
