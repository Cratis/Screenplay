// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses property lines - a lowercase name followed by a type reference, such as <c>lines InvoiceLine[]</c>,
/// with the optional <c>identifier</c> modifier.
/// </summary>
/// <remarks>
/// The name accepts the <c>@</c> escape, so a property can be named after a directive keyword the enclosing
/// block reserves - <c>@tag TagType</c> declares a property called <c>tag</c>.
/// </remarks>
internal static partial class PropertyLineParser
{
    /// <summary>
    /// Attempts to parse a property line.
    /// </summary>
    /// <param name="line">The <see cref="SourceLine"/> to parse.</param>
    /// <returns>The parsed <see cref="PropertySyntax"/>, or <c>null</c> when the line is not a property line.</returns>
    public static PropertySyntax? TryParse(SourceLine line)
    {
        var match = PropertyRegex().Match(line.Content);
        if (!match.Success)
        {
            return null;
        }

        return new(
            LineText.Unescape(match.Groups[1].Value),
            ParseTypeRef(match.Groups[2].Value, line.LocationAt(match.Groups[2].Index)),
            line.Location,
            match.Groups[4].Success)
        {
            IsGenerated = match.Groups[3].Success
        };
    }

    /// <summary>
    /// Parses a type reference with its collection and optionality modifiers.
    /// </summary>
    /// <param name="text">The type reference text.</param>
    /// <param name="location">The <see cref="SourceLocation"/> of the reference.</param>
    /// <returns>The parsed <see cref="TypeRefSyntax"/>.</returns>
    public static TypeRefSyntax ParseTypeRef(string text, SourceLocation location)
    {
        var legacy = text.EndsWith('?');
        var canonical = text.Length > "optional".Length && text.EndsWith("optional", StringComparison.Ordinal) && char.IsWhiteSpace(text[^("optional".Length + 1)]);
        var isOptional = legacy || canonical;
        if (isOptional)
        {
            text = (legacy ? text[..^1] : text[..^"optional".Length]).TrimEnd();
        }

        var isCollection = text.EndsWith("[]", StringComparison.Ordinal);
        if (isCollection)
        {
            text = text[..^2];
        }

        return new(text, isCollection, isOptional, location);
    }

    /// <summary>
    /// Parses a property in a committed property position, reporting spelling diagnostics.
    /// </summary>
    /// <param name="context">The parser collecting diagnostics.</param>
    /// <param name="line">The committed property line.</param>
    /// <returns>The property, or null for an invalid line.</returns>
    public static PropertySyntax? Parse(ParserContext context, SourceLine line)
    {
        var property = TryParse(line);
        if (property is not null)
        {
            ReportLegacyOptionalSuffix(context, property.Type, line);
        }
        else
        {
            ReportInvalidModifierOrder(context, line);
        }

        return property;
    }

    /// <summary>
    /// Reports a legacy suffix only after its owning parser has committed the type.
    /// </summary>
    /// <param name="context">The parser collecting diagnostics.</param>
    /// <param name="type">The committed type occurrence.</param>
    /// <param name="line">The source line holding the type.</param>
    public static void ReportLegacyOptionalSuffix(ParserContext context, TypeRefSyntax type, SourceLine line)
    {
        var length = type.Name.Length + (type.IsCollection ? 2 : 0);
        var offset = type.Location.Column - line.Indent - 1 + length;
        if (type.IsOptional && offset < line.Content.Length && line.Content[offset] == '?')
        {
            context.Add(new(
                DiagnosticSeverity.Information,
                DiagnosticCodes.LegacyOptionalSuffix,
                $"Write '{type.Name}{(type.IsCollection ? "[]" : string.Empty)} optional' instead of '{type.Name}{(type.IsCollection ? "[]" : string.Empty)}?'.",
                type.Location));
        }
    }

    /// <summary>
    /// Explains the order of property modifiers without accepting the reversed form.
    /// </summary>
    /// <param name="context">The parser collecting diagnostics.</param>
    /// <param name="line">The invalid property line.</param>
    public static void ReportInvalidModifierOrder(ParserContext context, SourceLine line)
    {
        if (ReversedModifiersRegex().IsMatch(line.Content))
        {
            context.Error(DiagnosticCodes.InvalidOptionalModifierOrder, "Write 'optional' before 'identifier': '<name> <Type> optional identifier'.", line.Location);
        }
        else if (InvalidGeneratedModifiersRegex().Match(line.Content) is { Success: true } modifiers &&
            modifiers.Groups[1].Value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Any(modifier => modifier == "generated" || modifier == "identifier"))
        {
            context.Error(DiagnosticCodes.InvalidGeneratedModifierOrder, "Write each modifier once in order: '<name> <Type> optional generated identifier'.", line.Location);
        }
    }

    [GeneratedRegex(@"^(@?[a-z_]\w*)\s+([\w.]+(?:\[\])?(?:\?|\s+optional)?)(?:\s+(generated))?(?:\s+(identifier))?$", RegexOptions.None, 1000)]
    private static partial Regex PropertyRegex();

    [GeneratedRegex(@"^@?[a-z_]\w*\s+[\w.]+(?:\[\])?\s+identifier\s+optional(?:\s*=.*)?$", RegexOptions.None, 1000)]
    private static partial Regex ReversedModifiersRegex();

    [GeneratedRegex(@"^@?[a-z_]\w*\s+[\w.]+(?:\[\])?\??\s+((?:optional|generated|identifier)(?:\s+(?:optional|generated|identifier))*)$", RegexOptions.None, 1000)]
    private static partial Regex InvalidGeneratedModifiersRegex();
}
