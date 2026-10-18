// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations.for_ObservationComparison.when_a_reaction_has_no_registered_consumer;

public class and_no_application_consumer_exists : given.a_compiled_model
{
    void Establish()
    {
        Reaction();
        _snapshot.Consumers = [];
    }

    void Because() => _result = Compare();

    [Fact] void should_report_missing_consumer_drift_without_a_mapping() => Finding(ObservationFindingKind.ConsumerNotRegistered).Outcome.ShouldEqual(ObservationOutcome.Drift);
    [Fact] void should_also_disclose_the_unmapped_reaction() => _result.Findings.Any(value => value.Reason == ObservationUnknownReason.UnmappedConstruct).ShouldBeTrue();
    [Fact] void should_keep_per_slice_consumer_evidence() => _result.Slices.Single().Consumers.Single().Outcome.ShouldEqual(ObservationOutcome.Drift);
}
