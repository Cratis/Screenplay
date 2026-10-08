// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Files;

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph;

public class when_matching_timeline_groups : Specification
{
    [Theory]
    [InlineData("Library")]
    [InlineData("Invoicing")]
    [InlineData("Commerce")]
    [InlineData("TimeTracking")]
    public void should_include_fact_groups_and_non_feedback_reads_in_timeline_groups(string sample)
    {
        var folder = Path.Combine(given.a_conformance_suite.Root(), "Samples", sample);
        var files = Directory.GetFiles(folder, "*.play", SearchOption.AllDirectories).Order(StringComparer.Ordinal)
            .ToDictionary(path => Path.GetRelativePath(folder, path).Replace('\\', '/'), File.ReadAllText, StringComparer.Ordinal);
        var compiler = new ScreenplayCompiler();
        PlayApplicationAssembly.Compile(compiler, files.Keys, new InMemoryPlayDocumentSource(files), compiler.Languages, out var timeline);
        var groups = DependencyGraph.For(timeline).SiblingGroups(["usesFactsFrom", "reactsTo"]);

        // The full graph also includes feedback reads, deliberately absent from the timeline.
        var expected = groups.Select(group => string.Join('|', group.Members.Select(node => (node.Kind == "slice" ? "slice:" : "container:") + node.Scope[^1])));
        timeline.Findings.Where(finding => finding.Members.Length > 0).Select(finding => string.Join('|', finding.Members)).ShouldEqual(expected);
    }
}
