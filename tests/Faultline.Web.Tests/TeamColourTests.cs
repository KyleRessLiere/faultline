using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace Faultline.Web.Tests;

/// <summary>
/// A side has one colour, declared once, and every surface that draws a side reads it.
/// </summary>
/// <remarks>
/// <para>
/// Player B used to be drawn three ways at once — <c>--pt-cyan</c> teal on the board,
/// <c>--pt-green</c> olive in the status band, <c>--b</c> mint in the strip and the inspector. Each
/// of the three declarations was correct-looking on its own, which is why nothing caught it: the
/// defect was not in any one file, it was in the fact that three files each chose.
/// </para>
/// <para>
/// <b>What this test can and cannot do.</b> It is a static reading of the stylesheets, so it cannot
/// prove what a browser paints — <c>tools/ui-checks/team-colour-check.mjs</c> does that, by driving
/// the real battle screen and comparing <c>getComputedStyle</c> across surfaces. What this one does
/// is close the door the browser check cannot be run through on every push: a rule that draws a side
/// may name that side's token and nothing else. No raw hex, no borrowed affordance colour, no second
/// variable that happens to be the same today. That is enough to make the three-way split
/// unwritable, and it runs in CI without a browser.
/// </para>
/// </remarks>
public class TeamColourTests
{
    /// <summary>The tokens, and the only names a team surface may use.</summary>
    private const string TokenA = "--player-a";
    private const string TokenB = "--player-b";
    private const string TokenE = "--enemy";

    /// <summary>
    /// Selector fragments that mean "this rule draws a side", mapped to the token it must use.
    /// </summary>
    private static readonly (string Fragment, string Token)[] TeamSelectors =
    {
        (".team-a", TokenA), (".team-b", TokenB), (".team-e", TokenE),
        (".slot.a", TokenA), (".slot.b", TokenB),
        (".slot-a", TokenA), (".slot-b", TokenB),
        (".zone-a", TokenA), (".zone-b", TokenB),
        (".card.enemy", TokenE), (".beast.enemy", TokenE), (".jump-link.enemy", TokenE),
    };

    /// <summary>Colour-valued properties. A team rule's colour is what is under test, not its box.</summary>
    private static readonly Regex ColourDeclaration = new Regex(
        @"(?<prop>[-a-z]*colou?r|background|box-shadow|outline|border(?:-(?:top|right|bottom|left))?)\s*:\s*(?<value>[^;}]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex Rule = new Regex(
        @"(?<selector>[^{}/][^{}]*?)\{(?<body>[^{}]*)\}", RegexOptions.Compiled);

    private static readonly Regex Hex = new Regex(@"#[0-9a-fA-F]{3,8}\b", RegexOptions.Compiled);

    /// <summary>A colour written out rather than named. <c>in srgb</c> is a colour space, not one.</summary>
    private static readonly Regex Literal = new Regex(
        @"\b(rgba?|hsla?)\s*\(", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static string WebRoot
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null)
            {
                var candidate = Path.Combine(dir.FullName, "src", "Faultline.Web");
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }

                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException("src/Faultline.Web is not above the test binary.");
        }
    }

    private static IEnumerable<string> StyleSheets() =>
        Directory.EnumerateFiles(WebRoot, "*.css", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));

    private static string AppCss => Path.Combine(WebRoot, "wwwroot", "css", "app.css");

    // ---- the tokens exist, once, where every scope can reach them -------------------------------

    /// <summary>
    /// Blazor's scoped CSS cannot see another component's variables, so a token declared inside a
    /// component is invisible to every other surface — which is how three of them ended up each
    /// declaring their own. The three tokens live in <c>:root</c> and nowhere else.
    /// </summary>
    [Fact]
    public void EachTeamToken_IsDeclaredExactlyOnce_AtRootScope()
    {
        var root = Regex.Match(File.ReadAllText(AppCss), @":root\s*\{(?<body>[^}]*)\}");
        Assert.True(root.Success, "app.css has no :root block.");

        foreach (var token in new[] { TokenA, TokenB, TokenE })
        {
            Assert.True(
                Regex.IsMatch(root.Groups["body"].Value, Regex.Escape(token) + @"\s*:\s*#[0-9a-fA-F]{3,8}"),
                $"{token} is not declared as a literal colour in app.css :root.");
        }

        foreach (var path in StyleSheets())
        {
            string text = File.ReadAllText(path);
            foreach (var token in new[] { TokenA, TokenB, TokenE })
            {
                int declarations = Regex.Matches(text, Regex.Escape(token) + @"\s*:").Count;
                int expected = path == AppCss ? 1 : 0;
                Assert.True(
                    declarations == expected,
                    $"{Path.GetFileName(path)} declares {token} {declarations} time(s); a side's colour "
                    + "is chosen in app.css :root and nowhere else.");
            }
        }
    }

    /// <summary>
    /// The old per-surface names are gone rather than merely unused. Leaving <c>--a</c>, <c>--b</c>
    /// and <c>--e</c> declared is leaving the next surface somewhere to reach for.
    /// </summary>
    [Fact]
    public void TheOldPerSurfaceTeamHues_AreGone()
    {
        foreach (var path in StyleSheets())
        {
            string text = File.ReadAllText(path);
            foreach (var dead in new[] { "--a", "--b", "--e" })
            {
                Assert.False(
                    Regex.IsMatch(text, $@"var\(\s*{Regex.Escape(dead)}\s*[,)]")
                    || Regex.IsMatch(text, $@"(?<![-\w]){Regex.Escape(dead)}\s*:\s*#"),
                    $"{Path.GetFileName(path)} still uses {dead}; a team colour is "
                    + $"{TokenA}/{TokenB}/{TokenE} now.");
            }
        }
    }

    // ---- no surface may choose for itself ------------------------------------------------------

    /// <summary>
    /// Every rule whose selector names a side must colour it with that side's token: not a hex, not
    /// an affordance colour borrowed from the playtest palette, not a second variable. This is the
    /// assertion the three-hue split fails.
    /// </summary>
    [Fact]
    public void EveryRuleThatDrawsASide_UsesThatSidesTokenAndNothingElse()
    {
        var offences = new List<string>();

        foreach (var path in StyleSheets())
        {
            foreach (Match rule in Rule.Matches(StripComments(File.ReadAllText(path))))
            {
                string selector = rule.Groups["selector"].Value.Trim();

                var tokens = TeamSelectors
                    .Where(t => selector.Contains(t.Fragment, StringComparison.Ordinal))
                    .Select(t => t.Token)
                    .Distinct()
                    .ToList();

                if (tokens.Count != 1)
                {
                    continue;
                }

                string token = tokens[0];

                foreach (Match declaration in ColourDeclaration.Matches(rule.Groups["body"].Value))
                {
                    string value = declaration.Groups["value"].Value;

                    // "in srgb" is a colour space, not a colour: the function form is what names one.
                    bool literal = Hex.IsMatch(value) || Literal.IsMatch(value);

                    // Colourless shorthands - "1px dashed", "transparent" - say nothing about a side.
                    bool namesAColour = literal || Regex.IsMatch(value, @"var\(\s*--(?!pt-border)");

                    if (!namesAColour)
                    {
                        continue;
                    }

                    var named = Regex.Matches(value, @"var\(\s*(?<name>--[-\w]+)")
                        .Select(m => m.Groups["name"].Value)
                        .Where(n => n != "--pt-border" && n != "--pt-border-soft" && n != "--pt-bg-raised")
                        .Distinct()
                        .ToList();

                    string where = $"{Path.GetFileName(path)}  {selector}  {declaration.Value.Trim()}";

                    if (literal)
                    {
                        offences.Add($"{where}\n      -> hard-codes a colour instead of reading {token}");
                        continue;
                    }

                    if (named.Count != 1 || named[0] != token)
                    {
                        offences.Add(
                            $"{where}\n      -> names {string.Join(", ", named)} but this selector draws "
                            + $"{token}");
                    }
                }
            }
        }

        Assert.True(
            offences.Count == 0,
            "A surface is choosing a side's colour for itself. That is how Player B ended up teal on "
            + "the board, olive in the status band and mint in the strip:\n  "
            + string.Join("\n  ", offences));
    }

    /// <summary>
    /// The playtest palette is affordances — action points, aiming, danger, the rescue landing — and
    /// none of it may be pressed into service as a side. Named separately from the rule above
    /// because this is the specific relapse that already happened three times.
    /// </summary>
    [Theory]
    [InlineData("--pt-cyan")]
    [InlineData("--pt-green")]
    [InlineData("--pt-blue")]
    [InlineData("--pt-red")]
    public void NoAffordanceColour_IsUsedToDrawASide(string affordance)
    {
        foreach (var path in StyleSheets())
        {
            foreach (Match rule in Rule.Matches(StripComments(File.ReadAllText(path))))
            {
                string selector = rule.Groups["selector"].Value;
                if (!TeamSelectors.Any(t => selector.Contains(t.Fragment, StringComparison.Ordinal)))
                {
                    continue;
                }

                Assert.False(
                    rule.Groups["body"].Value.Contains($"var({affordance})", StringComparison.Ordinal),
                    $"{Path.GetFileName(path)}: \"{selector.Trim()}\" draws a side with {affordance}, "
                    + "which is an affordance colour and not a side.");
            }
        }
    }

    private static string StripComments(string css) =>
        Regex.Replace(css, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
}
