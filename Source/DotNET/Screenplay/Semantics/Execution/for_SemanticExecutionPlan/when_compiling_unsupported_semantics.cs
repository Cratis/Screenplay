// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticExecutionPlan;

public class when_compiling_unsupported_semantics : a_valid_semantic_model
{
    SemanticExecutionPlanCompilation _result;

    void Because()
    {
        var slice = _application.Modules.Single().Features.Single().Slices.Single(_ => _.Queries.Length > 0);
        var query = slice.Queries.Single() with { Delivery = SemanticQueryDelivery.Live };
        var model = ExecutableSemanticModel.Create(
            LanguageVersion.V1,
            SemanticVersion.V1,
            ReplaceSlice(slice with { Queries = [query] }));
        _result = SemanticExecutionPlan.Compile(model);
    }

    [Fact] void should_block_plan_creation() => _result.Success.ShouldBeFalse();
    [Fact] void should_not_return_a_partial_plan() => _result.Plan.ShouldBeNull();
    [Fact] void should_report_the_query_capability() => _result.Issues.Single().Kind.ShouldEqual(SemanticPlanIssueKind.UnsupportedQuery);
    [Fact] void should_identify_the_query() => _result.Issues.Single().Artifact.ShouldEqual(_queryId);
}
