// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel.when_validating_reaction_identity;

public class a_v10_model_with_an_exact_non_route_number : given.a_model_to_corrupt
{
    Exception _error;
    void Because() => _error = Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V10, SemanticVersion.V10, _application));
    [Fact] void should_keep_accepting_decimals_in_the_released_contract() => _error.ShouldBeNull();
}
