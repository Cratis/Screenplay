// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner;

public class when_running_filtered_queries : Specification
{
    SemanticExecutionPlan _plan;
    SemanticSpecificationRun[] _runs;

    void Establish() => _plan = SemanticExecutionPlan.Compile(query_filter_source.Compile().Model).Plan!;

    void Because() => _runs = [.. _plan.Specifications.Values.Select(specification => new SemanticSpecificationRunner().Run(_plan, specification.Id))];

    [Fact] void should_match_rows_without_regard_to_order() => _runs.Single(run => run.Specification == _plan.Specifications.Values.Single(specification => specification.Name == "ReversedRows").Id).Passed.ShouldBeTrue();
    [Fact] void should_distinguish_unkeyed_calls_by_supplied_filters() => _runs.All(run => run.Passed).ShouldBeTrue();
    [Fact] void should_have_no_comparison_failures() => _runs.SelectMany(run => run.Failures).ShouldBeEmpty();

    [Fact]
    void should_fail_a_missing_row()
    {
        var model = query_filter_source.Compile(query_filter_source.Source.Replace("        then result\n          itemId = \"b\"\n", string.Empty, StringComparison.Ordinal)).Model;
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        new SemanticSpecificationRunner().Run(plan, plan.Specifications.Values.Single(specification => specification.Name == "ReversedRows").Id).Passed.ShouldBeFalse();
    }

    [Fact]
    void should_fail_a_wrong_row()
    {
        var model = query_filter_source.Compile(query_filter_source.Source.Replace("then result\n          itemId = \"b\"", "then result\n          itemId = \"c\"", StringComparison.Ordinal)).Model;
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        new SemanticSpecificationRunner().Run(plan, plan.Specifications.Values.Single(specification => specification.Name == "ReversedRows").Id).Passed.ShouldBeFalse();
    }
}
