using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Faultline.Core
{
    /// <summary>
    /// The authored fights, read from the <c>.fight</c> files embedded in this assembly.
    /// </summary>
    /// <remarks>
    /// Adding a fight is adding a text file to <c>Fights/Data</c> — no code change, no registration.
    /// The files are embedded resources rather than files on disk, so Core still performs no file IO
    /// and the DLL stays self-contained when it is dropped into Unity.
    /// </remarks>
    public static class FightLibrary
    {
        private const string ResourcePrefix = "Faultline.Core.Fights.Data.";
        private const string ResourceSuffix = ".fight";

        // Parsed once. The files are embedded resources and cannot change while the process runs, so
        // "read them again" could only ever produce the same answer at the price of re-parsing sixty-six
        // files. That price was real: a screen that asked the library a question per node — which every
        // map screen does — spent seconds per frame re-reading a constant. Static initialisers are
        // thread-safe, so the eager form needs no lock.
        private static readonly IReadOnlyList<FightParseResult> Parsed = ParseAll();

        private static readonly IReadOnlyList<FightDefinition> ActiveFights = Sort(retired: false);

        private static readonly IReadOnlyList<FightDefinition> RetiredFights = Sort(retired: true);

        /// <summary>
        /// Every embedded fight file, parsed, in filename order — including ones that failed, so a
        /// broken file is visible rather than silently absent.
        /// </summary>
        /// <returns>One parse result per file.</returns>
        public static IReadOnlyList<FightParseResult> LoadAll() => Parsed;

        private static IReadOnlyList<FightParseResult> ParseAll()
        {
            var results = new List<FightParseResult>();
            var assembly = typeof(FightLibrary).GetTypeInfo().Assembly;

            var names = new List<string>(assembly.GetManifestResourceNames());
            names.Sort(StringComparer.Ordinal);

            foreach (var name in names)
            {
                if (name.StartsWith(ResourcePrefix, StringComparison.Ordinal)
                    && name.EndsWith(ResourceSuffix, StringComparison.Ordinal))
                {
                    results.Add(FightParser.Parse(ReadResource(assembly, name)));
                }
            }

            return results;
        }

        /// <summary>
        /// Every active fight that parsed cleanly, in run order. Retired battles are left out — that
        /// is what retiring one does (docs/RETIRING_BATTLES.md).
        /// </summary>
        /// <returns>The playable fights.</returns>
        public static IReadOnlyList<FightDefinition> All() => ActiveFights;

        /// <summary>
        /// Every retired battle, in run order, each carrying the reason its <c>retired:</c> key gave.
        /// Nothing is deleted, so "should we bring that back?" stays a question the library can answer.
        /// </summary>
        /// <returns>The retired fights.</returns>
        public static IReadOnlyList<FightDefinition> Retired() => RetiredFights;

        /// <summary>Looks up one fight by its id, retired or not.</summary>
        /// <remarks>
        /// Retiring hides a battle from the playable list; it does not make it unreachable. A picker
        /// showing its retired section, and any test pinned to a retired board, both still resolve.
        /// </remarks>
        /// <param name="id">The fight's <c>id:</c> slug.</param>
        /// <returns>The fight.</returns>
        public static FightDefinition ById(string id)
        {
            foreach (var result in LoadAll())
            {
                if (result.Fight is not null && string.Equals(result.Fight.Id, id, StringComparison.Ordinal))
                {
                    return result.Fight;
                }
            }

            throw new ArgumentException("No fight with id '" + id + "'.", nameof(id));
        }

        /// <summary>The fight the run opens on.</summary>
        /// <returns>Fight 1.</returns>
        public static FightDefinition Fight1() => ById("first-contact");

        private static IReadOnlyList<FightDefinition> Sort(bool retired)
        {
            var fights = new List<FightDefinition>();
            foreach (var result in LoadAll())
            {
                if (result.Fight is not null && result.Fight.IsRetired == retired)
                {
                    fights.Add(result.Fight);
                }
            }

            fights.Sort((a, b) => a.Number != b.Number
                ? a.Number.CompareTo(b.Number)
                : string.CompareOrdinal(a.Id, b.Id));

            return fights;
        }

        private static string ReadResource(Assembly assembly, string name)
        {
            using (var stream = assembly.GetManifestResourceStream(name))
            {
                if (stream is null)
                {
                    throw new InvalidOperationException("Embedded fight '" + name + "' could not be opened.");
                }

                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
