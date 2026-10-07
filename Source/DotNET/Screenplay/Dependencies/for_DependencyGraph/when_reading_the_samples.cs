// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Files;
using Xunit.Abstractions;

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph;

public class when_reading_the_samples(ITestOutputHelper output)
{
    [Theory]
    [InlineData("Library", 2)]
    [InlineData("Invoicing", 3)]
    [InlineData("Commerce", 7)]
    [InlineData("TimeTracking", 7)]
    public void should_preserve_timeline_findings_and_report_the_inferred_graph(string sample, int findings)
    {
        var folder = Path.Combine(when_holding_the_typescript_compiler_to_the_dependency_graph.Root(), "Samples", sample);
        var files = Directory.GetFiles(folder, "*.play", SearchOption.AllDirectories).Order(StringComparer.Ordinal)
            .ToDictionary(path => Path.GetRelativePath(folder, path).Replace('\\', '/'), File.ReadAllText, StringComparer.Ordinal);
        var compiler = new ScreenplayCompiler();
        PlayApplicationAssembly.Compile(compiler, files.Keys, new InMemoryPlayDocumentSource(files), compiler.Languages, out var timeline);
        timeline.SourceValid.ShouldBeTrue();
        timeline.Findings.Count.ShouldEqual(findings);
        var graph = DependencyGraph.For(timeline);
        var timelineGroups = timeline.Findings.Where(finding => finding.Members.Length > 0).Select(finding => string.Join('|', finding.Members)).ToArray();
        var groups = graph.SiblingGroups(["usesFactsFrom", "reactsTo"]).Select(group => string.Join('|', group.Members.Select(node => (node.Kind == "slice" ? "slice:" : "container:") + node.Scope[^1]))).ToArray();
        groups.ShouldEqual(timelineGroups);
        object Edges(string from, string to) => graph.Implied(from, to).Select(edge => new { source = edge.Source.Address, target = edge.Target.Address, edge.SliceEdges, edge.References }).ToArray();
        output.WriteLine(JsonSerializer.Serialize(new
        {
            sample,
            modules = Edges("module", "module"), features = Edges("feature", "feature"), featureToModule = Edges("feature", "module"), moduleToFeature = Edges("module", "feature"), outside = Edges("slice", "context"),
            moduleCycles = graph.Cycles("module").Select(group => group.Members.Select(node => node.Address)),
            featureCycles = graph.Cycles("feature").Select(group => group.Members.Select(node => node.Address)),
            order = graph.SuggestedOrder().Containers.Select(order => new { container = order.Container.Address, children = order.Children.Select(node => node.Address), order.Changed }),
            graph.UnusedImports,
            unresolved = graph.Unresolved.Select(item => new { consumer = item.Consumer.Address, item.Role, item.Name })
        }));
        if (sample == "Commerce")
        {
            graph.Implied("module", "module").Single(edge => edge.Source.Address == "Ordering").SliceEdges.ShouldEqual(4);
            graph.Implied("module", "module").Single(edge => edge.Source.Address == "Fulfillment").SliceEdges.ShouldEqual(1);
            graph.Cycles("module").Single().Members.Select(node => node.Address).ShouldEqual(["Ordering", "Fulfillment"]);
            graph.Cycles("feature").Single().Members.Select(node => node.Address).ShouldEqual(["Fulfillment.Shipping", "Fulfillment.Tracking"]);
            graph.SuggestedOrder().Containers.Single(order => order.Container.Address == "Ordering").Children.Select(node => node.Address).ShouldEqual(["Ordering.Payments", "Ordering.Orders", "Ordering.Support"]);
        }
        if (sample == "TimeTracking")
        {
            graph.Implied("module", "module").Select(edge => edge.Source.Address + "|" + edge.Target.Address).ShouldEqual(["Engagements|Timesheets", "Payroll|Timesheets", "Timesheets|Engagements"]);
            graph.Cycles("module").Count.ShouldEqual(0);
            graph.SuggestedOrder().Containers[0].Children.Select(node => node.Address).ShouldEqual(["Engagements", "Timesheets", "Payroll"]);
        }
        if (sample == "Library")
        {
            graph.Cycles("feature").Single().Members.Select(node => node.Address).ShouldEqual(["Lending.Catalog", "Lending.Loans"]);
            graph.SuggestedOrder().Containers.Any(order => order.Changed).ShouldBeFalse();
        }
        if (sample == "Invoicing")
        {
            graph.Implied("slice", "context").Select(edge => edge.Target.Address).Distinct().Order(StringComparer.Ordinal).ShouldEqual(["context:Customers", "context:Payments", "context:Shipping"]);
            graph.UnusedImports.Count.ShouldEqual(0);
            graph.Implied("feature", "feature").Any(edge => edge.Source.Address == "Invoicing.InvoiceManagement" && edge.Target.Address == "Invoicing.InvoiceManagement.Adjustments").ShouldBeFalse();
        }
    }
}
