// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Defines declarative validation behavior in ESM v1.
/// </summary>
public enum SemanticValidationRuleKind
{
    /// <summary>
    /// An unknown rule. Unknown values are never admitted.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// The value must not be empty.
    /// </summary>
    NotEmpty = 0,

    /// <summary>
    /// The value must be no greater than the operand.
    /// </summary>
    Maximum = 1,

    /// <summary>
    /// The value must be no less than the operand.
    /// </summary>
    Minimum = 2,

    /// <summary>
    /// The value must equal the operand.
    /// </summary>
    Equal = 3,

    /// <summary>
    /// The value must not equal the operand.
    /// </summary>
    NotEqual = 4,

    /// <summary>
    /// The value must match the operand.
    /// </summary>
    Matches = 5
}

/// <summary>
/// Defines how many read model instances an occurrence can affect.
/// </summary>
public enum AffectedInstanceCardinality
{
    /// <summary>
    /// An unknown cardinality. Unknown values are never admitted.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// Exactly one instance.
    /// </summary>
    One = 0,

    /// <summary>
    /// Zero or one instance.
    /// </summary>
    ZeroOrOne = 1,

    /// <summary>
    /// Zero or more instances.
    /// </summary>
    Many = 2
}

/// <summary>
/// Defines query result cardinality.
/// </summary>
public enum SemanticQueryCardinality
{
    /// <summary>
    /// An unknown cardinality. Unknown values are never admitted.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// Exactly one result.
    /// </summary>
    One = 0,

    /// <summary>
    /// Zero or one result.
    /// </summary>
    ZeroOrOne = 1
}

/// <summary>
/// Defines query delivery behavior.
/// </summary>
public enum SemanticQueryDelivery
{
    /// <summary>
    /// An unknown delivery. Unknown values are never admitted.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// A deterministic snapshot.
    /// </summary>
    Snapshot = 0,

    /// <summary>
    /// A live result stream.
    /// </summary>
    Live = 1
}

/// <summary>
/// Represents one declarative validation rule.
/// </summary>
/// <param name="Property">The property identity, or a default identity for a concept value rule.</param>
/// <param name="Kind">The rule kind.</param>
/// <param name="Operand">The optional operand.</param>
/// <param name="Message">The optional rejection message.</param>
public sealed record SemanticValidationRule(
    SemanticId Property,
    SemanticValidationRuleKind Kind,
    SemanticExpression? Operand,
    string? Message);

/// <summary>
/// Represents a resolved property mapping.
/// </summary>
/// <param name="TargetProperty">The target property identity.</param>
/// <param name="Source">The source expression.</param>
public sealed record SemanticPropertyMapping(SemanticId TargetProperty, SemanticExpression Source);

/// <summary>
/// Represents a persisted event contract in ESM v1.
/// </summary>
/// <param name="Id">The semantic identity of the declaration.</param>
/// <param name="ContractId">The stable persisted-fact identity.</param>
/// <param name="Revision">The immutable contract revision.</param>
/// <param name="Name">The current display name.</param>
/// <param name="Properties">The event properties.</param>
public sealed record SemanticEventContract(
    SemanticId Id,
    EventContractId ContractId,
    EventContractRevision Revision,
    string Name,
    ImmutableArray<SemanticProperty> Properties);

/// <summary>
/// Represents one event a command can produce.
/// </summary>
/// <param name="EventContract">The event contract identity.</param>
/// <param name="Condition">The optional production condition.</param>
/// <param name="Destination">The optional modeled event-source destination.</param>
/// <param name="Mappings">The mappings in behavior order.</param>
public sealed record SemanticProducedEvent(
    EventContractId EventContract,
    SemanticExpression? Condition,
    SemanticExpression? Destination,
    ImmutableArray<SemanticPropertyMapping> Mappings);

/// <summary>
/// Represents a portable command contract.
/// </summary>
/// <param name="Id">The stable semantic identity.</param>
/// <param name="Name">The display name.</param>
/// <param name="Properties">The command properties.</param>
/// <param name="Validations">The validation rules in behavior order.</param>
/// <param name="Produces">The produced events in behavior order.</param>
public sealed record SemanticCommand(
    SemanticId Id,
    string Name,
    ImmutableArray<SemanticProperty> Properties,
    ImmutableArray<SemanticValidationRule> Validations,
    ImmutableArray<SemanticProducedEvent> Produces);

/// <summary>
/// Represents a keyed read model.
/// </summary>
/// <param name="Id">The stable semantic identity.</param>
/// <param name="Name">The display name.</param>
/// <param name="Properties">The read model properties.</param>
public sealed record SemanticReadModel(SemanticId Id, string Name, ImmutableArray<SemanticProperty> Properties);

/// <summary>
/// Represents the deterministic identity of read model instances affected by a transition.
/// </summary>
/// <param name="Cardinality">The affected cardinality.</param>
/// <param name="Key">The expression producing the affected key or keys.</param>
public sealed record SemanticAffectedInstance(AffectedInstanceCardinality Cardinality, SemanticExpression Key);

/// <summary>
/// Represents one event-driven projection transition.
/// </summary>
/// <param name="EventContract">The event contract that causes the transition.</param>
/// <param name="AffectedInstance">The affected read model identity.</param>
/// <param name="Mappings">The state mappings in behavior order.</param>
public sealed record SemanticProjectionTransition(
    EventContractId EventContract,
    SemanticAffectedInstance AffectedInstance,
    ImmutableArray<SemanticPropertyMapping> Mappings);

/// <summary>
/// Represents a portable projection into one read model.
/// </summary>
/// <param name="Id">The stable semantic identity.</param>
/// <param name="Name">The display name.</param>
/// <param name="ReadModel">The target read model identity.</param>
/// <param name="Transitions">The transitions in behavior order.</param>
public sealed record SemanticProjection(
    SemanticId Id,
    string Name,
    SemanticId ReadModel,
    ImmutableArray<SemanticProjectionTransition> Transitions);

/// <summary>
/// Represents a deterministic keyed query.
/// </summary>
/// <param name="Id">The stable semantic identity.</param>
/// <param name="Name">The display name.</param>
/// <param name="Argument">The typed key argument.</param>
/// <param name="ReadModel">The queried read model identity.</param>
/// <param name="KeyProperty">The read model property matched by the key.</param>
/// <param name="Cardinality">The result cardinality.</param>
/// <param name="Delivery">The result delivery behavior.</param>
public sealed record SemanticKeyedQuery(
    SemanticId Id,
    string Name,
    SemanticReadModelQueryArgument Argument,
    SemanticId ReadModel,
    SemanticId KeyProperty,
    SemanticQueryCardinality Cardinality,
    SemanticQueryDelivery Delivery);

/// <summary>
/// Represents a typed keyed-query argument.
/// </summary>
/// <param name="Name">The argument name.</param>
/// <param name="Type">The argument type.</param>
public sealed record SemanticReadModelQueryArgument(string Name, SemanticTypeReference Type);
