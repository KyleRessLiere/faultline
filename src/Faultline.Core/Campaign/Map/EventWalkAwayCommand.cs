namespace Faultline.Core
{
    /// <summary>
    /// Leave an Offer without paying. Always legal at an Offer, and never at a Strait — which is the
    /// only difference between the two (MASTER_DESIGN §8.5).
    /// </summary>
    public sealed record EventWalkAwayCommand : RunCommand
    {
    }
}
