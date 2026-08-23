// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.given;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_validating_query_result_cardinality : a_valid_semantic_model
{
    Exception _missingRequiredResult;
    Exception _manyResults;

    void Because()
    {
        var feature = _application.Modules.Single().Features.Single();
        var stateChange = feature.Slices.Single(_ => _.Commands.Length > 0);
        var stateView = feature.Slices.Single(_ => _.Queries.Length > 0);
        var query = stateView.Queries.Single();
        var success = stateChange.Specifications.Single(_ => _.ThenQueries.Length > 0);
        var result = success.ThenQueries.Single();

        _missingRequiredResult = Validate(
            query with { Cardinality = SemanticQueryCardinality.One },
            success with { ThenQueries = [result with { Results = [] }] });
        _manyResults = Validate(
            query,
            success with { ThenQueries = [result with { Results = [result.Results[0], result.Results[0]] }] });

        Exception Validate(SemanticKeyedQuery replacementQuery, SemanticSpecification replacementSpecification) =>
            Catch.Exception(() => ExecutableSemanticModel.Create(
                LanguageVersion.V1,
                SemanticVersion.V1,
                ReplaceSlices(
                    stateView with { Queries = [replacementQuery] },
                    stateChange with
                    {
                        Specifications = [.. stateChange.Specifications.Select(_ => _.Id == replacementSpecification.Id ? replacementSpecification : _)]
                    })));
    }

    [Fact] void should_reject_a_missing_required_result() => _missingRequiredResult.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_multiple_optional_results() => _manyResults.ShouldBeOfExactType<InvalidSemanticContract>();
}
#endif
