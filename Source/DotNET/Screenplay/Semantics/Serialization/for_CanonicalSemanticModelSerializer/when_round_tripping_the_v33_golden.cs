// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_the_v33_golden : Specification
{
    byte[] _bytes;
    ExecutableSemanticModel _model;

    void Because()
    {
        golden_vector_regeneration.RegenerateWhenRequested();
        _model = canonical_serialization_golden_vectors.CreateSemanticModelV33();
        _bytes = SemanticModelSerializer.Serialize(_model);
    }

    [Fact] void should_pin_the_new_golden() => _bytes.ShouldEqual(canonical_serialization_golden_vectors.EsmV33Bytes);
    [Fact] void should_round_trip_strictly() => SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(_bytes)).ShouldEqual(_bytes);
    [Fact] void should_select_v33() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V33);
    [Fact] void should_leave_earlier_readers_unextended() => EsmSchemaV32Support.Supports(LanguageVersion.V33, SemanticVersion.V33).ShouldBeFalse();
    [Fact] void should_write_the_event_source_key() => _model.Application.Modules.Single().Features.Single().Slices[0].Captures.Single().EventsSource!.KeyIsEventSource.ShouldBeTrue();

    [Fact]
    void should_refuse_the_shape_in_every_earlier_reader()
    {
        foreach (var version in EsmSchemaV32Support.SemanticVersions)
        {
            var json = Encoding.UTF8.GetString(_bytes).Replace("\"schemaVersion\":32", $"\"schemaVersion\":{version.Major}", StringComparison.Ordinal).Replace("\"32.0\"", $"\"{version}\"", StringComparison.Ordinal);
            var error = Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(json)));
            error.ShouldBeOfExactType<InvalidSemanticContract>();
            error.Message.Contains("revision", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
        }
    }
}
