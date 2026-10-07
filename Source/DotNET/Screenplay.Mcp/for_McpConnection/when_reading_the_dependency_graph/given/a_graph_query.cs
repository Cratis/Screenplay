// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_reading_the_dependency_graph.given;

public class a_graph_query : Specification
{
    static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
    protected const string Source = "module A\n  feature F\n    slice StateView V\n      projection R\n        from E\n      specification T\n        given E\nmodule B\n  feature G\n    slice StateChange W\n      event E\n";
    private protected McpSnapshot _snapshot;
    protected JsonElement _result;

    void Establish() => _snapshot = Snapshot(Source);

    private protected static McpSnapshot Snapshot(string source) => new([WorkspaceDocument.Create("application.play", PortablePlayPath.Parse("application.play"), Encoding.UTF8.GetBytes(source))]);
    private protected static JsonElement Read(McpSnapshot snapshot, object arguments) => JsonSerializer.SerializeToElement(McpDependencyGraphQueries.Read(snapshot, JsonSerializer.SerializeToElement(arguments)), Options);

    protected static string Root([System.Runtime.CompilerServices.CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation"))) directory = directory.Parent;

        return directory!.FullName;
    }
}
