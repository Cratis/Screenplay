// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer.when_serializing_possible_events;

public class when_gating_one_of : Specification
{
    static SemanticId Property => given.canonical_serialization_golden_vectors.CreateSemanticModelV25().Application.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.Commands).Single().Properties[0].Id;

    [Fact]
    void should_refuse_the_kind_below_v35()
    {
        var error = Catch.Exception(() => Read(/*lang=json*/ "{\"kind\":\"oneOf\"}", 34, true));
        error.ShouldBeOfExactType<InvalidSemanticContract>();
        error.Message.ShouldContain("oneOf requires schema v35");
    }

    [Fact]
    void should_admit_the_destination_at_v35() => ((SemanticOneOfExpression)Read(JsonSerializer.Serialize(new { kind = "oneOf", property = Property.ToString() }), 35, true)).CollectionProperty.ShouldEqual(Property);

    [Fact]
    void should_refuse_one_of_outside_possible_event_destinations()
    {
        var error = Catch.Exception(() => Read(JsonSerializer.Serialize(new { kind = "oneOf", property = Property.ToString() }), 35, false));
        error.ShouldBeOfExactType<InvalidSemanticContract>();
    }

    [Fact]
    void should_refuse_extra_expression_members()
    {
        var error = Catch.Exception(() => Read(JsonSerializer.Serialize(new { kind = "oneOf", property = Property.ToString(), read = "View" }), 35, true));
        error.ShouldBeOfExactType<InvalidSemanticContract>();
    }

    static SemanticExpression Read(string json, uint version, bool possibleEventDestination)
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        reader.Read();
        return SemanticModelRead.Expression(ref reader, version, possibleEventDestination);
    }
}
