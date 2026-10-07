// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_reading_the_dependency_graph;

public class and_every_evidence_resolves : given.a_graph_query
{
    McpSnapshot[] _samples;
    bool _allResolve;

    void Establish() => _samples = [.. new[] { "Library", "Commerce", "Invoicing", "TimeTracking" }.Select(sample =>
    {
        var folder = Path.Combine(Root(), "Samples", sample);
        return new McpSnapshot([.. Directory.GetFiles(folder, "*.play", SearchOption.AllDirectories).Select(path => WorkspaceDocument.Create(Path.GetRelativePath(folder, path).Replace('\\', '_').Replace('/', '_'), PortablePlayPath.Parse(Path.GetRelativePath(folder, path).Replace('\\', '/')), File.ReadAllBytes(path)))]);
    })];

    void Because() => _allResolve = _samples.All(snapshot => McpDependencyGraphQueries.Graph(snapshot).Edges.SelectMany(edge => edge.Evidence).All(item => snapshot.Index.References.Any(reference => reference.Name == item.Name && reference.Role == item.Role && reference.Location == item.Location)));

    [Fact] void should_keep_every_evidence_location_name_and_role_in_the_index() => _allResolve.ShouldBeTrue();
}
