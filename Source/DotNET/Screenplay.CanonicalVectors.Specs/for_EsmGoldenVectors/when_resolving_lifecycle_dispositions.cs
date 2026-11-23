// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_EsmGoldenVectors;

public class when_resolving_lifecycle_dispositions : given.corpus_lifecycle_vectors
{
    [Fact]
    void should_publish_exactly_the_seventeen_known_outcomes_without_duplicate_keys()
    {
        var entries = EsmGoldenVectors.LifecycleDispositions;
        entries.Length.ShouldEqual(17);
        entries.Select(entry => (entry.VectorId, entry.OwnerKind, entry.Owner, entry.Specification)).Distinct().Count().ShouldEqual(17);
        Assert.Equal(new[] { (36u, 3), (51u, 2), (52u, 2), (53u, 2), (54u, 2), (55u, 2), (56u, 2), (57u, 2) },
            entries.GroupBy(entry => entry.SemanticVersion.Major).OrderBy(group => group.Key).Select(group => (group.Key, group.Count())));
    }

    [Fact]
    void should_resolve_every_original_to_its_exact_frozen_bytes_and_result()
    {
        foreach (var entry in EsmGoldenVectors.LifecycleDispositions)
        {
            var vector = _vectors.Single(vector => vector.Id == entry.VectorId);
            var model = SemanticModelSerializer.Deserialize(vector.Bytes.AsSpan());
            model.SemanticVersion.ShouldEqual(entry.SemanticVersion);
            model.Revision.ShouldEqual(entry.SemanticRevision);
            Convert.ToHexStringLower(SHA256.HashData(vector.Bytes.AsSpan())).ShouldEqual(entry.Sha256);
            Specifications(model).ShouldContain((entry.OwnerKind, entry.Owner, entry.Specification));
            var result = SemanticLifecycleRunner.RunAll(model).Single(result => result.Owner == entry.Owner && result.Name == entry.Specification);
            result.Passed.ShouldBeFalse();
            Assert.Equal(entry.ExpectedFailures, result.Failures);
            entry.ExpectedFailures.IsEmpty.ShouldBeFalse();
            string.IsNullOrWhiteSpace(entry.Reason).ShouldBeFalse();
        }
    }

    [Fact]
    void should_resolve_every_replacement_without_cycles_or_execution_waivers()
    {
        foreach (var entry in EsmGoldenVectors.LifecycleDispositions)
        {
            EsmGoldenVectors.LifecycleDispositions.Any(other => other.VectorId == entry.ReplacementVectorId).ShouldBeFalse();
            var vector = _vectors.Single(vector => vector.Id == entry.ReplacementVectorId);
            var model = SemanticModelSerializer.Deserialize(vector.Bytes.AsSpan());
            var results = SemanticLifecycleRunner.RunAll(model);
            results.IsEmpty.ShouldBeFalse();
            results.All(result => result.Passed && result.Failures.IsEmpty).ShouldBeTrue();
            results.Any(result => result.Owner == entry.Owner && result.Name == entry.Specification).ShouldBeTrue();
        }
    }

    [Fact]
    void should_not_relax_strict_admission_of_disposed_vectors()
    {
        foreach (var id in EsmGoldenVectors.LifecycleDispositions.Select(entry => entry.VectorId).Distinct())
        {
            var vector = _vectors.Single(vector => vector.Id == id);
            var malformed = JsonNode.Parse(vector.Bytes.AsSpan())!;
            malformed["revision"] = "rev1:" + new string('0', 64);
            Assert.Throws<InvalidSemanticContract>(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(malformed.ToJsonString())));
            SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(vector.Bytes.AsSpan())).SequenceEqual(vector.Bytes).ShouldBeTrue();
        }
    }
}
