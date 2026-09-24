// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner;

public class when_proving_a_collision_between_two_event_sources : Execution.given.a_constrained_plan
{
    SemanticSpecificationRun _run = null!;

    void Because() => _run = new SemanticSpecificationRunner().Run(
        _plan,
        _plan.Specifications.Values.Single(value => value.Name == "CodeCollidesAcrossEventSources").Id);

    [Fact] void should_pass_the_two_source_specification() => _run.Passed.ShouldBeTrue();
    [Fact] void should_reject_the_other_sources_duplicate_value() => ((SemanticRejected)_run.Execution).Code.ShouldEqual("ProjectCodeIsUnique");
    [Fact] void should_keep_the_given_source_in_its_fact_context() => _run.Execution.World.Facts.Single().Context!.EventSource.Value.ShouldEqual(SemanticValue.Text(First));
}
