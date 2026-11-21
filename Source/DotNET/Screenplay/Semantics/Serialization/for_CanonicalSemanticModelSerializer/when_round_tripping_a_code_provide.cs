// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.for_SemanticModelBinder.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_a_code_provide : a_semantic_binder
{
    byte[] _bytes;
    string _text;
    ExecutableSemanticModel _model;
    Exception? _inV55;
    Exception? _inV56;

    void Because()
    {
        var result = Bind("module Fleet\n  feature Quality\n    slice StateView Vessels\n      provide limit Int\n        ```csharp\n        return 5;\n        ```\n      event VesselRegistered\n        vesselId Uuid\n");
        _bytes = SemanticModelSerializer.Serialize(result.Value!.Model);
        _text = Encoding.UTF8.GetString(_bytes);
        _model = SemanticModelSerializer.Deserialize(_bytes);
        _inV55 = ReadAsVersion(55);
        _inV56 = ReadAsVersion(56);
    }

    [Fact] void should_select_v57() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V57);
    [Fact] void should_write_the_code_kind() => _text.ShouldContain("\"kind\":\"code\"");
    [Fact] void should_write_the_requirement_id() => _text.ShouldContain("\"requirementId\":");
    [Fact] void should_round_trip_strictly() => SemanticModelSerializer.Serialize(_model).ShouldEqual(_bytes);
    [Fact] void should_refuse_it_in_v55() => _inV55.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_refuse_it_in_v56() => _inV56.ShouldBeOfExactType<InvalidSemanticContract>();

    Exception? ReadAsVersion(int version)
    {
        var downgraded = _text
            .Replace("\"schemaVersion\":57", $"\"schemaVersion\":{version}", StringComparison.Ordinal)
            .Replace("\"languageVersion\":\"57.0\"", $"\"languageVersion\":\"{version}.0\"", StringComparison.Ordinal)
            .Replace("\"semanticVersion\":\"57.0\"", $"\"semanticVersion\":\"{version}.0\"", StringComparison.Ordinal);
        return Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(downgraded)));
    }
}
