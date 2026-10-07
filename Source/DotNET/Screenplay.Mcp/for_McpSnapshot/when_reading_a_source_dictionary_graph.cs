// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpSnapshot;

public class when_reading_a_source_dictionary_graph : given.an_imported_model
{
    void Establish() => _snapshot = new(Sources);

    void Because()
    {
        _ = _snapshot.Compilation;
        _ = _snapshot.DependencyGraph;
        _ = _snapshot.Index;
    }

    [Fact] void should_compile_each_document_once() => _snapshot.ParsedDocumentCount.ShouldEqual(3);
    [Fact] void should_keep_authored_import_order() => _snapshot.DependencyGraph.OrderSource.ShouldEqual("authored");
    [Fact] void should_keep_the_cross_module_dependency() => _snapshot.DependencyGraph.Edges.Single().Kind.ShouldEqual("usesFactsFrom");
    [Fact] void should_keep_the_producer_before_the_consumer() => _snapshot.DependencyGraph.Edges.Single().Producer.Rank.ShouldBeLessThan(_snapshot.DependencyGraph.Edges.Single().Consumer.Rank);
    [Fact] void should_retain_scoped_validation() => ScopedDiagnostics.Select(_snapshot, "A.F.V")!.Scope.ShouldEqual("A.F.V");
}
