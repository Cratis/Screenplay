// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Cryptography;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_EsmGoldenVectors;

public class when_running_corpus_lifecycle_specifications : given.corpus_lifecycle_vectors
{
    readonly HashSet<EsmLifecycleVectorDisposition> _consumed = [];
    int _lifecycleVectors;
    int _results;

    void Because()
    {
        foreach (var (id, bytes) in _vectors)
        {
            var model = SemanticModelSerializer.Deserialize(bytes.AsSpan());
            var expected = Specifications(model).ToArray();
            var results = SemanticLifecycleRunner.RunAll(model);
            Assert.True(expected.Select(key => (key.Owner, key.Name)).SequenceEqual(results.Select(result => (result.Owner, result.Name))),
                $"{id} (ESM {model.SemanticVersion}): lifecycle results do not match the declared specifications.");
            if (expected.Length > 0) _lifecycleVectors++;
            _results += results.Length;
            for (var index = 0; index < results.Length; index++)
            {
                AssertOutcome((id, bytes), model, expected[index].Kind, results[index]);
            }
        }
    }

    [Fact] void should_execute_lifecycle_bearing_vectors() => _lifecycleVectors.ShouldBeGreaterThan(0);
    [Fact] void should_execute_nonzero_lifecycle_results() => _results.ShouldBeGreaterThan(0);
    [Fact] void should_consume_every_disposition_exactly_once() => Assert.Equal(EsmGoldenVectors.LifecycleDispositions.Length, _consumed.Count);

    void AssertOutcome((string Id, ImmutableArray<byte> Bytes) vector, ExecutableSemanticModel model, string kind, SemanticLifecycleSpecificationResult result)
    {
        var label = $"{vector.Id} (ESM {model.SemanticVersion}) / {kind} {result.Owner}/{result.Name}";
        var disposition = EsmGoldenVectors.LifecycleDispositions.SingleOrDefault(disposition =>
            disposition.VectorId == vector.Id && disposition.OwnerKind == kind && disposition.Owner == result.Owner && disposition.Specification == result.Name);
        if (disposition is null)
        {
            Assert.True(result.Passed && result.Failures.IsEmpty, $"{label}: {string.Join(';', result.Failures)}");
            return;
        }

        Assert.Equal(disposition.SemanticVersion, model.SemanticVersion);
        Assert.Equal(disposition.SemanticRevision, model.Revision);
        Assert.Equal(disposition.Sha256, Convert.ToHexStringLower(SHA256.HashData(vector.Bytes.AsSpan())));
        Assert.False(result.Passed, $"Unused disposition: {label} now passes.");
        Assert.True(disposition.ExpectedFailures.SequenceEqual(result.Failures), $"{label}: {string.Join(';', result.Failures)}");
        Assert.True(_consumed.Add(disposition), $"Disposition consumed twice: {label}");
    }
}
