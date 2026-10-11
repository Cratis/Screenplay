// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel.when_validating_reaction_identity;

public class a_v9_model_with_an_exact_non_route_number : given.a_model_without_v10_features
{
    Exception _error;
    void Establish() => _application = WithNonRouteNumber(9007199254740991m);
    void Because() => _error = Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V9, SemanticVersion.V9, _application));
    [Fact] void should_keep_accepting_programmatic_decimals_at_the_released_version() => _error.ShouldBeNull();
}
