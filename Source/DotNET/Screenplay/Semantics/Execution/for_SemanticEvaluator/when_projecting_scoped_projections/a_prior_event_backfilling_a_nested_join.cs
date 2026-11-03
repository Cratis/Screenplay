// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections;

// Chronicle 19.99.1's and_a_declaration_contains_a_nested_join backfills details.reference and details.value.
public class a_prior_event_backfilling_a_nested_join : given.a_nested_join_projection
{
    void Establish() => Plan(NestedJoin);

    void Because() => Project(
        Fact("Joined", "company", ("value", "old value")),
        Fact("Joined", "company", ("value", "joined value")),
        Fact("Joined", "other", ("value", "unrelated value")),
        Fact("Created", "root", ("reference", "company")));

    [Fact] void should_admit_the_projection() => _compilation.Issues.ShouldBeEmpty();
    [Fact] void should_project() => _failure.ShouldBeNull();
    [Fact] void should_backfill_the_latest_matching_event() => Member(Value("root", "details"), "Details", "value").ShouldEqual(SemanticValue.Text("joined value"));
    [Fact] void should_preserve_the_nested_reference() => Member(Value("root", "details"), "Details", "reference").ShouldEqual(SemanticValue.Text("company"));
    [Fact] void should_not_map_the_joined_value_at_the_root() => Value("root", "value").ShouldBeOfExactType<SemanticNullValue>();
    [Fact] void should_preserve_the_root_mapping() => Value("root", "label").ShouldEqual(SemanticValue.Text("company"));
    [Fact] void should_not_create_instances_for_the_joined_sources() => _instances.Length.ShouldEqual(1);
}
