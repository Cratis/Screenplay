// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

internal static partial class PossibleEventsParser
{
    internal static PossibleEventsSyntax Parse(ParserContext context, SourceLine header)
    {
        var entries = new List<PossibleEventSyntax>();
        var names = new HashSet<string>(StringComparer.Ordinal);
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            var match = EntryRegex().Match(line.Content);
            if (!match.Success)
            {
                Invalid(context, line);
                continue;
            }

            if (!names.Add(match.Groups[1].Value))
            {
                context.Error(DiagnosticCodes.InvalidPossibleEventsDeclaration, $"Possible event '{match.Groups[1].Value}' is declared more than once; one destination rule is allowed per event type.", line.Location);
                context.SkipBlock(line.Indent);
                continue;
            }
            var (stream, tags) = ParseMetadata(context, line);
            entries.Add(new(match.Groups[1].Value, match.Groups[3].Success ? match.Groups[3].Value : null, match.Groups[2].Success, stream, tags, line.Location));
        }

        return new(entries, header.Location);
    }

    static (CommandStreamSyntax? Stream, List<TagSyntax> Tags) ParseMetadata(ParserContext context, SourceLine line)
    {
        CommandStreamSyntax? stream = null;
        var tags = new List<TagSyntax>();
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(child.Content))
            {
                case "stream": stream = ProducesParser.ParseRoute(context, child, stream, true); break;
                case "tag":
                    if (TagParser.Parse(context, child) is { } tag) tags.Add(tag);
                    break;
                default: Invalid(context, child); break;
            }
        }
        return (stream, tags);
    }

    static void Invalid(ParserContext context, SourceLine line)
    {
        context.Error(DiagnosticCodes.InvalidPossibleEventsDeclaration, "Expected an event name, optionally followed by 'for <property>' or 'for one of <collection property>'; entries accept only stream and tag children.", line.Location);
        context.SkipBlock(line.Indent);
    }

    [GeneratedRegex(@"^([A-Z]\w*(?:\.[A-Za-z_]\w*)*)(?:\s+for\s+(?:(one\s+of)\s+)?([A-Za-z_]\w*))?$", RegexOptions.NonBacktracking)]
    private static partial Regex EntryRegex();
}
