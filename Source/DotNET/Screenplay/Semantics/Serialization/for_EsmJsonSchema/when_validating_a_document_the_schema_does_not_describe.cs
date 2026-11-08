// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics.Serialization.for_EsmJsonSchema.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_EsmJsonSchema;

public class when_validating_a_document_the_schema_does_not_describe : Specification
{
    JsonObject _golden = null!;
    json_schema_validator _validator = null!;

    void Establish()
    {
        var (_, _, document) = published_esm_schemas.Documents().First(document => document.Version == SemanticVersion.V41);
        _golden = JsonNode.Parse(document.GetRawText())!.AsObject();
        using var schema = JsonDocument.Parse(EsmJsonSchema.For(SemanticVersion.V41));
        _validator = new(schema.RootElement.Clone());
    }

    [Fact] void should_accept_the_golden() => Errors(_golden).ShouldBeEmpty();
    [Fact] void should_refuse_an_unknown_member() => Errors(With(document => document["application"]!["unknownMember"] = true)).ShouldContain(error => error.Contains("unknown member 'unknownMember'", StringComparison.Ordinal));
    [Fact] void should_refuse_a_value_of_the_wrong_kind() => Errors(With(document => document["application"]!["name"] = 42)).ShouldContain(error => error.StartsWith("$.application.name", StringComparison.Ordinal));
    [Fact] void should_refuse_an_object_where_an_array_is_expected() => Errors(With(document => document["application"]!["modules"] = new JsonObject())).ShouldContain(error => error.StartsWith("$.application.modules", StringComparison.Ordinal));
    [Fact] void should_refuse_another_version() => Errors(With(document => document["schemaVersion"] = 40)).ShouldContain(error => error.StartsWith("$.schemaVersion", StringComparison.Ordinal));
    [Fact] void should_refuse_a_missing_envelope_member() => Errors(With(document => document.Remove("revision"))).ShouldContain(error => error.Contains("missing required member 'revision'", StringComparison.Ordinal));
    [Fact] void should_refuse_a_version_without_a_schema() => Catch.Exception(() => EsmJsonSchema.For(new SemanticVersion(39, 0))).ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_not_allow_a_member_added_by_a_later_version() => EsmJsonSchema.For(new SemanticVersion(5, 0)).ShouldNotContain("\"triggers\"");

    IReadOnlyList<string> Errors(JsonObject document) => _validator.Validate(JsonDocument.Parse(document.ToJsonString()).RootElement);

    JsonObject With(Action<JsonObject> change)
    {
        var copy = _golden.DeepClone().AsObject();
        change(copy);
        return copy;
    }
}
#endif
