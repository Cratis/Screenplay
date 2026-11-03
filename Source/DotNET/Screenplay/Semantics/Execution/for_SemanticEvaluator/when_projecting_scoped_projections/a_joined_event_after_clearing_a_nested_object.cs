// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections;

public class a_joined_event_after_clearing_a_nested_object : given.a_nested_join_projection
{
    void Establish() => Plan(NestedJoin + "\n  clear with Cleared");

    void Because() => Project(
        Fact("Joined", "company", ("value", "old value")),
        Fact("Created", "root", ("reference", "company")),
        Fact("Cleared", "root", ("reference", "company")),
        Fact("Joined", "company", ("value", "new value")));

    [Fact] void should_admit_the_projection() => _compilation.Issues.ShouldBeEmpty();
    [Fact] void should_project() => _failure.ShouldBeNull();
    [Fact] void should_not_recreate_the_cleared_nested_object() => Value("root", "details").ShouldBeOfExactType<SemanticNullValue>();
    [Fact] void should_preserve_the_root_instance() => _instances.Length.ShouldEqual(1);
    [Fact] void should_preserve_the_root_mapping() => Value("root", "label").ShouldEqual(SemanticValue.Text("company"));
}
