// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_EsmGoldenVectors;

public class when_reading_the_revised_v36_lifecycle_vector : given.revised_lifecycle_models
{
    ExecutableSemanticModel _revised;

    void Because()
    {
        _revised = Revise36();
        Regenerate(36, _revised);
    }

    [Fact]
    void should_preserve_the_original_hash_and_revision()
    {
        var original = EsmGoldenVectors.Get(36);
        Convert.ToHexStringLower(SHA256.HashData(original.Bytes.AsSpan())).ShouldEqual("70666a008fd778a38501c729040d00539bf97d5685d3ff4000d7c1811ece4acc");
        original.SemanticRevision.ToString().ShouldEqual("rev1:432ee29dd9f149cbe03050c91b5895fbafe8ddabbad44768c1f36cf4b9941a97");
    }

    [Fact]
    void should_pin_the_replacement_and_strict_round_trip()
    {
        var vector = EsmGoldenVectors.LifecycleV36R2;
        Convert.ToHexStringLower(SHA256.HashData(vector.Bytes.AsSpan())).ShouldEqual("6f8c417543c69b6e5f948c3e7daba57ee61c22cd40722811b2a8e59f685b00e5");
        vector.SemanticRevision.ToString().ShouldEqual("rev1:0baeb43590fad565e575ff8a56afabb9059fd34605059874cee3b1e65e8ce0f3");
        _revised.LanguageVersion.ShouldEqual(new LanguageVersion(36, 0));
        _revised.SemanticVersion.ShouldEqual(SemanticVersion.V36);
        SemanticModelSerializer.Serialize(_revised).SequenceEqual(vector.Bytes).ShouldBeTrue();
        SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(vector.Bytes.AsSpan())).SequenceEqual(vector.Bytes).ShouldBeTrue();
    }

    [Fact]
    void should_change_only_the_three_collect_terminal_expectations_and_revision()
    {
        var original = JsonNode.Parse(EsmGoldenVectors.Get(36).Bytes.AsSpan())!;
        var revised = JsonNode.Parse(EsmGoldenVectors.LifecycleV36R2.Bytes.AsSpan())!;
        original["revision"]!.ToString().ShouldNotEqual(revised["revision"]!.ToString());
        original["revision"] = revised["revision"]!.DeepClone();
        var collect = original["application"]!["workflows"]!.AsArray().Single(workflow => workflow!["name"]!.ToString() == "Collect")!;
        foreach (var name in CollectSpecifications)
        {
            var specification = collect["specifications"]!.AsArray().Single(specification => specification!["name"]!.ToString() == name)!;
            var terminal = specification["then"]!.AsArray().Single(expectation => expectation!["kind"]!.ToString() == "completes")!;
            terminal["kind"] = "waits";
            terminal["target"] = "Final";
        }
        JsonNode.DeepEquals(original, revised).ShouldBeTrue();
    }

    [Fact]
    void should_pass_all_seven_replacement_results_and_retain_exactly_three_original_failures()
    {
        var revised = SemanticLifecycleRunner.RunAll(_revised);
        revised.Length.ShouldEqual(7);
        revised.All(result => result.Passed).ShouldBeTrue();
        var original = SemanticLifecycleRunner.RunAll(SemanticModelSerializer.Deserialize(EsmGoldenVectors.Get(36).Bytes.AsSpan()));
        original.Length.ShouldEqual(7);
        var failed = original.Where(result => !result.Passed).ToArray();
        Assert.Equal(CollectSpecifications.Order(), failed.Select(result => result.Name).Order());
        foreach (var result in failed)
        {
            result.Owner.ShouldEqual("Collect");
            Assert.Equal(["Expected the run to complete, but the run waits at 'Final'."], result.Failures);
        }
    }
}
