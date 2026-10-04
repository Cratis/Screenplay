// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Works out which clock triggers fall due as the clock advances.
/// </summary>
/// <remarks>
/// The clock moves from one instant to a later one; every clock trigger whose schedule falls after the first and at or
/// before the second is due exactly once per occurrence, in time order. An interval counts from the Unix epoch, and a
/// schedule is in UTC. Occurrences at the same instant run in the order of their reactions' semantic identities, then
/// of the triggers within a reaction. The reference does not model a scheduler's at-least-once delivery.
/// </remarks>
internal static class SemanticClock
{
    /// <summary>
    /// The most occurrences one advance of the clock may make due.
    /// </summary>
    internal const int MaximumOccurrences = 10_000;

    /// <summary>
    /// Finds the clock occurrences due between two instants.
    /// </summary>
    /// <param name="reactions">The reactions in their running order.</param>
    /// <param name="from">The instant the clock moves from, exclusive.</param>
    /// <param name="to">The instant the clock moves to, inclusive.</param>
    /// <param name="occurrences">The due occurrences in time order.</param>
    /// <returns><c>false</c> when more than <see cref="MaximumOccurrences"/> are due.</returns>
    public static bool TryFindDue(
        IEnumerable<SemanticReaction> reactions,
        DateTimeOffset from,
        DateTimeOffset to,
        out List<(DateTimeOffset At, SemanticReaction Reaction, SemanticReactionTrigger Trigger)> occurrences)
    {
        var due = new List<(DateTimeOffset At, int Order, SemanticReaction Reaction, SemanticReactionTrigger Trigger)>();
        var order = 0;
        foreach (var reaction in reactions)
        {
            foreach (var trigger in reaction.Triggers)
            {
                var position = order++;
                foreach (var at in Due(trigger, from, to))
                {
                    if (due.Count >= MaximumOccurrences)
                    {
                        occurrences = [];
                        return false;
                    }

                    due.Add((at, position, reaction, trigger));
                }
            }
        }

        occurrences = [.. due.OrderBy(value => value.At).ThenBy(value => value.Order).Select(value => (value.At, value.Reaction, value.Trigger))];
        return true;
    }

    static IEnumerable<DateTimeOffset> Due(SemanticReactionTrigger trigger, DateTimeOffset from, DateTimeOffset to)
    {
        if (trigger is { Kind: SemanticReactionTriggerKind.Interval, Every: { } every })
        {
            var start = from.ToUnixTimeSeconds() / every * every;
            for (var seconds = start; seconds <= to.ToUnixTimeSeconds(); seconds += every)
            {
                var at = DateTimeOffset.FromUnixTimeSeconds(seconds);
                if (at > from && at <= to)
                {
                    yield return at;
                }
            }

            yield break;
        }

        if (trigger is not { Kind: SemanticReactionTriggerKind.Schedule, At: { } secondOfDay })
        {
            yield break;
        }

        for (var day = from.UtcDateTime.Date; day <= to.UtcDateTime.Date; day = day.AddDays(1))
        {
            var at = new DateTimeOffset(day.AddSeconds(secondOfDay), TimeSpan.Zero);
            if (at > from && at <= to &&
                (trigger.OnDayOfWeek is null || (int)day.DayOfWeek == trigger.OnDayOfWeek) &&
                (trigger.OnDayOfMonth is null || day.Day == trigger.OnDayOfMonth))
            {
                yield return at;
            }
        }
    }
}
