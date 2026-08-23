// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Screenplay.Semantics.for_SemanticDocuments;

public class when_mapping_a_duplicate_source_span : Specification
{
    Exception _exception;

    void Because()
    {
        var documentId = DocumentId.Create("source");
        var document = SemanticSourceDocument.Create(documentId, "source", "source.play", "source");
        var semanticId = SemanticId.Parse($"sem1:{new string('a', 64)}");
        var span = SemanticSourceSpan.Create(documentId, 0, 6, 1, 1, 1, 7);
        _exception = Catch.Exception(() => SemanticSourceMap.Create(
        [
            new(semanticId, span, SemanticIdentityOrigin.Persisted),
            new(semanticId, span, SemanticIdentityOrigin.LegacyBootstrap)
        ],
        [document]));
    }

    [Fact] void should_throw_invalid_semantic_contract() => _exception.ShouldBeOfExactType<InvalidSemanticContract>();
}
#endif
