// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel.when_validating_reaction_identity;

public class a_v10_model_without_identity : given.a_model_without_v10_features
{
    Exception _error;
    void Because() => _error = Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V10, SemanticVersion.V10, WithIdentity(null)));
    [Fact] void should_require_activation_by_use() => _error.ShouldBeOfExactType<InvalidSemanticContract>();
}
