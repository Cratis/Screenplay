// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticGraphQueryRunner.given;

/// <summary>
/// The shared graph views fixtures, copied byte-identical from the cross-repository specification, and the executable model of
/// <c>Fixtures/graph-views-organization.play</c>, which declares the fixture's views with <c>collect</c>.
/// </summary>
public class the_shared_graph_views : for_SemanticModelBinder.given.a_semantic_binder
{
    protected static readonly string Fixtures = FixturesDirectory();

    protected ExecutableSemanticModel _model = null!;

    protected static JsonNode Fixture(string name) => JsonNode.Parse(File.ReadAllText(Path.Combine(Fixtures, name)))!;

    void Establish()
    {
        var result = Bind(File.ReadAllText(Path.Combine(Fixtures, "graph-views-organization.play")));
        result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        _model = result.Value!.Model;
    }

    static string FixturesDirectory()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(root.FullName, "Screenplay.slnx"))) root = root.Parent!;
        return Path.Combine(root.FullName, "Source/DotNET/Screenplay/Semantics/Execution/for_SemanticGraphQueryRunner/Fixtures");
    }
}
