// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections;

public class a_recreated_nested_object_backfilling_a_join : given.a_nested_join_projection
{
    void Establish() => Plan(NestedJoin + "\n  clear with Cleared");

    void Because() => Project(
        Fact("Joined", "company", ("value", "old value")),
        Fact("Created", "root", ("reference", "company")),
        Fact("Cleared", "root", ("reference", "company")),
        Fact("Joined", "company", ("value", "new value")),
        Fact("Created", "root", ("reference", "company")));

    [Fact] void should_admit_the_projection() => _compilation.Issues.ShouldBeEmpty();
    [Fact] void should_project() => _failure.ShouldBeNull();
    [Fact] void should_restore_the_nested_reference() => Member(Value("root", "details"), "Details", "reference").ShouldEqual(SemanticValue.Text("company"));
    [Fact] void should_backfill_the_event_observed_while_the_nested_object_was_cleared() => Member(Value("root", "details"), "Details", "value").ShouldEqual(SemanticValue.Text("new value"));
    [Fact] void should_not_map_the_joined_value_at_the_root() => Value("root", "value").ShouldBeOfExactType<SemanticNullValue>();
    [Fact] void should_preserve_the_root_instance() => _instances.Length.ShouldEqual(1);
}
