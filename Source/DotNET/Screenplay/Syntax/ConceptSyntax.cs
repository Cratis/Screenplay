// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents an attribute applied to a concept, such as <c>@pii</c>, together with the optional
/// documented reason for it.
/// </summary>
/// <param name="Name">The name of the attribute, without the <c>@</c> prefix.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <param name="Reason">The optional reason declared with <c>&lt;attribute&gt; reason "&lt;text&gt;"</c> in the concept body.</param>
/// <remarks>
/// The marker says a value is personal data; the reason says why - the purpose, the lawful basis and
/// whose subject it lives under. A compliance reader needs both, so the reason travels with the concept
/// rather than being lost between the source system and the document.
/// </remarks>
public record ConceptAttributeSyntax(string Name, SourceLocation Location, string? Reason = null) : SyntaxNode(Location)
{
    /// <summary>
    /// The <c>@pii</c> attribute - the value is personally identifiable information.
    /// </summary>
    public const string Pii = "pii";

    /// <summary>
    /// The <c>@sensitive</c> attribute - the value is sensitive.
    /// </summary>
    public const string Sensitive = "sensitive";
}

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

    /// <summary>
    /// Gets the names of the attributes applied to the concept, without the <c>@</c> prefix.
    /// </summary>
    public IEnumerable<string> AttributeNames => Attributes.Select(attribute => attribute.Name);
}
