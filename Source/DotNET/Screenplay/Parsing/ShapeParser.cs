// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses a top-level <c>shape</c> declaration: a name and typed fields, like a read model without a builder.
/// </summary>
internal static partial class ShapeParser
{
    /// <summary>
    /// Parses a shape from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed <see cref="SourceLine"/> holding <c>shape &lt;Name&gt;</c>.</param>
    /// <returns>The parsed <see cref="ShapeSyntax"/>.</returns>
    public static ShapeSyntax Parse(ParserContext context, SourceLine header)
    {
        var name = HeaderRegex().Match(header.Content);
        if (!name.Success)
        {
            context.Error(DiagnosticCodes.InvalidShapeDeclaration, $"Invalid shape declaration '{header.Content}' - expected 'shape <Name>'", header.Location);
        }

        var shapeName = name.Success ? name.Groups[1].Value : string.Empty;
        var properties = new List<PropertySyntax>();
        string? description = null;
        var directiveLocations = new Dictionary<string, SourceLocation>();

        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            if (LineText.FirstWord(line.Content) == "description" && PropertyLineParser.TryParse(line) is null)
            {
                var previous = description;
                description = DescriptionParser.Parse(context, line, description, $"Shape '{shapeName}'");
                if (previous is null && description is not null)
                {
                    directiveLocations["description"] = line.Location;
                }

                continue;
            }

            if (PropertyLineParser.Parse(context, line) is not { } property)
            {
                context.Error(DiagnosticCodes.InvalidShapeDeclaration, $"Invalid shape field '{line.Content}' - expected '<name> <Type>'", line.Location);
                continue;
            }

            if (property.IsIdentifier || property.IsKey || property.IsGenerated || property.IsSubject)
            {
                context.Error(DiagnosticCodes.InvalidShapeDeclaration, $"Shape field '{property.Name}' cannot carry a key, identifier, subject or generated modifier - a shape is a contract on data, not a stored model", line.Location);
                property = property with { IsIdentifier = false, IsKey = false, IsGenerated = false, IsSubject = false };
            }

            properties.Add(property);
        }

        return new(shapeName, properties, header.Location, description) { DirectiveLocations = directiveLocations };
    }

    [GeneratedRegex(@"^shape\s+([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex HeaderRegex();
}
