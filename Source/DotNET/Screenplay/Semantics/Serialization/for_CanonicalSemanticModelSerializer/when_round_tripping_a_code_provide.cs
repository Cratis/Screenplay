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
    Exception _belowV55;

    void Because()
    {
        var result = Bind("module Fleet\n  feature Quality\n    slice StateView Vessels\n      provide limit Int\n        ```csharp\n        return 5;\n        ```\n      event VesselRegistered\n        vesselId Uuid\n");
        _bytes = SemanticModelSerializer.Serialize(result.Value!.Model);
        _text = Encoding.UTF8.GetString(_bytes);
        _model = SemanticModelSerializer.Deserialize(_bytes);
        var downgraded = _text
            .Replace("\"schemaVersion\":55", "\"schemaVersion\":53", StringComparison.Ordinal)
            .Replace("\"languageVersion\":\"55.0\"", "\"languageVersion\":\"53.0\"", StringComparison.Ordinal)
            .Replace("\"semanticVersion\":\"55.0\"", "\"semanticVersion\":\"53.0\"", StringComparison.Ordinal);
        _belowV55 = Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(downgraded)));
    }

    [Fact] void should_select_v55() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V55);
    [Fact] void should_write_the_code_kind() => _text.ShouldContain("\"kind\":\"code\"");
    [Fact] void should_write_the_requirement_id() => _text.ShouldContain("\"requirementId\":");
    [Fact] void should_round_trip_strictly() => SemanticModelSerializer.Serialize(_model).ShouldEqual(_bytes);
    [Fact] void should_refuse_it_below_v55() => _belowV55.ShouldBeOfExactType<InvalidSemanticContract>();
}
