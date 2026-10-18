// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Observations;

/// <summary>
/// Holds immutable, sorted host-supplied evidence for one store, namespace and sequence. Contains no event contents.
/// </summary>
public sealed class ObservationSnapshot
{
    ObservationSnapshot(string eventStore, string @namespace, ObservedSequenceCoverage coverage, DateTimeOffset capturedAt, ImmutableArray<ObservedEventType> registered, ImmutableArray<ObservedAppend> appends, ImmutableArray<ObservedEdge> edges, ImmutableArray<UnattributedAppend> unattributed, ImmutableArray<ObservedConsumer> consumers)
    {
        EventStore = eventStore;
        Namespace = @namespace;
        Coverage = coverage;
        CapturedAt = capturedAt;
        RegisteredEventTypes = registered;
        Appends = appends;
        Edges = edges;
        UnattributedAppends = unattributed;
        Consumers = consumers;
    }

    /// <summary>
    /// Gets the store identifier.
    /// </summary>
    public string EventStore { get; }

    /// <summary>
    /// Gets the namespace identifier.
    /// </summary>
    public string Namespace { get; }

    /// <summary>
    /// Gets the scan boundaries.
    /// </summary>
    public ObservedSequenceCoverage Coverage { get; }

    /// <summary>
    /// Gets the host's capture time.
    /// </summary>
    public DateTimeOffset CapturedAt { get; }

    /// <summary>
    /// Gets the whole registry, independent of scan boundaries.
    /// </summary>
    public ImmutableArray<ObservedEventType> RegisteredEventTypes { get; }

    /// <summary>
    /// Gets examined append counts.
    /// </summary>
    public ImmutableArray<ObservedAppend> Appends { get; }

    /// <summary>
    /// Gets attributed event counts.
    /// </summary>
    public ImmutableArray<ObservedEdge> Edges { get; }

    /// <summary>
    /// Gets explicitly unattributed event counts.
    /// </summary>
    public ImmutableArray<UnattributedAppend> UnattributedAppends { get; }

    /// <summary>
    /// Gets registered subscriptions, including subscriptions on other sequences.
    /// </summary>
    public ImmutableArray<ObservedConsumer> Consumers { get; }

    /// <summary>
    /// Validates and sorts evidence. Missing attribution is treated conservatively by the comparator.
    /// </summary>
    /// <param name="eventStore">The store identifier.</param>
    /// <param name="namespace">The namespace identifier.</param>
    /// <param name="coverage">The scanned sequence and bounds.</param>
    /// <param name="registeredEventTypes">The whole event registry.</param>
    /// <param name="appends">Examined append counts.</param>
    /// <param name="edges">Innermost named producer counts.</param>
    /// <param name="unattributedAppends">Counts without attributable producers.</param>
    /// <param name="consumers">The registered subscriptions.</param>
    /// <param name="capturedAt">The capture time.</param>
    /// <returns>The immutable sorted snapshot.</returns>
    /// <exception cref="InvalidObservationSnapshot">Evidence contains duplicates, invalid ranges or inconsistent counts.</exception>
    public static ObservationSnapshot Create(string eventStore, string @namespace, ObservedSequenceCoverage coverage, IEnumerable<ObservedEventType> registeredEventTypes, IEnumerable<ObservedAppend> appends, IEnumerable<ObservedEdge> edges, IEnumerable<UnattributedAppend> unattributedAppends, IEnumerable<ObservedConsumer> consumers, DateTimeOffset capturedAt)
    {
        var registered = SortTypes(registeredEventTypes);
        var appended = appends.OrderBy(value => value.Type.TypeId, StringComparer.Ordinal).ThenBy(value => value.Type.Generation).ToImmutableArray();
        var attributed = edges.OrderBy(value => value.Producer.Kind).ThenBy(value => value.Producer.Identifier, StringComparer.Ordinal)
            .ThenBy(value => value.Type.TypeId, StringComparer.Ordinal).ThenBy(value => value.Type.Generation).ToImmutableArray();
        var unattributed = unattributedAppends.OrderBy(value => value.Type.TypeId, StringComparer.Ordinal).ThenBy(value => value.Type.Generation).ToImmutableArray();
        var subscriptions = consumers.Select(value => value with { EventTypes = SortTypes(value.EventTypes) })
            .OrderBy(value => value.Identifier, StringComparer.Ordinal).ThenBy(value => value.SequenceId, StringComparer.Ordinal).ToImmutableArray();
        var sortedCoverage = coverage with { Unexamined = [.. coverage.Unexamined.OrderBy(value => value.From).ThenBy(value => value.To)] };
        ObservationSnapshotValidation.Validate(sortedCoverage, registered, appended, attributed, unattributed, subscriptions);

        return new(eventStore, @namespace, sortedCoverage, capturedAt, registered, appended, attributed, unattributed, subscriptions);
    }

    internal static ImmutableArray<ObservedEventType> SortTypes(IEnumerable<ObservedEventType> values) =>
        [.. values.OrderBy(value => value.TypeId, StringComparer.Ordinal).ThenBy(value => value.Generation)];
}
