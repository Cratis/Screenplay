// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Semantics.Serialization.given;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_using_the_maximum_canonical_depth : Specification
{
    const int DeepestSerializableFeatureLevels = 29;
    byte[] _serialized;
    byte[] _reserialized;
    int _maximumObservedDepth;
    Exception _writeError;
    Exception _readError;

    void Because()
    {
        var deepestAccepted = canonical_serialization_golden_vectors.CreateNestedFeatureModel(DeepestSerializableFeatureLevels);
        _serialized = SemanticModelSerializer.Serialize(deepestAccepted);
        _reserialized = SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(_serialized));
        _maximumObservedDepth = MaximumObservedDepth(_serialized);
        _writeError = Catch.Exception(() => canonical_serialization_golden_vectors.CreateNestedFeatureModel(DeepestSerializableFeatureLevels + 1));
        _readError = Catch.Exception(() => SemanticModelSerializer.Deserialize(CreateRawModel(DeepestSerializableFeatureLevels + 1)));
    }

    [Fact] void should_use_one_shared_maximum_depth() => CanonicalJson.MaximumDepth.ShouldEqual(64);
    [Fact] void should_place_the_practical_feature_boundary_at_sixty_three_containers() => _maximumObservedDepth.ShouldEqual(62);
    [Fact] void should_serialize_and_deserialize_the_deepest_model_byte_identically() => _reserialized.SequenceEqual(_serialized).ShouldBeTrue();
    [Fact] void should_reject_writing_one_more_feature_level() => _writeError.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_reading_one_more_raw_feature_level() => _readError.ShouldBeOfExactType<InvalidSemanticContract>();

    static int MaximumObservedDepth(ReadOnlySpan<byte> json)
    {
        var reader = new Utf8JsonReader(json, CanonicalJson.ReaderOptions);
        var maximum = 0;
        while (reader.Read())
        {
            maximum = Math.Max(maximum, reader.CurrentDepth);
        }

        return maximum;
    }

    static byte[] CreateRawModel(int featureLevels)
    {
        var json = new StringBuilder();
        json
            .Append("{\"schema\":\"cratis.screenplay.esm\",\"schemaVersion\":1,\"languageVersion\":\"1.0\",\"semanticVersion\":\"1.0\",\"revision\":\"rev1:")
            .Append('0', 64)
            .Append("\",\"application\":{\"id\":\"sem1:")
            .Append('0', 63)
            .Append("1\",\"name\":\"Depth Boundary\",\"concepts\":[],\"types\":[],\"modules\":[{\"id\":\"sem1:")
            .Append('0', 63)
            .Append("2\",\"name\":\"Depth\",\"features\":[");
        AppendFeature(json, 1, featureLevels);
        json.Append("]}]}}");
        return Encoding.UTF8.GetBytes(json.ToString());
    }

    static void AppendFeature(StringBuilder json, int level, int featureLevels)
    {
        json.Append("{\"id\":\"sem1:").Append(level.ToString("x64")).Append("\",\"name\":\"Level").Append(level).Append("\",\"features\":[");
        if (level < featureLevels)
        {
            AppendFeature(json, level + 1, featureLevels);
        }

        json.Append("],\"slices\":[]}");
    }
}
#endif
