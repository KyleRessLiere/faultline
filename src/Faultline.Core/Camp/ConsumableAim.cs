namespace Faultline.Core
{
    /// <summary>
    /// What a one-shot asks the player to pick before it can come out of the pocket.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Four members, and deliberately no fifth. The component review's blacklist names "a schema that
    /// attempts to anticipate mechanics not yet designed": an aim kind nothing aims with is a shape
    /// no legality generator and no shell surface has ever been asked to draw, so it would be a
    /// promise the code does not keep. A new aim kind is a member here plus a case in
    /// <see cref="Consumables.Legal"/> plus a test.
    /// </para>
    /// <para>
    /// The aim answers only <em>what does the player select</em>. What happens afterwards is
    /// <see cref="ConsumableDefinition.Effects"/> or a named <see cref="ConsumableRule"/> — the same
    /// split <see cref="AbilityDefinition"/> makes.
    /// </para>
    /// </remarks>
    public enum ConsumableAim
    {
        /// <summary>Nothing to pick: it acts on the duck that carries it.</summary>
        None = 0,

        /// <summary>One unit on the board.</summary>
        Unit = 1,

        /// <summary>One tile on the board.</summary>
        Tile = 2,

        /// <summary>One unit and the tile it is put down on.</summary>
        UnitAndTile = 3,

        /// <summary>
        /// Two units, in the order the card reads them — the pair a Signal Whistle exchanges in the
        /// activation queue.
        /// </summary>
        TwoUnits = 4,
    }
}
