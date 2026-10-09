// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents an attribute applied to a concept, such as <c>pii</c>, together with the optional
/// documented reason for it.
/// </summary>
/// <param name="Name">The name of the attribute, without the <c>@</c> prefix.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <param name="Reason">The optional reason declared with <c>&lt;attribute&gt; reason "&lt;text&gt;"</c> in the concept body.</param>
/// <remarks>
/// The marker classifies the value; the reason remains a free-text concept note, not a lawful basis.
/// Additive members use init properties, following the binary-safe pattern documented on ConceptSyntax.File.
/// </remarks>
public record ConceptAttributeSyntax(string Name, SourceLocation Location, string? Reason = null) : SyntaxNode(Location)
{
    /// <summary>
    /// The <c>pii</c> marker - personal data (GDPR Art. 4(1)); renders Chronicle [PII].
    /// </summary>
    public const string Pii = "pii";

    /// <summary>
    /// The <c>secret</c> marker - an operational secret. Its wire name remains sensitive.
    /// </summary>
    public const string Sensitive = "sensitive";

    /// <summary>
    /// Gets the explicitly declared secret encryption scope, or null for Chronicle's default.
    /// </summary>
    public string? Scope { get; init; }

    /// <summary>
    /// Gets the GDPR Art. 9(1) special category, when declared.
    /// </summary>
    public string? SpecialCategory { get; init; }

    /// <summary>
    /// Gets whether the personal data relates to criminal convictions and offenses (GDPR Art. 10).
    /// </summary>
    public bool Criminal { get; init; }
}

/// <summary>
/// Represents a <c>concept</c> declaration - a strongly typed domain value.
/// </summary>
/// <param name="Name">The name of the concept.</param>
/// <param name="Type">The primitive type of the concept, or <c>Enum</c> for enumeration concepts.</param>
/// <param name="Attributes">The <see cref="ConceptAttributeSyntax">attributes</see> applied to the concept, such as <c>pii</c> and <c>secret</c>.</param>
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

    /// <summary>
    /// Gets the <see cref="FileReferenceSyntax"/> naming the file the concept is realized by, and
    /// <c>null</c> when the document does not name one.
    /// </summary>
    /// <remarks>
    /// An <c>init</c> property rather than a parameter of the primary constructor, deliberately. A trailing
    /// parameter on a positional record is source compatible and <em>binary</em> breaking: it replaces the
    /// constructor and <c>Deconstruct</c> in the compiled signature, so a package built against the previous
    /// version fails at run time with a missing method and no compiler error anywhere. Adding capability as
    /// an init property is neither, and is how this record should grow from here.
    /// </remarks>
    public FileReferenceSyntax? File { get; init; }

    /// <summary>
    /// Gets the report-only description of this value.
    /// </summary>
    public string? Description { get; init; }
}
