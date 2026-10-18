// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations.for_ObservationComparison;

public class when_an_event_is_appended_without_registration : given.a_compiled_model
{
    void Establish() => _snapshot.Registered = [Audited];
    void Because() => _result = Compare();

    [Fact] void should_preserve_historical_evidence() => Finding(ObservationFindingKind.AppendedWithoutRegistration).Outcome.ShouldEqual(ObservationOutcome.Informational);
    [Fact] void should_also_report_the_missing_current_registration() => Finding(ObservationFindingKind.EventNotRegistered).EventTypeId.ShouldEqual("Registered");
}
