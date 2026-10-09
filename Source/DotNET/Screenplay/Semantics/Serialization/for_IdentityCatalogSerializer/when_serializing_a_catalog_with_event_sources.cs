// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text.Json;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_IdentityCatalogSerializer;

public class when_serializing_a_catalog_with_event_sources : Specification
{
    SemanticIdentityCatalog _catalog;
    SemanticIdentityCatalog _roundTripped;
    SemanticAddress _source;
    SemanticAddress _stream;
    byte[] _serialized;
    byte[] _reserialized;
    byte[] _withoutEventRoutes;

    void Establish()
    {
        var baseline = canonical_serialization_golden_vectors.CreateIdentityCatalog();
        _source = SemanticAddress.ForEventSource(baseline.Application, "Project");
        _stream = SemanticAddress.ForEventStream(_source, "Ledger");
        _catalog = SemanticIdentityCatalog.Create(
            baseline.Application,
            baseline.Documents,
            [
                .. baseline.Semantics,
                new(_source, SemanticId.Create(_source), SemanticIdentityOrigin.Persisted),
                new(_stream, SemanticId.Create(_stream), SemanticIdentityOrigin.Persisted)
            ],
            baseline.EventContracts);
    }

    void Because()
    {
        _serialized = SemanticIdentityCatalogSerializer.Serialize(_catalog);
        _roundTripped = SemanticIdentityCatalogSerializer.Deserialize(_serialized);
        _reserialized = SemanticIdentityCatalogSerializer.Serialize(_roundTripped);
        var withoutEventRoutes = SemanticIdentityCatalog.Create(
            _roundTripped.Application,
            _roundTripped.Documents,
            [.. _roundTripped.Semantics.Where(assignment => assignment.Address.Kind is not (SemanticKind.EventSource or SemanticKind.EventStream))],
            _roundTripped.EventContracts);
        _withoutEventRoutes = SemanticIdentityCatalogSerializer.Serialize(withoutEventRoutes);
    }

    [Fact] void should_round_trip_the_canonical_bytes() => _reserialized.SequenceEqual(_serialized).ShouldBeTrue();
    [Fact] void should_preserve_the_catalog_revision() => _roundTripped.Revision.ShouldEqual(_catalog.Revision);
    [Fact] void should_preserve_the_source_address_and_identity() => _roundTripped.Semantics.Single(assignment => assignment.Address.Equals(_source)).Id.ShouldEqual(SemanticId.Create(_source));
    [Fact] void should_preserve_the_stream_address_and_identity() => _roundTripped.Semantics.Single(assignment => assignment.Address.Equals(_stream)).Id.ShouldEqual(SemanticId.Create(_stream));
    [Fact] void should_keep_catalogs_without_event_routes_byte_identical() => _withoutEventRoutes.SequenceEqual(canonical_serialization_golden_vectors.IdentityCatalogBytes).ShouldBeTrue();

    [Fact]
    void should_keep_the_catalog_schema_at_the_initial_version()
    {
        using var document = JsonDocument.Parse(_serialized);
        document.RootElement.GetProperty("schemaVersion").GetUInt32().ShouldEqual(1u);
    }
}
#endif
