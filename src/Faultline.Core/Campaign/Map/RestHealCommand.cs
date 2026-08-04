namespace Faultline.Core
{
    /// <summary>
    /// Spend the campfire on healing. The only thing a v1 campfire can be spent on.
    /// </summary>
    /// <remarks>
    /// A command rather than something entering the node does, because the campfire is a choice —
    /// heal, or forge, or scrape a curse (MASTER_DESIGN §8.5) — and two of the three are unbuilt. The
    /// choice arrives with one option in it rather than arriving later as a different shape.
    /// </remarks>
    public sealed record RestHealCommand : RunCommand
    {
    }
}
