namespace Faultline.Core
{
    /// <summary>
    /// Stable identifier for one member of a run's squad, for the whole run.
    /// </summary>
    /// <remarks>
    /// Deliberately not a <see cref="UnitId"/>. A <see cref="UnitId"/> is dense within one fight and
    /// is reassigned by <see cref="Game.Start(FightDefinition, int)"/> every time a fight begins, so
    /// the Vanguard is <c>u0</c> in one fight and <c>u2</c> in the next. A run has to say "this same
    /// Vanguard, six fights later, still carrying the damage" — that needs an id the fight does not
    /// own (CLAUDE.md: ids over references).
    /// </remarks>
    /// <param name="Value">Dense index assigned once, when the run starts.</param>
    public readonly record struct RunUnitId(int Value)
    {
        /// <summary>Sentinel meaning "no run unit".</summary>
        public static readonly RunUnitId None = new RunUnitId(-1);

        /// <inheritdoc/>
        public override string ToString() => "r" + Value;
    }
}
