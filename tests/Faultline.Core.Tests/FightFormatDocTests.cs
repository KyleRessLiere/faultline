using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// FIGHT_FORMAT.md is the authoring reference: it claims to print <c>first-contact.fight</c> "in
/// full", and it names every key the parser accepts. Both claims are checkable, so they are checked.
/// </summary>
/// <remarks>
/// This is the same bargain the Stop hook makes for GAMEPLAY.md, enforced instead of asked for. The
/// worked example had drifted into showing a board the real file does not have, and the error table
/// still listed "those eight keys" several keys later — neither is the sort of thing anyone notices
/// by reading, and both mislead the next author.
/// </remarks>
public class FightFormatDocTests
{
    [Fact]
    public void WorkedExample_IsTheFileItClaimsToBe()
    {
        var doc = ReadRepoFile("FIGHT_FORMAT.md");
        var real = Normalise(FirstContactSource());

        int anchor = doc.IndexOf("in full:", StringComparison.Ordinal);
        Assert.True(anchor > 0, "FIGHT_FORMAT.md no longer has a worked example.");

        int open = doc.IndexOf("```", anchor, StringComparison.Ordinal);
        int close = doc.IndexOf("```", open + 3, StringComparison.Ordinal);
        Assert.True(open > 0 && close > open, "The worked example's code fence is malformed.");

        var printed = Normalise(doc.Substring(open + 3, close - open - 3));

        Assert.Equal(real, printed);
    }

    [Fact]
    public void KeyTable_NamesEveryKeyTheParserAccepts()
    {
        var doc = ReadRepoFile("FIGHT_FORMAT.md");

        // Every key the parser has a case for. A key the reference does not mention is a key nobody
        // outside the C# knows exists — which is exactly what happened to design, objective and wave.
        foreach (var key in new[]
        {
            "id", "name", "description", "design", "number", "roster a", "roster b",
            "objective", "turn-limit", "protected", "footing", "retired", "spawn", "wave", "board",
        })
        {
            Assert.True(
                doc.Contains("`" + key + ":`", StringComparison.Ordinal)
                || doc.Contains("`" + key + " ", StringComparison.Ordinal),
                "FIGHT_FORMAT.md never mentions the '" + key + "' key.");
        }
    }

    [Fact]
    public void KeyTable_DoesNotStillClaimAFixedCountOfKeys()
    {
        // "Only those eight keys" was true once. A count in prose goes stale the first time a key is
        // added and cannot be checked by anything, so it should not be there at all.
        var doc = ReadRepoFile("FIGHT_FORMAT.md");

        Assert.DoesNotContain("those eight keys", doc, StringComparison.OrdinalIgnoreCase);
    }

    private static string FirstContactSource()
    {
        var path = Path.Combine(
            RepoRoot(), "src", "Faultline.Core", "Fights", "Data", "first-contact.fight");

        return File.ReadAllText(path);
    }

    private static string ReadRepoFile(string name) => File.ReadAllText(Path.Combine(RepoRoot(), name));

    private static string Normalise(string text) =>
        string.Join("\n", text.Replace("\r\n", "\n").Split('\n').Select(l => l.TrimEnd())).Trim('\n');

    /// <summary>
    /// The repo root, found from this file's own compile-time path rather than from the working
    /// directory — a test runner's cwd is not something to build on.
    /// </summary>
    private static string RepoRoot([CallerFilePath] string here = "")
    {
        var dir = Directory.GetParent(here);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "FIGHT_FORMAT.md")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return dir!.FullName;
    }
}
