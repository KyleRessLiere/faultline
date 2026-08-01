using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>Every node cleared.</summary>
    /// <param name="FightsWon">How many fights it took.</param>
    /// <param name="Survivors">The squad at the end, voided members included, so losses show.</param>
    public sealed record RunWon(int FightsWon, IReadOnlyList<RunUnit> Survivors) : RunEvent;
}
