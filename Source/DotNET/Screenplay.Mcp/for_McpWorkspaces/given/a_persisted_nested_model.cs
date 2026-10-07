// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpWorkspaces.given;

public class a_persisted_nested_model : for_McpConnection.given.a_connection
{
    internal McpWorkspaces Workspaces = null!;
    internal McpManagedFiles Files = null!;
    internal ScreenplayWorkspace Applied = null!;
    internal byte[] StateBytes = [];
    protected virtual string ModelFolder => "Models";

    void Establish()
    {
        var original = Workspace();
        var proposal = Move(original, $"{ModelFolder}/application.play");
        new McpDisk(Root).Apply(proposal).Success.ShouldBeTrue();
        Applied = proposal.Workspace;
        Files = new(Root);
        StateBytes = Files.Read(McpState.FileName)!;
        Workspaces = new()
        {
            ClientRoots = [new Uri(RootPath + Path.DirectorySeparatorChar).AbsoluteUri],
            CurrentDirectoryHint = RootPath
        };
    }

    internal static McpProposal Move(ScreenplayWorkspace workspace, string path) => new(workspace, workspace.Propose(new()
    {
        ExpectedRevision = workspace.Revision,
        ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
        Operations = [new MoveWorkspaceDocument { Document = workspace.Documents.Single().Id, Path = PortablePlayPath.Parse(path) }]
    }));

    internal static JsonElement Result(object value) => JsonSerializer.SerializeToElement(value).GetProperty("structuredContent");
}
