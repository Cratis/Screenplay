// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations.for_ObservationComparison.when_a_reaction_is_mapped;

public class and_its_subscription_differs : given.a_compiled_model
{
    void Establish()
    {
        Reaction(mapped: true);
        _snapshot.Consumers = [_snapshot.Consumers[0] with { EventTypes = [Registered, Audited] }];
    }

    void Because() => _result = Compare();

    [Fact] void should_report_the_extra_subscription() => Finding(ObservationFindingKind.ConsumerMismatch).Outcome.ShouldEqual(ObservationOutcome.Drift);
    [Fact] void should_keep_the_slice_view_consistent() => _result.Slices.Single().Consumers.Single().Outcome.ShouldEqual(ObservationOutcome.Drift);
}
