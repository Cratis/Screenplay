// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_using_an_unsupported_version : a_valid_semantic_model
{
    Exception _languageException;
    Exception _semanticException;
    Exception _v2PairException;

    void Because()
    {
        _languageException = Catch.Exception(() => ExecutableSemanticModel.Create(new(1, 1), SemanticVersion.V1, _application));
        _semanticException = Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V1, new(1, 1), _application));
        _v2PairException = Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V2, SemanticVersion.V2, _application));
    }

    [Fact] void should_reject_the_language_version() => _languageException.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_the_semantic_version() => _semanticException.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_the_reserved_v2_pair_until_the_v2_schema_is_implemented() => _v2PairException.ShouldBeOfExactType<InvalidSemanticContract>();
}
