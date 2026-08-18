// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses <c>readmodel</c> declarations with their properties.
/// </summary>
internal static partial class ReadModelParser
{
    /// <summary>
    /// Parses a read model from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed <see cref="SourceLine"/> holding the <c>readmodel</c> header.</param>
    /// <returns>The parsed <see cref="ReadModelSyntax"/>.</returns>
    public static ReadModelSyntax Parse(ParserContext context, SourceLine header)
    {
        var name = HeaderRegex().Match(header.Content);
        if (!name.Success)
        {
            context.Error(DiagnosticCodes.InvalidReadModelDeclaration, $"Invalid read model declaration '{header.Content}' - expected 'readmodel <Name>'", header.Location);
        }

        var properties = new List<PropertySyntax>();
        string? description = null;
        FileReferenceSyntax? file = null;

        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            if (LineText.FirstWord(line.Content) == "description" && PropertyLineParser.TryParse(line) is null)
            {
                description = DescriptionParser.Parse(context, line, description, $"Read model '{name.Groups[1].Value}'");
            }
            else if (FileReferenceParser.IsDirectiveAmongProperties(line))
            {
                file = FileReferenceParser.Parse(context, line);
            }
            else if (PropertyLineParser.TryParse(line) is { } property)
            {
                if (property.IsIdentifier)
                {
                    context.Error(
                        DiagnosticCodes.IdentifierOutsideCommand,
                        $"Property '{property.Name}' of read model '{name.Groups[1].Value}' cannot be marked identifier - only a command property can be",
                        line.Location);
                    property = property with { IsIdentifier = false };
                }

                properties.Add(property);
            }
            else
            {
                context.Error(DiagnosticCodes.InvalidPropertyDeclaration, $"Invalid property '{line.Content}' - expected '<name> <Type>'", line.Location);
            }
        }

        return new(name.Groups[1].Value, properties, header.Location, description, file);
    }

    [GeneratedRegex(@"^readmodel\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex HeaderRegex();
}
