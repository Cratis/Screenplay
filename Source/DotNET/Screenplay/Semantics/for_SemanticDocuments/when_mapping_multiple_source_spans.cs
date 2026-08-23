// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Screenplay.Semantics.for_SemanticDocuments;

public class when_mapping_multiple_source_spans : Specification
{
    SemanticSourceMap _sourceMap;

    void Because()
    {
        var documentId = DocumentId.Create("source");
        var document = SemanticSourceDocument.Create(documentId, "source", "source.play", "first\nsecond");
        var semanticId = SemanticId.Parse($"sem1:{new string('a', 64)}");
        _sourceMap = SemanticSourceMap.Create(
        [
            new(semanticId, SemanticSourceSpan.Create(documentId, 6, 6, 2, 1, 2, 7), SemanticIdentityOrigin.Persisted),
            new(semanticId, SemanticSourceSpan.Create(documentId, 0, 5, 1, 1, 1, 6), SemanticIdentityOrigin.Persisted)
        ],
        [document]);
    }

    [Fact] void should_preserve_every_distinct_span() => _sourceMap.Entries.Length.ShouldEqual(2);
    [Fact] void should_order_the_first_span_by_source_offset() => _sourceMap.Entries[0].Span.Start.ShouldEqual(0);
    [Fact] void should_order_the_second_span_by_source_offset() => _sourceMap.Entries[1].Span.Start.ShouldEqual(6);
}
#endif
