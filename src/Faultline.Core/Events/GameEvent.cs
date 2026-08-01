namespace Faultline.Core
{
    /// <summary>
    /// Base type for everything Core reports about a step. Brief §1: Core emits events, never
    /// visuals — each event carries a payload complete enough that a renderer never has to query
    /// state to draw it.
    /// </summary>
    public abstract record GameEvent
    {
    }
}
