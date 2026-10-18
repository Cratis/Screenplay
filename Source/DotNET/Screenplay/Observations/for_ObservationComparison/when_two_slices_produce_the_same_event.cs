// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations.for_ObservationComparison;

public class when_two_slices_produce_the_same_event : given.a_compiled_model
{
    void Establish()
    {
        Model(Source + "\n    slice StateChange Another\n      command Update\n        produces Registered\n          name = \"another\"\n");
        _snapshot.Appends = [new(Registered, 4), new(Audited, 1)];
        _snapshot.Edges.Add(new(new(ObservedProducerKind.Command, "Update"), Registered, 3));
    }

    void Because() => _result = Compare();

    [Fact] void should_count_appends_only_once() => _result.Coverage.AppendedEvents.ShouldEqual(5);
    [Fact] void should_keep_append_counts_in_the_owning_slice() => _result.Slices[0].Events[0].GenerationCounts.Single().Count.ShouldEqual(4);
    [Fact] void should_not_duplicate_owned_events() => _result.Slices[1].Events.ShouldBeEmpty();
    [Fact] void should_show_only_the_second_producers_attributed_count() => _result.Slices[1].Producers.Single().Edges.Single().AttributedEvents.ShouldEqual(3);
    [Fact] void should_have_no_drift() => _result.HasDrift.ShouldEqual(false);
}
