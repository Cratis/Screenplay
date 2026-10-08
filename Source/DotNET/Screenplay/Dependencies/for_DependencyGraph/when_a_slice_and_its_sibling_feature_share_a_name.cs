// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph;

public class when_a_slice_and_its_sibling_feature_share_a_name : given.a_model
{
    void Establish() => _source = "module M\n  feature F\n    slice StateView Sub\n      projection P\n        from E\n    feature Sub\n      slice StateChange Producer\n        event E\n";
    void Because() => _graph = Graph(_source);

    [Fact] void should_keep_the_slice_dependency() => _graph.Implied("slice", "slice").Select(edge => edge.Source.Address + "|" + edge.Target.Address).ShouldEqual(["M.F.Sub|M.F.Sub.Producer"]);
    [Fact] void should_not_treat_the_sibling_slice_as_an_ancestor() => _graph.Implied("slice", "feature").Select(edge => edge.Source.Address + "|" + edge.Target.Address).ShouldEqual(["M.F.Sub|M.F.Sub"]);
    [Fact] void should_exclude_genuine_ancestor_edges() => _graph.Implied("feature", "feature").ShouldBeEmpty();
    [Fact] void should_order_the_sibling_feature_before_the_slice() => _graph.SuggestedOrder().Containers.Single(order => order.Container.Address == "M.F").Children.Select(node => node.Kind).ShouldEqual(["feature", "slice"]);
    [Fact] void should_include_the_feature_child_in_feature_scope() => _graph.IsWithin(_graph.Nodes.Single(node => node.Address == "M.F.Sub.Producer"), _graph.Nodes.Single(node => node.Kind == "feature" && node.Address == "M.F.Sub")).ShouldBeTrue();
    [Fact] void should_exclude_the_feature_child_from_sibling_slice_scope() => _graph.IsWithin(_graph.Nodes.Single(node => node.Address == "M.F.Sub.Producer"), _graph.Nodes.Single(node => node.Kind == "slice" && node.Address == "M.F.Sub")).ShouldBeFalse();
    [Fact] void should_exclude_the_sibling_slice_from_feature_scope() => _graph.IsWithin(_graph.Nodes.Single(node => node.Kind == "slice" && node.Address == "M.F.Sub"), _graph.Nodes.Single(node => node.Kind == "feature" && node.Address == "M.F.Sub")).ShouldBeFalse();
    [Fact] void should_include_the_slice_itself_in_slice_scope() => _graph.IsWithin(_graph.Nodes.Single(node => node.Kind == "slice" && node.Address == "M.F.Sub"), _graph.Nodes.Single(node => node.Kind == "slice" && node.Address == "M.F.Sub")).ShouldBeTrue();
}
