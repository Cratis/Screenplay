// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph;

public class when_documents_arrive_in_another_order : given.a_conformance_suite
{
    (string Forward, string Reversed)[] _outputs;

    void Because() => _outputs = [.. _vectors.Select(vector => (Output(Graph(vector)), Output(Graph(vector, reverse: true))))];

    [Fact] void should_keep_the_complete_graph_identical() => _outputs.All(item => item.Forward == item.Reversed).ShouldBeTrue();

    static string Output(DependencyGraph graph) => JsonSerializer.Serialize(new { graph.Edges, Implied = graph.Implied("feature", "feature"), Cycles = graph.Cycles("feature"), Order = graph.SuggestedOrder(), graph.Unresolved });
}
