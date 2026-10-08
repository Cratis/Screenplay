// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph.when_reading_the_samples;

public class and_invoicing : given.a_sample
{
    void Establish() => Load("Invoicing");
    void Because() => _graph = DependencyGraph.For(_timeline);

    [Fact] void should_compile_valid_source() => _timeline.SourceValid.ShouldBeTrue();
    [Fact] void should_preserve_timeline_findings() => _timeline.Findings.Count.ShouldEqual(4);
    [Fact] void should_resolve_all_consumed_contexts() => _graph.Implied("slice", "context").Select(edge => edge.Target.Address).Distinct().Order(StringComparer.Ordinal).ShouldEqual(["context:Customers", "context:Payments", "context:Shipping"]);
    [Fact] void should_count_every_import_as_used() => _graph.UnusedImports.Count.ShouldEqual(0);
    [Fact] void should_exclude_edges_to_owned_subfeatures() => _graph.Implied("feature", "feature").Any(edge => edge.Source.Address == "Invoicing.InvoiceManagement" && edge.Target.Address == "Invoicing.InvoiceManagement.Adjustments").ShouldBeFalse();
}
