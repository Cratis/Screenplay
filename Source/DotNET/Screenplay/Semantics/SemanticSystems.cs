// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// What a retry may do when an ability invocation's outcome is unknown (ESM v50, decision 0159).
/// </summary>
public enum SemanticAbilityEffect
{
    /// <summary>No external effect, such as a lookup; retry freely.</summary>
    Safe,

    /// <summary>Repeating with the same idempotency key has the effect once; retry with the same key.</summary>
    Idempotent,

    /// <summary>Repeating may repeat the effect; never retry, stop for reconciliation. The default.</summary>
    Once
}

/// <summary>
/// How an ability is realized (ESM v50). Extension packages and built-in realization blocks are realization metadata and stay outside the model.
/// </summary>
public enum SemanticAbilityRealizationKind
{
    /// <summary>The ability is intent only; its invocations are specified but nothing carries them out.</summary>
    None,

    /// <summary>The ability carries an <c>AbilityEffect</c> implementation attachment, identified by the realization's requirement id.</summary>
    Code,

    /// <summary>An extension package realizes the ability; reserved until extension realization is admitted (#1253).</summary>
    Extension
}

/// <summary>
/// A typed value an ability takes or returns.
/// </summary>
/// <param name="Name">The field name.</param>
/// <param name="Type">The field type.</param>
public sealed record SemanticAbilityField(string Name, SemanticTypeReference Type);

/// <summary>
/// How an ability is realized.
/// </summary>
/// <param name="Kind">The realization kind.</param>
/// <param name="RequirementId">The implementation requirement of a <see cref="SemanticAbilityRealizationKind.Code"/> realization; otherwise null.</param>
public sealed record SemanticAbilityRealization(SemanticAbilityRealizationKind Kind, string? RequirementId);

/// <summary>
/// An ability of an external system: a typed contract with inputs, returned values, named outcomes and an effect class.
/// </summary>
/// <param name="Name">The ability name, unique within its system ignoring case.</param>
/// <param name="Effect">The effect class.</param>
/// <param name="Inputs">The typed inputs.</param>
/// <param name="Returns">The typed returned values, empty when the ability returns nothing.</param>
/// <param name="Outcomes">The named business outcomes other than success; they carry no payload.</param>
/// <param name="AnswersLater">Whether the answer arrives after the invocation returns.</param>
/// <param name="Realization">How the ability is realized.</param>
public sealed record SemanticAbility(
    string Name,
    SemanticAbilityEffect Effect,
    ImmutableArray<SemanticAbilityField> Inputs,
    ImmutableArray<SemanticAbilityField> Returns,
    ImmutableArray<string> Outcomes,
    bool AnswersLater,
    SemanticAbilityRealization Realization);

/// <summary>
/// An external system and the abilities it can be asked to perform.
/// </summary>
/// <param name="Id">The stable semantic identity.</param>
/// <param name="Name">The system name.</param>
/// <param name="Abilities">The abilities, in declaration order.</param>
public sealed record SemanticSystem(SemanticId Id, string Name, ImmutableArray<SemanticAbility> Abilities);
