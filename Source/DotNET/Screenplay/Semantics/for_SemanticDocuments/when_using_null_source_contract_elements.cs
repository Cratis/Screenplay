// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Screenplay.Semantics.for_SemanticDocuments;

public class when_using_null_source_contract_elements : Specification
{
    Exception[] _exceptions;

    void Because()
    {
        var application = ApplicationIdentity.Create("Projects");
        var catalog = SemanticIdentityCatalog.Empty(application);
        var document = SemanticSourceDocument.Create(DocumentId.Create("source"), "source", "source.play", string.Empty);
        _exceptions =
        [
            Catch.Exception(() => SemanticDocumentSet.Create([null!], catalog)),
            Catch.Exception(() => SemanticSourceMap.Create([null!], [document])),
            Catch.Exception(() => SemanticSourceMap.Create([], [null!]))
        ];
    }

    [Fact] void should_reject_every_null_element_before_access_as_an_invalid_semantic_contract() =>
        _exceptions.All(_ => _.GetType() == typeof(InvalidSemanticContract)).ShouldBeTrue();
}
#endif
