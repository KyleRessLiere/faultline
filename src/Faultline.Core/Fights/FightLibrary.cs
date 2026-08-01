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

        /// <summary>
        /// Every embedded fight file, parsed, in filename order — including ones that failed, so a
        /// broken file is visible rather than silently absent.
        /// </summary>
        /// <returns>One parse result per file.</returns>
        public static IReadOnlyList<FightParseResult> LoadAll()
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

        /// <summary>Every fight that parsed cleanly, in run order.</summary>
        /// <returns>The playable fights.</returns>
        public static IReadOnlyList<FightDefinition> All()
        {
            var fights = new List<FightDefinition>();
            foreach (var result in LoadAll())
            {
                if (result.Fight is not null)
                {
                    fights.Add(result.Fight);
                }
            }

            fights.Sort((a, b) => a.Number != b.Number
                ? a.Number.CompareTo(b.Number)
                : string.CompareOrdinal(a.Id, b.Id));

            return fights;
        }

        /// <summary>Looks up one fight by its id.</summary>
        /// <param name="id">The fight's <c>id:</c> slug.</param>
        /// <returns>The fight.</returns>
        public static FightDefinition ById(string id)
        {
            foreach (var fight in All())
            {
                if (string.Equals(fight.Id, id, StringComparison.Ordinal))
                {
                    return fight;
                }
            }

            throw new ArgumentException("No fight with id '" + id + "'.", nameof(id));
        }

        /// <summary>The fight the run opens on.</summary>
        /// <returns>Fight 1.</returns>
        public static FightDefinition Fight1() => ById("first-contact");

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
