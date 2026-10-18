// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations.for_ObservationComparison.when_a_declared_event_is_not_appended;

public class and_it_is_not_registered : given.a_compiled_model
{
    void Establish()
    {
        OnlyAudited();
        _snapshot.Registered = [Audited];
    }

    void Because() => _result = Compare();

    [Fact] void should_report_registration_drift() => Finding(ObservationFindingKind.EventNotRegistered).Outcome.ShouldEqual(ObservationOutcome.Drift);
    [Fact] void should_report_known_drift() => _result.HasDrift.ShouldEqual(true);
}
