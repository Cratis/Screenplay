// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpWorkspaces.given;

public class a_competing_workspace_state : a_persisted_nested_model
{
    internal string ModelRoot = null!;
    internal McpRoot NestedRoot = null!;
    internal McpManagedFiles NestedFiles = null!;
    internal ScreenplayWorkspace NestedWorkspace = null!;
    internal byte[] NestedState = [];

    void Establish()
    {
        ModelRoot = Path.Combine(RootPath, ModelFolder);
        NestedRoot = new(ModelRoot);
        NestedWorkspace = ScreenplayWorkspace.Create("Other", NestedRoot.Read(), SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Other")));
        NestedFiles = new(NestedRoot);
        NestedState = McpState.Serialize(NestedWorkspace);
        McpManagedFiles.WritePrivate(NestedFiles.PathFor(McpState.FileName, create: true), NestedState);
        Connection = new(new McpTools { ClientRoots = Workspaces.ClientRoots, CurrentDirectoryHint = RootPath });
        Initialize();
    }

    internal McpRecoveryJournal PrepareNestedJournal()
    {
        var proposal = Move(NestedWorkspace, "renamed.play");

        return McpRecoveryJournal.Prepare(NestedRoot, proposal, new(NestedState, McpState.Serialize(proposal.Workspace)));
    }
}
