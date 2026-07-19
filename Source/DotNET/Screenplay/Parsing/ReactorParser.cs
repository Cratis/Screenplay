// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses <c>reactor</c> declarations with their event triggers.
/// </summary>
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
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            var trigger = OnRegex().Match(line.Content);
            if (!trigger.Success)
            {
                context.Error($"Expected 'on <EventType>' in reactor body, got '{line.Content}'", line.Location);
                context.SkipBlock(line.Indent);
                continue;
            }

            var (file, code) = ParseBody(context, line);
            triggers.Add(new(trigger.Groups[1].Value, file, code, line.Location));
        }

        if (triggers.Count == 0)
        {
            context.Error($"Reactor '{name.Groups[1].Value}' must declare at least one 'on <EventType>' trigger", header.Location);
        }

        return new(name.Groups[1].Value, triggers, header.Location);
    }

    static (FileReferenceSyntax? File, CodeBlockSyntax? Code) ParseBody(ParserContext context, SourceLine trigger)
    {
        FileReferenceSyntax? file = null;
        CodeBlockSyntax? code = null;

        while (context.TryPeekChild(trigger.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            if (LineText.FirstWord(line.Content) == "file")
            {
                file = new(line.Content["file".Length..].Trim(), line.Location);
            }
            else if (CodeBlockParser.Languages.Contains(line.Content))
            {
                code = CodeBlockParser.Parse(context, line.Content, line);
            }
            else
            {
                context.Error($"Unexpected '{line.Content}' in reactor trigger - expected 'file <path>' or an inline code block", line.Location);
                context.SkipBlock(line.Indent);
            }
        }

        if (file is null && code is null)
        {
            context.Error("Reactor trigger must have a 'file' directive or an inline code block", trigger.Location);
        }

        return (file, code);
    }

    [GeneratedRegex(@"^reactor\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"^on\s+([A-Z]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex OnRegex();
}
