// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_the_v30_golden : Specification
{
    byte[] _bytes;
    ExecutableSemanticModel _model;

    void Because()
    {
        golden_vector_regeneration.RegenerateWhenRequested();
        _model = canonical_serialization_golden_vectors.CreateSemanticModelV30();
        _bytes = SemanticModelSerializer.Serialize(_model);
    }

    [Fact] void should_pin_the_new_golden() => _bytes.ShouldEqual(canonical_serialization_golden_vectors.EsmV30Bytes);
    [Fact] void should_round_trip_strictly() => SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(_bytes)).ShouldEqual(_bytes);
    [Fact] void should_select_v30() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V30);
    [Fact] void should_leave_earlier_readers_unextended() => EsmSchemaV29Support.Supports(LanguageVersion.V30, SemanticVersion.V30).ShouldBeFalse();
    [Fact] void should_write_the_pinned_generated_values() => Encoding.UTF8.GetString(_bytes).ShouldContain("\"givenGenerated\":");
    [Fact] void should_write_the_second_event_source() => Encoding.UTF8.GetString(_bytes).ShouldContain("StockReserved");

    [Fact]
    void should_refuse_the_shape_in_every_earlier_reader()
    {
        foreach (var version in EsmSchemaV29Support.SemanticVersions)
        {
            var json = Encoding.UTF8.GetString(_bytes).Replace("\"schemaVersion\":30", $"\"schemaVersion\":{version.Major}", StringComparison.Ordinal).Replace("\"30.0\"", $"\"{version}\"", StringComparison.Ordinal);
            var error = Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(json)));
            error.ShouldBeOfExactType<InvalidSemanticContract>();
            error.Message.Contains("revision", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
        }
    }
}
