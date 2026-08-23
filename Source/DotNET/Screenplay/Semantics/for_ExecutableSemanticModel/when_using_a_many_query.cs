// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.given;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_using_a_many_query : a_valid_semantic_model
{
    Exception _mismatchedResult;
    ExecutableSemanticModel _roundTripped;

    void Because()
    {
        var feature = _application.Modules.Single().Features.Single();
        var stateChange = feature.Slices.Single(_ => _.Commands.Length > 0);
        var stateView = feature.Slices.Single(_ => _.Queries.Length > 0);
        var query = stateView.Queries.Single() with
        {
            Argument = stateView.Queries.Single().Argument with { Type = SemanticTypeReference.ForConcept(_projectNameConceptId) },
            KeyProperty = _readModelNamePropertyId,
            Cardinality = SemanticQueryCardinality.Many
        };
        var success = stateChange.Specifications.Single(_ => _.ThenQueries.Length > 0);
        var queryResult = success.ThenQueries.Single() with { Key = SemanticValue.Text("Screenplay") };
        var stateViewWithQuery = stateView with { Queries = [query] };
        var stateChangeWithQuery = stateChange with
        {
            Specifications = [.. stateChange.Specifications.Select(_ => _.Id == success.Id ? success with { ThenQueries = [queryResult] } : _)]
        };
        var application = ReplaceSlices(stateViewWithQuery, stateChangeWithQuery);
        var model = ExecutableSemanticModel.Create(LanguageVersion.V1, SemanticVersion.V1, application);
        _roundTripped = SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(model));

        var state = queryResult.Results.Single();
        var mismatchedState = state with
        {
            Values = [.. state.Values.Select(_ => _.TargetProperty == _readModelNamePropertyId
                ? _ with { Value = SemanticValue.Text("Other") }
                : _)]
        };
        var mismatchedResult = queryResult with { Results = [mismatchedState] };
        var invalidStateChange = stateChange with
        {
            Specifications = [.. stateChange.Specifications.Select(_ => _.Id == success.Id ? success with { ThenQueries = [mismatchedResult] } : _)]
        };
        _mismatchedResult = Catch.Exception(() => ExecutableSemanticModel.Create(
            LanguageVersion.V1,
            SemanticVersion.V1,
            ReplaceSlices(stateViewWithQuery, invalidStateChange)));
    }

    [Fact] void should_reject_a_result_that_does_not_match_the_query_key() => _mismatchedResult.ShouldBeOfExactType<InvalidSemanticContract>();

    [Fact]
    void should_preserve_many_cardinality() =>
        _roundTripped.Application.Modules.Single().Features.Single().Slices.Single(_ => _.Queries.Length > 0)
            .Queries.Single().Cardinality.ShouldEqual(SemanticQueryCardinality.Many);
}
#endif
