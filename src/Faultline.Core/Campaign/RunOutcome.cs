namespace Faultline.Core
{
    /// <summary>How a run ended, or that it has not.</summary>
    public enum RunOutcome
    {
        /// <summary>Still being played.</summary>
        InProgress = 0,

        /// <summary>Every node cleared.</summary>
        Won = 1,

        /// <summary>A fight was lost. There is no second chance and no branch.</summary>
        Lost = 2,
    }
}
