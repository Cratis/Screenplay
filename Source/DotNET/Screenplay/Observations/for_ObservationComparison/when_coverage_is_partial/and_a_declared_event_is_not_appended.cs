// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations.for_ObservationComparison.when_coverage_is_partial;

public class and_a_declared_event_is_not_appended : given.a_compiled_model
{
    void Establish()
    {
        OnlyAudited();
        _snapshot.Coverage = _snapshot.Coverage with { StartsAtFirstEvent = false };
    }

    void Because() => _result = Compare();

    [Fact] void should_not_claim_absence() => Finding(ObservationFindingKind.EventNotAppended).Outcome.ShouldEqual(ObservationOutcome.Unknown);
    [Fact] void should_explain_partial_coverage() => Finding(ObservationFindingKind.DeclaredEdgeNotObserved).Reason.ShouldEqual(ObservationUnknownReason.PartialCoverage);
    [Fact] void should_leave_the_verdict_unknown() => _result.HasDrift.ShouldBeNull();
    [Fact] void should_be_incomplete() => _result.Complete.ShouldBeFalse();
}
