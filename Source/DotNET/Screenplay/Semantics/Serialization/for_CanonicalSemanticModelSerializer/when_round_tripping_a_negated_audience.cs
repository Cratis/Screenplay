// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.for_SemanticModelBinder.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_a_negated_audience : a_semantic_binder
{
    const string Source =
        """
        audience Plus
        audience Incident

        module Fleet
          available for Plus and not Incident
          feature Quality
            slice StateView Vessels
              event VesselRegistered
                vesselId Uuid
        """;

    ExecutableSemanticModel _model;
    ExecutableSemanticModel _roundTripped;
    string _text;
    Exception _falseNegation;

    void Because()
    {
        _model = Bind(Source).Value!.Model;
        var bytes = SemanticModelSerializer.Serialize(_model);
        _text = Encoding.UTF8.GetString(bytes);
        _roundTripped = SemanticModelSerializer.Deserialize(bytes);
        _falseNegation = Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(_text.Replace("\"negated\":true", "\"negated\":false", StringComparison.Ordinal))));
    }

    [Fact] void should_select_v58() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V58);
    [Fact] void should_write_the_negation_after_the_audience() => _text.ShouldContain("\"audience\":\"Incident\",\"negated\":true");
    [Fact] void should_write_no_negation_for_the_other_audience() => _text.ShouldContain("\"kind\":\"audience\",\"audience\":\"Plus\"}");
    [Fact] void should_round_trip_strictly() => SemanticModelSerializer.Serialize(_roundTripped).ShouldEqual(Encoding.UTF8.GetBytes(_text));
    [Fact] void should_refuse_a_negated_member_written_false() => _falseNegation.ShouldBeOfExactType<InvalidSemanticContract>();
}
