// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses the <c>reads</c> declaration naming a view a command or reaction trigger consults before it decides.
/// </summary>
internal static partial class ReadsParser
{
    /// <summary>
    /// Parses a <c>reads</c> declaration from its line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <param name="line">The <see cref="SourceLine"/> holding the declaration.</param>
    /// <returns>The parsed <see cref="ReadsSyntax"/>, or <c>null</c> when the line is malformed.</returns>
    public static ReadsSyntax? Parse(ParserContext context, SourceLine line)
    {
        if (OptionalReadsRegex().IsMatch(line.Content))
        {
            RejectChildren(context, line);
            context.Error(DiagnosticCodes.OptionalReadsNotSupported, "Optional reads are not yet supported (see #308).", line.Location);
            return null;
        }

        var match = ReadsRegex().Match(line.Content);
        if (!match.Success)
        {
            RejectChildren(context, line);
            context.Error(
                DiagnosticCodes.InvalidReadsDeclaration,
                $"Invalid reads declaration '{line.Content}' - expected 'reads <ReadModel> [as <alias>] [by <value>]'",
                line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        var alias = match.Groups[2];
        if (alias.Success && (alias.Value == "as" || alias.Value == "by" || alias.Value == "reads"))
        {
            RejectChildren(context, line);
            context.Error(
                DiagnosticCodes.InvalidReadsDeclaration,
                $"Invalid reads declaration '{line.Content}' - '{alias.Value}' cannot be used as a reads alias",
                line.Location);
            return null;
        }

        var by = match.Groups[3];
        var parts = new List<PropertyMappingSyntax>();
        var hasBlock = false;
        var directiveLocations = new Dictionary<string, SourceLocation>();
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (child.Content != "by" || hasBlock || by.Success)
            {
                context.Error(DiagnosticCodes.ReadsWithChildren, "A reads line allows only one by child block, without a single by value.", child.Location);
                context.SkipBlock(child.Indent);
                continue;
            }
            hasBlock = true;
            directiveLocations["by"] = child.Location;
            while (context.TryPeekChild(child.Indent, out var part))
            {
                context.Reader.TakeSignificant();
                var mapping = PartRegex().Match(part.Content);
                if (!mapping.Success)
                {
                    context.Error(DiagnosticCodes.InvalidReadModelKeyLookup, "A reads by part must be '<part> = <source>'.", part.Location);
                    context.SkipBlock(part.Indent);
                    continue;
                }
                parts.Add(ExpressionParser.ParseMapping(context, mapping.Groups[1].Value, mapping.Groups[2], part));
            }
        }
        if (hasBlock && (parts.Count < 2 || parts.Select(part => part.Property).Distinct(StringComparer.Ordinal).Count() != parts.Count))
        {
            context.Error(DiagnosticCodes.InvalidReadModelKeyLookup, "A reads by block requires at least two distinct named key parts.", line.Location);
        }
        return new(match.Groups[1].Value, by.Success ? by.Value : null, line.Location)
        {
            Alias = alias.Success ? alias.Value : null,
            ByParts = parts,
            DirectiveLocations = directiveLocations
        };
    }

    internal static void RejectChildren(ParserContext context, SourceLine line)
    {
        if (context.TryPeekChild(line.Indent, out var child))
        {
            context.Error(DiagnosticCodes.ReadsWithChildren, "A reads line allows only one by child block, without a single by value.", child.Location);
            context.SkipBlock(line.Indent);
        }
    }

    [GeneratedRegex(@"^([a-z_]\w*)\s*=\s*(.+)$", RegexOptions.None, 1000)]
    private static partial Regex PartRegex();

    [GeneratedRegex(@"^reads\s+([A-Z]\w*)(?:\s+as\s+([a-z_]\w*))?(?:\s+by\s+([a-z_]\w*))?$", RegexOptions.None, 1000)]
    private static partial Regex ReadsRegex();

    [GeneratedRegex(@"^reads\s+[A-Z]\w*\s+optional(?:\s|$)", RegexOptions.None, 1000)]
    private static partial Regex OptionalReadsRegex();
}
