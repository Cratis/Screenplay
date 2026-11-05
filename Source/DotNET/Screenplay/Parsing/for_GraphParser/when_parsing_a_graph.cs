// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.given;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_GraphParser;

public class when_parsing_a_graph : Specification
{
    GraphSyntax _graph;
    SliceSyntax _slice;
    IEnumerable<Cratis.Screenplay.Diagnostics.Diagnostic> _diagnostics;

    void Because()
    {
        var result = new ScreenplayCompiler().Compile(GraphDocuments.Document(GraphDocuments.Graph, GraphDocuments.Events, GraphDocuments.Projection));
        _diagnostics = result.Diagnostics;
        _slice = result.Value!.Modules.Single().Features.Single().Slices.Single();
        _graph = _slice.Graphs.Single();
    }

    [Fact] void should_report_nothing() => _diagnostics.Where(diagnostic => diagnostic.Severity != Diagnostics.DiagnosticSeverity.Information).ShouldBeEmpty();
    [Fact] void should_name_the_graph() => _graph.Name.ShouldEqual("Organization");
    [Fact] void should_read_the_node_labels() => _graph.Nodes.Select(node => node.Label).ShouldEqual("Person", "Group");
    [Fact] void should_mark_the_node_key() => _graph.Nodes.First().Properties.Single(property => property.IsKey).Name.ShouldEqual("personId");
    [Fact] void should_read_the_edge_endpoints() => _graph.Edges.Single().Source.ShouldEqual("Person");
    [Fact] void should_read_the_edge_target() => _graph.Edges.Single().Target.ShouldEqual("Group");
    [Fact] void should_read_the_edge_properties() => _graph.Edges.Single().Properties.Single().Name.ShouldEqual("since");
    [Fact] void should_keep_graph_projections_apart_from_read_model_projections() => _slice.Projections.ShouldBeEmpty();
    [Fact] void should_read_the_graph_projection() => _slice.GraphProjections.Single().Graph.ShouldEqual("Organization");
}
