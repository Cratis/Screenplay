// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_the_v35_golden : Specification
{
    ExecutableSemanticModel _model;
    byte[] _bytes;

    void Because()
    {
        golden_vector_regeneration.RegenerateWhenRequested();
        _model = canonical_serialization_golden_vectors.CreateSemanticModelV35();
        _bytes = SemanticModelSerializer.Serialize(_model);
    }

    [Fact] void should_pin_the_new_golden() => _bytes.ShouldEqual(canonical_serialization_golden_vectors.EsmV35Bytes);
    [Fact] void should_round_trip_strictly() => SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(_bytes)).ShouldEqual(_bytes);
    [Fact] void should_select_v35() => (_model.LanguageVersion, _model.SemanticVersion).ShouldEqual((LanguageVersion.V35, SemanticVersion.V35));
    [Fact] void should_leave_earlier_readers_unextended() => EsmSchemaV34Support.Supports(LanguageVersion.V35, SemanticVersion.V35).ShouldBeFalse();

    [Fact]
    void should_write_the_possible_events()
    {
        var json = Encoding.UTF8.GetString(_bytes);
        json.ShouldContain("\"possibleEvents\":[");
        json.ShouldContain("\"kind\":\"oneOf\"");
    }

    [Fact]
    void should_refuse_the_shape_in_every_earlier_reader()
    {
        var json = Encoding.UTF8.GetString(_bytes);
        foreach (var version in EsmSchemaV34Support.SemanticVersions)
        {
            var older = json.Replace("\"schemaVersion\":35", $"\"schemaVersion\":{version.Major}", StringComparison.Ordinal).Replace("\"35.0\"", $"\"{version}\"", StringComparison.Ordinal);
            Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(older))).ShouldBeOfExactType<InvalidSemanticContract>();
        }
    }
}
#endif
