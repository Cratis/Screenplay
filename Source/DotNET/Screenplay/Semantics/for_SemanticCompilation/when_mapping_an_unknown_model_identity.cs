// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.for_SemanticCompilation;

public class when_mapping_an_unknown_model_identity : given.a_valid_semantic_compilation
{
    Exception _exception;

    void Because()
    {
        var unknown = SemanticId.Parse($"sem1:{new string('f', 64)}");
        var sourceMap = SemanticSourceMap.Create(
            [new(unknown, SemanticSourceSpan.Create(_document.Id, 0, 7, 1, 1, 1, 8), SemanticIdentityOrigin.Persisted)],
            [_document]);
        _exception = Catch.Exception(() => SemanticCompilation.Create(_model, _documents, sourceMap));
    }

    [Fact] void should_throw_invalid_semantic_contract() => _exception.ShouldBeOfExactType<InvalidSemanticContract>();
}
#endif
