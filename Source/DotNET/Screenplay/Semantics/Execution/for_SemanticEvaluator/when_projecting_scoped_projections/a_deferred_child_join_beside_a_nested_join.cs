// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections;

// Root-nested scopes do not defer. Exercise the deferred backfill entry point with a child join while the same
// parent-creation event backfills a root-nested join; nested joins under children remain refused by admission.
public class a_deferred_child_join_beside_a_nested_join : given.a_nested_join_projection
{
    void Establish() => Plan(NestedJoin + """

        children items identified by id
          from Attached key itemId
            parent rootId
            id = itemId
          join linked on id
            with Joined
              value = value
        """);

    void Because() => Project(
        Fact("Joined", "company", ("value", "nested value")),
        Fact("Joined", "item", ("value", "old child value")),
        Fact("Joined", "item", ("value", "child value")),
        Fact("Attached", "item", ("rootId", "root"), ("itemId", "item"), ("reference", "company")),
        Fact("Created", "root", ("reference", "company")));

    [Fact] void should_project() => _failure.ShouldBeNull();
    [Fact] void should_backfill_the_nested_join() => Member(Value("root", "details"), "Details", "value").ShouldEqual(SemanticValue.Text("nested value"));
    [Fact] void should_backfill_the_latest_joined_event_during_deferred_replay() =>
        Member(((SemanticArrayValue)Value("root", "items")).Values.Single(), "Item", "value").ShouldEqual(SemanticValue.Text("child value"));
    [Fact] void should_not_leak_either_joined_value_to_the_root() => Value("root", "value").ShouldBeOfExactType<SemanticNullValue>();
}
