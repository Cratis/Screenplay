// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections;

public class a_join_inside_a_deeper_root_nested_object : given.a_nested_join_projection
{
    void Establish() => Plan("""
        from Created
          label = reference
        nested envelope
          nested details
            from Created
              reference = reference
            join linked on reference
              with Joined
                value = value
        """);

    void Because() => Project(
        Fact("Joined", "company", ("value", "joined value")),
        Fact("Created", "root", ("reference", "company")));

    [Fact] void should_admit_the_projection() => _compilation.Issues.ShouldBeEmpty();
    [Fact] void should_project() => _failure.ShouldBeNull();
    [Fact] void should_backfill_inside_the_full_nested_path() =>
        Member(Member(Value("root", "envelope"), "Envelope", "details"), "Details", "value").ShouldEqual(SemanticValue.Text("joined value"));
}
