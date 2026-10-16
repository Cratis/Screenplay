// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_gating_protected_read_nodes : Specification
{
    static SemanticId Property => given.canonical_serialization_golden_vectors.CreateSemanticModelV25().Application.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.Commands).Single().Properties[0].Id;

    [Fact]
    void should_gate_read_operands_in_every_earlier_schema_before_model_validation()
    {
        var json = JsonSerializer.Serialize(new { read = "View", property = Property.ToString() });
        foreach (var version in EsmSchemaV24Support.LanguageVersions)
        {
            var error = Catch.Exception(() => Operand(json, version.Major));
            error.ShouldBeOfExactType<InvalidSemanticContract>();
            error.Message.ShouldContain("read");
        }
        Operand(json, 25).Read.ShouldEqual("View");
    }

    [Fact]
    void should_gate_the_read_path_kind_in_every_earlier_schema()
    {
        foreach (var version in EsmSchemaV24Support.LanguageVersions)
        {
            var error = Catch.Exception(() => Expression("{\"kind\":\"readPath\"}", version.Major));
            error.ShouldBeOfExactType<InvalidSemanticContract>();
            error.Message.ShouldContain("requires schema v25");
        }
    }

    [Theory]
    [InlineData("read", "\"View\"")]
    [InlineData("property", null)]
    void should_gate_expression_read_members_before_shape_validation(string field, string? literal)
    {
        var json = $"{{\"kind\":\"value\",\"{field}\":{literal ?? JsonSerializer.Serialize(Property.ToString())}}}";
        foreach (var version in EsmSchemaV24Support.LanguageVersions)
        {
            var error = Catch.Exception(() => Expression(json, version.Major));
            error.ShouldBeOfExactType<InvalidSemanticContract>();
            error.Message.ShouldContain($"'{field}'");
        }
    }

    [Fact]
    void should_admit_the_expression_at_v25()
    {
        var json = JsonSerializer.Serialize(new { kind = "readPath", read = "View", property = Property.ToString() });
        var expression = (SemanticReadPathExpression)Expression(json, 25);
        expression.Read.ShouldEqual("View");
        expression.PropertyId.ShouldEqual(Property);
    }

    static SemanticConditionOperand Operand(string json, uint version)
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        reader.Read();
        return SemanticModelRead.ConditionOperand(ref reader, version);
    }

    static SemanticExpression Expression(string json, uint version)
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        reader.Read();
        return SemanticModelRead.Expression(ref reader, version);
    }
}
