// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations.for_ObservationComparison.when_a_declared_event_is_not_appended;

public class and_it_is_registered : given.a_compiled_model
{
    void Establish() => OnlyAudited();
    void Because() => _result = Compare();

    [Fact] void should_explain_absence_as_information() => Finding(ObservationFindingKind.EventNotAppended).Outcome.ShouldEqual(ObservationOutcome.Informational);
    [Fact] void should_not_report_missing_registration() => _result.Findings.Any(value => value.Kind == ObservationFindingKind.EventNotRegistered).ShouldBeFalse();
}
