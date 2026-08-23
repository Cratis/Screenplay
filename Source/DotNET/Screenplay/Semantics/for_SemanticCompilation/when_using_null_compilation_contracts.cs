// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.for_SemanticCompilation;

public class when_using_null_compilation_contracts : given.a_valid_semantic_compilation
{
    Exception[] _exceptions;

    void Because() => _exceptions =
    [
        Catch.Exception(() => SemanticCompilation.Create(null!, _documents, _sourceMap)),
        Catch.Exception(() => SemanticCompilation.Create(_model, null!, _sourceMap)),
        Catch.Exception(() => SemanticCompilation.Create(_model, _documents, null!))
    ];

    [Fact] void should_reject_every_null_contract_before_access_as_an_invalid_semantic_contract() =>
        _exceptions.All(_ => _.GetType() == typeof(InvalidSemanticContract)).ShouldBeTrue();
}
#endif
