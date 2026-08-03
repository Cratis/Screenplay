// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses <c>reactor</c> declarations with their event triggers.
/// </summary>
/// <remarks>
/// A trigger states intent on its own - <c>on &lt;EventType&gt;</c> with no body says the reactor observes
/// that event. A <c>file</c> reference or an inline code block is realization metadata a slice gains once
/// it is implemented, never something the author must invent to make the document parse.
/// </remarks>
internal static partial class ReactorParser
{
    /// <summary>
    /// Parses a reactor from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed <see cref="SourceLine"/> holding the <c>reactor</c> header.</param>
    /// <returns>The parsed <see cref="ReactorSyntax"/>.</returns>
    public static ReactorSyntax Parse(ParserContext context, SourceLine header)
    {
        var name = HeaderRegex().Match(header.Content);
        if (!name.Success)
        {
            context.Error($"Invalid reactor declaration '{header.Content}' - expected 'reactor <Name>'", header.Location);
        }

        var triggers = new List<ReactorTriggerSyntax>();
        string? description = null;

        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            if (LineText.FirstWord(line.Content) == "description")
            {
                description = DescriptionParser.Parse(context, line, description, $"Reactor '{name.Groups[1].Value}'");
                continue;
            }

            var trigger = OnRegex().Match(line.Content);
            if (!trigger.Success)
            {
                context.Error($"Expected 'on <EventType>' in reactor body, got '{line.Content}'", line.Location);
                context.SkipBlock(line.Indent);
                continue;
            }

            triggers.Add(ParseTrigger(context, line, trigger.Groups[1].Value));
        }

        if (triggers.Count == 0)
        {
            context.Error($"Reactor '{name.Groups[1].Value}' must declare at least one 'on <EventType>' trigger", header.Location);
        }

        return new(name.Groups[1].Value, triggers, header.Location, description);
    }

    static ReactorTriggerSyntax ParseTrigger(ParserContext context, SourceLine line, string @event)
    {
        FileReferenceSyntax? file = null;
        CodeBlockSyntax? code = null;
        string? description = null;

        while (context.TryPeekChild(line.Indent, out var body))
        {
            context.Reader.TakeSignificant();
            if (LineText.FirstWord(body.Content) == "description")
            {
                description = DescriptionParser.Parse(context, body, description, $"Trigger 'on {@event}'");
            }
            else if (LineText.FirstWord(body.Content) == "file")
            {
                file = new(body.Content["file".Length..].Trim(), body.Location);
            }
            else if (CodeBlockParser.Languages.Contains(body.Content))
            {
                code = CodeBlockParser.Parse(context, body.Content, body);
            }
            else
            {
                context.Error($"Unexpected '{body.Content}' in reactor trigger - expected description, 'file <path>' or an inline code block", body.Location);
                context.SkipBlock(body.Indent);
            }
        }

        return new(@event, file, code, line.Location, description);
    }

    [GeneratedRegex(@"^reactor\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"^on\s+([A-Z]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex OnRegex();
}
