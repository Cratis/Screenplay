// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses <c>type</c> declarations - the composite value types events, commands and other types reference.
/// </summary>
internal static partial class TypeParser
{
    /// <summary>
    /// Parses a type from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed <see cref="SourceLine"/> holding the <c>type</c> header.</param>
    /// <returns>The parsed <see cref="TypeSyntax"/>.</returns>
    public static TypeSyntax Parse(ParserContext context, SourceLine header)
    {
        var name = HeaderRegex().Match(header.Content);
        if (!name.Success)
        {
            context.Error(DiagnosticCodes.InvalidTypeDeclaration, $"Invalid type declaration '{header.Content}' - expected 'type <Name>'", header.Location);
        }

        var properties = new List<PropertySyntax>();
        string? description = null;

        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();

            // 'description' takes no type reference, so a line with property shape is a property named
            // description - the same rule the command body follows.
            if (LineText.FirstWord(line.Content) == "description" && PropertyLineParser.TryParse(line) is null)
            {
                description = DescriptionParser.Parse(context, line, description, $"Type '{name.Groups[1].Value}'");
                continue;
            }

            if (PropertyLineParser.TryParse(line) is not { } property)
            {
                context.Error(DiagnosticCodes.InvalidPropertyDeclaration, $"Invalid property '{line.Content}' - expected '<name> <Type>'", line.Location);
                continue;
            }

            if (property.IsIdentifier)
            {
                context.Error(DiagnosticCodes.IdentifierOutsideCommand, $"Property '{property.Name}' of type '{name.Groups[1].Value}' cannot be marked identifier - only a command property can be", line.Location);
                property = property with { IsIdentifier = false };
            }

            properties.Add(property);
        }

        if (properties.Count == 0)
        {
            context.Error(DiagnosticCodes.TypeWithoutProperties, $"Type '{name.Groups[1].Value}' must declare at least one property", header.Location);
        }

        return new(name.Groups[1].Value, properties, header.Location, description);
    }

    [GeneratedRegex(@"^type\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex HeaderRegex();
}
