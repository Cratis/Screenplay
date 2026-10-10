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
        var item = new NavigationItem();
        var directiveLocations = new Dictionary<string, SourceLocation>();

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
                    navigate = ScreenParser.ParseNavigate(context, line.Content, line);
                    break;
                case "id" or "icon" or "presentation" or "group" or "destination":
                    ParseItemDirective(context, line, item);
                    break;
                case "label":
                    if (hasLabel)
                    {
                        context.Error(DiagnosticCodes.DuplicateContributionLabel, "This contribution already declares 'label' - at most one is allowed", line.Location);
                        break;
                    }

                    hasLabel = true;
                    label = ParseLabel(context, line);
                    if (label is not null)
                    {
                        directiveLocations["label"] = line.Location;
                    }
                    break;
                case "order":
                    if (hasOrder)
                    {
                        context.Error(DiagnosticCodes.DuplicateContributionOrder, "This contribution already declares 'order' - at most one is allowed", line.Location);
                        break;
                    }

                    hasOrder = true;
                    order = ParseOrder(context, line);
                    if (order is not null)
                    {
                        directiveLocations["order"] = line.Location;
                    }
                    break;
                default:
                    context.Error(DiagnosticCodes.UnknownContributionDirective, $"Unexpected '{LineText.FirstWord(line.Content)}' in contribution body - expected navigate, label, order, id, icon, presentation, group or destination", line.Location);
                    context.SkipBlock(line.Indent);
                    break;
            }
        }

        return new(contributionPoint, navigate, label, order, header.Location)
        {
            DirectiveLocations = directiveLocations,
            Id = item.Id,
            Icon = item.Icon,
            Presentation = item.Presentation,
            Group = item.Group,
            Destination = item.Destination
        };
    }

    static void ParseItemDirective(ParserContext context, SourceLine line, NavigationItem item)
    {
        var directive = LineText.FirstWord(line.Content);
        if (item.Seen.Contains(directive))
        {
            context.Error(DiagnosticCodes.UnknownContributionDirective, $"This contribution already declares '{directive}' - at most one is allowed", line.Location);
            context.SkipBlock(line.Indent);
            return;
        }

        item.Seen.Add(directive);
        context.SkipBlock(line.Indent);
        if (directive == "destination")
        {
            var destination = DestinationRegex().Match(line.Content);
            if (!destination.Success)
            {
                context.Error(DiagnosticCodes.UnknownContributionDirective, $"Invalid destination '{line.Content}' - expected 'destination outlet <name>', 'destination dialog <Name>' or 'destination external \"<route>\"'", line.Location);
                return;
            }

            var kind = destination.Groups[1].Value switch
            {
                "outlet" => ContributionDestinationKind.Outlet,
                "dialog" => ContributionDestinationKind.Dialog,
                _ => ContributionDestinationKind.External
            };
            var target = destination.Groups[2].Success ? destination.Groups[2].Value : StringLiteral.Unescape(destination.Groups[3].Value);
            item.Destination = new(kind, target, line.Location);
            return;
        }

        var value = ItemValueRegex().Match(line.Content[directive.Length..].Trim());
        if (!value.Success)
        {
            context.Error(DiagnosticCodes.UnknownContributionDirective, $"Invalid {directive} '{line.Content}' - expected '{directive} <name>' or '{directive} \"<text>\"'", line.Location);
            return;
        }

        var text = value.Groups[1].Success ? value.Groups[1].Value : StringLiteral.Unescape(value.Groups[2].Value);
        switch (directive)
        {
            case "id": item.Id = text; break;
            case "icon": item.Icon = text; break;
            case "presentation": item.Presentation = text; break;
            default: item.Group = text; break;
        }
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

    [GeneratedRegex("^destination\\s+(?:(outlet|dialog)\\s+([A-Za-z_][\\w.]*)|(?:external)\\s+\"(" + StringLiteral.BodyPattern + ")\")$", RegexOptions.None, 1000)]
    private static partial Regex DestinationRegex();

    [GeneratedRegex("^(?:([A-Za-z_][\\w.-]*)|\"(" + StringLiteral.BodyPattern + ")\")$", RegexOptions.None, 1000)]
    private static partial Regex ItemValueRegex();

    [GeneratedRegex("^label\\s+(?:\"(" + StringLiteral.BodyPattern + ")\"|(\\$strings\\.\\w+(?:\\.\\w+)*))$", RegexOptions.None, 1000)]
    private static partial Regex LabelRegex();

    [GeneratedRegex(@"^order\s+(\d+)$", RegexOptions.None, 1000)]
    private static partial Regex OrderRegex();

    /// <summary>
    /// Collects the navigation item directives while a contribution body is parsed.
    /// </summary>
    sealed class NavigationItem
    {
        public HashSet<string> Seen { get; } = new(StringComparer.Ordinal);

        public string? Id { get; set; }

        public string? Icon { get; set; }

        public string? Presentation { get; set; }

        public string? Group { get; set; }

        public ContributionDestinationSyntax? Destination { get; set; }
    }
}
