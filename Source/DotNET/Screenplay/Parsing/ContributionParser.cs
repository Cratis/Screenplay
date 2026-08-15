// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses <c>contribute to</c> declarations - one item contributed into a named contribution point.
/// </summary>
internal static partial class ContributionParser
{
    /// <summary>
    /// Parses a contribution from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed <see cref="SourceLine"/> holding the <c>contribute to</c> header.</param>
    /// <returns>The parsed <see cref="ContributionSyntax"/>.</returns>
    public static ContributionSyntax Parse(ParserContext context, SourceLine header)
    {
        var match = HeaderRegex().Match(header.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidContributionDeclaration, $"Invalid contribute declaration '{header.Content}' - expected 'contribute to <ContributionPoint>'", header.Location);
            context.SkipBlock(header.Indent);
            return new(LineText.FirstWord(header.Content), null, null, null, header.Location);
        }

        var contributionPoint = match.Groups[1].Value;
        ScreenNavigateSyntax? navigate = null;
        string? label = null;
        int? order = null;
        var hasNavigate = false;
        var hasLabel = false;
        var hasOrder = false;

        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(line.Content))
            {
                case "navigate":
                    if (hasNavigate)
                    {
                        context.Error(DiagnosticCodes.DuplicateContributionNavigate, "This contribution already declares 'navigate to' - at most one is allowed", line.Location);
                        break;
                    }

                    hasNavigate = true;
                    navigate = ParseNavigate(context, line);
                    break;
                case "label":
                    if (hasLabel)
                    {
                        context.Error(DiagnosticCodes.DuplicateContributionLabel, "This contribution already declares 'label' - at most one is allowed", line.Location);
                        break;
                    }

                    hasLabel = true;
                    label = ParseLabel(context, line);
                    break;
                case "order":
                    if (hasOrder)
                    {
                        context.Error(DiagnosticCodes.DuplicateContributionOrder, "This contribution already declares 'order' - at most one is allowed", line.Location);
                        break;
                    }

                    hasOrder = true;
                    order = ParseOrder(context, line);
                    break;
                default:
                    context.Error(DiagnosticCodes.UnknownContributionDirective, $"Unexpected '{LineText.FirstWord(line.Content)}' in contribution body - expected navigate, label or order", line.Location);
                    context.SkipBlock(line.Indent);
                    break;
            }
        }

        return new(contributionPoint, navigate, label, order, header.Location);
    }

    static ScreenNavigateSyntax? ParseNavigate(ParserContext context, SourceLine line)
    {
        var match = NavigateRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidNavigation, $"Invalid navigation '{line.Content}' - expected 'navigate to <Screen> [by <param>]'", line.Location);
            return null;
        }

        return new(match.Groups[1].Value, match.Groups[2].Success ? match.Groups[2].Value : null, line.Location);
    }

    static string? ParseLabel(ParserContext context, SourceLine line)
    {
        var match = LabelRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidContributionLabel, $"Invalid label declaration '{line.Content}' - expected 'label \"...\"' or 'label $strings....'", line.Location);
            return null;
        }

        return match.Groups[1].Success ? StringLiteral.Unescape(match.Groups[1].Value) : match.Groups[2].Value;
    }

    static int? ParseOrder(ParserContext context, SourceLine line)
    {
        var match = OrderRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidOrderDeclaration, $"Invalid order declaration '{line.Content}' - expected 'order <number>'", line.Location);
            return null;
        }

        return int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
    }

    [GeneratedRegex(@"^contribute\s+to\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"^navigate\s+to\s+(\w+(?:\.\w+)*)(?:\s+by\s+(\w+))?$", RegexOptions.None, 1000)]
    private static partial Regex NavigateRegex();

    [GeneratedRegex("^label\\s+(?:\"(" + StringLiteral.BodyPattern + ")\"|(\\$strings\\.\\w+(?:\\.\\w+)*))$", RegexOptions.None, 1000)]
    private static partial Regex LabelRegex();

    [GeneratedRegex(@"^order\s+(\d+)$", RegexOptions.None, 1000)]
    private static partial Regex OrderRegex();
}
