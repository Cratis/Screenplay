// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations.for_ObservationComparison.when_coverage_is_partial;

public class and_an_event_is_not_registered : given.a_compiled_model
{
    void Establish()
    {
        _snapshot.Registered = [Audited];
        _snapshot.Coverage = _snapshot.Coverage with { StartsAtFirstEvent = false };
    }

    void Because() => _result = Compare();

    [Fact] void should_still_compare_the_whole_registry() => Finding(ObservationFindingKind.EventNotRegistered).Outcome.ShouldEqual(ObservationOutcome.Drift);
    [Fact] void should_preserve_known_drift() => _result.HasDrift.ShouldEqual(true);
    [Fact] void should_not_claim_a_complete_comparison() => _result.Complete.ShouldBeFalse();
}
