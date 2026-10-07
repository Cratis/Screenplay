// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph.when_reading_the_samples;

public class and_library : given.a_sample
{
    void Establish() => Load("Library");
    void Because() => _graph = DependencyGraph.For(_timeline);

    [Fact] void should_compile_valid_source() => _timeline.SourceValid.ShouldBeTrue();
    [Fact] void should_preserve_timeline_findings() => _timeline.Findings.Count.ShouldEqual(2);
    [Fact] void should_find_the_catalog_loans_cycle() => _graph.Cycles("feature").Single().Members.Select(node => node.Address).ShouldEqual(["Lending.Catalog", "Lending.Loans"]);
    [Fact] void should_retain_authored_order() => _graph.SuggestedOrder().Containers.Any(order => order.Changed).ShouldBeFalse();
}
