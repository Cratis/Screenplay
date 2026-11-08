// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses <c>storyline &lt;Name&gt; ["&lt;title&gt;"]</c> with its description and ordered <c>&lt;kind&gt; &lt;reference&gt; ["&lt;note&gt;"]</c> steps.
/// </summary>
internal static partial class StorylineParser
{
    /// <summary>
    /// The step keywords in source, each with the element kind it names.
    /// </summary>
    internal static readonly IReadOnlyDictionary<string, StorylineElementKind> StepKeywords = new Dictionary<string, StorylineElementKind>(StringComparer.Ordinal)
    {
        ["module"] = StorylineElementKind.Module,
        ["feature"] = StorylineElementKind.Feature,
        ["slice"] = StorylineElementKind.Slice,
        ["command"] = StorylineElementKind.Command,
        ["event"] = StorylineElementKind.Event,
        ["readmodel"] = StorylineElementKind.ReadModel,
        ["query"] = StorylineElementKind.Query,
        ["screen"] = StorylineElementKind.Screen,
        ["reaction"] = StorylineElementKind.Reaction
    };

    /// <summary>
    /// Gets the source keyword for an element kind.
    /// </summary>
    /// <param name="kind">The <see cref="StorylineElementKind"/>.</param>
    /// <returns>The keyword a step of that kind starts with.</returns>
    internal static string Keyword(StorylineElementKind kind) => StepKeywords.First(pair => pair.Value == kind).Key;

    internal static StorylineSyntax Parse(ParserContext context, SourceLine header)
    {
        var match = HeaderRegex().Match(header.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidStorylineDeclaration, $"Invalid storyline declaration '{header.Content}' - expected 'storyline <Name> [\"<title>\"]'", header.Location);
            context.SkipBlock(header.Indent);
            return new(string.Empty, null, [], header.Location);
        }

        var name = match.Groups[1].Value;
        var title = match.Groups[2].Success ? StringLiteral.Unescape(match.Groups[2].Value) : null;
        string? description = null;
        var locations = new Dictionary<string, SourceLocation>();
        var steps = new List<StorylineStepSyntax>();
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            if (LineText.FirstWord(line.Content) == "description")
            {
                var previous = description;
                description = DescriptionParser.Parse(context, line, description, $"Storyline '{name}'");
                if (previous is null && description is not null) locations["description"] = line.Location;
                continue;
            }

            if (Step(context, line) is { } step) steps.Add(step);
        }

        return new(name, title, steps, header.Location) { Description = description, DirectiveLocations = locations };
    }

    static StorylineStepSyntax? Step(ParserContext context, SourceLine line)
    {
        var match = StepRegex().Match(line.Content);
        if (!match.Success || !StepKeywords.TryGetValue(match.Groups[1].Value, out var kind))
        {
            context.Error(
                DiagnosticCodes.InvalidStorylineStep,
                $"Invalid storyline step '{line.Content}' - expected '<kind> <reference> [\"<note>\"]' where kind is {string.Join(", ", StepKeywords.Keys)}, or 'description \"<text>\"'",
                line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        // A step is one line: anything nested under it has no meaning, so it is reported instead of silently dropped.
        if (context.TryPeekChild(line.Indent, out var child))
        {
            context.Error(DiagnosticCodes.InvalidStorylineStep, $"A storyline step is one line; '{child.Content}' has no place under it", child.Location);
            context.SkipBlock(line.Indent);
        }

        var note = match.Groups[3].Success ? StringLiteral.Unescape(match.Groups[3].Value) : null;
        return new(kind, match.Groups[2].Value, note, line.Location);
    }

    [GeneratedRegex(@"^storyline\s+([A-Za-z_]\w*)(?:\s+""(" + StringLiteral.BodyPattern + @")"")?$", RegexOptions.NonBacktracking)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"^([a-z]+)\s+([A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)(?:\s+""(" + StringLiteral.BodyPattern + @")"")?$", RegexOptions.NonBacktracking)]
    private static partial Regex StepRegex();
}
