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
/// Defines the concrete portable value variants admitted by ESM v1.
/// </summary>
public enum SemanticValueKind
{
    /// <summary>
    /// An unknown value. Unknown values are never admitted.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// The null value.
    /// </summary>
    Null = 0,

    /// <summary>
    /// A text value.
    /// </summary>
    Text = 1,

    /// <summary>
    /// An invariant decimal number value.
    /// </summary>
    Number = 2,

    /// <summary>
    /// A Boolean value.
    /// </summary>
    Boolean = 3
}

/// <summary>
/// Defines the portable expression variants admitted by ESM v1.
/// </summary>
public enum SemanticExpressionKind
{
    /// <summary>
    /// An unknown expression. Unknown values are never admitted.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// A concrete value expression.
    /// </summary>
    Value = 0,

    /// <summary>
    /// A resolved semantic reference expression.
    /// </summary>
    Resolved = 1
}

/// <summary>
/// Defines the semantic root that supplies a resolved expression value.
/// </summary>
public enum SemanticExpressionRootKind
{
    /// <summary>
    /// An unknown root. Unknown values are never admitted.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// The command currently being evaluated.
    /// </summary>
    Command = 0,

    /// <summary>
    /// The event currently being projected.
    /// </summary>
    Event = 1,

    /// <summary>
    /// The query currently being evaluated.
    /// </summary>
    Query = 2
}

/// <summary>
/// Defines the kind of semantic target resolved by an expression.
/// </summary>
public enum SemanticExpressionSourceKind
{
    /// <summary>
    /// An unknown source. Unknown values are never admitted.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// A property declared by the expression root.
    /// </summary>
    Property = 0,

    /// <summary>
    /// An argument declared by the expression root.
    /// </summary>
    Argument = 1
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
/// Represents a concrete portable value.
/// </summary>
/// <param name="Kind">The value kind.</param>
public abstract record SemanticValue(SemanticValueKind Kind)
{
    /// <summary>
    /// Gets the null value.
    /// </summary>
    public static SemanticValue Null { get; } = new SemanticNullValue();

    /// <summary>
    /// Creates a text value.
    /// </summary>
    /// <param name="value">The text.</param>
    /// <returns>The value.</returns>
    public static SemanticValue Text(string value) => new SemanticTextValue(value);

    /// <summary>
    /// Creates an invariant number value.
    /// </summary>
    /// <param name="value">The number.</param>
    /// <returns>The value.</returns>
    public static SemanticValue Number(decimal value) => new SemanticNumberValue(value);

    /// <summary>
    /// Creates a Boolean value.
    /// </summary>
    /// <param name="value">The Boolean.</param>
    /// <returns>The value.</returns>
    public static SemanticValue Boolean(bool value) => new SemanticBooleanValue(value);
}

/// <summary>
/// Represents the null portable value.
/// </summary>
public sealed record SemanticNullValue() : SemanticValue(SemanticValueKind.Null);

/// <summary>
/// Represents a text portable value.
/// </summary>
/// <param name="Value">The text.</param>
public sealed record SemanticTextValue(string Value) : SemanticValue(SemanticValueKind.Text);

/// <summary>
/// Represents an invariant decimal portable value.
/// </summary>
/// <param name="Value">The number.</param>
public sealed record SemanticNumberValue(decimal Value) : SemanticValue(SemanticValueKind.Number);

/// <summary>
/// Represents a Boolean portable value.
/// </summary>
/// <param name="Value">The Boolean.</param>
public sealed record SemanticBooleanValue(bool Value) : SemanticValue(SemanticValueKind.Boolean);

/// <summary>
/// Represents a constrained portable expression used by mappings and keys.
/// </summary>
/// <param name="Kind">The expression kind.</param>
public abstract record SemanticExpression(SemanticExpressionKind Kind)
{
    /// <summary>
    /// Creates a concrete value expression.
    /// </summary>
    /// <param name="value">The concrete value.</param>
    /// <returns>The expression.</returns>
    public static SemanticExpression FromValue(SemanticValue value) => new SemanticValueExpression(value);

    /// <summary>
    /// Creates a resolved property expression.
    /// </summary>
    /// <param name="root">The semantic root.</param>
    /// <param name="property">The stable property identity.</param>
    /// <returns>The expression.</returns>
    public static SemanticExpression Property(SemanticExpressionRootKind root, SemanticId property) =>
        new SemanticResolvedExpression(root, SemanticExpressionSourceKind.Property, property);

    /// <summary>
    /// Creates a resolved argument expression.
    /// </summary>
    /// <param name="root">The semantic root.</param>
    /// <param name="argument">The stable argument identity.</param>
    /// <returns>The expression.</returns>
    public static SemanticExpression Argument(SemanticExpressionRootKind root, SemanticId argument) =>
        new SemanticResolvedExpression(root, SemanticExpressionSourceKind.Argument, argument);
}

/// <summary>
/// Represents a concrete value expression.
/// </summary>
/// <param name="Value">The concrete value.</param>
public sealed record SemanticValueExpression(SemanticValue Value) : SemanticExpression(SemanticExpressionKind.Value);

/// <summary>
/// Represents a resolved reference expression using stable semantic identity.
/// </summary>
/// <param name="Root">The semantic root that supplies the value.</param>
/// <param name="Source">The target kind within the root.</param>
/// <param name="Target">The stable property or argument identity.</param>
public sealed record SemanticResolvedExpression(
    SemanticExpressionRootKind Root,
    SemanticExpressionSourceKind Source,
    SemanticId Target) : SemanticExpression(SemanticExpressionKind.Resolved);
