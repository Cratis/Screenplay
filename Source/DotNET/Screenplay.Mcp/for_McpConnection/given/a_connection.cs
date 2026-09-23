// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpConnection.given;

public class a_connection : Specification
{
    internal const string Source = """
        concept ProjectId : Uuid
        concept ProjectName : String
        module Projects
          feature Registration
            slice StateChange RegisterProject
              description "Registers a new project"
              command RegisterProject
                projectId ProjectId identifier
                name ProjectName
                produces ProjectRegistered
                  for projectId
                  projectId = projectId
                  name = name
              event ProjectRegistered
                projectId ProjectId
                name ProjectName
        """;

    internal string RootPath = null!;
    internal McpRoot Root = null!;
    internal McpConnection Connection = null!;

    void Establish()
    {
        var repository = new DirectoryInfo(Directory.GetCurrentDirectory());

        // A worktree checkout has a .git file rather than a .git directory.
        while (repository is not null && !Directory.Exists(Path.Combine(repository.FullName, ".git")) && !File.Exists(Path.Combine(repository.FullName, ".git")))
        {
            repository = repository.Parent;
        }

        var output = Environment.GetEnvironmentVariable("AI_WORK_OUTPUT") ?? Path.Combine(repository.FullName, ".ai-work", "mcp-specs");
        RootPath = Path.Combine(output, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(RootPath);
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Source, new UTF8Encoding(false));
        Root = new(RootPath);
        Connection = new(new McpTools(Root));
    }

    internal void Initialize()
    {
        Connection.Handle(/*lang=json,strict*/ """{"jsonrpc":"2.0","id":0,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"spec","version":"1"}}}""");
        Connection.Handle(/*lang=json,strict*/ """{"jsonrpc":"2.0","method":"notifications/initialized"}""");
    }

    internal JsonElement Call(string name, object? arguments = null)
    {
        var request = JsonSerializer.Serialize(new { jsonrpc = "2.0", id = 1, method = "tools/call", @params = new { name, arguments = arguments ?? new { } } });
        using var result = JsonDocument.Parse(Connection.Handle(request));
        return result.RootElement.Clone();
    }

    internal ScreenplayWorkspace Workspace() => ScreenplayWorkspace.Create("Projects", Root.Read(), SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));

    internal static McpProposal Rename(ScreenplayWorkspace workspace) => new(workspace, workspace.Propose(new()
    {
        ExpectedRevision = workspace.Revision,
        ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
        Operations = [new MoveWorkspaceDocument { Document = workspace.Documents[0].Id, Path = PortablePlayPath.Parse("renamed.play") }]
    }));

    void Destroy() => Directory.Delete(RootPath, true);
}
