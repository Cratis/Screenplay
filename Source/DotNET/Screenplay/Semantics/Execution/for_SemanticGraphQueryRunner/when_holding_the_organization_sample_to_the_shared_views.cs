// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticGraphQueryRunner;

/// <summary>
/// Compiles <c>Samples/Organization</c> and reads its collecting queries back as graph views - a root read model of
/// property and relation members, and a nested row type of property and edge property members - and holds every view
/// of the shared fixture to them: the same root, edges, directions, cardinalities and sources, member for member.
/// </summary>
public class when_holding_the_organization_sample_to_the_shared_views : given.the_shared_graph_views
{
    readonly Dictionary<string, JsonObject> _views = new(StringComparer.Ordinal);
    JsonArray _expected = null!;

    void Because()
    {
        var root = new DirectoryInfo(Fixtures);
        while (!File.Exists(Path.Combine(root.FullName, "Screenplay.slnx"))) root = root.Parent!;
        var result = Bind(File.ReadAllText(Path.Combine(root.FullName, "Samples/Organization/organization.play")), displayPath: "organization.play");
        result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        var application = result.Value!.Model.Application;
        var readModels = application.Modules.SelectMany(SemanticGraphQueryRunner.SlicesOf).SelectMany(slice => slice.ReadModels).ToDictionary(readModel => readModel.Id);
        var types = application.Types.ToDictionary(type => type.Id);
        foreach (var query in application.GraphQueries.Where(query => !query.Collects.IsDefaultOrEmpty))
        {
            var readModel = readModels[query.ReadModel];
            var members = new JsonArray();
            foreach (var property in readModel.Properties)
            {
                if (query.Returns.FirstOrDefault(@return => @return.Property == property.Id) is { } returned)
                {
                    members.Add(new JsonObject { ["name"] = property.Name, ["kind"] = "property", ["source"] = returned.Source });
                }
                else if (query.Collects.FirstOrDefault(collect => collect.Property == property.Id) is { } collect)
                {
                    var row = types[collect.Row];
                    members.Add(new JsonObject
                    {
                        ["name"] = property.Name,
                        ["kind"] = "relation",
                        ["edge"] = collect.Hop.Type,
                        ["direction"] = collect.Hop.Outgoing ? "outgoing" : "incoming",
                        ["cardinality"] = collect.Cardinality == SemanticGraphCollectCardinality.Many ? "many" : "one",
                        ["view"] = row.Name
                    });
                    _views[row.Name] = View(row.Name, collect.Hop.Node.Label, new JsonArray([.. row.Properties.Select(rowProperty =>
                        collect.Members.Single(member => member.Property == rowProperty.Id) is var member
                            ? new JsonObject { ["name"] = rowProperty.Name, ["kind"] = member.Kind == SemanticGraphCollectMemberKind.Property ? "property" : "edgeProperty", ["source"] = member.Source }
                            : null)]));
                }
            }

            _views[readModel.Name] = View(readModel.Name, query.Matches[0].Start.Label, members);
        }

        _expected = Fixture("graph-views-organization.json")["views"]!.AsArray();
    }

    [Fact] void should_declare_every_view_of_the_fixture() => _views.Keys.Order(StringComparer.Ordinal).ShouldEqual(_expected.Select(view => view!["name"]!.GetValue<string>()));
    [Fact] void should_give_every_view_the_structure_of_the_fixture() => _expected.Where(view => !_views.TryGetValue(view!["name"]!.GetValue<string>(), out var actual) || !JsonNode.DeepEquals(actual, view)).Select(view => view!.ToJsonString()).ShouldBeEmpty();

    static JsonObject View(string name, string root, JsonArray members) => new() { ["name"] = name, ["root"] = root, ["members"] = members };
}
