// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

internal static partial class ReadModelParser
{
    static bool IsMetadataDirective(SourceLine line, string keyword) =>
        string.Equals(LineText.FirstWord(line.Content), keyword, StringComparison.Ordinal) && PropertyLineParser.TryParse(line) is null;

    static void RecordDescriptionLocation(
        SourceLine line,
        string? previousDescription,
        string? description,
        Dictionary<string, SourceLocation> directiveLocations)
    {
        if (previousDescription is null && description is not null)
        {
            directiveLocations["description"] = line.Location;
        }
    }

    static bool IsPassiveDirective(SourceLine line) =>
        string.Equals(line.Content, "passive", StringComparison.Ordinal) ||
        (string.Equals(LineText.FirstWord(line.Content), "passive", StringComparison.Ordinal) && IsBooleanLiteral(line.Content["passive".Length..].Trim()));

    static bool ParsePassiveDirective(
        ParserContext context,
        SourceLine line,
        bool passive,
        Dictionary<string, SourceLocation> directiveLocations)
    {
        if (!string.Equals(line.Content, "passive", StringComparison.Ordinal))
        {
            ReportInvalidProperty(context, line);
            return passive;
        }

        if (passive)
        {
            context.Warning(DiagnosticCodes.RepeatedPassiveDirective, "A read model repeats 'passive'; the directive is idempotent.", line.Location);
            return true;
        }

        directiveLocations["passive"] = line.Location;
        return true;
    }

    static PropertySyntax? ParseProperty(ParserContext context, SourceLine line, string modelName, int bodyIndent)
    {
        var property = PropertyLineParser.Parse(context, line);
        if (property is null)
        {
            ReportInvalidProperty(context, line);
            return null;
        }

        if (line.Indent > bodyIndent)
        {
            property = PropertyLineParser.WithoutKey(context, property);
        }

        if (property.IsIdentifier)
        {
            context.Error(
                DiagnosticCodes.IdentifierOutsideCommand,
                $"Property '{property.Name}' of read model '{modelName}' cannot be marked identifier - only a command property can be",
                line.Location);
            property = property with { IsIdentifier = false };
        }

        return property;
    }

    static void ReportInvalidProperty(ParserContext context, SourceLine line) =>
        context.Error(DiagnosticCodes.InvalidPropertyDeclaration, $"Invalid property '{line.Content}' - expected '<name> <Type>'", line.Location);

    static bool IsBooleanLiteral(string value) =>
        string.Equals(value, "true", StringComparison.Ordinal) || string.Equals(value, "false", StringComparison.Ordinal);
}
