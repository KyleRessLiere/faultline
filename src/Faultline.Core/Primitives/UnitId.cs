namespace Faultline.Core
{
    /// <summary>
    /// Stable identifier for a unit. Commands and events reference units by id, never by object
    /// reference, so a renderer can resolve them against any snapshot (CLAUDE.md: ids over references).
    /// </summary>
    /// <param name="Value">Dense, deterministic index assigned at fight setup.</param>
    public readonly record struct UnitId(int Value)
    {
        /// <summary>Sentinel meaning "no unit".</summary>
        public static readonly UnitId None = new UnitId(-1);

        /// <inheritdoc/>
        public override string ToString() => "u" + Value;
    }
}
