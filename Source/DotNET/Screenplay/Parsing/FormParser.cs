// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses <c>form</c> declarations - command-bound input surfaces declared at module level.
/// </summary>
internal static partial class FormParser
{
    /// <summary>
    /// Parses a form from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed <see cref="SourceLine"/> holding the <c>form</c> header.</param>
    /// <returns>The parsed <see cref="FormSyntax"/>.</returns>
    public static FormSyntax Parse(ParserContext context, SourceLine header)
    {
        var match = HeaderRegex().Match(header.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidFormDeclaration, $"Invalid form declaration '{header.Content}' - expected 'form <Name> for <Command>'", header.Location);
            context.SkipBlock(header.Indent);
            return new(LineText.FirstWord(header.Content), string.Empty, null, [], null, header.Location);
        }

        var name = match.Groups[1].Value;
        var forCommand = match.Groups[2].Value;
        FormPopulateSource? populate = null;
        var fields = new List<FormFieldSyntax>();
        var behaviors = new List<BehaviorSyntax>();
        var usedBehaviors = new List<UsesBehaviorSyntax>();
        ScreenNavigateSyntax? onSubmit = null;
        var columns = new List<FormColumnSyntax>();
        var columnMode = FormColumnMode.Unspecified;
        var generationMode = FormGenerationMode.Unspecified;
        CommandFormLayoutSyntax? layout = null;
        var hasPopulate = false;
        var hasSubmit = false;
        string? description = null;
        var locations = new Dictionary<string, SourceLocation>();

        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            if (DescriptionParser.TryParse(context, line, ref description, $"Form '{name}'", locations)) continue;
            switch (LineText.FirstWord(line.Content))
            {
                case "populate":
                    if (hasPopulate)
                    {
                        context.Error(DiagnosticCodes.DuplicatePopulate, $"Form '{name}' already declares 'populate' - at most one is allowed", line.Location);
                        break;
                    }

                    hasPopulate = true;
                    populate = ParsePopulate(context, line);
                    break;
                case "field":
                    if (ParseField(context, line) is { } field)
                    {
                        fields.Add(field);
                    }

                    break;
                case "generation":
                    generationMode = ParseGeneration(context, line) ?? generationMode;
                    break;
                case "layout":
                    layout = ParseLayout(context, line);
                    break;
                case "columns":
                    if (ParseColumns(context, line, columns) is { } mode)
                    {
                        columnMode = mode;
                    }

                    break;
                case "on":
                    // 'on submit navigate to <Screen>' is the existing one line sugar and keeps its meaning.
                    // Any other 'on' is an interaction binding attached to the form - including a multi line
                    // 'on submit' that runs actions rather than only navigating.
                    if (!SubmitNavigationRegex().IsMatch(line.Content))
                    {
                        InteractionParser.ParseAttachment(context, line, behaviors, usedBehaviors);
                        break;
                    }

                    if (hasSubmit)
                    {
                        context.Error(DiagnosticCodes.DuplicateFormSubmit, $"Form '{name}' already declares 'on submit' - at most one is allowed", line.Location);
                        break;
                    }

                    hasSubmit = true;
                    onSubmit = ParseSubmit(context, line);
                    break;
                case "uses":
                    InteractionParser.ParseAttachment(context, line, behaviors, usedBehaviors);
                    break;
                default:
                    context.Error(DiagnosticCodes.UnknownFormDirective, $"Unexpected '{LineText.FirstWord(line.Content)}' in form body - expected populate, field, 'on submit', 'on <trigger>' or 'uses <Behavior>'", line.Location);
                    context.SkipBlock(line.Indent);
                    break;
            }
        }

        return new(name, forCommand, populate, fields, onSubmit, header.Location)
        {
            Behaviors = behaviors,
            UsedBehaviors = usedBehaviors,
            Description = description,
            DirectiveLocations = locations,
            ColumnMode = columnMode,
            GenerationMode = generationMode,
            Columns = columns,
            Layout = layout
        };
    }

    static FormPopulateSource? ParsePopulate(ParserContext context, SourceLine line)
    {
        var viaQuery = PopulateViaQueryRegex().Match(line.Content);
        if (viaQuery.Success)
        {
            return new FormPopulateViaQuerySyntax(viaQuery.Groups[1].Value, viaQuery.Groups[2].Success ? viaQuery.Groups[2].Value : null, line.Location);
        }

        if (PopulateFromItemRegex().IsMatch(line.Content))
        {
            return new FormPopulateFromItemSyntax(line.Location);
        }

        context.Error(DiagnosticCodes.InvalidPopulateDeclaration, $"Invalid populate declaration '{line.Content}' - expected 'populate via query <Query> [by <param>]' or 'populate from item'", line.Location);
        return null;
    }

    static FormFieldSyntax? ParseField(ParserContext context, SourceLine line)
    {
        var match = FieldRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(
                DiagnosticCodes.InvalidFormField,
                $"Invalid field declaration '{line.Content}' - expected 'field <property> [from <source>|compose using <Callback>] [label \"...\"]'",
                line.Location);
            return null;
        }

        var from = match.Groups[2].Success ? match.Groups[2].Value : null;
        var composeUsing = match.Groups[3].Success ? match.Groups[3].Value : null;
        var label = match.Groups[4].Success || match.Groups[5].Success ? OperandText(match, 4) : null;
        return new(match.Groups[1].Value, label, from, composeUsing, line.Location);
    }

    static FormGenerationMode? ParseGeneration(ParserContext context, SourceLine line)
    {
        var match = GenerationRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.UnknownFormDirective, $"Invalid generation declaration '{line.Content}' - expected 'generation auto' or 'generation manual'", line.Location);
            return null;
        }

        return match.Groups[1].Value == "auto" ? FormGenerationMode.Auto : FormGenerationMode.Manual;
    }

    static CommandFormLayoutSyntax ParseLayout(ParserContext context, SourceLine line)
    {
        var columns = new List<FormLayoutColumnSyntax>();
        var placements = new List<FormFieldPlacementSyntax>();
        FormWidthSyntax? columnGap = null;
        FormWidthSyntax? rowGap = null;
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            var first = LineText.FirstWord(child.Content);
            if (first == "column")
            {
                if (ParseLayoutColumn(context, child) is { } column) columns.Add(column);
            }
            else if (first == "place")
            {
                if (ParsePlacement(context, child) is { } placement) placements.Add(placement);
            }
            else if (first == "columnGap")
            {
                columnGap = ParseWidthDirective(context, child, "columnGap");
            }
            else if (first == "rowGap")
            {
                rowGap = ParseWidthDirective(context, child, "rowGap");
            }
            else
            {
                context.Error(DiagnosticCodes.UnknownFormDirective, $"Unexpected '{child.Content}' in form layout - expected column, place, columnGap or rowGap", child.Location);
            }
        }

        return new(columns, placements, columnGap, rowGap, line.Location);
    }

    static FormLayoutColumnSyntax? ParseLayoutColumn(ParserContext context, SourceLine line)
    {
        var tokens = line.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length < 2 || !int.TryParse(tokens[1], NumberStyles.None, CultureInfo.InvariantCulture, out var index) || index < 1)
        {
            context.Error(DiagnosticCodes.UnknownFormDirective, $"Invalid form layout column '{line.Content}' - expected 'column <index> [width <width>] [min <width>] [max <width>]'", line.Location);
            return null;
        }

        FormWidthSyntax? width = null;
        FormWidthSyntax? minWidth = null;
        FormWidthSyntax? maxWidth = null;
        for (var position = 2; position < tokens.Length; position += 2)
        {
            if (position + 1 >= tokens.Length)
            {
                context.Error(DiagnosticCodes.UnknownFormDirective, $"Invalid form layout column '{line.Content}' - expected width, min and max values to have a width", line.Location);
                return null;
            }

            var parsed = ParseWidth(context, tokens[position + 1], line.Location);
            switch (tokens[position])
            {
                case "width": width = parsed; break;
                case "min": minWidth = parsed; break;
                case "max": maxWidth = parsed; break;
                default:
                    context.Error(DiagnosticCodes.UnknownFormDirective, $"Invalid form layout column option '{tokens[position]}' - expected width, min or max", line.Location);
                    return null;
            }
        }

        return new(index, width, minWidth, maxWidth, line.Location);
    }

    static FormFieldPlacementSyntax? ParsePlacement(ParserContext context, SourceLine line)
    {
        var tokens = line.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length < 6 || tokens[2] != "row" || tokens[4] != "column" ||
            !int.TryParse(tokens[3], NumberStyles.None, CultureInfo.InvariantCulture, out var row) ||
            !int.TryParse(tokens[5], NumberStyles.None, CultureInfo.InvariantCulture, out var column) ||
            row < 1 || column < 1)
        {
            context.Error(DiagnosticCodes.UnknownFormDirective, $"Invalid form field placement '{line.Content}' - expected 'place <field> row <row> column <column> [rowSpan <n>] [columnSpan <n>] [width <width>]'", line.Location);
            return null;
        }

        int? rowSpan = null;
        int? columnSpan = null;
        FormWidthSyntax? width = null;
        for (var position = 6; position < tokens.Length; position += 2)
        {
            if (position + 1 >= tokens.Length)
            {
                context.Error(DiagnosticCodes.UnknownFormDirective, $"Invalid form field placement '{line.Content}' - expected placement options to have values", line.Location);
                return null;
            }

            switch (tokens[position])
            {
                case "rowSpan" when int.TryParse(tokens[position + 1], NumberStyles.None, CultureInfo.InvariantCulture, out var span) && span > 0:
                    rowSpan = span;
                    break;
                case "columnSpan" when int.TryParse(tokens[position + 1], NumberStyles.None, CultureInfo.InvariantCulture, out var span) && span > 0:
                    columnSpan = span;
                    break;
                case "width":
                    width = ParseWidth(context, tokens[position + 1], line.Location);
                    break;
                default:
                    context.Error(DiagnosticCodes.UnknownFormDirective, $"Invalid form field placement option '{tokens[position]}' - expected rowSpan, columnSpan or width", line.Location);
                    return null;
            }
        }

        return new(tokens[1], row, column, rowSpan, columnSpan, width, line.Location);
    }

    static FormWidthSyntax? ParseWidthDirective(ParserContext context, SourceLine line, string keyword)
    {
        var tokens = line.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length != 2)
        {
            context.Error(DiagnosticCodes.UnknownFormDirective, $"Invalid form layout {keyword} '{line.Content}' - expected '{keyword} <width>'", line.Location);
            return null;
        }

        return ParseWidth(context, tokens[1], line.Location);
    }

    static FormWidthSyntax? ParseWidth(ParserContext context, string text, SourceLocation location)
    {
        if (text == "auto") return new(FormWidthUnitSyntax.Auto, null, location);
        if (text.EndsWith("fr", StringComparison.Ordinal) && double.TryParse(text[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var fraction)) return new(FormWidthUnitSyntax.Fraction, fraction, location);
        if (text.EndsWith("px", StringComparison.Ordinal) && double.TryParse(text[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var pixels)) return new(FormWidthUnitSyntax.Pixels, pixels, location);
        if (text.EndsWith('%') && double.TryParse(text[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var percent)) return new(FormWidthUnitSyntax.Percent, percent, location);
        context.Error(DiagnosticCodes.UnknownFormDirective, $"Invalid form width '{text}' - expected auto, <number>fr, <number>px or <number>%", location);
        return null;
    }

    static FormColumnMode? ParseColumns(ParserContext context, SourceLine line, List<FormColumnSyntax> columns)
    {
        if (ColumnsAutoRegex().IsMatch(line.Content))
        {
            if (context.TryPeekChild(line.Indent, out _))
            {
                context.Error(DiagnosticCodes.UnknownFormDirective, "'columns auto' cannot have a body - use 'columns manual' when authoring columns", line.Location);
                context.SkipBlock(line.Indent);
            }

            return FormColumnMode.Auto;
        }

        if (!ColumnsManualRegex().IsMatch(line.Content))
        {
            context.Error(DiagnosticCodes.UnknownFormDirective, $"Invalid columns declaration '{line.Content}' - expected 'columns auto' or 'columns manual'", line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            var column = ColumnRegex().Match(child.Content);
            if (!column.Success)
            {
                context.Error(DiagnosticCodes.UnknownFormDirective, $"Invalid form column '{child.Content}' - expected 'column <property> [label \"...\"]'", child.Location);
                continue;
            }

            columns.Add(new(column.Groups[1].Value, column.Groups[2].Success || column.Groups[3].Success ? OperandText(column, 2) : null, child.Location));
        }

        return FormColumnMode.Manual;
    }

    static ScreenNavigateSyntax? ParseSubmit(ParserContext context, SourceLine line)
    {
        var match = SubmitRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidFormSubmit, $"Invalid submit declaration '{line.Content}' - expected 'on submit navigate to <Screen> [by <param>]'", line.Location);
            return null;
        }

        var navigate = NavigateRegex().Match(match.Groups[1].Value);
        if (!navigate.Success)
        {
            context.Error(DiagnosticCodes.InvalidFormSubmit, $"Invalid submit declaration '{line.Content}' - expected 'on submit navigate to <Screen> [by <param>]'", line.Location);
            return null;
        }

        return new(navigate.Groups[1].Value, navigate.Groups[2].Success ? navigate.Groups[2].Value : null, line.Location);
    }

    static string OperandText(Match match, int quotedGroup) =>
        match.Groups[quotedGroup].Success ? StringLiteral.Unescape(match.Groups[quotedGroup].Value) : match.Groups[quotedGroup + 1].Value;

    [GeneratedRegex(@"^form\s+([A-Za-z_]\w*)\s+for\s+([A-Za-z_]\w*(?:\.\w+)*)$", RegexOptions.None, 1000)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"^generation\s+(auto|manual)$", RegexOptions.None, 1000)]
    private static partial Regex GenerationRegex();

    [GeneratedRegex(@"^on\s+submit\s+navigate\b", RegexOptions.None, 1000)]
    private static partial Regex SubmitNavigationRegex();

    [GeneratedRegex(@"^populate\s+via\s+query\s+(\w+(?:\.\w+)*)(?:\s+by\s+(\w+))?$", RegexOptions.None, 1000)]
    private static partial Regex PopulateViaQueryRegex();

    [GeneratedRegex(@"^populate\s+from\s+item$", RegexOptions.None, 1000)]
    private static partial Regex PopulateFromItemRegex();

    [GeneratedRegex(
        "^field\\s+([\\w.]+)(?:\\s+(?:from\\s+([\\w.]+)|compose\\s+using\\s+([A-Za-z_]\\w*)))?(?:\\s+label\\s+(?:\"(" + StringLiteral.BodyPattern + ")\"|(\\$strings\\.\\w+(?:\\.\\w+)*)))?$",
        RegexOptions.None,
        1000)]
    private static partial Regex FieldRegex();

    [GeneratedRegex(@"^on\s+submit\s+(navigate\s+to\s+.+)$", RegexOptions.None, 1000)]
    private static partial Regex SubmitRegex();

    [GeneratedRegex(@"^columns\s+auto$", RegexOptions.None, 1000)]
    private static partial Regex ColumnsAutoRegex();

    [GeneratedRegex(@"^columns\s+manual$", RegexOptions.None, 1000)]
    private static partial Regex ColumnsManualRegex();

    [GeneratedRegex("^column\\s+([\\w.]+)(?:\\s+label\\s+(?:\"(" + StringLiteral.BodyPattern + ")\"|(\\$strings\\.\\w+(?:\\.\\w+)*)))?$", RegexOptions.None, 1000)]
    private static partial Regex ColumnRegex();

    [GeneratedRegex(@"^navigate\s+to\s+(\w+(?:\.\w+)*)(?:\s+by\s+(\w+))?$", RegexOptions.None, 1000)]
    private static partial Regex NavigateRegex();
}
