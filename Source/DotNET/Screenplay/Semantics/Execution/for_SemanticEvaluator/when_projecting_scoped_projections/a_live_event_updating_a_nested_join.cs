// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections;

public class a_live_event_updating_a_nested_join : given.a_nested_join_projection
{
    void Establish() => Plan(NestedJoin);

    void Because() => Project(
        Fact("Created", "first", ("reference", "company")),
        Fact("Created", "second", ("reference", "other")),
        Fact("Joined", "company", ("value", "joined value")));

    [Fact] void should_project() => _failure.ShouldBeNull();
    [Fact] void should_update_the_matching_nested_object() => Member(Value("first", "details"), "Details", "value").ShouldEqual(SemanticValue.Text("joined value"));
    [Fact] void should_leave_other_nested_objects_unchanged() => HasMember(Value("second", "details"), "Details", "value").ShouldBeFalse();
    [Fact] void should_not_create_a_joined_instance() => _instances.Length.ShouldEqual(2);
    [Fact] void should_not_map_the_joined_value_at_the_root() => Value("first", "value").ShouldBeOfExactType<SemanticNullValue>();
}
