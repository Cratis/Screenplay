// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Files;

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph;

public class when_inferring_dependencies
{
    internal static DependencyGraph Graph(string source) => DependencyGraph.For(new ScreenplayCompiler().Parse(source, "model.play").Value!);
    const string Producer = "module M\n  feature A\n    slice StateChange Producer\n      event E\n      command C\n      query Q => R\n      readmodel R\n      projection R\n        from E\n      screen S\n";

    [Theory]
    [InlineData("projection P\n        from E", "usesFactsFrom", "from")]
    [InlineData("projection P\n        join value on id\n          with E", "usesFactsFrom", "join")]
    [InlineData("projection P\n        remove with E", "usesFactsFrom", "remove")]
    [InlineData("projection P\n        clear with E", "usesFactsFrom", "clear")]
    [InlineData("projection P\n        remove via join on E", "usesFactsFrom", "removeViaJoin")]
    [InlineData("projection P\n        children items identified by id\n          from E", "usesFactsFrom", "from")]
    [InlineData("projection P\n        nested item\n          from E", "usesFactsFrom", "from")]
    [InlineData("projection P\n        variant Variant\n          enters on E\n          from E", "usesFactsFrom", "entersOn")]
    [InlineData("reducer Fold => View\n        on E", "usesFactsFrom", "reduces")]
    [InlineData("constraint Unique\n        unique event E", "usesFactsFrom", "uniqueEvent")]
    [InlineData("constraint Unique\n        unique id on E", "usesFactsFrom", "uniqueProperty")]
    [InlineData("command D\n        concurrency\n          events E", "usesFactsFrom", "concurrency")]
    [InlineData("command D\n        reads R", "decidesFrom", "reads")]
    [InlineData("reaction React\n        when E\n          invokes C", "reactsTo", "trigger")]
    [InlineData("reaction React\n        when E\n          invokes C", "asks", "invokes")]
    [InlineData("screen V\n        action C", "asks", "action")]
    [InlineData("screen V\n        data R via query Q", "shows", "dataQuery")]
    [InlineData("screen V\n        action C\n          navigate to S", "shows", "navigate")]
    [InlineData("specification T\n        given E\n        when C\n        then E", "verifiedWith", "givenEvent")]
    public void should_collect_typed_references(string body, string kind, string role)
    {
        var graph = Graph(Producer + "  feature B\n    slice StateView Consumer\n      " + body + "\n");
        graph.Edges.Any(edge => edge.Consumer.Address == "M.B.Consumer" && edge.Producer.Address == "M.A.Producer" && edge.Kind == kind && edge.Evidence.Any(item => item.Role == role)).ShouldBeTrue();
    }

    [Fact]
    public void should_separate_test_only_self_shared_and_unresolved_references()
    {
        var graph = Graph("trigger Tick\nimport Outside.Imported\nmodule M\n  feature F\n    slice StateChange Producer\n      event E\n    slice StateView Consumer\n      event Local\n      projection P\n        from Local\n        from Missing\n        from Imported\n      reaction R\n        when Tick\n      specification T\n        given E\n");
        graph.Edges.Count.ShouldEqual(2);
        graph.Edges.Single(edge => edge.Kind == "outsideTheModel").Producer.Address.ShouldEqual("context:Outside");
        graph.Implied("slice", "slice").Count.ShouldEqual(0);
        graph.Implied("slice", "slice", includeTestOnly: true).Count.ShouldEqual(1);
        graph.Unresolved.Single().Name.ShouldEqual("Missing");
        graph.ExcludedReferences.ShouldEqual(1);
    }

    [Fact]
    public void should_select_the_earliest_producer_and_report_alternatives_without_generations_counting_twice()
    {
        var graph = Graph("module M\n  feature F\n    slice StateChange First\n      event E\n      event E generation 2\n    slice StateChange Second\n      event e\n    slice StateView V\n      projection P\n        from E\n");
        var evidence = graph.Edges.Single().Evidence.Single();
        evidence.Producer.Address.ShouldEqual("M.F.First");
        evidence.Ambiguous.ShouldBeTrue();
        evidence.Alternatives.Select(node => node.Address).ShouldEqual(["M.F.Second"]);
    }

    [Fact]
    public void should_imply_only_disjoint_containers_and_count_evidence_not_kinds_as_slice_pairs()
    {
        var graph = Graph("module M\n  feature F\n    slice StateView V\n      projection P\n        from E\n        from E\n      command C\n        reads R\n    feature Sub\n      slice StateChange W\n        event E\n        projection R\n          from E\nmodule N\n  feature G\n    slice StateView Other\n      projection P\n        from E\n");
        graph.Implied("feature", "module").Select(edge => edge.Source.Address + "|" + edge.Target.Address).ShouldEqual(["N.G|M"]);
        graph.Implied("feature", "feature").Any(edge => edge.Source.Address == "M.F" && edge.Target.Address == "M.F.Sub").ShouldBeFalse();
        var edge = graph.Implied("slice", "slice", evidenceLimit: 1)[0];
        edge.SliceEdges.ShouldEqual(1);
        edge.References.ShouldEqual(3);
        edge.EvidenceCount.ShouldEqual(3);
        edge.EvidenceTruncated.ShouldBeTrue();
        edge.ByKind["usesFactsFrom"].ShouldEqual(2);
        edge.ByKind["decidesFrom"].ShouldEqual(1);
        edge.Consumers.Count.ShouldEqual(1);
        edge.Producers.Count.ShouldEqual(1);
    }

    [Fact]
    public void should_find_ordering_cycles_and_keep_members_in_authored_order()
    {
        var graph = Graph("module M\n  feature A\n    slice StateView V\n      projection R\n        from E\n  feature B\n    slice StateChange W\n      event E\n      command C\n        reads R\n");
        graph.Cycles("feature").Single().Members.Select(node => node.Address).ShouldEqual(["M.A", "M.B"]);
        graph.SiblingGroups().Single().Members.Select(node => node.Address).ShouldEqual(["M.A", "M.B"]);
        graph.SuggestedOrder().Slices.Select(node => node.Address).ShouldEqual(["M.A.V", "M.B.W"]);
        graph.SuggestedOrder().Containers.Any(container => container.Changed).ShouldBeFalse();
    }

    [Fact]
    public void should_suggest_producers_first_and_support_traversal_in_both_directions()
    {
        var graph = Graph("module M\n  feature F\n    slice StateView V\n      projection P\n        from E\n    slice StateChange W\n      event E\n    slice StateView Last\n      command C\n        reads P\n");
        graph.SuggestedOrder().Slices.Select(node => node.Address).ShouldEqual(["M.F.W", "M.F.V", "M.F.Last"]);
        graph.SuggestedOrder().Containers.Single(container => container.Container.Address == "M.F").Changed.ShouldBeTrue();
        graph.Traverse("M.F.Last", "outgoing").Select(node => node.Address).ShouldEqual(["M.F.V", "M.F.W"]);
        graph.Traverse("M.F.W", "incoming").Select(node => node.Address).ShouldEqual(["M.F.V", "M.F.Last"]);
        var timeline = TimelineOrder.Analyze(GraphSource());
        timeline.Count.ShouldEqual(1);
    }

    [Theory]
    [InlineData("readmodel View")]
    [InlineData("readmodel View\n      projection Builder => View\n        from E")]
    [InlineData("readmodel View\n      reducer Builder => View\n        on E")]
    [InlineData("readmodel View\n      projection Builder\n        variant View\n          enters on E\n          from E")]
    public void should_resolve_read_model_builders_variants_and_declaration_fallback(string declaration)
    {
        var graph = Graph("module M\n  feature F\n    slice StateChange Shape\n      readmodel View\n    slice StateView Builder\n      event E\n      " + declaration + "\n    slice StateChange Consumer\n      command C\n        reads View\n");
        graph.Edges.Single(edge => edge.Kind == "decidesFrom").Producer.Address.ShouldEqual(declaration == "readmodel View" ? "M.F.Shape" : "M.F.Builder");
    }

    [Fact]
    public void should_keep_imported_specification_facts_hidden_and_list_unused_contracts()
    {
        var graph = Graph("import Outside.E\nimport Unused.U\nmodule M\n  feature F\n    slice StateChange C\n      specification T\n        given E\n");
        graph.Implied("slice", "context").Count.ShouldEqual(0);
        graph.Implied("slice", "context", includeTestOnly: true).Single().Evidence.Single().TestOnly.ShouldBeTrue();
        graph.UnusedImports.SequenceEqual(["Unused.U"]).ShouldBeTrue();
    }

    [Fact]
    public void should_resolve_inline_events_and_ignore_asks_and_shows_for_ordering()
    {
        var graph = Graph("module M\n  feature A\n    slice StateChange First\n      command C\n        produces event E\n      screen S\n        action D\n  feature B\n    slice StateChange Second\n      command D\n      projection P\n        from E\n");
        graph.Edges.Single(edge => edge.Kind == "usesFactsFrom").Producer.Address.ShouldEqual("M.A.First");
        graph.Edges.Single(edge => edge.Kind == "asks").Producer.Address.ShouldEqual("M.B.Second");
        graph.Cycles("feature").Count.ShouldEqual(0);
        graph.SuggestedOrder().Containers.Single(order => order.Container.Address == "M").Changed.ShouldBeFalse();
    }

    [Fact]
    public void should_count_shared_types_and_policies_without_turning_them_into_edges()
    {
        var graph = Graph("concept Id : Uuid\npolicy Allowed\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        id Id\n        authorize Allowed\n");
        graph.Edges.Count.ShouldEqual(0);
        graph.ExcludedReferences.ShouldEqual(2);
    }

    [Fact]
    public void should_preserve_timeline_reference_ties_even_when_graph_evidence_has_ordinal_ties()
    {
        var application = new ScreenplayCompiler().Parse("module M\n  feature F\n    slice StateView V\n      projection P\n        from Z, A\n    slice StateChange W\n      event Z\n      event A\n").Value!;
        TimelineOrder.Analyze(application).Select(finding => finding.Event).ShouldEqual(["Z", "A"]);
        DependencyGraph.For(application).Edges.Single().Evidence.Select(item => item.Name).ShouldEqual(["A", "Z"]);
    }

    static Syntax.ApplicationSyntax GraphSource() => new ScreenplayCompiler().Parse("module M\n  feature F\n    slice StateView V\n      projection P\n        from E\n    slice StateChange W\n      event E\n").Value!;
}
