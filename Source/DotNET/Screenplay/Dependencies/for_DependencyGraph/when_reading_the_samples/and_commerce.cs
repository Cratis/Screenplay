// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph.when_reading_the_samples;

public class and_commerce : given.a_sample
{
    void Establish() => Load("Commerce");
    void Because() => _graph = DependencyGraph.For(_timeline);

    [Fact] void should_compile_valid_source() => _timeline.SourceValid.ShouldBeTrue();
    [Fact] void should_preserve_timeline_findings() => _timeline.Findings.Count.ShouldEqual(7);
    [Fact] void should_count_ordering_dependencies() => _graph.Implied("module", "module").Single(edge => edge.Source.Address == "Ordering").SliceEdges.ShouldEqual(4);
    [Fact] void should_count_fulfillment_dependencies() => _graph.Implied("module", "module").Single(edge => edge.Source.Address == "Fulfillment").SliceEdges.ShouldEqual(1);
    [Fact] void should_find_the_module_cycle() => _graph.Cycles("module").Single().Members.Select(node => node.Address).ShouldEqual(["Ordering", "Fulfillment"]);
    [Fact] void should_find_the_feature_cycle() => _graph.Cycles("feature").Single().Members.Select(node => node.Address).ShouldEqual(["Fulfillment.Shipping", "Fulfillment.Tracking"]);
    [Fact] void should_put_payments_before_orders() => _graph.SuggestedOrder().Containers.Single(order => order.Container.Address == "Ordering").Children.Select(node => node.Address).ShouldEqual(["Ordering.Payments", "Ordering.Orders", "Ordering.Support"]);
}
