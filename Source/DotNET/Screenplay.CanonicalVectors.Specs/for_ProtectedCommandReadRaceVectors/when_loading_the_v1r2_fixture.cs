// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.CanonicalVectors.Specs.for_ProtectedCommandReadRaceVectors.given;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ProtectedCommandReadRaceVectors;

public class when_loading_the_v1r2_fixture : Specification
{
    ImmutableArray<byte> _bytes;
    JsonNode _document;
    JsonArray _original;

    void Establish()
    {
        if (Environment.GetEnvironmentVariable(RaceVectorExecution.RegenerationVariableV1R2) == "1") RaceVectorExecution.RegenerateV1R2();
        _bytes = ProtectedCommandReadRaceVectors.BytesV1R2;
        _document = JsonNode.Parse(_bytes.AsSpan())!;
        _original = JsonNode.Parse(ProtectedCommandReadRaceVectors.Bytes.AsSpan())!["vectors"]!.AsArray();
    }

    [Fact] void should_keep_the_v1_contract_version() => ProtectedCommandReadRaceVectors.VersionV1R2.ShouldEqual(ProtectedCommandReadRaceVectors.Version);
    [Fact] void should_pin_the_contract_version() => _document["version"]!.GetValue<int>().ShouldEqual(ProtectedCommandReadRaceVectors.VersionV1R2);
    [Fact] void should_pin_the_exact_bytes() => Convert.ToHexString(SHA256.HashData(_bytes.AsSpan())).ToLowerInvariant().ShouldEqual(ProtectedCommandReadRaceVectors.Sha256V1R2);
    [Fact] void should_pin_the_fixture_format() => RaceVectorExecution.Format(_document).ShouldEqual(Encoding.UTF8.GetString(_bytes.AsSpan()));
    [Fact] void should_match_the_embedded_file() => _bytes.SequenceEqual(File.ReadAllBytes(RaceVectorExecution.ResourcePathV1R2("races.v1r2.json"))).ShouldBeTrue();
    [Fact] void should_have_an_additive_model_name() => ProtectedCommandReadRaceVectors.ModelV1R2.Name.ShouldEqual("protected-command-reads/races-v1r2");
    [Fact] void should_keep_the_esm_bytes() => ProtectedCommandReadRaceVectors.ModelV1R2.EsmBytes.SequenceEqual(ProtectedCommandReadRaceVectors.Model.EsmBytes).ShouldBeTrue();
    [Fact] void should_keep_the_model_revision() => ProtectedCommandReadRaceVectors.ModelV1R2.SemanticRevision.ShouldEqual(ProtectedCommandReadRaceVectors.Model.SemanticRevision);
    [Fact] void should_keep_the_source_bytes() => ProtectedCommandReadRaceVectors.ModelV1R2.SourceForms.Single().Documents.Single().Bytes.SequenceEqual(ProtectedCommandReadRaceVectors.Model.SourceForms.Single().Documents.Single().Bytes).ShouldBeTrue();
    [Fact] void should_keep_the_identity_catalog() => ProtectedCommandReadRaceVectors.ModelV1R2.SourceForms.Single().IdentityCatalogBytes.SequenceEqual(ProtectedCommandReadRaceVectors.Model.SourceForms.Single().IdentityCatalogBytes).ShouldBeTrue();
    [Fact] void should_publish_seventeen_vectors() => _document["vectors"]!.AsArray().Count.ShouldEqual(17);

    [Fact]
    void should_preserve_every_v1_vector_in_order()
    {
        _original.Count.ShouldEqual(16);
        for (var index = 0; index < _original.Count; index++) JsonNode.DeepEquals(_original[index], _document["vectors"]![index]).ShouldBeTrue();
    }

    [Fact]
    void should_pin_the_added_constraint_precedence_vector()
    {
        var vectors = _document["vectors"]!.AsArray();
        var added = vectors[^1]!;
        added["name"]!.GetValue<string>().ShouldEqual("read-race-and-late-uniqueness-claim");
        var expected = added["commits"]![0]!["expect"]!;
        expected["outcome"]!.GetValue<string>().ShouldEqual("rejected");
        expected["category"]!.GetValue<string>().ShouldEqual("Constraint");
        expected["code"]!.GetValue<string>().ShouldEqual("UniqueValue");
    }

    [Fact]
    void should_match_every_outcome_through_the_public_calls()
    {
        var plan = RaceVectorExecution.Plan(SemanticModelSerializer.Deserialize(ProtectedCommandReadRaceVectors.ModelV1R2.EsmBytes.AsSpan()));
        foreach (var vector in _document["vectors"]!.AsArray()) RaceVectorExecution.Verify(plan, vector!);
    }
}
