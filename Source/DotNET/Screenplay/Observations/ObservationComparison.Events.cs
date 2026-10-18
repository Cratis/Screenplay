// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Observations;

internal sealed partial class ObservationComparisonRun
{
    void CompareEvents()
    {
        foreach (var contract in _index.Events.Values)
        {
            var type = CurrentType(contract.Id);
            if (contract.Origin is not null)
            {
                Add(ObservationFindingKind.ForeignEvent, ObservationOutcome.Excluded, "Foreign event contracts are excluded from local event comparison.", contract.Id, type);
            }
            else if (type is null || AmbiguousEvent(contract.Id))
            {
                Add(ObservationFindingKind.NotEstablished, ObservationOutcome.Unknown, "The event has no unambiguous stored type mapping.", contract.Id, type, reason: type is null ? ObservationUnknownReason.UnmappedConstruct : ObservationUnknownReason.AmbiguousIdentity);
            }
            else
            {
                CompareDeclaredEvent(contract, type);
            }
        }

        foreach (var type in snapshot.RegisteredEventTypes.Concat(snapshot.Appends.Where(value => value.Count > 0).Select(value => value.Type)).Distinct())
        {
            CompareObservedType(type);
        }

        if (!snapshot.Coverage.Complete)
        {
            Add(ObservationFindingKind.NotEstablished, ObservationOutcome.Unknown, "Only part of the sequence through the captured tail was examined.", reason: ObservationUnknownReason.PartialCoverage);
        }

        var missing = snapshot.Appends.Sum(value => value.Count) - snapshot.Edges.Sum(value => value.Events);
        if (missing > 0)
        {
            Add(ObservationFindingKind.NotEstablished, ObservationOutcome.Unknown, "Some examined events have no attributable innermost named producer.", count: missing, reason: ObservationUnknownReason.UnattributedEvents);
        }
    }

    void CompareDeclaredEvent(SemanticEventContract contract, ObservedEventType type)
    {
        if (!snapshot.RegisteredEventTypes.Contains(type))
        {
            Add(ObservationFindingKind.EventNotRegistered, ObservationOutcome.Drift, "The declared event generation is not registered.", contract.Id, type);
        }

        if (!snapshot.Appends.Any(value => value.Type == type && value.Count > 0))
        {
            Add(ObservationFindingKind.EventNotAppended, snapshot.Coverage.Complete ? ObservationOutcome.Informational : ObservationOutcome.Unknown, "No appends of the declared event generation were examined.", contract.Id, type, count: 0, reason: snapshot.Coverage.Complete ? null : ObservationUnknownReason.PartialCoverage);
        }
    }

    void CompareObservedType(ObservedEventType type)
    {
        var contracts = _index.Events.Values.Where(value => map.EventTypes.GetValueOrDefault(value.Id) == type.TypeId).ToArray();
        if (contracts.Any(value => value.Origin is not null) || contracts.Length > 1)
        {
            return;
        }

        var appended = snapshot.Appends.Where(value => value.Type == type).Sum(value => value.Count);
        var registered = snapshot.RegisteredEventTypes.Contains(type);
        var contract = contracts.SingleOrDefault();
        if (contract is null)
        {
            Add(
                appended > 0 ? ObservationFindingKind.UndeclaredEventAppended : ObservationFindingKind.UndeclaredEventRegistered,
                appended > 0 ? ObservationOutcome.Drift : ObservationOutcome.Informational,
                appended > 0 ? "An undeclared event type was appended within coverage." : "An undeclared event type is registered without examined appends.",
                type: type,
                count: appended);
        }
        else if (contract.Revision.Value != type.Generation && !contract.PriorRevisions.Any(value => value.Revision.Value == type.Generation))
        {
            Add(ObservationFindingKind.UndeclaredGeneration, ObservationOutcome.Drift, "A stored event generation is not declared by the model.", contract.Id, type, count: appended);
        }

        if (appended > 0 && !registered)
        {
            Add(ObservationFindingKind.AppendedWithoutRegistration, ObservationOutcome.Informational, "Examined historical appends have no matching registration.", contract?.Id, type, count: appended);
        }
    }
}
