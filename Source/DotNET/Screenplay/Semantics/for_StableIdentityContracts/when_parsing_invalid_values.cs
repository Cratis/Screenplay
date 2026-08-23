// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_StableIdentityContracts;

public class when_parsing_invalid_values : Specification
{
    bool _applicationResult;
    bool _catalogRevisionResult;
    bool _documentResult;
    bool _eventResult;
    bool _semanticRevisionResult;
    bool _semanticResult;
    ApplicationIdentity _applicationIdentity;
    CatalogRevision _catalogRevision;
    DocumentId _documentId;
    EventContractId _eventContractId;
    SemanticRevision _semanticRevision;
    SemanticId _semanticId;

    void Because()
    {
        _applicationResult = ApplicationIdentity.TryParse("app1:ABC", out _applicationIdentity);
        _documentResult = DocumentId.TryParse("doc1:ABC", out _documentId);
        _semanticResult = SemanticId.TryParse("sem1:abc", out _semanticId);
        _eventResult = EventContractId.TryParse("sem1:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef", out _eventContractId);
        _semanticRevisionResult = SemanticRevision.TryParse("rev1:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdeg", out _semanticRevision);
        _catalogRevisionResult = CatalogRevision.TryParse("rev1:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef", out _catalogRevision);
    }

    [Fact] void should_reject_application_identity() => _applicationResult.ShouldBeFalse();
    [Fact] void should_reject_document_identity() => _documentResult.ShouldBeFalse();
    [Fact] void should_reject_semantic_identity() => _semanticResult.ShouldBeFalse();
    [Fact] void should_reject_event_contract_identity() => _eventResult.ShouldBeFalse();
    [Fact] void should_reject_semantic_revision() => _semanticRevisionResult.ShouldBeFalse();
    [Fact] void should_reject_catalog_revision_from_the_semantic_domain() => _catalogRevisionResult.ShouldBeFalse();
    [Fact] void should_return_default_application_identity() => _applicationIdentity.IsSet.ShouldBeFalse();
    [Fact] void should_return_default_document_identity() => _documentId.IsSet.ShouldBeFalse();
    [Fact] void should_return_default_semantic_identity() => _semanticId.IsSet.ShouldBeFalse();
    [Fact] void should_return_default_event_contract_identity() => _eventContractId.IsSet.ShouldBeFalse();
    [Fact] void should_return_default_semantic_revision() => _semanticRevision.IsSet.ShouldBeFalse();
    [Fact] void should_return_default_catalog_revision() => _catalogRevision.IsSet.ShouldBeFalse();
}
