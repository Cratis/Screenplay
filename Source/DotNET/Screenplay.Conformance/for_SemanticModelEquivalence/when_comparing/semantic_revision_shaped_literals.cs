// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Conformance.for_SemanticModelEquivalence.when_comparing;

public class semantic_revision_shaped_literals : given.compiled_models
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => _comparison = SemanticModelEquivalence.Compare(
        Compile(Changed("First", $"rev1:{new string('0', 64)}")),
        Compile(Changed("First", $"rev1:{new string('1', 64)}"))));

    [Fact] void should_not_treat_literals_as_unmapped_identities() => _error.ShouldBeNull();
    [Fact] void should_preserve_the_literal_difference() => Assert.True(_comparison is { IsEquivalent: false });
}
