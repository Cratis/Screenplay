// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpDisk;

public class when_moving_a_document_to_a_case_only_name : for_McpConnection.given.a_connection
{
    McpDiskResult _result = null!;

    void Because()
    {
        var workspace = Workspace();
        var transaction = workspace.Propose(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Operations = [new MoveWorkspaceDocument { Document = workspace.Documents[0].Id, Path = PortablePlayPath.Parse("Application.play") }]
        });
        transaction.Success.ShouldBeTrue();
        _result = new McpDisk(Root).Apply(new McpProposal(workspace, transaction));
    }

    [Fact] void should_apply_the_move() => _result.Success.ShouldBeTrue();
    [Fact] void should_use_the_new_case_on_disk() => Directory.EnumerateFiles(RootPath, "*.play").Select(Path.GetFileName).ShouldContainOnly(["Application.play"]);
    [Fact] void should_preserve_the_exact_source() => File.ReadAllText(Path.Combine(RootPath, "Application.play")).ShouldEqual(Source);
}
