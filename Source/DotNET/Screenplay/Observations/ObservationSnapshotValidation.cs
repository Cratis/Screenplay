// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Observations;

static class ObservationSnapshotValidation
{
    internal static void Validate(ObservedSequenceCoverage coverage, ImmutableArray<ObservedEventType> registered, ImmutableArray<ObservedAppend> appends, ImmutableArray<ObservedEdge> edges, ImmutableArray<UnattributedAppend> unattributed, ImmutableArray<ObservedConsumer> consumers)
    {
        RequireUnique(registered);
        RequireUnique(appends.Select(value => value.Type));
        RequireUnique(edges.Select(value => (value.Producer, value.Type)));
        RequireUnique(unattributed.Select(value => value.Type));
        RequireUnique(consumers.Select(value => (value.Identifier, value.SequenceId)));
        foreach (var consumer in consumers)
        {
            RequireUnique(consumer.EventTypes);
            if (consumer.NextSequenceNumber < 0 || consumer.LastHandledSequenceNumber < 0 || !Enum.IsDefined(consumer.Kind) || !Enum.IsDefined(consumer.Owner))
            {
                throw new InvalidObservationSnapshot("Consumer positions and kinds must be valid.");
            }
        }

        if (appends.Any(value => value.Count < 0) || edges.Any(value => value.Events < 0 || !Enum.IsDefined(value.Producer.Kind)) || unattributed.Any(value => value.Count < 0))
        {
            throw new InvalidObservationSnapshot("Event counts must be non-negative and producer kinds must be valid.");
        }

        ValidateRange(coverage);
        ValidateCounts(coverage, appends, edges, unattributed);
    }

    static void RequireUnique<T>(IEnumerable<T> values)
    {
        if (values.GroupBy(value => value).Any(group => group.Count() > 1))
        {
            throw new InvalidObservationSnapshot("Observation entries must have unique keys.");
        }
    }

    static void ValidateCounts(ObservedSequenceCoverage coverage, ImmutableArray<ObservedAppend> appends, ImmutableArray<ObservedEdge> edges, ImmutableArray<UnattributedAppend> unattributed)
    {
        var counts = appends.ToDictionary(value => value.Type, value => (decimal)value.Count);
        var attributed = edges.GroupBy(value => value.Type).ToDictionary(group => group.Key, group => group.Sum(value => (decimal)value.Events));
        var missing = unattributed.ToDictionary(value => value.Type, value => (decimal)value.Count);
        foreach (var type in attributed.Keys.Concat(missing.Keys).Distinct())
        {
            if (attributed.GetValueOrDefault(type) + missing.GetValueOrDefault(type) > counts.GetValueOrDefault(type))
            {
                throw new InvalidObservationSnapshot("Attribution counts must not exceed append counts for a generation.");
            }
        }

        if (counts.Values.Sum() > long.MaxValue || (coverage.FirstExamined is null && counts.Values.Any(value => value > 0)))
        {
            throw new InvalidObservationSnapshot("Append totals must fit a signed count and require an examined range.");
        }
    }

    static void ValidateRange(ObservedSequenceCoverage coverage)
    {
        var first = coverage.FirstExamined;
        var last = coverage.LastExamined;
        var tail = coverage.CapturedTail;
        if (first < 0 || last < 0 || tail < 0 || (first is null) != (last is null) || first > last || last > tail ||
            (tail is null && (first is not null || !coverage.Unexamined.IsEmpty)))
        {
            throw new InvalidObservationSnapshot("Examined bounds must be paired, ordered, non-negative and no later than the captured tail.");
        }

        long? previousEnd = null;
        foreach (var range in coverage.Unexamined)
        {
            if (first is null || range.From < first || range.To > last || range.From > range.To || range.From <= previousEnd)
            {
                throw new InvalidObservationSnapshot("Unexamined ranges must be disjoint ordered gaps inside the examined bounds.");
            }

            previousEnd = range.To;
        }
    }
}
