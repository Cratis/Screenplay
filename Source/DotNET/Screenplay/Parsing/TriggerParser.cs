// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses <c>trigger</c> declarations, and the trigger clauses a reaction opens with.
/// </summary>
/// <remarks>
/// The built-in clock triggers get their own words rather than a name and arguments. <c>every 5 minutes</c>
/// and <c>at 08:00</c> are how the schedule is said out loud, and the language spells the common ones the
/// way a reader would say them. Everything else is a name - an event, a declared trigger, or one a consumer
/// registered - so the open set needs no new syntax to grow.
/// </remarks>
internal static partial class TriggerParser
{
    /// <summary>
    /// The words a trigger clause opens with, so a reaction body can tell a trigger from anything else.
    /// </summary>
    public static readonly IReadOnlySet<string> ClauseKeywords =
        new HashSet<string>(StringComparer.Ordinal) { "when", "every", "at" };

    /// <summary>
    /// Parses a trigger declaration from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed <see cref="SourceLine"/> holding the <c>trigger</c> header.</param>
    /// <returns>The parsed <see cref="TriggerSyntax"/>.</returns>
    public static TriggerSyntax Parse(ParserContext context, SourceLine header)
    {
        var match = HeaderRegex().Match(header.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidTriggerDeclaration, $"Invalid trigger declaration '{header.Content}' - expected 'trigger <Name>'", header.Location);
            context.SkipBlock(header.Indent);
            return new(LineText.FirstWord(header.Content), [], header.Location);
        }

        var name = match.Groups[1].Value;
        string? description = null;
        var data = new List<TriggerDataSyntax>();
        FileReferenceSyntax? file = null;

        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            if (LineText.FirstWord(line.Content) == "description")
            {
                description = DescriptionParser.Parse(context, line, description, $"Trigger '{name}'");
                continue;
            }

            // The directive wins over a value of the same name, exactly as it does in the trigger clause of a
            // reaction - a value named after it is written '@file', which is what the printer has always emitted.
            if (FileReferenceParser.IsDirective(line))
            {
                file = FileReferenceParser.Parse(context, line);
                continue;
            }

            if (ParseData(context, line) is { } datum)
            {
                data.Add(datum);
            }
        }

        return new(name, data, header.Location, description) { File = file };
    }

    /// <summary>
    /// Parses one value a trigger provides, or one a reaction takes from an occurrence.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <param name="line">The consumed <see cref="SourceLine"/> holding the value.</param>
    /// <returns>The parsed <see cref="TriggerDataSyntax"/>, or <c>null</c> when the line is not one.</returns>
    /// <remarks>
    /// A bare name is a complete statement - it says the reaction is handed something by that name. The type
    /// is there for a declaration that has settled on one, and the language does not force it early.
    /// </remarks>
    public static TriggerDataSyntax? ParseData(ParserContext context, SourceLine line)
    {
        if (PropertyLineParser.TryParse(line) is { } property)
        {
            return new(property.Name, property.Type, line.Location);
        }

        if (UntypedDataRegex().Match(line.Content) is { Success: true } untyped)
        {
            return new(LineText.Unescape(untyped.Groups[1].Value), null, line.Location);
        }

        context.Error(
            DiagnosticCodes.InvalidTriggerData,
            $"Invalid trigger value '{line.Content}' - expected '<name>' or '<name> <Type>'",
            line.Location);
        context.SkipBlock(line.Indent);
        return null;
    }

    /// <summary>
    /// Parses what sets a reaction off, from the line the trigger clause opens on.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <param name="line">The consumed <see cref="SourceLine"/> holding the clause.</param>
    /// <returns>The parsed <see cref="TriggerSourceSyntax"/>, or <c>null</c> when the clause is malformed.</returns>
    public static TriggerSourceSyntax? ParseSource(ParserContext context, SourceLine line) =>
        LineText.FirstWord(line.Content) switch
        {
            "when" => ParseNamed(context, line),
            "every" => ParseInterval(context, line),
            "at" => ParseSchedule(context, line),
            _ => null
        };

    static NamedTriggerSourceSyntax? ParseNamed(ParserContext context, SourceLine line)
    {
        var match = WhenRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidReactionTrigger, $"Invalid trigger '{line.Content}' - expected 'when <Name>'", line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        return new(match.Groups[1].Value, line.Location);
    }

    static IntervalTriggerSourceSyntax? ParseInterval(ParserContext context, SourceLine line)
    {
        var match = EveryRegex().Match(line.Content);
        if (!match.Success || !int.TryParse(match.Groups[1].Value, CultureInfo.InvariantCulture, out var amount) || amount < 1)
        {
            context.Error(
                DiagnosticCodes.InvalidIntervalTrigger,
                $"Invalid interval '{line.Content}' - expected 'every <n> <seconds|minutes|hours|days>'",
                line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        return new(amount, UnitOf(match.Groups[2].Value), line.Location);
    }

    static ScheduleTriggerSourceSyntax? ParseSchedule(ParserContext context, SourceLine line)
    {
        var match = AtRegex().Match(line.Content);
        if (!match.Success ||
            !TimeOnly.TryParseExact(match.Groups[1].Value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
        {
            context.Error(
                DiagnosticCodes.InvalidScheduleTrigger,
                $"Invalid schedule '{line.Content}' - expected 'at <HH:mm>', optionally followed by 'on <Weekday>' or 'on day <n>'",
                line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        if (match.Groups[3].Success)
        {
            var day = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
            if (day is < 1 or > 31)
            {
                context.Error(
                    DiagnosticCodes.InvalidScheduleTrigger,
                    $"Invalid day of month '{day}' in '{line.Content}' - a day of the month is between 1 and 31",
                    line.Location);
                context.SkipBlock(line.Indent);
                return null;
            }

            return new(time, null, day, line.Location);
        }

        if (match.Groups[2].Success)
        {
            return new(time, Enum.Parse<DayOfWeek>(match.Groups[2].Value), null, line.Location);
        }

        return new(time, null, null, line.Location);
    }

    static IntervalUnit UnitOf(string text) => text.TrimEnd('s') switch
    {
        "second" => IntervalUnit.Seconds,
        "minute" => IntervalUnit.Minutes,
        "hour" => IntervalUnit.Hours,
        _ => IntervalUnit.Days
    };

    [GeneratedRegex(@"^trigger\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"^(@?[a-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex UntypedDataRegex();

    [GeneratedRegex(@"^when\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex WhenRegex();

    [GeneratedRegex(@"^every\s+(\d+)\s+(seconds?|minutes?|hours?|days?)$", RegexOptions.None, 1000)]
    private static partial Regex EveryRegex();

    [GeneratedRegex(@"^at\s+(\d{2}:\d{2})(?:\s+on\s+(?:(Monday|Tuesday|Wednesday|Thursday|Friday|Saturday|Sunday)|day\s+(\d{1,2})))?$", RegexOptions.None, 1000)]
    private static partial Regex AtRegex();
}
