// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.for_SemanticCompilation;

public class when_mapping_catalog_assignment_origins : given.a_valid_semantic_compilation
{
    SemanticCompilation _compilation;
    Exception _eventOriginMismatch;
    Exception _queryArgumentOriginMismatch;

    void Because()
    {
        var queryArgumentAddress = Address(SemanticKind.QueryArgument, "ProjectById.projectId");
        var catalog = SemanticIdentityCatalog.Create(
            _applicationIdentity,
            _catalog.Documents,
            [.. _catalog.Semantics, new(queryArgumentAddress, _queryArgumentId, SemanticIdentityOrigin.Persisted)],
            _catalog.EventContracts);
        var documents = SemanticDocumentSet.Create([_document], catalog);
        var span = SemanticSourceSpan.Create(_document.Id, 0, 7, 1, 1, 1, 8);
        var queryArgumentMap = SemanticSourceMap.Create(
            [new(_queryArgumentId, span, SemanticIdentityOrigin.Persisted)],
            [_document]);
        _compilation = SemanticCompilation.Create(_model, documents, queryArgumentMap);

        var mismatchedQueryArgumentMap = SemanticSourceMap.Create(
            [new(_queryArgumentId, span, SemanticIdentityOrigin.LegacyBootstrap)],
            [_document]);
        _queryArgumentOriginMismatch = Catch.Exception(() => SemanticCompilation.Create(_model, documents, mismatchedQueryArgumentMap));

        var mismatchedEventMap = SemanticSourceMap.Create(
            [new(_eventId, span, SemanticIdentityOrigin.Persisted)],
            [_document]);
        _eventOriginMismatch = Catch.Exception(() => SemanticCompilation.Create(_model, documents, mismatchedEventMap));
    }

    [Fact] void should_include_query_arguments_in_source_mapping() => _compilation.SourceMap.Entries.Single().SemanticId.ShouldEqual(_queryArgumentId);
    [Fact] void should_reject_an_event_origin_that_disagrees_with_its_effective_assignment() => _eventOriginMismatch.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_a_query_argument_origin_that_disagrees_with_its_effective_assignment() => _queryArgumentOriginMismatch.ShouldBeOfExactType<InvalidSemanticContract>();
}
#endif
