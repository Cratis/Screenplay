// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_handling_possible_events;

public class with_valid_items : given.a_possible_events_handler
{
    SemanticAccepted _accepted = null!;

    void Establish() => Answer = SemanticCommandBodyResult.ProducedEvents([Item("Recorded", "B"), Item("Placed"), Item("Recorded", "B")]);
    void Because() => _accepted = (SemanticAccepted)Execute();

    [Fact] void should_preserve_returned_order() => _accepted.Facts.Select(fact => fact.EventContract).ShouldEqual(Answer.Items.Select(item => item.EventContract));
    [Fact] void should_preserve_destinations() => _accepted.Facts.Select(fact => fact.Destination).ShouldEqual([SemanticValue.Text("B"), SemanticValue.Text("First"), SemanticValue.Text("B")]);
    [Fact] void should_commit_the_batch() => Assert.Equivalent(_accepted.Facts, _accepted.World.Facts, strict: true);
    [Fact] void should_fill_optional_payload_fields() => _accepted.Facts[1].Values[^1].Value.ShouldEqual(SemanticValue.Null);
    [Fact] void should_keep_event_and_production_tags() => _accepted.Facts[1].Tags.Length.ShouldEqual(2);
    [Fact] void should_keep_occurrence() => _accepted.Facts.All(fact => fact.Occurred == Request.Occurrence.Occurred).ShouldBeTrue();
    [Fact] void should_use_element_types_for_one_of_destinations() => _accepted.Facts[0].Context.EventSource.Type.IsCollection.ShouldBeFalse();
}
