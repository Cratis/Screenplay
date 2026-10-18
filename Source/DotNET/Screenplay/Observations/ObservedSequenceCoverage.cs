// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Observations;

/// <summary>
/// Describes an inclusive sequence-number range.
/// </summary>
/// <param name="From">The first sequence number.</param>
/// <param name="To">The last sequence number.</param>
public sealed record ObservedRange(long From, long To);

/// <summary>
/// Describes the bounded scan, independently of event occurrence times.
/// </summary>
/// <param name="SequenceId">The scanned sequence identifier.</param>
/// <param name="StartsAtFirstEvent">Whether the scan begins at the first event.</param>
/// <param name="FirstExamined">The first examined sequence number, or null for no examined events.</param>
/// <param name="LastExamined">The last examined sequence number, or null for no examined events.</param>
/// <param name="CapturedTail">The captured tail, or null for an empty sequence.</param>
/// <param name="Unexamined">Inclusive gaps in the examined range.</param>
/// <param name="OccurredFrom">Informational occurrence-time lower bound.</param>
/// <param name="OccurredTo">Informational occurrence-time upper bound.</param>
public sealed record ObservedSequenceCoverage(
    string SequenceId,
    bool StartsAtFirstEvent,
    long? FirstExamined,
    long? LastExamined,
    long? CapturedTail,
    ImmutableArray<ObservedRange> Unexamined,
    DateTimeOffset? OccurredFrom = null,
    DateTimeOffset? OccurredTo = null)
{
    /// <summary>
    /// Gets whether every event through the captured tail was examined; an empty sequence is complete.
    /// </summary>
    public bool Complete => CapturedTail is null || (StartsAtFirstEvent && FirstExamined is not null && LastExamined == CapturedTail && Unexamined.IsEmpty);
}
