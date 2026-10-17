// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Globalization;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_GoldenVectors;

public class when_regenerating_the_current_full_golden : Specification
{
    string _directory;

    void Establish()
    {
        _directory = Path.GetFullPath(Path.Combine(frozen_esm_goldens.Directory(), "../../../../../..", ".ai-work", "golden", "regeneration", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(_directory);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_write_only_the_current_full_golden_and_fail_deliberately(bool includeSidecars)
    {
        var current = frozen_esm_goldens.CurrentVersion.ToString(CultureInfo.InvariantCulture);
        var request = includeSidecars ? "1" : current;
        var error = Catch.Exception(() => golden_vector_regeneration.Regenerate(request, _directory));
        error.ShouldBeOfExactType<GoldenVectorsRegenerated>();
        error.Message.ShouldContain("regenerated");
        var fullName = $"full-esm-v{current}.json";
        Directory.GetFiles(_directory, "full-esm-*.json").Select(Path.GetFileName).ShouldContainOnly([fullName]);
        File.ReadAllBytes(Path.Combine(_directory, fullName)).ShouldEqual(canonical_serialization_golden_vectors.EsmV28Bytes);
        Directory.GetFiles(_directory).Length.ShouldEqual(includeSidecars ? 5 : 1);
    }

    void Destroy() => Directory.Delete(_directory, recursive: true);
}
#endif
