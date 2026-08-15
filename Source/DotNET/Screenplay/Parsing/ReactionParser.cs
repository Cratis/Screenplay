// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses <c>reaction</c> declarations with the triggers that set them off.
/// </summary>
/// <remarks>
/// A trigger states intent on its own - a clause with no body says the reaction runs when that happens. A
/// <c>file</c> reference or an inline code block is realization metadata a slice gains once it is
/// implemented, never something the author must invent to make the document parse.
/// </remarks>
internal static partial class ReactionParser
{
    /// <summary>
    /// Parses a reaction from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed <see cref="SourceLine"/> holding the <c>reaction</c> header.</param>
    /// <returns>The parsed <see cref="ReactionSyntax"/>.</returns>
    public static ReactionSyntax Parse(ParserContext context, SourceLine header)
    {
        var match = HeaderRegex().Match(header.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidReactionDeclaration, $"Invalid reaction declaration '{header.Content}' - expected 'reaction <Name>'", header.Location);
        }

        var name = match.Groups[1].Value;
        var triggers = new List<ReactionTriggerSyntax>();
        string? description = null;
        ConditionSyntax? where = null;

        // Whether the body already told the author something is wrong. A reaction whose only trigger is
        // misspelled has no trigger, but saying so as well turns one mistake into two diagnostics and points
        // the second at the header rather than at the line to fix.
        var reported = false;

        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            var keyword = LineText.FirstWord(line.Content);
            if (keyword == "description")
            {
                description = DescriptionParser.Parse(context, line, description, $"Reaction '{name}'");
                continue;
            }

            if (keyword == "where")
            {
                where = ParseWhere(context, line, where, name);
                continue;
            }

            if (!TriggerParser.ClauseKeywords.Contains(keyword))
            {
                context.Error(
                    DiagnosticCodes.InvalidReactionTrigger,
                    $"Expected a trigger in reaction body, got '{line.Content}' - a reaction is set off by 'when <Name>', 'every <n> <unit>' or 'at <HH:mm>'",
                    line.Location);
                context.SkipBlock(line.Indent);
                reported = true;
                continue;
            }

            if (TriggerParser.ParseSource(context, line) is not { } source)
            {
                reported = true;
                continue;
            }

            if (triggers.Exists(existing => SameSource(existing.Source, source)))
            {
                context.Error(
                    DiagnosticCodes.DuplicateReactionTrigger,
                    $"Reaction '{name}' already declares '{line.Content}' - a second says nothing the first did not",
                    line.Location);
                context.SkipBlock(line.Indent);
                continue;
            }

            triggers.Add(ParseTrigger(context, line, source));
        }

        if (triggers.Count == 0 && !reported)
        {
            context.Error(DiagnosticCodes.ReactionWithoutTrigger, $"Reaction '{name}' must declare at least one trigger - nothing sets it off", header.Location);
        }

        return new(name, triggers, header.Location, description, where);
    }

    // Two triggers are the same when they name the same occurrence, wherever in the file they were written -
    // the records carry their source location, so plain equality would call two identical clauses different.
    static bool SameSource(TriggerSourceSyntax left, TriggerSourceSyntax right) => (left, right) switch
    {
        (NamedTriggerSourceSyntax first, NamedTriggerSourceSyntax second) => first.Name == second.Name,
        (IntervalTriggerSourceSyntax first, IntervalTriggerSourceSyntax second) => first.Amount == second.Amount && first.Unit == second.Unit,
        (ScheduleTriggerSourceSyntax first, ScheduleTriggerSourceSyntax second) =>
            first.Time == second.Time && first.DayOfWeek == second.DayOfWeek && first.DayOfMonth == second.DayOfMonth,
        _ => false
    };

    static ConditionSyntax? ParseWhere(ParserContext context, SourceLine line, ConditionSyntax? existing, string name)
    {
        var match = WhereRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.ExpectedCondition, $"Invalid condition '{line.Content}' - expected 'where <condition>'", line.Location);
            return existing;
        }

        if (existing is not null)
        {
            context.Error(
                DiagnosticCodes.DuplicateReactionCondition,
                $"Reaction '{name}' already declares a condition - write one condition, using 'and' or 'or' to combine",
                line.Location);
            return existing;
        }

        return ConditionParser.Parse(context, match.Groups[1].Value, line.Location) ?? existing;
    }

    static ReactionTriggerSyntax ParseTrigger(ParserContext context, SourceLine line, TriggerSourceSyntax source)
    {
        FileReferenceSyntax? file = null;
        CodeBlockSyntax? code = null;
        string? description = null;
        var data = new List<TriggerDataSyntax>();
        var produces = new List<ProducesSyntax>();
        var invokes = new List<InvokesSyntax>();

        while (context.TryPeekChild(line.Indent, out var body))
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(body.Content))
            {
                case "description":
                    description = DescriptionParser.Parse(context, body, description, $"Trigger '{body.Content}'");
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

            // Anything left with the shape of a name is a value the reaction takes from the occurrence. The
            // directives above are checked first, so a trigger value named after one is written '@file' and
            // the escape is undone on the way in, the same as every other block in the language.
            if (TriggerParser.ParseData(context, body) is { } datum)
            {
                data.Add(datum);
            }
        }

        return new(source, data, file, code, line.Location, description, produces, invokes);
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

    [GeneratedRegex(@"^reaction\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"^where\s+(\S.*)$", RegexOptions.None, 1000)]
    private static partial Regex WhereRegex();

    [GeneratedRegex(@"^invokes\s+([A-Z]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex InvokesRegex();

    [GeneratedRegex(@"^(@?[\w.]+)\s*=(?!=|>)\s*(.+)$", RegexOptions.None, 1000)]
    private static partial Regex MappingRegex();
}
