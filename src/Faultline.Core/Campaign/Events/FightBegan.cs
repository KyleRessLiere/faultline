namespace Faultline.Core
{
    /// <summary>A fight node started its fight.</summary>
    /// <param name="Index">Node index.</param>
    /// <param name="FightId">Fight id.</param>
    /// <param name="Number">The fight's authoring number.</param>
    /// <param name="Name">The fight's display name.</param>
    /// <param name="Fielded">How many squad members walked onto the board.</param>
    public sealed record FightBegan(
        int Index,
        string FightId,
        int Number,
        string Name,
        int Fielded) : RunEvent;
}
