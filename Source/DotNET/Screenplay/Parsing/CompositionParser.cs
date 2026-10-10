// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses the document-level <c>exposure for</c> and <c>instance</c> declarations that make templates configurable.
/// </summary>
internal static partial class CompositionParser
{
    const string ExposedPropertyShape = "'property <component>.<path> [label \"<text>\"] [operations <add|remove|reorder|edit-fields>, ...] [fields <field>, ...] [reexposes <Owner>]'";
    static readonly string[] _operations = ["add", "remove", "reorder", "edit-fields"];

    /// <summary>
    /// Parses an <c>exposure for &lt;Owner&gt;</c> declaration from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed header line.</param>
    /// <returns>The parsed <see cref="ExposureSyntax"/>.</returns>
    public static ExposureSyntax ParseExposure(ParserContext context, SourceLine header)
    {
        var match = ExposureHeaderRegex().Match(header.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidExposureDeclaration, $"Invalid exposure declaration '{header.Content}' - expected 'exposure for <Owner>'", header.Location);
        }

        var properties = new List<ExposedPropertySyntax>();
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            if (ParseExposedProperty(context, line) is { } property)
            {
                properties.Add(property);
            }
        }

        return new(match.Success ? match.Groups[1].Value : header.Content, properties, header.Location);
    }

    /// <summary>
    /// Parses an <c>instance &lt;Instance&gt;</c> block from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed header line.</param>
    /// <returns>The parsed <see cref="InstanceContributionsSyntax"/>.</returns>
    public static InstanceContributionsSyntax ParseInstance(ParserContext context, SourceLine header)
    {
        var match = InstanceHeaderRegex().Match(header.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidInstanceContribution, $"Invalid instance declaration '{header.Content}' - expected 'instance <Screen or Template>'", header.Location);
        }

        var contributions = new List<InstanceContributionSyntax>();
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            if (ParseContribution(context, line) is { } contribution)
            {
                contributions.Add(contribution);
            }
        }

        return new(match.Success ? match.Groups[1].Value : header.Content, contributions, header.Location);
    }

    static ExposedPropertySyntax? ParseExposedProperty(ParserContext context, SourceLine line)
    {
        var match = ExposedPropertyRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidExposureDeclaration, $"Invalid exposed property '{line.Content}' - expected {ExposedPropertyShape}", line.Location);
            return null;
        }

        var operations = match.Groups[5].Success ? List(match.Groups[5].Value) : [];
        if (operations.FirstOrDefault(operation => !_operations.Contains(operation, StringComparer.Ordinal)) is { } unknown)
        {
            context.Error(DiagnosticCodes.InvalidExposureDeclaration, $"Unknown collection operation '{unknown}' - expected add, remove, reorder, edit-fields or none", line.Location);
            return null;
        }

        return new(ComponentReference(match, 1), match.Groups[3].Value, line.Location)
        {
            Label = match.Groups[4].Success ? StringLiteral.Unescape(match.Groups[4].Value) : null,
            IsCollection = match.Groups[5].Success,
            Operations = operations,
            RestrictsFields = match.Groups[6].Success,
            EditableFields = match.Groups[6].Success ? List(match.Groups[6].Value) : [],
            ReExposes = match.Groups[7].Success ? match.Groups[7].Value : null
        };
    }

    static InstanceContributionSyntax? ParseContribution(ParserContext context, SourceLine line)
    {
        var set = SetRegex().Match(line.Content);
        if (set.Success)
        {
            context.SkipBlock(line.Indent);
            return new(ComponentReference(set, 1), set.Groups[3].Value, ExpressionParser.ParseMappingSource(context, set.Groups[4].Value, line.Location), [], line.Location);
        }

        var items = ItemsRegex().Match(line.Content);
        if (items.Success)
        {
            var parsed = new List<ContributedItemSyntax>();
            while (context.TryPeekChild(line.Indent, out var child))
            {
                context.Reader.TakeSignificant();
                if (ParseItem(context, child) is { } item)
                {
                    parsed.Add(item);
                }
            }

            return new(ComponentReference(items, 1), items.Groups[3].Value, null, parsed, line.Location);
        }

        context.Error(DiagnosticCodes.InvalidInstanceContribution, $"Invalid instance contribution '{line.Content}' - expected 'set <component>.<path> = <value>' or 'items <component>.<path>'", line.Location);
        context.SkipBlock(line.Indent);
        return null;
    }

    static ContributedItemSyntax? ParseItem(ParserContext context, SourceLine line)
    {
        var match = ItemRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidInstanceContribution, $"Invalid contributed item '{line.Content}' - expected 'item <id>'", line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        var values = new List<ContributedItemValueSyntax>();
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            var value = ItemValueRegex().Match(child.Content);
            if (!value.Success)
            {
                context.Error(DiagnosticCodes.InvalidInstanceContribution, $"Invalid item value '{child.Content}' - expected '<field> = <value>'", child.Location);
                context.SkipBlock(child.Indent);
                continue;
            }

            values.Add(new(value.Groups[1].Value, ExpressionParser.ParseMappingSource(context, value.Groups[2].Value, child.Location), child.Location));
        }

        return new(ComponentReference(match, 1), values, line.Location);
    }

    static string ComponentReference(Match match, int group) =>
        match.Groups[group].Success ? match.Groups[group].Value : StringLiteral.Unescape(match.Groups[group + 1].Value);

    static string[] List(string text) => text.Trim() == "none" ? [] : [.. text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)];

    [GeneratedRegex(@"^exposure\s+for\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex ExposureHeaderRegex();

    [GeneratedRegex(@"^instance\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex InstanceHeaderRegex();

    [GeneratedRegex("^property\\s+(?:([A-Za-z_]\\w*)|\"(" + StringLiteral.BodyPattern + ")\")\\.(\\w+(?:\\.\\w+)*)(?:\\s+label\\s+\"(" + StringLiteral.BodyPattern + ")\")?(?:\\s+operations\\s+([\\w-]+(?:\\s*,\\s*[\\w-]+)*))?(?:\\s+fields\\s+(\\w+(?:\\s*,\\s*\\w+)*))?(?:\\s+reexposes\\s+([A-Za-z_]\\w*))?$", RegexOptions.None, 1000)]
    private static partial Regex ExposedPropertyRegex();

    [GeneratedRegex("^set\\s+(?:([A-Za-z_]\\w*)|\"(" + StringLiteral.BodyPattern + ")\")\\.(\\w+(?:\\.\\w+)*)\\s*=\\s*(.+)$", RegexOptions.None, 1000)]
    private static partial Regex SetRegex();

    [GeneratedRegex("^items\\s+(?:([A-Za-z_]\\w*)|\"(" + StringLiteral.BodyPattern + ")\")\\.(\\w+(?:\\.\\w+)*)$", RegexOptions.None, 1000)]
    private static partial Regex ItemsRegex();

    [GeneratedRegex("^item\\s+(?:([A-Za-z_][\\w-]*)|\"(" + StringLiteral.BodyPattern + ")\")$", RegexOptions.None, 1000)]
    private static partial Regex ItemRegex();

    [GeneratedRegex(@"^([A-Za-z_]\w*)\s*=\s*(.+)$", RegexOptions.None, 1000)]
    private static partial Regex ItemValueRegex();
}
