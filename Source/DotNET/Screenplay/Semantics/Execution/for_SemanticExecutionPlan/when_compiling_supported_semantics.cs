// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticExecutionPlan;

public class when_compiling_supported_semantics : a_valid_semantic_model
{
    SemanticExecutionPlanCompilation _result;

    void Because() => _result = SemanticExecutionPlan.Compile(_model);

    [Fact] void should_compile_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_issues() => _result.Issues.ShouldBeEmpty();
    [Fact] void should_bind_the_model_revision() => _result.Plan!.Revision.ShouldEqual(_model.Revision);
    [Fact] void should_index_the_command() => _result.Plan!.Commands.ContainsKey(_commandId).ShouldBeTrue();
    [Fact] void should_index_the_event() => _result.Plan!.Events.ContainsKey(_eventId).ShouldBeTrue();
    [Fact] void should_index_the_read_model() => _result.Plan!.ReadModels.ContainsKey(_readModelId).ShouldBeTrue();
    [Fact] void should_index_the_query() => _result.Plan!.Queries.ContainsKey(_queryId).ShouldBeTrue();
    [Fact] void should_index_both_specifications() => _result.Plan!.Specifications.Count.ShouldEqual(2);
}
