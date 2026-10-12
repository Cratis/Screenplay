// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.CanonicalCorpus;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_EsmGoldenVectors;

public class when_reading_full_vectors : given.golden_files
{
    EsmGoldenVector[] _vectors;

    void Because() => _vectors = [.. EsmGoldenVectors.Versions.Select(EsmGoldenVectors.Get)];

    [Fact] void should_preserve_every_golden_file_byte() => _vectors.All(vector => vector.Bytes.SequenceEqual(File.ReadAllBytes(Path.Combine(_goldenFolder, $"full-esm-v{vector.Version.ToString(CultureInfo.InvariantCulture)}.json")))).ShouldBeTrue();
    [Fact]
    void should_select_each_vectors_own_version()
    {
        foreach (var vector in _vectors)
        {
            var model = SemanticModelSerializer.Deserialize(vector.Bytes.AsSpan());
            (model.SemanticVersion.Major == vector.Version && model.SemanticVersion.Minor == 0).ShouldBeTrue();
            (model.LanguageVersion.Major == vector.Version && model.LanguageVersion.Minor == 0).ShouldBeTrue();
        }
    }
    [Fact] void should_preserve_each_semantic_revision() => _vectors.All(vector => SemanticModelSerializer.Deserialize(vector.Bytes.AsSpan()).Revision == vector.SemanticRevision).ShouldBeTrue();
    [Fact] void should_round_trip_through_the_strict_reader() => _vectors.All(vector => SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(vector.Bytes.AsSpan())).SequenceEqual(vector.Bytes)).ShouldBeTrue();
}
