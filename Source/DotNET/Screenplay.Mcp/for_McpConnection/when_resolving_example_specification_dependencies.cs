// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_resolving_example_specification_dependencies : given.a_connection
{
    McpSnapshot _snapshot;

    void Establish() => File.WriteAllText(Path.Combine(RootPath, "application.play"), """
        module M
          feature F
            slice StateChange Producer
              event E
              command C
              example Fact : E
              example Input : C
            slice StateView Consumer
              specification T
                given Fact
                when Input
                then Fact
        """);

    void Because() => _snapshot = new McpSnapshot(Root.Read());

    [Fact] void should_resolve_example_backed_dependency_edges() => _snapshot.DependencyGraph.Edges.Single().Producer.Address.ShouldEqual("M.F.Producer");
    [Fact] void should_retain_all_example_backed_evidence() => _snapshot.DependencyGraph.Edges.Single().Evidence.Count.ShouldEqual(3);
    [Fact] void should_not_report_example_names_as_unresolved() => _snapshot.DependencyGraph.Unresolved.ShouldBeEmpty();
    [Fact] void should_retain_authored_example_references() => _snapshot.Index.Incoming(_snapshot.Index.Find("M.F.Producer.Fact", "Example").Single()).Count().ShouldEqual(2);
}
