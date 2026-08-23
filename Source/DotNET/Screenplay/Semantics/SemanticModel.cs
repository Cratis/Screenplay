// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Defines the portable slice kinds admitted by ESM v1.
/// </summary>
public enum SemanticSliceKind
{
    /// <summary>
    /// An unknown kind. Unknown values are never admitted.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// A state-changing behavior.
    /// </summary>
    StateChange = 0,

    /// <summary>
    /// A state view behavior.
    /// </summary>
    StateView = 1
}

/// <summary>
/// Represents an ESM property.
/// </summary>
/// <param name="Id">The stable semantic identity.</param>
/// <param name="Name">The display name.</param>
/// <param name="Type">The resolved type.</param>
/// <param name="IsIdentifier">Whether the property supplies a modeled runtime identity.</param>
public sealed record SemanticProperty(SemanticId Id, string Name, SemanticTypeReference Type, bool IsIdentifier);

/// <summary>
/// Represents a strongly typed primitive concept.
/// </summary>
/// <param name="Id">The stable semantic identity.</param>
/// <param name="Name">The display name.</param>
/// <param name="Primitive">The underlying primitive type.</param>
/// <param name="Values">The declared enumeration values, in declaration order.</param>
/// <param name="Validations">The validation rules, in behavior order.</param>
public sealed record SemanticConcept(
    SemanticId Id,
    string Name,
    SemanticPrimitiveType Primitive,
    ImmutableArray<string> Values,
    ImmutableArray<SemanticValidationRule> Validations);

/// <summary>
/// Represents a named composite value type.
/// </summary>
/// <param name="Id">The stable semantic identity.</param>
/// <param name="Name">The display name.</param>
/// <param name="Properties">The properties of the type.</param>
public sealed record SemanticCompositeType(SemanticId Id, string Name, ImmutableArray<SemanticProperty> Properties);

/// <summary>
/// Represents the immutable root application in ESM v1.
/// </summary>
/// <param name="Id">The stable semantic identity.</param>
/// <param name="Name">The display name.</param>
/// <param name="Concepts">The application concepts.</param>
/// <param name="Types">The application composite types.</param>
/// <param name="Modules">The application modules.</param>
public sealed record SemanticApplication(
    SemanticId Id,
    string Name,
    ImmutableArray<SemanticConcept> Concepts,
    ImmutableArray<SemanticCompositeType> Types,
    ImmutableArray<SemanticModule> Modules);

/// <summary>
/// Represents a bounded semantic module.
/// </summary>
/// <param name="Id">The stable semantic identity.</param>
/// <param name="Name">The display name.</param>
/// <param name="Features">The module features.</param>
public sealed record SemanticModule(SemanticId Id, string Name, ImmutableArray<SemanticFeature> Features);

/// <summary>
/// Represents a semantic feature, optionally containing nested features.
/// </summary>
/// <param name="Id">The stable semantic identity.</param>
/// <param name="Name">The display name.</param>
/// <param name="Features">The nested features.</param>
/// <param name="Slices">The behavior slices.</param>
public sealed record SemanticFeature(
    SemanticId Id,
    string Name,
    ImmutableArray<SemanticFeature> Features,
    ImmutableArray<SemanticSlice> Slices);

/// <summary>
/// Represents the initial executable vertical slice.
/// </summary>
/// <param name="Id">The stable semantic identity.</param>
/// <param name="Name">The display name.</param>
/// <param name="Kind">The portable slice kind.</param>
/// <param name="Events">The event contracts.</param>
/// <param name="Commands">The commands.</param>
/// <param name="ReadModels">The read models.</param>
/// <param name="Projections">The projections.</param>
/// <param name="Queries">The keyed queries.</param>
/// <param name="Specifications">The executable baseline specifications.</param>
public sealed record SemanticSlice(
    SemanticId Id,
    string Name,
    SemanticSliceKind Kind,
    ImmutableArray<SemanticEventContract> Events,
    ImmutableArray<SemanticCommand> Commands,
    ImmutableArray<SemanticReadModel> ReadModels,
    ImmutableArray<SemanticProjection> Projections,
    ImmutableArray<SemanticKeyedQuery> Queries,
    ImmutableArray<SemanticSpecification> Specifications);
