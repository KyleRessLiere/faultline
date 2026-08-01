namespace Faultline.Core
{
    /// <summary>
    /// An enemy announced what it is going to do. Fired for every enemy at round start, and again
    /// for a single enemy the moment its target dies or is removed and it has to pick a new one.
    /// </summary>
    /// <param name="Intent">The full plan, complete enough to telegraph without querying state.</param>
    /// <param name="Replanned">
    /// False for the round-start declaration, true when this replaces an intent whose target became
    /// invalid mid-round (Brief §2: "re-plans — same priority list, immediately, visibly").
    /// </param>
    public sealed record IntentDeclared(EnemyIntent Intent, bool Replanned) : GameEvent;
}
