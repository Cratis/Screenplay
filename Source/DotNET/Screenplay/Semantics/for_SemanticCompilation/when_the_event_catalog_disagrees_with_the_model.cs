// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.for_SemanticCompilation;

public class when_the_event_catalog_disagrees_with_the_model : given.a_valid_semantic_compilation
{
    Exception _exception;

    void Because()
    {
        var eventAddress = Address(SemanticKind.EventContract, "ProjectRegistered");
        var conflictingId = EventContractId.Parse($"evt1:{new string('f', 64)}");
        var catalog = SemanticIdentityCatalog.Create(
            _applicationIdentity,
            [],
            _catalog.Semantics,
            [new(eventAddress, conflictingId, EventContractRevision.Initial, SemanticIdentityOrigin.Persisted)]);
        var documents = SemanticDocumentSet.Create([_document], catalog);
        _exception = Catch.Exception(() => SemanticCompilation.Create(_model, documents, _sourceMap));
    }

    [Fact] void should_throw_invalid_semantic_contract() => _exception.ShouldBeOfExactType<InvalidSemanticContract>();
}
#endif
