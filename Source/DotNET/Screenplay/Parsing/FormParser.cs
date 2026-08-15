// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        ScreenNavigateSyntax? onSubmit = null;
        var hasPopulate = false;
        var hasSubmit = false;

        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
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
                case "on":
                    if (hasSubmit)
                    {
                        context.Error(DiagnosticCodes.DuplicateFormSubmit, $"Form '{name}' already declares 'on submit' - at most one is allowed", line.Location);
                        break;
                    }

                    hasSubmit = true;
                    onSubmit = ParseSubmit(context, line);
                    break;
                default:
                    context.Error(DiagnosticCodes.UnknownFormDirective, $"Unexpected '{LineText.FirstWord(line.Content)}' in form body - expected populate, field or on submit", line.Location);
                    context.SkipBlock(line.Indent);
                    break;
            }
        }

        return new(name, forCommand, populate, fields, onSubmit, header.Location);
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

    [GeneratedRegex(@"^navigate\s+to\s+(\w+(?:\.\w+)*)(?:\s+by\s+(\w+))?$", RegexOptions.None, 1000)]
    private static partial Regex NavigateRegex();
}
