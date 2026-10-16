// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_the_v28_golden : Specification
{
    byte[] _bytes;
    ExecutableSemanticModel _model;

    void Because()
    {
        golden_vector_regeneration.RegenerateWhenRequested();
        _model = canonical_serialization_golden_vectors.CreateSemanticModelV28();
        _bytes = SemanticModelSerializer.Serialize(_model);
    }

    [Fact] void should_pin_the_new_golden() => _bytes.ShouldEqual(canonical_serialization_golden_vectors.EsmV28Bytes);
    [Fact] void should_round_trip_strictly() => SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(_bytes)).ShouldEqual(_bytes);
    [Fact] void should_select_v28() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V28);
    [Fact] void should_leave_earlier_readers_unextended() => EsmSchemaV27Support.Supports(LanguageVersion.V28, SemanticVersion.V28).ShouldBeFalse();
    [Fact] void should_keep_the_literal_as_canonical_text() => Encoding.UTF8.GetString(_bytes).ShouldContain("3f2504e0-4f89-41d3-9a0c-0305e82c3301");

    [Fact]
    void should_refuse_the_shape_in_every_earlier_reader()
    {
        foreach (var version in EsmSchemaV27Support.SemanticVersions)
        {
            var json = Encoding.UTF8.GetString(_bytes).Replace("\"schemaVersion\":28", $"\"schemaVersion\":{version.Major}", StringComparison.Ordinal).Replace("\"28.0\"", $"\"{version}\"", StringComparison.Ordinal);
            var error = Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(json)));
            error.ShouldBeOfExactType<InvalidSemanticContract>();
            error.Message.Contains("revision", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
        }
    }
}
