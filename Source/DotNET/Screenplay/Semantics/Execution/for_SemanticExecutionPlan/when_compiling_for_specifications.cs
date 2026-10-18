// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticExecutionPlan;

public class when_compiling_for_specifications : for_SemanticSpecificationRunner.when_one_projection_maps_an_unsupported_event_context.given.a_model
{
    SemanticExecutionPlanCompilation _result;

    void Because() => _result = SemanticExecutionPlan.CompileForSpecifications(_compilation.Model);

    [Fact] void should_create_the_plan() => _result.Success.ShouldBeTrue();
    [Fact] void should_not_block_on_slice_owned_issues() => _result.Issues.ShouldBeEmpty();
    [Fact] void should_report_the_deferred_issue() => _result.UnselectedIssues.Single().Kind.ShouldEqual(SemanticPlanIssueKind.UnsupportedEventContext);
    [Fact] void should_preserve_the_model_revision() => _result.Plan!.Revision.ShouldEqual(_compilation.Model.Revision);
    [Fact] void should_keep_whole_model_admission_strict() => SemanticExecutionPlan.Compile(_compilation.Model).Success.ShouldBeFalse();
}
