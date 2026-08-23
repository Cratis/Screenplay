// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Defines the portable primitive types admitted by ESM v1.
/// </summary>
public enum SemanticPrimitiveType
{
    /// <summary>
    /// An unknown type. Unknown values are never admitted.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// A UUID value.
    /// </summary>
    Uuid = 0,

    /// <summary>
    /// A text value.
    /// </summary>
    Text = 1,

    /// <summary>
    /// A signed whole-number value.
    /// </summary>
    WholeNumber = 2,

    /// <summary>
    /// A fixed-point decimal value.
    /// </summary>
    DecimalNumber = 3,

    /// <summary>
    /// A Boolean value.
    /// </summary>
    Boolean = 4,

    /// <summary>
    /// A date value.
    /// </summary>
    Date = 5,

    /// <summary>
    /// A date and time value.
    /// </summary>
    DateTime = 6
}

/// <summary>
/// Defines the target represented by a semantic type reference.
/// </summary>
public enum SemanticTypeReferenceKind
{
    /// <summary>
    /// An unknown reference. Unknown values are never admitted.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// A primitive type.
    /// </summary>
    Primitive = 0,

    /// <summary>
    /// A concept.
    /// </summary>
    Concept = 1,

    /// <summary>
    /// A composite type.
    /// </summary>
    CompositeType = 2
}

/// <summary>
/// Defines the portable expression forms admitted by the initial semantic model.
/// </summary>
public enum SemanticExpressionKind
{
    /// <summary>
    /// An unknown expression. Unknown values are never admitted.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// The null literal.
    /// </summary>
    Null = 0,

    /// <summary>
    /// A text literal.
    /// </summary>
    Text = 1,

    /// <summary>
    /// An invariant decimal number literal.
    /// </summary>
    Number = 2,

    /// <summary>
    /// A Boolean literal.
    /// </summary>
    Boolean = 3,

    /// <summary>
    /// A resolved dotted value path.
    /// </summary>
    Path = 4
}

/// <summary>
/// Represents a resolved portable type reference.
/// </summary>
/// <param name="Kind">The reference kind.</param>
/// <param name="Primitive">The primitive type, or <see cref="SemanticPrimitiveType.Unknown"/> for a semantic target.</param>
/// <param name="Target">The concept or composite type identity, or a default identity for a primitive.</param>
/// <param name="IsCollection">Whether the value is a collection.</param>
/// <param name="IsOptional">Whether the value is optional.</param>
public sealed record SemanticTypeReference(
    SemanticTypeReferenceKind Kind,
    SemanticPrimitiveType Primitive,
    SemanticId Target,
    bool IsCollection,
    bool IsOptional)
{
    /// <summary>
    /// Creates a primitive type reference.
    /// </summary>
    /// <param name="primitive">The primitive type.</param>
    /// <param name="isCollection">Whether the value is a collection.</param>
    /// <param name="isOptional">Whether the value is optional.</param>
    /// <returns>The type reference.</returns>
    public static SemanticTypeReference ForPrimitive(
        SemanticPrimitiveType primitive,
        bool isCollection = false,
        bool isOptional = false) =>
        new(SemanticTypeReferenceKind.Primitive, primitive, default, isCollection, isOptional);

    /// <summary>
    /// Creates a concept type reference.
    /// </summary>
    /// <param name="target">The concept identity.</param>
    /// <param name="isCollection">Whether the value is a collection.</param>
    /// <param name="isOptional">Whether the value is optional.</param>
    /// <returns>The type reference.</returns>
    public static SemanticTypeReference ForConcept(SemanticId target, bool isCollection = false, bool isOptional = false) =>
        new(SemanticTypeReferenceKind.Concept, SemanticPrimitiveType.Unknown, target, isCollection, isOptional);

    /// <summary>
    /// Creates a composite type reference.
    /// </summary>
    /// <param name="target">The composite type identity.</param>
    /// <param name="isCollection">Whether the value is a collection.</param>
    /// <param name="isOptional">Whether the value is optional.</param>
    /// <returns>The type reference.</returns>
    public static SemanticTypeReference ForCompositeType(SemanticId target, bool isCollection = false, bool isOptional = false) =>
        new(SemanticTypeReferenceKind.CompositeType, SemanticPrimitiveType.Unknown, target, isCollection, isOptional);
}

/// <summary>
/// Represents a constrained portable expression used by mappings, validation and keys.
/// </summary>
/// <param name="Kind">The expression kind.</param>
/// <param name="Text">The text for text and path expressions.</param>
/// <param name="Number">The value for number expressions.</param>
/// <param name="Boolean">The value for Boolean expressions.</param>
public sealed record SemanticExpression(
    SemanticExpressionKind Kind,
    string? Text,
    decimal? Number,
    bool? Boolean)
{
    /// <summary>
    /// Gets the null literal.
    /// </summary>
    public static SemanticExpression Null { get; } = new(SemanticExpressionKind.Null, null, null, null);

    /// <summary>
    /// Creates a text literal.
    /// </summary>
    /// <param name="value">The text value.</param>
    /// <returns>The expression.</returns>
    public static SemanticExpression TextValue(string value) => new(SemanticExpressionKind.Text, value, null, null);

    /// <summary>
    /// Creates an invariant number literal.
    /// </summary>
    /// <param name="value">The number value.</param>
    /// <returns>The expression.</returns>
    public static SemanticExpression NumberValue(decimal value) => new(SemanticExpressionKind.Number, null, value, null);

    /// <summary>
    /// Creates a Boolean literal.
    /// </summary>
    /// <param name="value">The Boolean value.</param>
    /// <returns>The expression.</returns>
    public static SemanticExpression BooleanValue(bool value) => new(SemanticExpressionKind.Boolean, null, null, value);

    /// <summary>
    /// Creates a resolved value path.
    /// </summary>
    /// <param name="path">The dotted value path.</param>
    /// <returns>The expression.</returns>
    public static SemanticExpression Path(string path) => new(SemanticExpressionKind.Path, path, null, null);
}
