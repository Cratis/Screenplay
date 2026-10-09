// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_serializing_identity_catalogs_with_sources : Specification
{
    [Fact]
    void should_keep_the_catalog_schema_and_existing_bytes()
    {
        var old = canonical_serialization_golden_vectors.CreateIdentityCatalog();
        SemanticIdentityCatalogSerializer.Serialize(old).SequenceEqual(canonical_serialization_golden_vectors.IdentityCatalogBytes).ShouldBeTrue();
        var source = SemanticAddress.ForEventSource(old.Application, "Project");
        var stream = SemanticAddress.ForEventStream(source, "Ledger");
        var semantics = old.Semantics.Add(new(source, SemanticId.Create(source), SemanticIdentityOrigin.Persisted))
            .Add(new(stream, SemanticId.Create(stream), SemanticIdentityOrigin.Persisted));
        var catalog = SemanticIdentityCatalog.Create(old.Application, old.Documents, semantics, old.EventContracts);
        var bytes = SemanticIdentityCatalogSerializer.Serialize(catalog);
        using var json = JsonDocument.Parse(bytes);
        json.RootElement.GetProperty("schemaVersion").GetUInt32().ShouldEqual(1u);
        var read = SemanticIdentityCatalogSerializer.Deserialize(bytes);
        read.Semantics.Single(entry => entry.Address.Equals(source)).Id.ShouldEqual(SemanticId.Create(source));
        read.Semantics.Single(entry => entry.Address.Equals(stream)).Id.ShouldEqual(SemanticId.Create(stream));
        SemanticIdentityCatalogSerializer.Serialize(read).SequenceEqual(bytes).ShouldBeTrue();
    }
}
