// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Screenplay.Semantics.for_SemanticDocuments;

public class when_validating_exact_source_coordinates : Specification
{
    SemanticSourceMap _sourceMap;

    void Because()
    {
        var documentId = DocumentId.Create("source");
        var document = SemanticSourceDocument.Create(documentId, "source", "source.play", "one\r\n😀x\n\nlast");
        var semanticId = SemanticId.Parse($"sem1:{new string('a', 64)}");
        _sourceMap = SemanticSourceMap.Create(
            [new(semanticId, SemanticSourceSpan.Create(documentId, 5, 5, 2, 1, 4, 1), SemanticIdentityOrigin.Persisted)],
            [document]);
    }

    [Fact] void should_admit_utf16_crlf_lf_unicode_empty_line_and_end_exclusive_coordinates() => _sourceMap.Entries.Length.ShouldEqual(1);
}
#endif
