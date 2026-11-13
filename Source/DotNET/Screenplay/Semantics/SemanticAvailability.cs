// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Represents a declared audience - a named group a tenant, user or system caller can belong to at runtime. ESM v53.
/// </summary>
/// <param name="Name">The audience name.</param>
/// <remarks>
/// The model declares audiences and never who is in them. A persona's audiences are authoring metadata and are not carried.
/// </remarks>
public sealed record SemanticAudience(string Name);

/// <summary>
/// Represents a reusable availability rule. ESM v53.
/// </summary>
/// <param name="Name">The availability name.</param>
/// <param name="Requirements">The requirements, one per <c>for</c>, <c>when</c> or <c>with</c> line; all of them must hold.</param>
public sealed record SemanticAvailability(string Name, ImmutableArray<SemanticAvailabilityRequirement> Requirements);

/// <summary>
/// Represents what has to hold for a part of the model to be there. A gate is decided before authorization, and unknown is not available.
/// </summary>
public abstract record SemanticAvailabilityRequirement;

/// <summary>
/// Holds when the caller's tenant, user or system holds the audience.
/// </summary>
/// <param name="Audience">The audience name.</param>
public sealed record SemanticAudienceRequirement(string Audience) : SemanticAvailabilityRequirement;

/// <summary>
/// Holds when the named, reusable availability holds.
/// </summary>
/// <param name="Availability">The availability name.</param>
public sealed record SemanticAvailabilityReference(string Availability) : SemanticAvailabilityRequirement;

/// <summary>
/// Holds when a condition over the request context is true.
/// </summary>
/// <param name="Condition">The condition.</param>
public sealed record SemanticConditionRequirement(SemanticAvailabilityCondition Condition) : SemanticAvailabilityRequirement;

/// <summary>
/// Combines two requirements with the authored logical operator.
/// </summary>
/// <param name="Left">The left requirement.</param>
/// <param name="Operator">The logical operator.</param>
/// <param name="Right">The right requirement.</param>
public sealed record SemanticLogicalAvailability(SemanticAvailabilityRequirement Left, SemanticLogicalOperator Operator, SemanticAvailabilityRequirement Right) : SemanticAvailabilityRequirement;

/// <summary>
/// Represents a condition over the request context that an availability reads.
/// </summary>
public abstract record SemanticAvailabilityCondition;

/// <summary>
/// Compares a context value with an operand.
/// </summary>
/// <param name="Path">The context path, written without the <c>$context.</c> prefix, for example <c>vessel.hasReported</c>.</param>
/// <param name="Operator">The comparison operator.</param>
/// <param name="Operand">What the value is compared with.</param>
public sealed record SemanticAvailabilityComparison(string Path, SemanticComparisonOperator Operator, SemanticContextOperand Operand) : SemanticAvailabilityCondition;

/// <summary>
/// Combines two conditions with the authored logical operator.
/// </summary>
/// <param name="Left">The left condition.</param>
/// <param name="Operator">The logical operator.</param>
/// <param name="Right">The right condition.</param>
public sealed record SemanticLogicalAvailabilityCondition(SemanticAvailabilityCondition Left, SemanticLogicalOperator Operator, SemanticAvailabilityCondition Right) : SemanticAvailabilityCondition;

/// <summary>
/// Represents a literal or another context value an availability condition or a provided value reads.
/// </summary>
/// <param name="Path">The context path, written without the <c>$context.</c> prefix, or <see langword="null"/> for a literal.</param>
/// <param name="Value">The literal, or <see langword="null"/> for a context path.</param>
public sealed record SemanticContextOperand(string? Path, SemanticValue? Value);

/// <summary>
/// Represents a value a <c>provide</c> adds to the request context for everything beneath the level that declares it. ESM v53.
/// </summary>
/// <param name="Name">The name, read as <c>$context.&lt;name&gt;</c>.</param>
/// <param name="Source">Where the value comes from.</param>
/// <remarks>
/// A provided value is typed either by <see cref="Type"/> - a primitive, concept or composite type - or by <see cref="ReadModel"/>.
/// </remarks>
public sealed record SemanticProvidedValue(string Name, SemanticProvideSource Source)
{
    /// <summary>Gets the type of the value, or <see langword="null"/> when it is a read model.</summary>
    public SemanticTypeReference? Type { get; init; }

    /// <summary>Gets the read model the value is an instance of, or <see langword="null"/> when it has a <see cref="Type"/>.</summary>
    public SemanticId? ReadModel { get; init; }

    /// <summary>Gets whether a read model value may be absent.</summary>
    public bool Optional { get; init; }

    /// <summary>Gets whether a read model value is a collection.</summary>
    public bool Collection { get; init; }
}

/// <summary>
/// Represents where a provided value comes from.
/// </summary>
public abstract record SemanticProvideSource;

/// <summary>
/// The value is the result of a keyed read model query, run once per request.
/// </summary>
/// <param name="Query">The identity of the query.</param>
/// <param name="By">The key operands the query is run with, in key order; empty when it takes none.</param>
public sealed record SemanticQueryProvideSource(SemanticId Query, ImmutableArray<SemanticContextOperand> By) : SemanticProvideSource;

/// <summary>
/// The value is supplied by the hosting runtime under the declared name and type.
/// </summary>
public sealed record SemanticHostProvideSource : SemanticProvideSource;

/// <summary>
/// The value is a literal or a value read from elsewhere in the context.
/// </summary>
/// <param name="Value">The operand.</param>
public sealed record SemanticValueProvideSource(SemanticContextOperand Value) : SemanticProvideSource;
