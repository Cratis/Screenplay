// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel.when_validating_reaction_identity;

public class a_malformed_identity : given.a_model_to_corrupt
{
    [Theory]
    [InlineData(1, "A", "B")]
    [InlineData(0, "z", "A")]
    [InlineData(0, "A", "A")]
    [InlineData(0, " ", "A")]
    void should_reject_unknown_kinds_and_noncanonical_roles(int kind, string first, string second) =>
        Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V10, SemanticVersion.V10, WithIdentity(new((SemanticReactionIdentityKind)kind, [first, second])))).ShouldBeOfExactType<InvalidSemanticContract>();

    [Fact]
    void should_reject_a_default_roles_array() =>
        Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V10, SemanticVersion.V10, WithIdentity(new(SemanticReactionIdentityKind.System, default)))).ShouldBeOfExactType<InvalidSemanticContract>();
}
