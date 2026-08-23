// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.Serialization.given;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Screenplay.Semantics.Serialization.for_IdentityCatalogSerializer;

public class when_serializing_the_golden_catalog : Specification
{
    byte[] _expected;
    byte[] _serialized;
    byte[] _reserialized;
    SemanticIdentityCatalog _catalog;
    SemanticIdentityCatalog _roundTripped;

    void Establish()
    {
        _expected = canonical_serialization_golden_vectors.IdentityCatalogBytes;
        _catalog = canonical_serialization_golden_vectors.CreateIdentityCatalog();
    }

    void Because()
    {
        _serialized = SemanticIdentityCatalogSerializer.Serialize(_catalog);
        _roundTripped = SemanticIdentityCatalogSerializer.Deserialize(_expected);
        _reserialized = SemanticIdentityCatalogSerializer.Serialize(_roundTripped);
    }

    [Fact] void should_match_the_checked_in_utf8_bytes() => _serialized.SequenceEqual(_expected).ShouldBeTrue();
    [Fact] void should_reserialize_the_golden_bytes_identically() => _reserialized.SequenceEqual(_expected).ShouldBeTrue();
    [Fact] void should_preserve_the_distinct_catalog_revision() => _roundTripped.Revision.ShouldEqual(_catalog.Revision);
    [Fact] void should_cover_every_semantic_kind() => _roundTripped.Semantics.Select(_ => _.Address.Kind).Distinct().ShouldContainOnly(Enum.GetValues<SemanticKind>().Where(_ => _ != SemanticKind.Unknown));
    [Fact] void should_cover_every_legal_property_owner() => _roundTripped.Semantics.Where(_ => _.Address.Kind == SemanticKind.Property).Select(_ => _.Address.OwnerKind).ShouldContainOnly([SemanticKind.CompositeType, SemanticKind.Command, SemanticKind.EventContract, SemanticKind.ReadModel]);
    [Fact] void should_cover_both_origins() => _roundTripped.Semantics.Select(_ => _.Origin).Distinct().ShouldContainOnly([SemanticIdentityOrigin.Persisted, SemanticIdentityOrigin.LegacyBootstrap]);
    [Fact] void should_cover_multiple_event_contracts() => _roundTripped.EventContracts.Length.ShouldEqual(2);
}
#endif
