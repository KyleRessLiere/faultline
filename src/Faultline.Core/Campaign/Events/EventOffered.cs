namespace Faultline.Core
{
    /// <summary>
    /// An event node was entered and its terms were put on the table. Every price is in the payload:
    /// known stakes, no hidden dice (MASTER_DESIGN §8.5).
    /// </summary>
    /// <param name="EventId">Which event.</param>
    /// <param name="Name">Its name.</param>
    /// <param name="Shape">Offer — walkable — or Strait, where every exit is priced.</param>
    /// <param name="Prompt">The scene, in voice.</param>
    /// <param name="HpCost">Hit points the paying duck gives up.</param>
    /// <param name="MaxHpGain">Hit points its ceiling gains, for the rest of the run.</param>
    /// <param name="WalkAwayLine">The line the offer leaves when it is refused — a scene, not a cancel.</param>
    public sealed record EventOffered(
        string EventId,
        string Name,
        EventShape Shape,
        string Prompt,
        int HpCost,
        int MaxHpGain,
        string WalkAwayLine) : RunEvent;
}
