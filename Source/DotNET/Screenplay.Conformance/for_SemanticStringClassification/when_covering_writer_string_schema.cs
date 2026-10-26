// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Screenplay.Conformance.for_SemanticStringClassification;

public class when_covering_writer_string_schema : Specification
{
    public static IEnumerable<TheoryDataRow<string>> Definitions =>
        "application concepts types modules features slices properties events externalEvents commands readModels projections queries argument keyParts filters specifications triggers reactions captures eventSources streams constraints"
            .Split(' ').Select(owner => new TheoryDataRow<string>(owner));

    public static IEnumerable<TheoryDataRow<string, string, string, bool>> Literals =>
        LiteralFields("", "schema languageVersion semanticVersion name primitive special scope kind storedName tags visibility origin reactionProductionMode commandReadMode result presence direction cardinality delivery message severity code read root contextValue member value values")
            .Concat(LiteralFields("route", "name sourceKind streamKind part"))
            .Concat(LiteralFields("generation", "alphabet encoding case checksum pattern prefix"))
            .Concat(LiteralFields("authorization", "role roles claim operator targetKind"))
            .Concat(LiteralFields("map", "field selector separator text from to expression constraint givenClock whenClock operation"))
            .Concat([
                new("captures", "key", "", false),
                new("reducers", "key", "", false),
                new("claims", "type", "", false),
                new("map", "source", "", false),
                new("source", "source", "resolved", false),
                new("value", "source", "resolved", false),
                new("key", "source", "resolved", false),
                new("streamId", "source", "resolved", false),
                new("eventSourceId", "source", "resolved", false),
                new("condition", "source", "resolved", false),
                new("destination", "source", "resolved", false),
                new("source", "path", "eventContext", true),
                new("value", "path", "eventContext", true),
                new("factor", "path", "eventContext", true),
                new("children", "identifiedBy", "", false),
                new("map", "targets", "", true),
                new("when", "fields", "", true),
                new("givenCaller", "roles", "", true),
                new("runsAs", "roles", "", true),
            ]);

    [Theory]
    [MemberData(nameof(Definitions))]
    public void should_classify_every_definition_emission(string owner)
    {
        using var document = JsonDocument.Parse("{}");
        SemanticStringClassification.Classify(document.RootElement, owner, "id").ShouldEqual(SemanticStringKind.Identity);
        SemanticStringClassification.Classify(document.RootElement, "events", "contractId").ShouldEqual(SemanticStringKind.Identity);
    }

    [Theory]
    [MemberData(nameof(Literals))]
    public void should_preserve_identity_looking_literals_and_fallback_text(string owner, string property, string kind, bool array)
    {
        foreach (var prefix in (string[])["sem1", "rev1", "catrev1", "req1"])
        {
            var text = $"{prefix}:{new string('0', 64)}";
            var container = new JsonObject();
            if (kind.Length > 0) container["kind"] = kind;
            if (property == "identifiedBy") container["field"] = "line";
            container[property] = array ? new JsonArray(text) : JsonValue.Create(text);
            using var document = JsonDocument.Parse(container.ToJsonString());
            SemanticStringClassification.Classify(document.RootElement, owner, property).ShouldEqual(SemanticStringKind.Literal);
            SemanticSiblingSegments.StrippedText(document.RootElement, owner).Contains(text, StringComparison.Ordinal).ShouldBeTrue();
        }
    }

    [Theory]
    [InlineData("", "newField")]
    [InlineData("newOwner", "source")]
    [InlineData("newOwner", "key")]
    [InlineData("newOwner", "type")]
    [InlineData("newOwner", "path")]
    [InlineData("newOwner", "identifiedBy")]
    [InlineData("newOwner", "targets")]
    public void should_refuse_unclassified_strings_instead_of_assuming_literal(string owner, string property)
    {
        foreach (var array in (bool[])[false, true])
        {
            var container = new JsonObject { [property] = array ? new JsonArray("value") : JsonValue.Create("value") };
            using var document = JsonDocument.Parse(container.ToJsonString());
            Catch.Exception(() => SemanticStringClassification.Classify(document.RootElement, owner, property)).ShouldBeOfExactType<SemanticNormalizationFailed>();
        }
    }

    static IEnumerable<TheoryDataRow<string, string, string, bool>> LiteralFields(string owner, string fields) =>
        fields.Split(' ').Select(field => new TheoryDataRow<string, string, string, bool>(owner, field, "", false));

    [Theory]
    [InlineData("generation", "requirement")]
    [InlineData("policies", "requirementId")]
    [InlineData("validations", "requirementId")]
    [InlineData("codeValidations", "requirementId")]
    [InlineData("transitions", "requirementId")]
    [InlineData("handler", "requirementId")]
    [InlineData("triggers", "requirementId")]
    public void should_classify_requirement_provenance(string owner, string property)
    {
        using var document = JsonDocument.Parse("{}");
        SemanticStringClassification.Classify(document.RootElement, owner, property).ShouldEqual(SemanticStringKind.Requirement);
    }
}
