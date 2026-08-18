// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses <c>constraint</c> declarations.
/// </summary>
internal static partial class ConstraintParser
{
    /// <summary>
    /// Parses a constraint from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed <see cref="SourceLine"/> holding the <c>constraint</c> header.</param>
    /// <returns>The parsed <see cref="ConstraintSyntax"/>.</returns>
    public static ConstraintSyntax Parse(ParserContext context, SourceLine header)
    {
        var name = HeaderRegex().Match(header.Content) is { Success: true } match
            ? match.Groups[1].Value
            : ReportInvalidHeader(context, header);

        ConstraintSyntax? constraint = null;
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            var parsed = ParseBody(context, name, line);
            if (parsed is null)
            {
                continue;
            }

            if (constraint is not null)
            {
                context.Error(DiagnosticCodes.DuplicateConstraintBody, $"Constraint '{name}' already has a body", line.Location);
                continue;
            }

            constraint = parsed;
        }

        if (constraint is null)
        {
            context.Error(DiagnosticCodes.ConstraintWithoutRule, $"Constraint '{name}' must declare 'unique ... on ...', 'unique event ...' or 'file ...'", header.Location);
            constraint = new UniqueEventConstraintSyntax(name, string.Empty, header.Location);
        }

        return constraint;
    }

    static ConstraintSyntax? ParseBody(ParserContext context, string name, SourceLine line)
    {
        var uniqueEvent = UniqueEventRegex().Match(line.Content);
        if (uniqueEvent.Success)
        {
            return new UniqueEventConstraintSyntax(name, uniqueEvent.Groups[1].Value, line.Location);
        }

        var uniqueProperty = UniquePropertyRegex().Match(line.Content);
        if (uniqueProperty.Success)
        {
            return new UniquePropertyConstraintSyntax(name, uniqueProperty.Groups[1].Value, uniqueProperty.Groups[2].Value, line.Location);
        }

        if (FileReferenceParser.IsDirective(line))
        {
            return new FileConstraintSyntax(name, FileReferenceParser.Parse(context, line), line.Location);
        }

        context.Error(DiagnosticCodes.InvalidConstraintBody, $"Invalid constraint body '{line.Content}'", line.Location);
        return null;
    }

    static string ReportInvalidHeader(ParserContext context, SourceLine header)
    {
        context.Error(DiagnosticCodes.InvalidConstraintDeclaration, $"Invalid constraint declaration '{header.Content}' - expected 'constraint <Name>'", header.Location);
        return LineText.FirstWord(header.Content);
    }

    [GeneratedRegex(@"^constraint\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"^unique\s+event\s+([A-Z]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex UniqueEventRegex();

    [GeneratedRegex(@"^unique\s+([a-z_][\w.]*)\s+on\s+([A-Z]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex UniquePropertyRegex();
}
