// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_StableIdentityContracts;

public class when_parsing_invalid_values : Specification
{
    bool _documentResult;
    bool _eventResult;
    bool _revisionResult;
    bool _semanticResult;
    DocumentId _documentId;
    EventContractId _eventContractId;
    SemanticRevision _revision;
    SemanticId _semanticId;

    void Because()
    {
        _documentResult = DocumentId.TryParse("doc1:ABC", out _documentId);
        _semanticResult = SemanticId.TryParse("sem1:abc", out _semanticId);
        _eventResult = EventContractId.TryParse("sem1:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef", out _eventContractId);
        _revisionResult = SemanticRevision.TryParse("rev1:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdeg", out _revision);
    }

    [Fact] void should_reject_document_identity() => _documentResult.ShouldBeFalse();
    [Fact] void should_reject_semantic_identity() => _semanticResult.ShouldBeFalse();
    [Fact] void should_reject_event_contract_identity() => _eventResult.ShouldBeFalse();
    [Fact] void should_reject_revision() => _revisionResult.ShouldBeFalse();
    [Fact] void should_return_default_document_identity() => _documentId.IsSet.ShouldBeFalse();
    [Fact] void should_return_default_semantic_identity() => _semanticId.IsSet.ShouldBeFalse();
    [Fact] void should_return_default_event_contract_identity() => _eventContractId.IsSet.ShouldBeFalse();
    [Fact] void should_return_default_revision() => _revision.IsSet.ShouldBeFalse();
}
