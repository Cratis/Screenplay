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
        var application = ReplaceSlices(
            stateView with { Queries = [query] },
            stateChange with
            {
                Specifications = [.. stateChange.Specifications.Select(_ => _.Id == success.Id ? success with { ThenQueries = [queryResult] } : _)]
            });
        var model = ExecutableSemanticModel.Create(LanguageVersion.V1, SemanticVersion.V1, application);
        _roundTripped = SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(model));
    }

    [Fact]
    void should_preserve_many_cardinality() =>
        _roundTripped.Application.Modules.Single().Features.Single().Slices.Single(_ => _.Queries.Length > 0)
            .Queries.Single().Cardinality.ShouldEqual(SemanticQueryCardinality.Many);
}
#endif
