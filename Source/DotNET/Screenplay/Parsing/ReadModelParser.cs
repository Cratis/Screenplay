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
        var name = ParseName(context, header);
        var properties = new List<PropertySyntax>();
        string? description = null;
        string? documentation = null;
        var directiveLocations = new Dictionary<string, SourceLocation>();
        FileReferenceSyntax? file = null;
        var passive = false;
        int? bodyIndent = null;

        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            var propertyIndent = bodyIndent ??= line.Indent;
            if (IsMetadataDirective(line, "description"))
            {
                var previousDescription = description;
                description = DescriptionParser.Parse(context, line, description, $"Read model '{name}'");
                RecordDescriptionLocation(line, previousDescription, description, directiveLocations);
            }
            else if (IsMetadataDirective(line, "documentation"))
            {
                documentation = DocumentationParser.Parse(context, line, documentation, $"Read model '{name}'", directiveLocations);
            }
            else if (IsPassiveDirective(line))
            {
                passive = ParsePassiveDirective(context, line, passive, directiveLocations);
            }
            else if (FileReferenceParser.IsDirectiveAmongProperties(line))
            {
                file = FileReferenceParser.ParseReplacing(context, line, file, directiveLocations);
            }
            else if (ParseProperty(context, line, name, propertyIndent) is { } property)
            {
                properties.Add(property);
            }
        }

        return new(name, properties, header.Location, description)
        {
            Documentation = documentation,
            File = file,
            IsPassive = passive,
            DirectiveLocations = directiveLocations
        };
    }

    static string ParseName(ParserContext context, SourceLine header)
    {
        var name = HeaderRegex().Match(header.Content);
        if (!name.Success)
        {
            context.Error(DiagnosticCodes.InvalidReadModelDeclaration, $"Invalid read model declaration '{header.Content}' - expected 'readmodel <Name>'", header.Location);
        }

        return name.Groups[1].Value;
    }

    [GeneratedRegex(@"^readmodel\s+([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex HeaderRegex();
}
