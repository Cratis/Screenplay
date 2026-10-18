// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations.for_ObservationComparison;

public class when_an_undeclared_type_is_only_registered : given.a_compiled_model
{
    void Establish() => _snapshot.Registered.Add(new("Unused", 1));
    void Because() => _result = Compare();

    [Fact] void should_report_registration_as_information() => Finding(ObservationFindingKind.UndeclaredEventRegistered).Outcome.ShouldEqual(ObservationOutcome.Informational);
    [Fact] void should_not_report_drift() => _result.HasDrift.ShouldEqual(false);
}
