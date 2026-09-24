// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpDisk;

public class when_a_case_only_installation_fails : for_McpConnection.given.a_connection
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
        var moves = 0;
        _result = new McpDisk(Root, (source, destination) =>
        {
            if (++moves == 3)
            {
                throw new McpFailure("Injected identity installation failure.");
            }

            File.Move(source, destination);
        }).Apply(new McpProposal(workspace, transaction));
    }

    [Fact] void should_report_rollback() => _result.Status.ShouldEqual("RolledBack");
    [Fact] void should_restore_the_original_case() => Directory.EnumerateFiles(RootPath, "*.play").Select(Path.GetFileName).ShouldContainOnly(["application.play"]);
    [Fact] void should_restore_the_original_bytes() => File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(Source);
}
