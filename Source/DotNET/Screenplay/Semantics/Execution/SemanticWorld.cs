// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Represents immutable portable event history and keyed read-model state.
/// </summary>
public sealed class SemanticWorld
{
    internal readonly ImmutableArray<SemanticScopedProjection.DeferredChild> PendingChildren;

    SemanticWorld(
        ImmutableArray<SemanticFact> facts,
        ImmutableArray<SemanticReadModelInstance> readModels,
        ImmutableArray<SemanticScopedProjection.DeferredChild> pending = default)
    {
        Facts = facts;
        ReadModels = readModels;
        PendingChildren = pending.IsDefault ? [] : pending;
    }

    /// <summary>
    /// Gets the empty world.
    /// </summary>
    public static SemanticWorld Empty { get; } = new([], []);

    /// <summary>
    /// Gets facts in committed occurrence order.
    /// </summary>
    public ImmutableArray<SemanticFact> Facts { get; }

    /// <summary>
    /// Gets keyed read-model state in deterministic semantic-identity/key order.
    /// </summary>
    public ImmutableArray<SemanticReadModelInstance> ReadModels { get; }

    /// <summary>
    /// Creates validated immutable world state.
    /// </summary>
    /// <param name="facts">The committed fact history.</param>
    /// <param name="readModels">The keyed read-model state.</param>
    /// <returns>The world.</returns>
    /// <exception cref="InvalidSemanticContract">The arrays or keyed state are malformed or ambiguous.</exception>
    public static SemanticWorld Create(
        ImmutableArray<SemanticFact> facts,
        ImmutableArray<SemanticReadModelInstance> readModels)
    {
        if (facts.IsDefault || readModels.IsDefault ||
            facts.Any(fact => !ValidFact(fact)) ||
            readModels.Any(readModel => readModel is not { ReadModel.IsSet: true, Key: not null } || !ValidValues(readModel.Values)))
        {
            throw new InvalidSemanticContract("Semantic world state is malformed.");
        }

        for (var first = 0; first < readModels.Length; first++)
        {
            for (var second = first + 1; second < readModels.Length; second++)
            {
                if (readModels[first].ReadModel == readModels[second].ReadModel &&
                    SemanticValueRules.AreEqual(readModels[first].Key, readModels[second].Key))
                {
                    throw new InvalidSemanticContract("Semantic world state contains a duplicate read-model key.");
                }
            }
        }

        return new(
            facts,
            [
                .. readModels
                    .OrderBy(_ => _.ReadModel.ToString(), StringComparer.Ordinal)
                    .ThenBy(_ => CanonicalKey(_.Key), StringComparer.Ordinal)
            ]);
    }

    internal SemanticWorld WithProjectionState(
        ImmutableArray<SemanticReadModelInstance> readModels,
        ImmutableArray<SemanticScopedProjection.DeferredChild> pending) =>
        new(Facts, Create([], readModels).ReadModels, pending);

    internal SemanticWorld Commit(
        ImmutableArray<SemanticFact> facts,
        ImmutableArray<SemanticReadModelInstance> readModels,
        ImmutableArray<SemanticScopedProjection.DeferredChild>? pending = null)
    {
        var committed = Create([.. Facts, .. facts], readModels);
        return new(committed.Facts, committed.ReadModels, pending ?? PendingChildren);
    }

    static bool ValidFact(SemanticFact fact) =>
        fact is { EventContract.IsSet: true, Destination: not null } &&
        ValidValues(fact.Values) && ValidEventSource(fact);

    static bool ValidEventSource(SemanticFact fact) =>
        fact.Context is not { } context ||
        (!context.EventSource.Type.IsCollection && !context.EventSource.Type.IsOptional &&
         SemanticValueRules.AreEqual(context.EventSource.Value, fact.Destination));

    static bool ValidValues(ImmutableArray<SemanticPropertyValue> values) =>
        !values.IsDefault && values.All(_ => _ is { TargetProperty.IsSet: true, Value: not null });

    static string CanonicalKey(SemanticValue value) => value switch
    {
        SemanticNullValue => "null",
        SemanticTextValue text => $"text:{Frame(text.Value)}",
        SemanticNumberValue number => $"number:{number.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
        SemanticBooleanValue boolean => $"boolean:{boolean.Value}",
        SemanticArrayValue array => $"array:[{string.Join(',', array.Values.Select(_ => Frame(CanonicalKey(_))))}]",
        SemanticCompositeValue composite => $"object:{{{string.Join(',', composite.Properties.OrderBy(_ => _.TargetProperty.ToString(), StringComparer.Ordinal).Select(_ => $"{_.TargetProperty}:{Frame(CanonicalKey(_.Value))}"))}}}",
        _ => throw new InvalidSemanticContract("Semantic world key contains an unsupported value variant.")
    };

    static string Frame(string value) => $"{value.Length}:{value}";
}
