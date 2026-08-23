// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_StableIdentityContracts;

public class when_parsing_canonical_values : Specification
{
    const string Hash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
    DocumentId _documentId;
    EventContractId _eventContractId;
    SemanticId _semanticId;
    SemanticRevision _revision;

    void Because()
    {
        _documentId = DocumentId.Parse($"doc1:{Hash}");
        _semanticId = SemanticId.Parse($"sem1:{Hash}");
        _eventContractId = EventContractId.Parse($"evt1:{Hash}");
        _revision = SemanticRevision.Parse($"rev1:{Hash}");
    }

    [Fact] void should_parse_document_identity() => _documentId.ToString().ShouldEqual($"doc1:{Hash}");
    [Fact] void should_parse_semantic_identity() => _semanticId.ToString().ShouldEqual($"sem1:{Hash}");
    [Fact] void should_parse_event_contract_identity() => _eventContractId.ToString().ShouldEqual($"evt1:{Hash}");
    [Fact] void should_parse_semantic_revision() => _revision.ToString().ShouldEqual($"rev1:{Hash}");
}
