// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph;

public class when_references_share_a_source_location : Specification
{
    ApplicationSyntax _application;
    IReadOnlyList<TimelineFinding> _findings;
    DependencyGraph _graph;

    void Establish() => _application = new ScreenplayCompiler().Parse("module M\n  feature F\n    slice StateView V\n      projection P\n        from Z, A\n    slice StateChange W\n      event Z\n      event A\n").Value!;
    void Because()
    {
        _findings = TimelineOrder.Analyze(_application);
        _graph = DependencyGraph.For(_application);
    }

    [Fact] void should_preserve_timeline_reference_order() => _findings.Select(finding => finding.Event).ShouldEqual(["Z", "A"]);
    [Fact] void should_sort_graph_evidence_ties_ordinally() => _graph.Edges.Single().Evidence.Select(item => item.Name).ShouldEqual(["A", "Z"]);
}
