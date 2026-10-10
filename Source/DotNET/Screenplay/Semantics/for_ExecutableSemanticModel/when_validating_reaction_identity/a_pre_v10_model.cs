// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel.when_validating_reaction_identity;

public class a_pre_v10_model : given.a_model_to_corrupt
{
    Exception _error;
    void Because() => _error = Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V9, SemanticVersion.V9, _application));
    [Fact] void should_refuse_identity_without_admission() => _error.ShouldBeOfExactType<InvalidSemanticContract>();
}
