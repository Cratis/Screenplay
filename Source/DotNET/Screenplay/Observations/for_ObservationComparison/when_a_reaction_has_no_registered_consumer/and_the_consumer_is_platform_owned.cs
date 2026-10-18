// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations.for_ObservationComparison.when_a_reaction_has_no_registered_consumer;

public class and_the_consumer_is_platform_owned : given.a_compiled_model
{
    void Establish()
    {
        Reaction();
        _snapshot.Consumers = [_snapshot.Consumers[0] with { Owner = ObservedConsumerOwner.Platform }];
    }

    void Because() => _result = Compare();

    [Fact] void should_exclude_the_platform_consumer() => _result.Findings.Single(value => value.Reason == ObservationUnknownReason.PlatformConsumer).Outcome.ShouldEqual(ObservationOutcome.Excluded);
    [Fact] void should_not_count_it_as_an_application_consumer() => Finding(ObservationFindingKind.ConsumerNotRegistered).Outcome.ShouldEqual(ObservationOutcome.Drift);
}
