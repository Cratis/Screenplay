// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.CanonicalVectors.Specs.for_ProtectedCommandReadRaceVectors.given;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ProtectedCommandReadRaceVectors;

public class when_loading_the_v1r3_replacement : Specification
{
    JsonNode _replacement;

    void Establish()
    {
        if (Environment.GetEnvironmentVariable(RaceVectorExecution.RegenerationVariableV1R3) == "1") RaceVectorExecution.RegenerateV1R3();
        _replacement = JsonNode.Parse(ProtectedReadReplacements.RacesV1R3.AsSpan())!;
    }

    [Fact]
    void should_pin_the_bytes_format_and_unchanged_fixture_contract_version()
    {
        var bytes = ProtectedReadReplacements.RacesV1R3;
        Hash(bytes.ToArray()).ShouldEqual(ProtectedReadReplacements.RacesV1R3Sha256);
        RaceVectorExecution.Format(_replacement).ShouldEqual(Encoding.UTF8.GetString(bytes.AsSpan()));
        bytes.SequenceEqual(File.ReadAllBytes(RaceVectorExecution.ResourcePathV1R3("races.v1r3.json"))).ShouldBeTrue();
        _replacement["version"]!.GetValue<int>().ShouldEqual(ProtectedCommandReadRaceVectors.VersionV1R2);
        _replacement["vectors"]!.AsArray().Count.ShouldEqual(17);
        ProtectedReadReplacements.RaceModelV1R3.EsmBytes.SequenceEqual(ProtectedCommandReadRaceVectors.Model.EsmBytes).ShouldBeTrue();
        ProtectedReadReplacements.RaceModelV1R3.SemanticRevision.ShouldEqual(ProtectedCommandReadRaceVectors.Model.SemanticRevision);
    }

    [Fact]
    void should_differ_from_the_frozen_v1r2_only_at_the_disposed_message()
    {
        var original = JsonNode.Parse(ProtectedCommandReadRaceVectors.BytesV1R2.AsSpan())!;
        var correction = original["vectors"]!.AsArray().Single(vector => vector!["name"]!.GetValue<string>() == "rejected-decision")!;
        correction["commits"]![0]!["expect"]!["message"]!.GetValue<string>().ShouldEqual("View 'read' does not exist.");
        correction["commits"]![0]!["expect"]!["message"] = "View does not exist.";
        JsonNode.DeepEquals(original, _replacement).ShouldBeTrue();
    }

    [Fact]
    void should_run_every_replacement_without_a_disposition()
    {
        var plan = RaceVectorExecution.Plan(SemanticModelSerializer.Deserialize(ProtectedReadReplacements.RaceModelV1R3.EsmBytes.AsSpan()));
        foreach (var vector in _replacement["vectors"]!.AsArray()) RaceVectorExecution.Verify(plan, vector!);
    }

    [Fact]
    void should_pin_one_disposition_per_original_fixture_and_no_replacement_disposition()
    {
        var dispositions = ProtectedReadReplacements.RaceDispositions;
        dispositions.Length.ShouldEqual(2);
        dispositions.Select(disposition => disposition.VectorId).Distinct(StringComparer.Ordinal).Count().ShouldEqual(2);
        foreach (var disposition in dispositions)
        {
            var bytes = disposition.VectorId switch
            {
                "ProtectedCommandReadRaceVectors.Bytes" => ProtectedCommandReadRaceVectors.Bytes,
                "ProtectedCommandReadRaceVectors.BytesV1R2" => ProtectedCommandReadRaceVectors.BytesV1R2,
                _ => throw new InvalidOperationException($"Unknown original '{disposition.VectorId}'.")
            };
            disposition.Sha256.ShouldEqual(Hash(bytes.ToArray()));
            disposition.SemanticRevision.ShouldEqual(ProtectedCommandReadRaceVectors.Model.SemanticRevision);
            disposition.ReplacementVectorId.ShouldEqual("ProtectedReadReplacements.RacesV1R3");
            disposition.CaseName.ShouldEqual("rejected-decision");
            disposition.CommitIndex.ShouldEqual(0);
            disposition.OriginalMessage.ShouldEqual("View 'read' does not exist.");
            disposition.ReplacementMessage.ShouldEqual("View does not exist.");
            disposition.ExpectedFailures.Length.ShouldEqual(1);
        }
    }

    static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
}
