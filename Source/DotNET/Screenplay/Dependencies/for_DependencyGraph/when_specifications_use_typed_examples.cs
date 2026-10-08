// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph;

public class when_specifications_use_typed_examples : given.a_model
{
    const string Source = """
        module M
          feature F
            slice StateChange Producer
              event E
              command C
              example Fact : E
              example Input : C
            slice StateChange Other
              event E
              command C
            slice StateView Consumer
              specification T
                given M.F.Producer.Fact
                when M.F.Producer.Input
                then M.F.Producer.Fact
              specification Append
                when append M.F.Producer.Fact
        """;
    DependencyGraph _plain;

    void Establish() => _plain = Graph(Source.Replace("M.F.Producer.Fact", "M.F.Producer.E", StringComparison.Ordinal).Replace("M.F.Producer.Input", "M.F.Producer.C", StringComparison.Ordinal));
    void Because() => _graph = Graph(Source);

    [Fact] void should_retain_the_test_only_edge() => _graph.Edges.Single().Kind.ShouldEqual("verifiedWith");
    [Fact] void should_resolve_the_underlying_qualified_declaration() => _graph.Edges.Single().Producer.Address.ShouldEqual("M.F.Producer");
    [Fact] void should_match_hand_written_dependency_edges() => _graph.Edges.Select(edge => (edge.Consumer.Key, edge.Producer.Key, edge.Kind)).ShouldContainOnly(_plain.Edges.Select(edge => (edge.Consumer.Key, edge.Producer.Key, edge.Kind)));
    [Fact] void should_retain_every_step_role() => _graph.Edges.Single().Evidence.Select(item => item.Role).ShouldContainOnly("givenEvent", "whenCommand", "thenEvent", "whenAppendedEvent");
    [Fact] void should_retain_the_authored_step_location() => _graph.Edges.Single().Evidence[0].Location.Line.ShouldEqual(13);
    [Fact] void should_not_report_example_names_as_unresolved() => _graph.Unresolved.ShouldBeEmpty();
    [Fact] void should_not_report_converted_sample_examples_as_unresolved() => Graph(File.ReadAllText(Path.Combine(FindRoot(), "Samples", "Invoicing", "invoicing.play"))).Unresolved.Where(reference => reference.Name == "AcmeInvoice").ShouldBeEmpty();

    static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (!Directory.Exists(Path.Combine(directory.FullName, "Samples"))) directory = directory.Parent!;

        return directory.FullName;
    }
}
