// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Observations;

/// <summary>
/// Maps model declarations to host identities without imposing a rendering convention.
/// </summary>
public sealed class ObservationIdentityMap
{
    ObservationIdentityMap(ImmutableDictionary<SemanticId, string> events, ImmutableDictionary<SemanticId, ObservedProducer> producers, ImmutableDictionary<SemanticId, string> consumers, ImmutableHashSet<SemanticId> ambiguous)
    {
        EventTypes = events;
        Producers = producers;
        Consumers = consumers;
        AmbiguousConstructs = ambiguous;
    }

    /// <summary>
    /// Gets stored type identifiers by event declaration identity.
    /// </summary>
    public ImmutableDictionary<SemanticId, string> EventTypes { get; }

    /// <summary>
    /// Gets producer identifiers by command or reaction declaration identity.
    /// </summary>
    public ImmutableDictionary<SemanticId, ObservedProducer> Producers { get; }

    /// <summary>
    /// Gets consumer identifiers by reaction declaration identity.
    /// </summary>
    public ImmutableDictionary<SemanticId, string> Consumers { get; }

    /// <summary>
    /// Gets command declarations whose default name is not unique.
    /// </summary>
    public ImmutableHashSet<SemanticId> AmbiguousConstructs { get; }

    /// <summary>
    /// Maps events by stored name and commands by unique name. Reactions remain unmapped.
    /// </summary>
    /// <param name="model">The executable model.</param>
    /// <returns>The render-neutral identity map.</returns>
    public static ObservationIdentityMap FromModel(ExecutableSemanticModel model)
    {
        var slices = ObservationModelIndex.Slices(model.Application).ToArray();
        var commands = slices.SelectMany(slice => slice.Commands).ToArray();
        var unique = commands.GroupBy(command => command.Name, StringComparer.Ordinal).Where(group => group.Count() == 1).Select(group => group.Single()).ToArray();

        return new(
            slices.SelectMany(slice => slice.Events).ToImmutableDictionary(value => value.Id, value => value.StoredName ?? value.Name),
            unique.ToImmutableDictionary(command => command.Id, command => new ObservedProducer(ObservedProducerKind.Command, command.Name)),
            ImmutableDictionary.Create<SemanticId, string>(EqualityComparer<SemanticId>.Default),
            [.. commands.Except(unique).Select(command => command.Id)]);
    }

    /// <summary>
    /// Returns a new map with explicit host identities for a declaration; unspecified mappings remain unchanged.
    /// </summary>
    /// <param name="construct">The declaration identity.</param>
    /// <param name="eventTypeId">An optional stored event type override.</param>
    /// <param name="producer">An optional producer override.</param>
    /// <param name="consumerIdentifier">An optional reaction consumer override.</param>
    /// <returns>The independently immutable map.</returns>
    public ObservationIdentityMap With(SemanticId construct, string? eventTypeId = null, ObservedProducer? producer = null, string? consumerIdentifier = null) => new(
        eventTypeId is null ? EventTypes : EventTypes.SetItem(construct, eventTypeId),
        producer is null ? Producers : Producers.SetItem(construct, producer),
        consumerIdentifier is null ? Consumers : Consumers.SetItem(construct, consumerIdentifier),
        producer is null ? AmbiguousConstructs : AmbiguousConstructs.Remove(construct));
}
