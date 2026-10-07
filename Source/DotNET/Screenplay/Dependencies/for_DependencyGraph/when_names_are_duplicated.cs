// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph;

public class when_names_are_duplicated : given.a_model
{
    void Establish() => _source = "module M\n  feature F\n    slice StateView Duplicate\n      projection P\n        from E\n    slice StateView Duplicate\n      projection Q\n        from Missing\n  feature F\n    slice StateChange Producer\n      event E\nmodule M\n  feature G\n    slice StateView Other\n      projection R\n        from E\n";
    void Because() => _graph = Graph(_source);

    [Fact] void should_keep_both_references() => _graph.Edges.Count.ShouldEqual(2);
    [Fact] void should_answer_the_edges_view() => _graph.Implied("slice", "slice").Count.ShouldEqual(2);
    [Fact] void should_answer_the_cycles_view() => _graph.Cycles("slice").ShouldBeEmpty();
    [Fact] void should_answer_the_sibling_groups_view() => _graph.SiblingGroups().ShouldBeEmpty();
    [Fact] void should_append_children_to_the_first_named_container() => _graph.SuggestedOrder().Slices.Select(node => node.Address).ShouldEqual(["M.F.Producer", "M.F.Duplicate", "M.G.Other"]);
    [Fact] void should_answer_the_unresolved_view() => _graph.Unresolved.Single().Name.ShouldEqual("Missing");
    [Fact] void should_merge_nodes_with_the_same_kind_and_address() => _graph.Nodes.Count.ShouldEqual(6);
}
