// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations.for_ObservationComparison;

public class when_an_event_is_foreign : given.a_compiled_model
{
    void Establish()
    {
        Model("module Projects\n  feature Registration\n    slice Automation Observe\n      event Imported from \"OtherStore\"\n        name String\n");
        _snapshot.Registered = [];
        _snapshot.Appends = [];
        _snapshot.Edges = [];
    }

    void Because() => _result = Compare();

    [Fact] void should_exclude_the_foreign_event() => Finding(ObservationFindingKind.ForeignEvent).Outcome.ShouldEqual(ObservationOutcome.Excluded);
    [Fact] void should_not_require_local_registration() => _result.Findings.Any(value => value.Kind == ObservationFindingKind.EventNotRegistered).ShouldBeFalse();
    [Fact] void should_not_claim_foreign_absence_is_drift() => _result.HasDrift.ShouldEqual(false);
}
