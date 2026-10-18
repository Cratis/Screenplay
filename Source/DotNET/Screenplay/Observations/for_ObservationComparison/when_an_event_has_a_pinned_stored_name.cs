// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations.for_ObservationComparison;

public class when_an_event_has_a_pinned_stored_name : given.a_compiled_model
{
    void Establish() => Model(Source.Replace("event Registered\n", "event Registered\n        id \"OldRegistered\"\n", StringComparison.Ordinal));
    void Because() => _result = Compare();

    [Fact] void should_compare_using_the_pin_not_the_display_name() => Finding(ObservationFindingKind.EventNotRegistered).EventTypeId.ShouldEqual("OldRegistered");
    [Fact] void should_not_silently_match_the_display_name() => Finding(ObservationFindingKind.UndeclaredEventAppended).EventTypeId.ShouldEqual("Registered");
    [Fact] void should_keep_the_pin_in_slice_evidence() => _result.Slices.Single().Events[0].TypeId.ShouldEqual("OldRegistered");
}
