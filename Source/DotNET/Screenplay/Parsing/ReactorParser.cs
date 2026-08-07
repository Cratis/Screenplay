// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
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
            context.Error(DiagnosticCodes.InvalidReactorDeclaration, $"Invalid reactor declaration '{header.Content}' - expected 'reactor <Name>'", header.Location);
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
                context.Error(DiagnosticCodes.InvalidReactorTrigger, $"Expected 'on <EventType>' in reactor body, got '{line.Content}'", line.Location);
                context.SkipBlock(line.Indent);
                continue;
            }

            triggers.Add(ParseTrigger(context, line, trigger.Groups[1].Value));
        }

        if (triggers.Count == 0)
        {
            context.Error(DiagnosticCodes.ReactorWithoutTrigger, $"Reactor '{name.Groups[1].Value}' must declare at least one 'on <EventType>' trigger", header.Location);
        }

        return new(name.Groups[1].Value, triggers, header.Location, description);
    }

    static ReactorTriggerSyntax ParseTrigger(ParserContext context, SourceLine line, string @event)
    {
        FileReferenceSyntax? file = null;
        CodeBlockSyntax? code = null;
        string? description = null;
        var produces = new List<ProducesSyntax>();
        var invokes = new List<InvokesSyntax>();

        while (context.TryPeekChild(line.Indent, out var body))
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(body.Content))
            {
                case "description":
                    description = DescriptionParser.Parse(context, body, description, $"Trigger 'on {@event}'");
                    continue;
                case "file":
                    file = new(body.Content["file".Length..].Trim(), body.Location);
                    continue;
                case "produces":
                    if (ProducesParser.Parse(context, body) is { } produced)
                    {
                        produces.Add(produced);
                    }

                    continue;
                case "invokes":
                    if (ParseInvokes(context, body) is { } invoked)
                    {
                        invokes.Add(invoked);
                    }

                    continue;
            }

            if (context.Languages.InlineLanguages.Contains(body.Content))
            {
                code = CodeBlockParser.Parse(context, body.Content, body);
                continue;
            }

            context.Error(
                DiagnosticCodes.UnknownReactorTriggerDirective,
                $"Unexpected '{body.Content}' in reactor trigger - expected description, 'produces <EventType>', 'invokes <Command>', 'file <path>' or an inline code block",
                body.Location);
            context.SkipBlock(body.Indent);
        }

        return new(@event, file, code, line.Location, description, produces, invokes);
    }

    /// <summary>
    /// Parses an <c>invokes</c> declaration and the mappings that fill the command.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="line">The consumed <see cref="SourceLine"/> holding the <c>invokes</c> keyword.</param>
    /// <returns>The parsed <see cref="InvokesSyntax"/>, or <c>null</c> when the declaration is malformed.</returns>
    static InvokesSyntax? ParseInvokes(ParserContext context, SourceLine line)
    {
        var match = InvokesRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidInvokesDeclaration, $"Invalid invokes declaration '{line.Content}' - expected 'invokes <Command>'", line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        var mappings = new List<PropertyMappingSyntax>();
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            var mapping = MappingRegex().Match(child.Content);
            if (!mapping.Success)
            {
                context.Error(DiagnosticCodes.InvalidPropertyMapping, $"Invalid property mapping '{child.Content}' - expected '<property> = <source>'", child.Location);
                continue;
            }

            mappings.Add(new(LineText.Unescape(mapping.Groups[1].Value), ExpressionParser.ParseMappingSource(context, mapping.Groups[2].Value, child.Location), child.Location));
        }

        return new(match.Groups[1].Value, mappings, line.Location);
    }

    [GeneratedRegex(@"^reactor\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"^on\s+([A-Z]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex OnRegex();

    [GeneratedRegex(@"^invokes\s+([A-Z]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex InvokesRegex();

    [GeneratedRegex(@"^(@?[\w.]+)\s*=(?!=|>)\s*(.+)$", RegexOptions.None, 1000)]
    private static partial Regex MappingRegex();
}
