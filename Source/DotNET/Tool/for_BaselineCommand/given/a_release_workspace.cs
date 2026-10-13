// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Mcp;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Tool.for_BaselineCommand.given;

public class a_release_workspace : for_ModelCheck.given.a_model
{
    protected const string Source = "module M\n  feature F\n    slice StateChange Record\n      event Recorded\n        value String\n";
    protected ScreenplayWorkspace Workspace = null!;
    protected string BaselinePath = null!;
    protected string IdentityPath = null!;

    void Establish()
    {
        File.WriteAllText(Path.Combine(Root, "application.play"), Source);
        var root = new McpRoot(Root);
        Workspace = ScreenplayWorkspace.Create("Model", root.Read(), SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Model")));
        var files = new McpManagedFiles(root);
        IdentityPath = files.PathFor(McpState.FileName, create: true);
        McpManagedFiles.WritePrivate(IdentityPath, McpState.Serialize(Workspace));
        BaselinePath = files.PathFor(ReleaseBaselineStore.FileName);
    }

    protected int Run(params string[] args) => ToolCommands.Run(args, Output, Error);

    protected void Admit() => Run("baseline", "admit", Root, "--release", "v1").ShouldEqual(0);
}
