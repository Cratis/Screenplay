// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_the_v26_golden : Specification
{
    byte[] _bytes;
    ExecutableSemanticModel _model;

    void Because()
    {
        golden_vector_regeneration.RegenerateWhenRequested();
        _model = canonical_serialization_golden_vectors.CreateSemanticModelV26();
        _bytes = SemanticModelSerializer.Serialize(_model);
    }

    [Fact] void should_pin_the_new_golden() => _bytes.ShouldEqual(canonical_serialization_golden_vectors.EsmV26Bytes);
    [Fact] void should_round_trip_strictly() => SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(_bytes)).ShouldEqual(_bytes);
    [Fact] void should_select_v26() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V26);
    [Fact] void should_leave_earlier_readers_unextended() => EsmSchemaV25Support.Supports(LanguageVersion.V26, SemanticVersion.V26).ShouldBeFalse();
    [Fact] void should_write_canonical_utc_tick_precision_text() => Encoding.UTF8.GetString(_bytes).ShouldContain("2026-01-01T00:00:00.0000000Z");

    [Fact]
    void should_refuse_the_shape_in_every_earlier_reader()
    {
        foreach (var version in EsmSchemaV25Support.SemanticVersions)
        {
            var json = Encoding.UTF8.GetString(_bytes).Replace("\"schemaVersion\":26", $"\"schemaVersion\":{version.Major}", StringComparison.Ordinal).Replace("\"26.0\"", $"\"{version}\"", StringComparison.Ordinal);
            var error = Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(json)));
            error.ShouldBeOfExactType<InvalidSemanticContract>();
            error.Message.Contains("revision", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
        }
    }
}
