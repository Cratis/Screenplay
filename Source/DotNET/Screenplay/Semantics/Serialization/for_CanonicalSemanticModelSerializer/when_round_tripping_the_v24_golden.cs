// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_the_v24_golden : Specification
{
    byte[] _bytes;
    ExecutableSemanticModel _model;
    void Because()
    {
        golden_vector_regeneration.RegenerateWhenRequested();
        _model = canonical_serialization_golden_vectors.CreateSemanticModelV24();
        _bytes = SemanticModelSerializer.Serialize(_model);
    }
    [Fact] void should_pin_the_new_golden() => _bytes.ShouldEqual(canonical_serialization_golden_vectors.EsmV24Bytes);
    [Fact] void should_round_trip_strictly() => SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(_bytes)).ShouldEqual(_bytes);
    [Fact] void should_select_v24() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V24);
    [Fact] void should_write_the_mode_as_the_final_application_member() => Encoding.UTF8.GetString(_bytes).ShouldContain("\"commandReadMode\":\"protected\"}");
    [Fact] void should_preserve_earlier_support() => EsmSchemaV22Support.Supports(LanguageVersion.V24, SemanticVersion.V24).ShouldBeFalse();
    [Theory]
    [InlineData("\"protected\"", "null")]
    [InlineData("\"protected\"", "false")]
    [InlineData("\"protected\"", "\"advisory\"")]
    [InlineData("\"presence\":\"required\"", "\"presence\":\"optional\"")]
    [InlineData("\"commandReadMode\":\"protected\"", "\"commandReadMode\":\"protected\",\"commandReadMode\":\"protected\"")]
    public void should_refuse_invalid_wire_contracts(string original, string replacement)
    {
        var json = Encoding.UTF8.GetString(_bytes);
        json.Contains(original, StringComparison.Ordinal).ShouldBeTrue();
        Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(json.Replace(original, replacement, StringComparison.Ordinal)))).ShouldBeOfExactType<InvalidSemanticContract>();
    }
    [Fact]
    void should_refuse_new_members_in_an_earlier_reader()
    {
        var json = Encoding.UTF8.GetString(_bytes).Replace("\"schemaVersion\":24", "\"schemaVersion\":22", StringComparison.Ordinal).Replace("\"24.0\"", "\"22.0\"", StringComparison.Ordinal);
        var exception = Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(json)));
        exception.ShouldBeOfExactType<InvalidSemanticContract>();
        exception.Message.Contains("revision", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
    }
}
