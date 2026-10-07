// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Cratis.Screenplay.Dependencies;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_reading_the_dependency_graph;

public partial class when_querying
{
    static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
    const string Source = "module A\n  feature F\n    slice StateView V\n      projection R\n        from E\n      specification T\n        given E\nmodule B\n  feature G\n    slice StateChange W\n      event E\n";
    static McpSnapshot Snapshot() => new([WorkspaceDocument.Create("application.play", PortablePlayPath.Parse("application.play"), Encoding.UTF8.GetBytes(Source))]);
    static JsonElement Read(McpSnapshot snapshot, object arguments) => JsonSerializer.SerializeToElement(McpDependencyGraphQueries.Read(snapshot, JsonSerializer.SerializeToElement(arguments)), Options);

    [Fact]
    public void should_default_to_module_levels_and_bound_the_evidence()
    {
        var result = Read(Snapshot(), new { evidenceLimit = 1 });
        var item = result.GetProperty("page").GetProperty("items").EnumerateArray().Single();
        item.GetProperty("source").GetProperty("kind").GetString().ShouldEqual("Module");
        item.GetProperty("source").GetProperty("address").GetString().ShouldEqual("A");
        item.GetProperty("target").GetProperty("address").GetString().ShouldEqual("B");
        item.GetProperty("references").GetInt32().ShouldEqual(1);
        result.GetProperty("orderSource").GetString().ShouldEqual("authored");
    }

    [Fact]
    public void should_support_mixed_levels_and_test_only_edges()
    {
        var result = Read(Snapshot(), new { from = "feature", to = "module", includeTestOnly = true, evidenceLimit = 1 });
        var item = result.GetProperty("page").GetProperty("items").EnumerateArray().Single();
        item.GetProperty("source").GetProperty("address").GetString().ShouldEqual("A.F");
        item.GetProperty("references").GetInt32().ShouldEqual(2);
        item.GetProperty("byKind").GetProperty("verifiedWith").GetInt32().ShouldEqual(1);
        item.GetProperty("evidenceCount").GetInt32().ShouldEqual(2);
        item.GetProperty("evidenceTruncated").GetBoolean().ShouldBeTrue();
        item.GetProperty("evidence").GetArrayLength().ShouldEqual(1);
    }

    [Fact]
    public void should_keep_every_evidence_location_name_and_role_in_the_index()
    {
        var root = Root();
        foreach (var sample in new[] { "Library", "Commerce", "Invoicing", "TimeTracking" })
        {
            var folder = Path.Combine(root, "Samples", sample);
            var snapshot = new McpSnapshot([.. Directory.GetFiles(folder, "*.play", SearchOption.AllDirectories).Select(path => WorkspaceDocument.Create(Path.GetRelativePath(folder, path).Replace('\\', '_').Replace('/', '_'), PortablePlayPath.Parse(Path.GetRelativePath(folder, path).Replace('\\', '/')), File.ReadAllBytes(path)))]);
            var graph = McpDependencyGraphQueries.Graph(snapshot);
            foreach (var item in graph.Edges.SelectMany(edge => edge.Evidence))
            {
                Assert.True(snapshot.Index.References.Any(reference => reference.Name == item.Name && reference.Role == item.Role && reference.Location == item.Location), $"{sample}: {item.Consumer.Address} {item.Role}:{item.Name} at {item.Location}; indexed: {string.Join(';', snapshot.Index.References.Where(reference => reference.Name == item.Name && reference.Location == item.Location).Select(reference => reference.Role))}");
            }
        }
    }

    [Fact]
    public void should_classify_every_index_role_and_explicitly_exclude_other_roles()
    {
        var text = File.ReadAllText(Path.Combine(Root(), "Source/DotNET/Screenplay.Mcp/McpReferenceKinds.cs"));
        foreach (var role in IndexRoles().Matches(text).Select(match => match.Groups[1].Value).Distinct()) SliceReferences.Classifications.ContainsKey(role).ShouldBeTrue();
        foreach (var role in new[] { "givenEvent", "whenAppendedEvent", "thenEvent", "givenReadModel", "thenReadModel", "thenAbsentReadModel" }) SliceReferences.Classifications.ContainsKey(role).ShouldBeTrue();
    }

    [Fact]
    public void should_page_at_a_pinned_revision()
    {
        var snapshot = Snapshot();
        var result = Read(snapshot, new { view = "order", limit = 1 });
        result.GetProperty("sourceRevision").GetString().ShouldEqual(snapshot.SourceRevision);
        result.GetProperty("page").GetProperty("nextOffset").GetInt32().ShouldEqual(1);
        var next = Read(snapshot, new { view = "order", limit = 1, offset = 1, expectedSourceRevision = snapshot.SourceRevision });
        next.GetProperty("page").GetProperty("offset").GetInt32().ShouldEqual(1);
    }

    [GeneratedRegex(",\\s*\"([^\"\\r\\n]+)\"\\)", RegexOptions.None, 1000)]
    private static partial Regex IndexRoles();

    static string Root([System.Runtime.CompilerServices.CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation"))) directory = directory.Parent;

        return directory!.FullName;
    }
}
