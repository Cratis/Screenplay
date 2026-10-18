// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations.for_ObservationComparison.when_coverage_is_partial;

public class and_a_consumer_is_absent : given.a_compiled_model
{
    void Establish()
    {
        Reaction(mapped: true);
        _snapshot.Consumers = [];
        _snapshot.Coverage = _snapshot.Coverage with { StartsAtFirstEvent = false };
    }

    void Because() => _result = Compare();

    [Fact] void should_not_claim_drift_from_absence() => Finding(ObservationFindingKind.ConsumerNotRegistered).Outcome.ShouldEqual(ObservationOutcome.Unknown);
    [Fact] void should_explain_the_partial_coverage() => Finding(ObservationFindingKind.ConsumerNotRegistered).Reason.ShouldEqual(ObservationUnknownReason.PartialCoverage);
}
