using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// The outcome of reading a fight file: the fight when it is playable, plus everything worth
    /// telling the author.
    /// </summary>
    /// <remarks>
    /// Errors and lints are deliberately separate. An error means the file cannot become a fight —
    /// a typo'd unit name, a deployment zone with nowhere to stand. A lint means it parsed fine but
    /// breaks a guideline from AGENT_BRIEF §2, which an author may be doing on purpose. Lints never
    /// block loading; they are a visible deviation rather than a failure, and callers are expected
    /// to surface them to whoever authored the file.
    /// </remarks>
    /// <param name="Fight">The parsed fight, or <c>null</c> when there were errors.</param>
    /// <param name="Issues">Everything found, errors and lints together, in source order.</param>
    public sealed record FightParseResult(FightDefinition? Fight, IReadOnlyList<FightIssue> Issues)
    {
        /// <summary>True when a playable fight came out.</summary>
        public bool Ok => Fight is not null;

        /// <summary>Only the fatal issues.</summary>
        public IReadOnlyList<FightIssue> Errors => Filter(true);

        /// <summary>Only the guideline warnings.</summary>
        public IReadOnlyList<FightIssue> Lints => Filter(false);

        /// <summary>A one-line summary suitable for a log or a test failure message.</summary>
        /// <returns>Summary text.</returns>
        public string Describe()
        {
            var name = Fight is null ? "(unparsed)" : Fight.Name;
            return name + ": " + Errors.Count + " error(s), " + Lints.Count + " lint(s)";
        }

        private IReadOnlyList<FightIssue> Filter(bool errors)
        {
            var result = new List<FightIssue>();
            foreach (var issue in Issues)
            {
                if (issue.IsError == errors)
                {
                    result.Add(issue);
                }
            }

            return result;
        }
    }
}
