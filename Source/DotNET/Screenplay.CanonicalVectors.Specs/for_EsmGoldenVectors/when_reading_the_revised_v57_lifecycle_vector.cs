// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_EsmGoldenVectors;

public class when_reading_the_revised_v57_lifecycle_vector : given.revised_lifecycle_models
{
    ExecutableSemanticModel _revised;

    void Because()
    {
        _revised = Revise57();
        Regenerate(57, _revised);
    }

    [Fact]
    void should_preserve_the_original_hash_and_revision()
    {
        var original = EsmGoldenVectors.Get(57);
        Convert.ToHexStringLower(SHA256.HashData(original.Bytes.AsSpan())).ShouldEqual("cb9570c09a0a9b0b44ae41941a668bd5b081132fe4ffe524a2d8c3643ced2651");
        original.SemanticRevision.ToString().ShouldEqual("rev1:203417cbd81e9f691285dfdc1d3cef68ffb2b817216839fe84fdf7f171409464");
    }

    [Fact]
    void should_pin_the_replacement_and_strict_round_trip()
    {
        var vector = EsmGoldenVectors.LifecycleV57R2;
        Convert.ToHexStringLower(SHA256.HashData(vector.Bytes.AsSpan())).ShouldEqual("994068e2ac37b1d2b2dbbaaaff7d1ff6b03383ad22a2508e65ce2f25c8c25201");
        vector.SemanticRevision.ToString().ShouldEqual("rev1:b8b6d98bedee652a9e8eae21f15f358860b8ba85fee247d4bd291a371b37e6d6");
        _revised.LanguageVersion.ShouldEqual(new LanguageVersion(57, 0));
        _revised.SemanticVersion.ShouldEqual(SemanticVersion.V57);
        SemanticModelSerializer.Serialize(_revised).SequenceEqual(vector.Bytes).ShouldBeTrue();
        SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(vector.Bytes.AsSpan())).SequenceEqual(vector.Bytes).ShouldBeTrue();
    }

    [Fact]
    void should_change_only_the_two_appended_parcel_ids_and_revision()
    {
        var original = JsonNode.Parse(EsmGoldenVectors.Get(57).Bytes.AsSpan())!;
        var revised = JsonNode.Parse(EsmGoldenVectors.LifecycleV57R2.Bytes.AsSpan())!;
        original["revision"]!.ToString().ShouldNotEqual(revised["revision"]!.ToString());
        original["revision"] = revised["revision"]!.DeepClone();
        var application = original["application"]!;
        var packed = application["modules"]!.AsArray().SelectMany(module => module!["features"]!.AsArray())
            .SelectMany(feature => feature!["slices"]!.AsArray()).SelectMany(slice => slice!["events"]!.AsArray())
            .Single(contract => contract!["name"]!.ToString() == "OrderPacked")!;
        foreach (var kind in new[] { "stateMachines", "workflows" })
        {
            var append = application[kind]!.AsArray().Single()!["specifications"]!.AsArray().Single()!["whenAppended"]!;
            append["values"]!.AsArray().Count.ShouldEqual(0);
            append["values"] = new JsonArray(new JsonObject
            {
                ["targetProperty"] = packed["properties"]!.AsArray().Single()!["id"]!.DeepClone(),
                ["value"] = append["eventSource"]!["value"]!.DeepClone()
            });
        }
        JsonNode.DeepEquals(original, revised).ShouldBeTrue();
    }

    [Fact]
    void should_pass_both_replacement_lifecycle_specifications()
    {
        var results = SemanticLifecycleRunner.RunAll(_revised);
        Assert.Equal(new[] { ("ParcelFlow", "PackingBooksAPickup"), ("Pickup", "BookingThenCancelling") }, results.Select(result => (result.Owner, result.Name)));
        Assert.True(results.All(result => result.Passed), string.Join('\n', results.SelectMany(result => result.Failures)));
    }
}
