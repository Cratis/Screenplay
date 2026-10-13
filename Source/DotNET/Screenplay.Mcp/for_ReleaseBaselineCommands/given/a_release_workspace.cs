// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_ReleaseBaselineCommands.given;

public class a_release_workspace : for_McpConnection.given.a_connection
{
    internal ScreenplayWorkspace Original = null!;
    internal McpManagedFiles Files = null!;
    internal byte[] OriginalState = [];
    internal string BaselinePath = null!;

    void Establish()
    {
        Original = Workspace();
        Original.Compilation.Success.ShouldBeTrue();
        Files = new(Root);
        OriginalState = McpState.Serialize(Original);
        McpManagedFiles.WritePrivate(Files.PathFor(McpState.FileName, create: true), OriginalState);
        BaselinePath = Files.PathFor(ReleaseBaselineStore.FileName);
    }

    internal void AdmitBaseline() => ReleaseBaselineCommands.Admit(RootPath, "v1").ExitCode.ShouldEqual(0);
}
