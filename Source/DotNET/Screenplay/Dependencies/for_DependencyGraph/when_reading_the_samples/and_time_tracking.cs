// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph.when_reading_the_samples;

public class and_time_tracking : given.a_sample
{
    void Establish() => Load("TimeTracking");
    void Because() => _graph = DependencyGraph.For(_timeline);

    [Fact] void should_compile_valid_source() => _timeline.SourceValid.ShouldBeTrue();
    [Fact] void should_preserve_timeline_findings() => _timeline.Findings.Count.ShouldEqual(9);
    [Fact] void should_infer_all_module_edges() => _graph.Implied("module", "module").Select(edge => edge.Source.Address + "|" + edge.Target.Address).ShouldEqual(["Engagements|Timesheets", "Payroll|Timesheets", "Timesheets|Engagements"]);
    [Fact] void should_exclude_module_cycles() => _graph.Cycles("module").Count.ShouldEqual(0);
    [Fact] void should_put_timesheets_before_payroll() => _graph.SuggestedOrder().Containers[0].Children.Select(node => node.Address).ShouldEqual(["Engagements", "Timesheets", "Payroll"]);
}
