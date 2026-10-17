// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Globalization;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_GoldenVectors;

public class when_requesting_a_frozen_full_golden : Specification
{
    [Fact]
    void should_refuse_every_older_version_before_writing_anything()
    {
        var versions = Enumerable.Range(2, frozen_esm_goldens.CurrentVersion - 2).ToArray();
        versions.ShouldNotBeEmpty();

        // 1 remains the regenerate-current-and-sidecars alias, so use 01 to request v1 explicitly.
        foreach (var request in versions.Select(version => version.ToString(CultureInfo.InvariantCulture)).Prepend("01"))
        {
            var error = Catch.Exception(() => golden_vector_regeneration.Regenerate(request, "not-a-golden-directory"));
            error.ShouldBeOfExactType<GoldenVectorsRegenerated>();
            error.Message.ShouldContain("frozen");
        }
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("999")]
    [InlineData("-1")]
    void should_refuse_unknown_requests(string request) => Catch.Exception(() => golden_vector_regeneration.Regenerate(request, "not-a-golden-directory")).ShouldBeOfExactType<GoldenVectorsRegenerated>();
}
#endif
