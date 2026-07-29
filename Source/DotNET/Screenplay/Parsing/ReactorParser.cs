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

            var body = ParseBody(context, line);
            triggers.Add(new(trigger.Groups[1].Value, body.File, body.Code, line.Location, body.Produces, body.Executes));
        }

        if (triggers.Count == 0)
        {
            context.Error($"Reactor '{name.Groups[1].Value}' must declare at least one 'on <EventType>' trigger", header.Location);
        }

        return new(name.Groups[1].Value, triggers, header.Location);
    }

    /// <summary>
    /// Parses the body of an <c>on</c> trigger.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="trigger">The <see cref="SourceLine"/> holding the trigger.</param>
    /// <returns>The realization the trigger declares, if any.</returns>
    /// <remarks>
    /// A trigger with no body is valid - <c>on InvitationAccepted</c> states the intent that this reactor
    /// observes the event, which a document written before any code exists needs to be able to say. The
    /// <c>file</c> reference and the inline block are realization metadata, not the source of meaning.
    /// </remarks>
    static TriggerBody ParseBody(ParserContext context, SourceLine trigger)
    {
        var body = new TriggerBody();

        while (context.TryPeekChild(trigger.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(line.Content))
            {
                case "file":
                    body.File = new(line.Content["file".Length..].Trim(), line.Location);
                    break;
                case "produces":
                    AddProduces(context, body, line);
                    break;
                case "executes":
                    AddExecutes(context, body, line);
                    break;
                default:
                    if (CodeBlockParser.Languages.Contains(line.Content))
                    {
                        body.Code = CodeBlockParser.Parse(context, line.Content, line);
                        break;
                    }

                    context.Error(
                        $"Unexpected '{line.Content}' in reactor trigger - expected 'produces <EventType>', 'executes <Command>', 'file <path>' or an inline code block",
                        line.Location);
                    context.SkipBlock(line.Indent);
                    break;
            }
        }

        return body;
    }

    static void AddProduces(ParserContext context, TriggerBody body, SourceLine line)
    {
        var match = ProducesRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error($"Invalid produces declaration '{line.Content}' - expected 'produces <EventType>'", line.Location);
            context.SkipBlock(line.Indent);
            return;
        }

        body.Produces.Add(new(match.Groups[1].Value, line.Location));
    }

    static void AddExecutes(ParserContext context, TriggerBody body, SourceLine line)
    {
        var match = ExecutesRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error($"Invalid executes declaration '{line.Content}' - expected 'executes <Command>'", line.Location);
            context.SkipBlock(line.Indent);
            return;
        }

        body.Executes.Add(new(match.Groups[1].Value, line.Location));
    }

    [GeneratedRegex(@"^reactor\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"^on\s+([A-Z]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex OnRegex();

    [GeneratedRegex(@"^produces\s+([A-Z]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex ProducesRegex();

    [GeneratedRegex(@"^executes\s+([A-Z]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex ExecutesRegex();

    sealed class TriggerBody
    {
        public FileReferenceSyntax? File { get; set; }

        public CodeBlockSyntax? Code { get; set; }

        public List<ReactorProducesSyntax> Produces { get; } = [];

        public List<ReactorExecutesSyntax> Executes { get; } = [];
    }
}
