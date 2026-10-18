// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Observations;

/// <summary>
/// Compares declared event relationships with bounded host-supplied evidence, without performing IO.
/// </summary>
public static class ObservationComparison
{
    /// <summary>
    /// Compares a compiled file or folder model against one observed sequence.
    /// </summary>
    /// <param name="compilation">The validated semantic compilation.</param>
    /// <param name="snapshot">Immutable host-supplied evidence.</param>
    /// <param name="map">Explicit host identity mappings, or null to use model defaults.</param>
    /// <returns>Typed findings, coverage and per-slice evidence.</returns>
    public static ObservationReport Compare(SemanticCompilation compilation, ObservationSnapshot snapshot, ObservationIdentityMap? map = null) =>
        new ObservationComparisonRun(compilation, snapshot, map ?? ObservationIdentityMap.FromModel(compilation.Model)).Compare();
}

internal sealed partial class ObservationComparisonRun(SemanticCompilation compilation, ObservationSnapshot snapshot, ObservationIdentityMap map)
{
    readonly ObservationModelIndex _index = new(compilation);
    readonly List<ObservationFinding> _findings = [];
    readonly Dictionary<SemanticId, List<SliceEdgeObservation>> _edges = [];
    readonly Dictionary<SemanticId, List<SliceConsumerObservation>> _consumers = [];

    internal ObservationReport Compare()
    {
        CompareEvents();
        CompareConsumers();
        CompareEdges();
        var findings = OrderFindings();
        var complete = snapshot.Coverage.Complete && findings.All(value => value.Outcome != ObservationOutcome.Unknown);
        var appends = snapshot.Appends.Sum(value => value.Count);
        var attributed = snapshot.Edges.Sum(value => value.Events);
        var coverage = new ObservationCoverage(snapshot.EventStore, snapshot.Namespace, snapshot.Coverage, snapshot.CapturedAt, appends, attributed, appends - attributed);

        bool? hasDrift = complete ? false : null;
        if (findings.Any(value => value.Outcome == ObservationOutcome.Drift)) hasDrift = true;

        return new(
            hasDrift,
            complete,
            coverage,
            findings,
            SliceViews(),
            ["This comparison does not establish behavioral correctness, payload agreement or specification execution.",
             "Counts describe events within the examined sequence coverage, never producer invocations or proof that a producer never ran.",
             "Projection and reducer consumer semantics, external effects and other event sequences are not compared."]);
    }

    void Add(ObservationFindingKind kind, ObservationOutcome outcome, string statement, SemanticId? construct = null, ObservedEventType? type = null, ObservedProducer? producer = null, string? consumer = null, long? count = null, ObservationUnknownReason? reason = null, string? sequence = null) =>
        _findings.Add(new(kind, outcome, statement, reason, construct, construct is { } id ? _index.Address(id) : null, construct is { } owner ? _index.Owner(owner) : null, type?.TypeId, type?.Generation, producer, consumer, sequence ?? snapshot.Coverage.SequenceId, count));

    ObservedEventType? CurrentType(SemanticId id) => map.EventTypes.TryGetValue(id, out var type) && _index.Events.TryGetValue(id, out var contract)
        ? new(type, contract.Revision.Value) : null;

    bool Foreign(SemanticId id) => _index.Events.TryGetValue(id, out var contract) && contract.Origin is not null;

    bool AmbiguousEvent(SemanticId id) => map.EventTypes.TryGetValue(id, out var type) &&
        _index.Events.Keys.Count(key => map.EventTypes.GetValueOrDefault(key) == type) > 1;

    bool AmbiguousProducer(SemanticId id) => map.AmbiguousConstructs.Contains(id) ||
        (map.Producers.TryGetValue(id, out var producer) && map.Producers.Count(value => value.Value == producer) > 1);

    long EdgeCount(ObservedProducer? producer, ObservedEventType? type) => producer is null || type is null ? 0 :
        snapshot.Edges.Where(value => value.Producer == producer && value.Type.TypeId == type.TypeId).Sum(value => value.Events);

    bool HasUnattributed(ObservedEventType type) => snapshot.Appends.Where(value => value.Type.TypeId == type.TypeId).Sum(value => value.Count) >
        snapshot.Edges.Where(value => value.Type.TypeId == type.TypeId).Sum(value => value.Events);

    ImmutableArray<ObservationFinding> OrderFindings() => [.. _findings.OrderBy(value => _index.Position(value.Construct))
        .ThenBy(value => value.Kind).ThenBy(value => value.EventTypeId, StringComparer.Ordinal).ThenBy(value => value.Generation)
        .ThenBy(value => value.Producer?.Kind).ThenBy(value => value.Producer?.Identifier, StringComparer.Ordinal)
        .ThenBy(value => value.Consumer, StringComparer.Ordinal).ThenBy(value => value.SequenceId, StringComparer.Ordinal)
        .ThenBy(value => value.Reason)];
}
