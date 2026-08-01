namespace Faultline.Core
{
    /// <summary>One problem found while reading a fight file.</summary>
    /// <param name="Code">Stable identifier, so tooling and tests never match on prose.</param>
    /// <param name="Message">Human-readable explanation, including what to do about it.</param>
    /// <param name="Line">1-based line in the source file, or 0 when the issue is about the file as a whole.</param>
    public sealed record FightIssue(FightIssueCode Code, string Message, int Line)
    {
        /// <summary>
        /// True when this stops the fight being playable. Errors and lints are split by code range:
        /// anything below 100 is fatal, anything at or above is a design guideline from the brief.
        /// </summary>
        public bool IsError => (int)Code < 100;

        /// <inheritdoc/>
        public override string ToString() =>
            (Line > 0 ? "line " + Line + ": " : string.Empty) + Code + " — " + Message;
    }
}
