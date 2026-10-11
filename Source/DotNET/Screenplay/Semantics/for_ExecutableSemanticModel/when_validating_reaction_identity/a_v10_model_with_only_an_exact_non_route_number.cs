// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel.when_validating_reaction_identity;

public class a_v10_model_with_only_an_exact_non_route_number : given.a_model_without_v10_features
{
    Exception _error;
    void Establish() => _application = WithNonRouteNumber(9007199254740991m);
    void Because() => _error = Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V10, SemanticVersion.V10, _application));
    [Fact] void should_validate_by_exact_number_use_without_identity() => _error.ShouldBeNull();
}
