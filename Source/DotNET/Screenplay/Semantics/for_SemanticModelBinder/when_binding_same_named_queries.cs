// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_same_named_queries : given.a_model_with_same_named_queries
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_bind_both_declarations() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_the_existing_semantic_version() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V1);
    [Fact] void should_give_each_query_a_distinct_identity() => Slices.SelectMany(slice => slice.Queries).Select(query => query.Id).Distinct().Count().ShouldEqual(2);
    [Fact] void should_resolve_each_assertion_to_its_own_query() => Slices.ShouldEachConformTo(slice => slice.Specifications.Single().ThenQueries.Single().Query == slice.Queries.Single().Id);
    [Fact] void should_bind_each_query_to_its_own_read_model() => Slices.ShouldEachConformTo(slice => slice.Queries.Single().ReadModel == slice.ReadModels.Single().Id);
    [Fact] void should_execute_each_query_against_its_own_read_model()
    {
        var plan = SemanticExecutionPlan.Compile(_result.Value!.Model).Plan!;
        foreach (var specification in Slices.SelectMany(slice => slice.Specifications))
        {
            new SemanticSpecificationRunner().Run(plan, specification.Id).Passed.ShouldBeTrue();
        }
    }

    IEnumerable<SemanticSlice> Slices => _result.Value!.Model.Application.Modules.Single().Features.Single().Slices;
}
