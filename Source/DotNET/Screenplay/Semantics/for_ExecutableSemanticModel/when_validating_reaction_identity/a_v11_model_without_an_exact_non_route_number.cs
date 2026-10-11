// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel.when_validating_reaction_identity;

public class a_v11_model_without_an_exact_non_route_number : given.a_model_without_v10_features
{
    Exception _error;
    void Because() => _error = Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V11, SemanticVersion.V11, WithIdentity(new(SemanticReactionIdentityKind.System, []))));
    [Fact] void should_require_exact_number_activation_even_with_a_v10_feature() => _error.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_name_the_missing_selecting_construct() => _error.Message.ShouldContain("exact Double-mode number literal lowering");
}
