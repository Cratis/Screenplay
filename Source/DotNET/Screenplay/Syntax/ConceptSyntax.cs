// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a <c>concept</c> declaration - a strongly typed domain value.
/// </summary>
/// <param name="Name">The name of the concept.</param>
/// <param name="Type">The primitive type of the concept, or <c>Enum</c> for enumeration concepts.</param>
/// <param name="Attributes">The <see cref="ConceptAttributeSyntax">attributes</see> applied to the concept, such as <c>@pii</c> and <c>@sensitive</c>.</param>
/// <param name="Values">The values of the concept when it is an enumeration, empty otherwise.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <param name="Validations">The <see cref="ValidateSyntax">validation blocks</see> for the concept. Rules use
/// <see cref="ValidationRuleSyntax.ConceptValue"/> as their implied property subject.</param>
public record ConceptSyntax(
    string Name,
    string Type,
    IEnumerable<ConceptAttributeSyntax> Attributes,
    IEnumerable<string> Values,
    SourceLocation Location,
    IEnumerable<ValidateSyntax>? Validations = null) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the well known primitive type names a concept can be based on.
    /// </summary>
    public static readonly IEnumerable<string> PrimitiveTypes = ["Uuid", "String", "Int", "Decimal", "Bool", "Date", "DateTime"];

    /// <summary>
    /// Gets a value indicating whether the concept is an enumeration.
    /// </summary>
    public bool IsEnum => Type == "Enum";
}

/// <summary>
/// Represents an attribute applied to a concept, with the reason it carries when one is declared.
/// </summary>
/// <param name="Name">The name of the attribute, without the <c>@</c> prefix - <c>pii</c>, <c>sensitive</c> and so on.</param>
/// <param name="Value">The optional argument - for <c>@pii</c> the reason the value is personal data.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <remarks>
/// Any <c>@word</c> is accepted, with or without an argument. The compiler does not enumerate the known
/// attributes, so a consumer can introduce its own without a grammar change.
/// </remarks>
public record ConceptAttributeSyntax(string Name, string? Value, SourceLocation Location) : SyntaxNode(Location);
