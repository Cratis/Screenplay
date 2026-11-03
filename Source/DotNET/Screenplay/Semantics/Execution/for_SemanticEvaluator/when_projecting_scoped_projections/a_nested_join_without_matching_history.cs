// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections;

public class a_nested_join_without_matching_history : given.a_nested_join_projection
{
    void Establish() => Plan(NestedJoin);

    void Because() => Project(
        Fact("Joined", "other", ("value", "unrelated value")),
        Fact("Created", "root", ("reference", "company")));

    [Fact] void should_project() => _failure.ShouldBeNull();
    [Fact] void should_not_backfill_an_unrelated_event() => HasMember(Value("root", "details"), "Details", "value").ShouldBeFalse();
    [Fact] void should_preserve_the_nested_reference() => Member(Value("root", "details"), "Details", "reference").ShouldEqual(SemanticValue.Text("company"));
}
