// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Languages;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>Establishes physical-document options before any value-bearing grammar is entered.</summary>
internal static partial class SourceOptionsParser
{
    internal static ParserContext Create(IReadOnlyList<SourceLine> lines, string? path = null, IScreenplayLanguageRegistry? languages = null, bool hashComments = false, CommandStreamCandidates? streamCandidates = null)
    {
        var prepared = new List<SourceLine>(lines.Count);
        var diagnostics = new List<Diagnostic>();
        var options = SourceOptions.Legacy;
        var seen = false;
        var contentSeen = false;
        var inFence = false;
        var nested = new Dictionary<int, Diagnostic>();
        foreach (var original in lines)
        {
            var line = original;
            if (line.Number == 1 && line.Raw.StartsWith('\uFEFF'))
            {
                var text = line.Raw[1..];
                var indent = text.Length - text.TrimStart().Length;
                var comment = SourceLineSplitter.CommentStart(text[indent..], hashComments);
                var content = (comment < 0 ? text[indent..] : text.Substring(indent, comment)).TrimEnd();
                line = line with { Indent = indent, Content = content, ContentOffset = 1 };
            }

            if (inFence)
            {
                // Code bodies are raw text: comments and embedded backticks are not source directives.
                if (CodeBlockParser.IsClosingFence(line)) inFence = false;
                prepared.Add(line);
                continue;
            }

            if (line.Content.StartsWith("```", StringComparison.Ordinal) &&
                (line.Content == "```" || line.Content == "```text" || line.Content == "```markdown" ||
                 (languages ?? ScreenplayLanguageRegistry.Default).InlineLanguages.Contains(line.Content[3..])))
            {
                inFence = true;
            }
            if (!inFence && line.Indent == 0 && LineText.FirstWord(line.Content) == "numbers")
            {
                var code = line.Content switch
                {
                    not "numbers exact" => DiagnosticCodes.InvalidNumericDirective,
                    _ when seen => DiagnosticCodes.DuplicateNumericDirective,
                    _ when contentSeen => DiagnosticCodes.LateNumericDirective,
                    _ => null
                };
                if (code is not null)
                {
                    diagnostics.Add(Diagnostic.Error(code, "Expected one 'numbers exact' preamble before domain, imports and declarations.", line.Location));

                    // Retain failed numeric assertions so a caller cannot bind or print the provisional tree as Legacy.
                    options = new((NumericMode)(-1));
                }
                else
                {
                    options = SourceOptions.Exact;
                }

                seen = true;
                prepared.Add(line with { Content = string.Empty });
                continue;
            }

            // A directive-shaped line below the top level is never a valid option, whichever construct owns the block.
            if (!inFence && line.Indent > 0 && NestedDirectiveRegex().IsMatch(line.Content))
            {
                nested[line.Number] = Diagnostic.Error(DiagnosticCodes.InvalidNumericDirective, "Expected 'numbers exact' only as the document preamble, not inside a body.", line.Location);
            }

            contentSeen |= !line.IsBlank;
            prepared.Add(line);
        }

        // Do not reinterpret an unmarked document's BOM or other legacy source handling.
        var context = new ParserContext(new(seen ? prepared : lines), path, languages) { SourceOptions = options, StreamCandidates = streamCandidates };
        foreach (var diagnostic in diagnostics) context.Add(diagnostic);

        // Reported when reading ends, once every property owner has claimed the lines it models as fields.
        context.NestedNumericDirectives = nested;
        return context;
    }

    [GeneratedRegex(@"^numbers(\s+(exact|legacy))?$", RegexOptions.None, 1000)]
    private static partial Regex NestedDirectiveRegex();
}
