// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Screenplay.Conformance.for_SemanticStringClassification;

public class when_covering_writer_references : Specification
{
    const string Identity = "sem1:0000000000000000000000000000000000000000000000000000000000000000";

    // Each row is an identity/reference emission context, not a list derived from the classifier under test.
    // Shared writers are expanded at every call site; scalar/array variants share the same resolution assertion.
    public static IEnumerable<TheoryDataRow<string, string, string, bool>> References =>
        Scalar("target", "type destinationType eventSourceType identifierType streamIdType")
        .Concat(Scalar("property", "validations filters parts children nested left right"))
        .Concat(Scalar("eventContract", "produces transitions from join removedWith removedWithJoin givenEvents thenEvents whenAppended targets appends"))
        .Concat(Scalar("readModel", "projections queries reads reducers givenReadModels thenReadModels thenAbsentReadModels"))
        .Concat(Scalar("event", "projections reducers"))
        .Concat(Scalar("stream", "from route"))
        .Concat(Scalar("capture", "givenCaptures whenCapture"))
        .Concat(Scalar("trigger", "whenTrigger"))
        .Concat(Scalar("reaction", "whenRedelivered"))
        .Concat(Scalar("command", "invokes thenInvocations when givenGenerated"))
        .Concat(Scalar("query", "thenQueries"))
        .Concat(Scalar("keyProperty", "queries keyParts"))
        .Concat(Scalar("filter", "filters"))
        .Concat(Scalar("targetProperty", "values givenGenerated mappings"))
        .Concat(Scalar("target", "source value key streamId eventSourceId condition destination", "resolved"))
        .Concat(Scalar("property", "source value key streamId eventSourceId condition destination", "readPath"))
        .Concat(Scalar("key", "reads"))
        .Concat(Scalar("source", "route from triggers response fields"))
        .Concat(Scalar("on", "join"))
        .Concat(Scalar("type", "key parentKey", "composite"))
        .Concat(Scalar("identifiedBy", "children"))
        .Concat(Arrays("sourceEvents", "captures"))
        .Concat(Arrays("releasedBy", "constraints"))
        .Concat(Arrays("properties", "targets"))
        .Concat(Arrays("target", "mappings"))
        .Concat(Arrays("path", "source value factor", "eventProperty"));

    [Theory]
    [MemberData(nameof(References))]
    public void should_classify_resolve_and_reject_unmapped_references(string owner, string property, string kind, bool array)
    {
        var container = new JsonObject();
        if (kind.Length > 0) container["kind"] = kind;
        if (property == "identifiedBy") container["property"] = Identity;
        container[property] = array ? new JsonArray(Identity) : JsonValue.Create(Identity);
        using var document = JsonDocument.Parse(container.ToJsonString());
        SemanticStringClassification.Classify(document.RootElement, owner, property).ShouldEqual(SemanticStringKind.Identity);
        var root = new JsonObject
        {
            ["application"] = new JsonObject { ["id"] = Identity, ["name"] = "App" },
            [owner] = container,
        };
        var normalized = Normalize(root);
        normalized.Root.Members[owner].Members[property].Text.Contains("/application", StringComparison.Ordinal).ShouldBeTrue();
        var unmapped = $"sem1:{new string('a', 64)}";
        container[property] = array ? new JsonArray(unmapped) : JsonValue.Create(unmapped);
        var error = Catch.Exception(() => Normalize(root)) as SemanticNormalizationFailed;
        error.ShouldNotBeNull();
        error.Path.Contains($"/{owner}/{property}", StringComparison.Ordinal).ShouldBeTrue();
    }

    static NormalizedSemanticModel Normalize(JsonNode root) =>
        SemanticModelNormalizer.NormalizeCanonicalJson(Encoding.UTF8.GetBytes(root.ToJsonString()));

    static IEnumerable<TheoryDataRow<string, string, string, bool>> Scalar(string property, string owners, string kind = "") =>
        owners.Split(' ').Select(owner => new TheoryDataRow<string, string, string, bool>(owner, property, kind, false));

    static IEnumerable<TheoryDataRow<string, string, string, bool>> Arrays(string property, string owners, string kind = "") =>
        owners.Split(' ').Select(owner => new TheoryDataRow<string, string, string, bool>(owner, property, kind, true));
}
