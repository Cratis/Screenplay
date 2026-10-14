// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ClockScheduleVectors;

public class when_ordering_simultaneous_occurrences : Specification
{
    SemanticReaction[] _reactions;
    List<(DateTimeOffset At, SemanticReaction Reaction, SemanticReactionTrigger Trigger)> _due;
    bool _accepted;

    void Establish()
    {
        _reactions =
        [
            new(
                SemanticId.Parse("sem1:3a829ffd770e18dc85cc1ecca5c121cf437ad54d97113a1edc6983dccc6903a9"),
                "First",
                [new(SemanticReactionTriggerKind.Interval) { Every = 1 }, new(SemanticReactionTriggerKind.Interval) { Every = 2 }]),
            new(
                SemanticId.Parse("sem1:4a829ffd770e18dc85cc1ecca5c121cf437ad54d97113a1edc6983dccc6903a9"),
                "Second",
                [new(SemanticReactionTriggerKind.Interval) { Every = 2 }])
        ];
    }

    void Because() => _accepted = SemanticClock.TryFindDue(_reactions, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddSeconds(2), out _due);

    [Fact] void should_admit_the_advance() => _accepted.ShouldBeTrue();

    [Fact]
    void should_order_by_time_then_reaction_then_trigger()
    {
        (DateTimeOffset At, SemanticReaction Reaction, SemanticReactionTrigger Trigger)[] expected =
        [
            (DateTimeOffset.UnixEpoch.AddSeconds(1), _reactions[0], _reactions[0].Triggers[0]),
            (DateTimeOffset.UnixEpoch.AddSeconds(2), _reactions[0], _reactions[0].Triggers[0]),
            (DateTimeOffset.UnixEpoch.AddSeconds(2), _reactions[0], _reactions[0].Triggers[1]),
            (DateTimeOffset.UnixEpoch.AddSeconds(2), _reactions[1], _reactions[1].Triggers[0])
        ];
        _due.ShouldEqual(expected);
    }
}
