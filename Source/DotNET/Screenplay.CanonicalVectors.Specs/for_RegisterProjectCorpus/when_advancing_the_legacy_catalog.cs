// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalVectors.for_RegisterProjectCorpus;

public class when_advancing_the_legacy_catalog : Specification
{
    SemanticIdentityCatalog _expected = null!;
    SemanticIdentityCatalog _advanced = null!;

    void Because()
    {
        var legacy = SemanticIdentityCatalogSerializer.Deserialize(RegisterProjectCorpus.LegacyV1.SourceForms[0].IdentityCatalogBytes.AsSpan());
        _expected = SemanticIdentityCatalogSerializer.Deserialize(RegisterProjectCorpus.V2.SourceForms[0].IdentityCatalogBytes.AsSpan());
        var @event = _expected.EventContracts.Single();
        _advanced = SemanticIdentityCatalog.PlanEventRevisionAdvancement(
            legacy,
            legacy.Revision,
            [.. _expected.Documents.Select(document => document.Key)],
            [.. _expected.Semantics.Select(assignment => assignment.Address)],
            [@event.Address],
            [new(@event.Address, @event.Revision)]).Catalog;
    }

    [Fact] void should_derive_the_reviewed_catalog() => SemanticIdentityCatalogSerializer.Serialize(_advanced).SequenceEqual(SemanticIdentityCatalogSerializer.Serialize(_expected)).ShouldBeTrue();
}
