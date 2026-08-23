// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Screenplay.Semantics.for_SemanticDocuments;

public class when_using_inexact_source_coordinates : Specification
{
    Exception[] _exceptions;

    void Because()
    {
        var documentId = DocumentId.Create("source");
        var document = SemanticSourceDocument.Create(documentId, "source", "source.play", "one\r\ntwo");
        var semanticId = SemanticId.Parse($"sem1:{new string('a', 64)}");
        _exceptions =
        [
            Catch.Exception(() => SemanticSourceMap.Create(
                [new(semanticId, SemanticSourceSpan.Create(documentId, 5, 3, 1, 1, 1, 4), SemanticIdentityOrigin.Persisted)],
                [document])),
            Catch.Exception(() => SemanticSourceMap.Create(
                [new(semanticId, SemanticSourceSpan.Create(documentId, 4, 0, 1, 5, 1, 5), SemanticIdentityOrigin.Persisted)],
                [document])),
            Catch.Exception(() => SemanticSourceMap.Create(
                [new(semanticId, SemanticSourceSpan.Create(documentId, 0, 9, 1, 1, 2, 4), SemanticIdentityOrigin.Persisted)],
                [document]))
        ];
    }

    [Fact] void should_reject_mismatched_split_crlf_and_out_of_range_coordinates_as_invalid_semantic_contracts() =>
        _exceptions.All(_ => _.GetType() == typeof(InvalidSemanticContract)).ShouldBeTrue();
}
#endif
