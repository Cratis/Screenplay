// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticGraphQueryRunner;

/// <summary>
/// Runs every case of the shared graph view query vectors through the reference runner and holds it to the exact expected result:
/// roots distinct and ordered by key, clauses that select roots without narrowing what is collected, collected rows ordered by the
/// reached key and the edge identity, empty lists, null single rows and bare nodes with absent properties.
/// </summary>
public class when_evaluating_the_shared_graph_view_vectors : given.the_shared_graph_views
{
    // Each clause shape of the vectors, as the view, the shape and the clause signature, names the query of the fixture model that
    // expresses it and the argument each clause value is passed as.
    static readonly Dictionary<string, (string Query, string[] Arguments)> _queries = new(StringComparer.Ordinal)
    {
        ["PersonWithGroups|list|"] = ("PeopleWithGroups", []),
        ["PersonWithGroups|list|related:groups(property:id)"] = ("PeopleInGroup", ["groupId"]),
        ["PersonWithGroups|list|property:name"] = ("PeopleNamed", ["name"]),
        ["PersonWithGroups|single|property:id"] = ("PersonById", ["personId"]),
        ["PersonWithGroups|single|property:name"] = ("PersonNamed", ["name"]),
        ["GroupWithMembers|list|"] = ("GroupsWithMembers", []),
        ["GroupWithMembers|list|related:members(property:name)"] = ("GroupsWithMemberNamed", ["name"]),
        ["GroupWithParent|list|"] = ("GroupsWithParent", []),
        ["PersonRoles|list|"] = ("PeopleWithRoles", [])
    };

    readonly List<string> _mismatches = [];
    int _cases;

    void Because()
    {
        var vectors = Fixture("graph-views-query-vectors.json");
        var snapshot = Snapshot(vectors);
        var application = _model.Application;
        var readModels = application.Modules.SelectMany(SemanticGraphQueryRunner.SlicesOf).SelectMany(slice => slice.ReadModels).ToDictionary(readModel => readModel.Id);
        var names = application.Types.SelectMany(type => type.Properties).ToDictionary(property => property.Id, property => property.Name);
        foreach (var vector in vectors["cases"]!.AsArray())
        {
            _cases++;
            var clauses = vector!["clauses"]!.AsArray();
            var key = $"{vector["view"]}|{vector["shape"]}|{string.Join(" and ", clauses.Select(Signature))}";
            if (!_queries.TryGetValue(key, out var mapped))
            {
                _mismatches.Add($"{vector["name"]}: no query expresses '{key}'");
                continue;
            }

            var query = application.GraphQueries.Single(candidate => candidate.Name == mapped.Query);
            var arguments = mapped.Arguments.Zip(clauses.SelectMany(Values)).ToDictionary(pair => pair.First, pair => (string?)pair.Second, StringComparer.Ordinal);
            var rows = SemanticGraphQueryRunner.Collect(application, snapshot, query, readModels[query.ReadModel], arguments).ConvertAll(row => Json(row, names));
            JsonNode? actual = vector["shape"]!.GetValue<string>() == "single" ? rows.FirstOrDefault() : new JsonArray([.. rows]);
            if (!JsonNode.DeepEquals(actual, vector["expected"]))
            {
                _mismatches.Add($"{vector["name"]}: expected {vector["expected"]?.ToJsonString() ?? "null"}, but the runner returned {actual?.ToJsonString() ?? "null"}");
            }
        }
    }

    [Fact] void should_run_every_vector() => _cases.ShouldEqual(9);
    [Fact] void should_return_exactly_the_expected_results() => _mismatches.ShouldBeEmpty();

    static string Signature(JsonNode? clause) => clause!["kind"]!.GetValue<string>() == "related"
        ? $"related:{clause["member"]}({string.Join(" and ", clause["clauses"]!.AsArray().Select(Signature))})"
        : $"property:{clause["member"]}";

    static IEnumerable<string> Values(JsonNode? clause) => clause!["kind"]!.GetValue<string>() == "related"
        ? clause["clauses"]!.AsArray().SelectMany(Values)
        : [clause["value"]!.GetValue<string>()];

    static SemanticGraphRunner.GraphSnapshot Snapshot(JsonNode vectors)
    {
        var graph = vectors["graph"]!.GetValue<string>();
        var nodes = new Dictionary<(string Graph, string Label, string Key), Dictionary<string, SemanticValue>>();
        foreach (var node in vectors["nodes"]!.AsArray())
        {
            nodes[(graph, node!["label"]!.GetValue<string>(), node["key"]!.GetValue<string>())] = Properties(node["properties"]!);
        }

        var edges = new Dictionary<(string Graph, string Type, string Source, string Target, string? Identity), Dictionary<string, SemanticValue>>();
        foreach (var edge in vectors["edges"]!.AsArray())
        {
            var identity = edge!["identity"]?.GetValue<string>();
            edges[(graph, edge["type"]!.GetValue<string>(), edge["source"]!["key"]!.GetValue<string>(), edge["target"]!["key"]!.GetValue<string>(), identity)] = Properties(edge["properties"]!);
        }

        return new(nodes, edges);
    }

    static Dictionary<string, SemanticValue> Properties(JsonNode properties) =>
        properties.AsObject().ToDictionary(property => property.Key, property => SemanticValue.Text(property.Value!.GetValue<string>()), StringComparer.Ordinal);

    static JsonObject Json(Dictionary<string, SemanticValue> row, Dictionary<SemanticId, string> names) =>
        new(row.Select(entry => KeyValuePair.Create(entry.Key, Json(entry.Value, names))));

    static JsonNode? Json(SemanticValue value, Dictionary<SemanticId, string> names) => value switch
    {
        SemanticNullValue => null,
        SemanticTextValue text => JsonValue.Create(text.Value),
        SemanticArrayValue list => new JsonArray([.. list.Values.Select(item => Json(item, names))]),
        SemanticCompositeValue composite => new JsonObject(composite.Properties.Select(member => KeyValuePair.Create(names[member.TargetProperty], Json(member.Value, names)))),
        _ => JsonValue.Create(value.ToString())
    };
}
