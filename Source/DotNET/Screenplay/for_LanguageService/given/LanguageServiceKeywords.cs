// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Cratis.Screenplay.for_LanguageService.given;

/// <summary>
/// Reads the keyword lists the Monaco language service highlights and completes, and the words the
/// parsers actually dispatch on, so the two can be compared.
/// </summary>
/// <remarks>
/// The language service and the compiler are two descriptions of one grammar, and nothing kept them
/// honest. <c>capture</c>, <c>projection</c>, <c>reducer</c>, <c>contribute</c>, <c>dialog</c>,
/// <c>form</c>, <c>theme</c> and <c>ui</c> were all constructs the parser dispatches on with no keyword
/// entry, so they got neither highlighting nor completion. Absent highlighting reads as "this construct
/// is wrong", which is the worst signal to give about something that compiles.
/// <para>
/// This derives the truth from the parsers rather than restating it, so adding a construct to the
/// compiler and forgetting the editor fails here instead of shipping.
/// </para>
/// </remarks>
public static partial class LanguageServiceKeywords
{
    static readonly string[] _parsers = ["ScreenplayParser", "SliceParser"];

    /// <summary>
    /// Gets the repository root, found by walking up from this file.
    /// </summary>
    /// <param name="path">The path of this source file, supplied by the compiler.</param>
    /// <returns>The full path of the repository root.</returns>
    public static string Root([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation")))
        {
            directory = directory.Parent;
        }

        return directory!.FullName;
    }

    /// <summary>
    /// Gets every word the parsers dispatch on at the top level, inside a module, or inside a slice.
    /// </summary>
    /// <returns>The dispatched construct keywords.</returns>
    public static IReadOnlySet<string> Dispatched()
    {
        var keywords = new HashSet<string>(StringComparer.Ordinal);
        foreach (var parser in _parsers)
        {
            var path = Path.Combine(Root(), "Source", "DotNET", "Screenplay", "Parsing", $"{parser}.cs");
            foreach (Match match in CaseLabelRegex().Matches(File.ReadAllText(path)))
            {
                keywords.Add(match.Groups[1].Value);
            }
        }

        return keywords;
    }

    /// <summary>
    /// Gets every keyword the Monaco language service knows, across both of its lists.
    /// </summary>
    /// <returns>The keywords the language service highlights and completes.</returns>
    public static IReadOnlySet<string> KnownToTheLanguageService()
    {
        var path = Path.Combine(Root(), "Source", "Screenplay", "Monaco", "screenplay-language", "language.ts");
        var source = File.ReadAllText(path);
        var keywords = new HashSet<string>(StringComparer.Ordinal);
        foreach (var list in new[] { "constructKeywords", "clauseKeywords" })
        {
            var declaration = Regex.Match(
                source,
                $@"export const {list} = \[(.*?)\];",
                RegexOptions.Singleline,
                TimeSpan.FromMilliseconds(1000));
            foreach (Match match in QuotedRegex().Matches(declaration.Groups[1].Value))
            {
                keywords.Add(match.Groups[1].Value);
            }
        }

        return keywords;
    }

    [GeneratedRegex("""case "([a-z][a-zA-Z]*)":""", RegexOptions.None, 1000)]
    private static partial Regex CaseLabelRegex();

    [GeneratedRegex("'([^']+)'", RegexOptions.None, 1000)]
    private static partial Regex QuotedRegex();
}
