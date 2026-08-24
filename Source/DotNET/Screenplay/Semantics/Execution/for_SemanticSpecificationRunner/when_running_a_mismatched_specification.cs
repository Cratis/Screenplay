// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner;

public class when_running_a_mismatched_specification : a_valid_semantic_model
{
    SemanticSpecificationRun _result;

    void Because()
    {
        var slice = _application.Modules.Single().Features.Single().Slices.Single(_ => _.Specifications.Length > 0);
        var success = slice.Specifications.Single(_ => _.ThenQueries.Length > 0);
        var query = success.ThenQueries.Single();
        var state = query.Results.Single();
        var mismatchedState = state with
        {
            Values = [.. state.Values.Select(value => value.TargetProperty == _readModelNamePropertyId
                ? value with { Value = SemanticValue.Text("Different") }
                : value)]
        };
        var mismatched = success with { ThenQueries = [query with { Results = [mismatchedState] }] };
        var model = ExecutableSemanticModel.Create(
            LanguageVersion.V1,
            SemanticVersion.V1,
            ReplaceSlice(slice with
            {
                Specifications = [.. slice.Specifications.Select(value => value.Id == success.Id ? mismatched : value)]
            }));
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        _result = new SemanticSpecificationRunner().Run(plan, mismatched.Id);
    }

    [Fact] void should_fail_the_specification() => _result.Passed.ShouldBeFalse();
    [Fact] void should_report_the_query_result_mismatch() => _result.Failures.ShouldNotBeEmpty();
}
