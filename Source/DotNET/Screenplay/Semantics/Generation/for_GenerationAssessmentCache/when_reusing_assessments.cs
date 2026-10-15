// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Generation.for_GenerationAssessmentCache;

public class when_reusing_assessments
{
    [Fact]
    void should_reuse_the_complete_key_and_bound_retention_with_fifo_eviction()
    {
        using var vectors = JsonDocument.Parse(File.ReadAllText(VectorPath()));
        var cache = new GenerationAssessmentCache(vectors.RootElement.GetProperty("capacity").GetInt32());
        var computations = 0;
        var requests = vectors.RootElement.GetProperty("requests").EnumerateArray().ToArray();
        requests.ShouldNotBeEmpty();
        foreach (var request in requests)
        {
            var checksum = request.TryGetProperty("checksum", out var algorithm) ? algorithm.GetString() : null;
            var result = cache.Get(request.GetProperty("pattern").GetString()!, checksum, request.GetProperty("year").GetInt32(), () =>
            {
                computations++;
                return BigInteger.Parse(request.GetProperty("value").GetString()!, CultureInfo.InvariantCulture);
            });
            result.ToString(CultureInfo.InvariantCulture).ShouldEqual(request.GetProperty("expected").GetString());
            computations.ShouldEqual(request.GetProperty("computations").GetInt32());
        }
    }

    [Fact]
    void should_not_retain_failed_assessments()
    {
        var cache = new GenerationAssessmentCache();
        Catch.Exception(() => cache.Get("#", null, 2024, () => throw new InvalidSemanticContract("failed assessment"))).ShouldBeOfExactType<InvalidSemanticContract>();
        cache.Get("#", null, 2024, () => new(10)).ShouldEqual(new BigInteger(10));
    }

    [Fact]
    void should_not_retain_oversized_keys()
    {
        var cache = new GenerationAssessmentCache();
        var pattern = new string('A', 4097);
        cache.Get(pattern, null, 2024, () => BigInteger.One).ShouldEqual(BigInteger.One);
        cache.Get(pattern, null, 2024, () => new(2)).ShouldEqual(new BigInteger(2));
    }

    [Fact]
    void should_share_one_in_flight_calculation_for_the_same_key()
    {
        var cache = new GenerationAssessmentCache();
        var computations = 0;
        Parallel.For(0, 32, _ => cache.Get("#", null, 2024, () =>
        {
            Interlocked.Increment(ref computations);
            return new(10);
        }).ShouldEqual(new BigInteger(10)));
        computations.ShouldEqual(1);
    }

    static string VectorPath([CallerFilePath] string path = "")
    {
        var root = Directory.GetParent(path);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "AGENTS.md"))) root = root.Parent;

        return Path.Combine(root!.FullName, "Source/Screenplay/Compiler/Conformance/generation-assessment-cache.json");
    }
}
