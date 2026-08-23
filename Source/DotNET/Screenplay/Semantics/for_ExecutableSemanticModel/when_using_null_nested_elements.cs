// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.given;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_using_null_nested_elements : a_valid_semantic_model
{
    Exception[] _exceptions;

    void Because()
    {
        var stateChange = _application.Modules.Single().Features.Single().Slices.Single(_ => _.Commands.Length > 0);
        var stateView = _application.Modules.Single().Features.Single().Slices.Single(_ => _.Queries.Length > 0);
        var eventContract = stateChange.Events.Single() with { Properties = [null!] };
        var projection = stateView.Projections.Single();
        var transition = projection.Transitions.Single() with { AffectedInstance = null! };
        var success = stateChange.Specifications.Single(_ => _.ThenQueries.Length > 0);
        var queryResult = success.ThenQueries.Single() with { Results = [null!] };
        _exceptions =
        [
            Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V1, SemanticVersion.V1, _application with { Modules = [null!] })),
            Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V1, SemanticVersion.V1, ReplaceSlice(stateChange with { Events = [eventContract] }))),
            Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V1, SemanticVersion.V1, ReplaceSlice(stateView with { Projections = [projection with { Transitions = [transition] }] }))),
            Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V1, SemanticVersion.V1, ReplaceSlice(stateChange with
            {
                Specifications = [.. stateChange.Specifications.Select(_ => _.Id == success.Id ? success with { ThenQueries = [queryResult] } : _)]
            })))
        ];
    }

    [Fact]
    void should_always_throw_invalid_semantic_contract() =>
        _exceptions.All(_ => _.GetType() == typeof(InvalidSemanticContract)).ShouldBeTrue();
}
#endif
