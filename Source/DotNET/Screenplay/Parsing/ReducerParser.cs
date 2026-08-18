// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses <c>reducer</c> declarations with their <c>on</c> rules.
/// </summary>
/// <remarks>
/// A reducer names what it builds with the same <c>=&gt;</c> arrow a projection uses, so the two read alike
/// and a reader follows one direction to find where a read model comes from.
/// </remarks>
internal static partial class ReducerParser
{
    /// <summary>
    /// Parses a reducer from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed <see cref="SourceLine"/> holding the <c>reducer</c> header.</param>
    /// <returns>The parsed <see cref="ReducerSyntax"/>.</returns>
    public static ReducerSyntax Parse(ParserContext context, SourceLine header)
    {
        var match = HeaderRegex().Match(header.Content);
        if (!match.Success)
        {
            context.Error(
                DiagnosticCodes.InvalidReducerDeclaration,
                $"Invalid reducer declaration '{header.Content}' - expected 'reducer <Name> => <ReadModel>'",
                header.Location);
        }

        var name = match.Groups[1].Value;
        var rules = new List<ReducerRuleSyntax>();
        string? description = null;

        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            if (LineText.FirstWord(line.Content) == "description")
            {
                description = DescriptionParser.Parse(context, line, description, $"Reducer '{name}'");
                continue;
            }

            var rule = OnRegex().Match(line.Content);
            if (!rule.Success)
            {
                context.Error(DiagnosticCodes.InvalidReducerRule, $"Expected 'on <EventType>' in reducer body, got '{line.Content}'", line.Location);
                context.SkipBlock(line.Indent);
                continue;
            }

            rules.Add(ParseRule(context, line, rule.Groups[1].Value));
        }

        if (rules.Count == 0)
        {
            context.Error(DiagnosticCodes.ReducerWithoutRule, $"Reducer '{name}' must declare at least one 'on <EventType>' rule", header.Location);
        }

        return new(name, match.Groups[2].Value, rules, header.Location, description);
    }

    static ReducerRuleSyntax ParseRule(ParserContext context, SourceLine line, string @event)
    {
        FileReferenceSyntax? file = null;
        CodeBlockSyntax? code = null;
        string? description = null;

        while (context.TryPeekChild(line.Indent, out var body))
        {
            context.Reader.TakeSignificant();
            if (LineText.FirstWord(body.Content) == "description")
            {
                description = DescriptionParser.Parse(context, body, description, $"Rule 'on {@event}'");
            }
            else if (LineText.FirstWord(body.Content) == "file")
            {
                file = new(body.Content["file".Length..].Trim(), body.Location);
            }
            else if (context.Languages.InlineLanguages.Contains(body.Content))
            {
                code = CodeBlockParser.Parse(context, body.Content, body);
            }
            else
            {
                context.Error(
                    DiagnosticCodes.UnknownReducerRuleDirective,
                    $"Unexpected '{body.Content}' in reducer rule - expected description, 'file <path>' or an inline code block",
                    body.Location);
                context.SkipBlock(body.Indent);
            }
        }

        return new(@event, file, code, line.Location, description);
    }

    [GeneratedRegex(@"^reducer\s+([A-Za-z_]\w*)\s*=>\s*([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"^on\s+([A-Z]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex OnRegex();
}
