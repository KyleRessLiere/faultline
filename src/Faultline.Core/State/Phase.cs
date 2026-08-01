namespace Faultline.Core
{
    /// <summary>Top-level stage of a fight.</summary>
    public enum Phase
    {
        /// <summary>Players are alternately placing their units in their corners.</summary>
        Deployment = 0,

        /// <summary>Rounds are running.</summary>
        Battle = 1,

        /// <summary>The fight has been won or lost; no further commands are legal.</summary>
        Complete = 2,
    }
}
