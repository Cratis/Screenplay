// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpDisk;

public class when_a_folder_expansion_has_stale_source : for_McpConnection.given.a_connection
{
    Exception? _error;
    McpProposal _proposal = null!;

    void Establish()
    {
        var workspace = Workspace();
        var transaction = workspace.Propose(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Operations = McpLayout.Expand(workspace)
        });
        transaction.Success.ShouldBeTrue();
        _proposal = new McpProposal(workspace, transaction);
        File.AppendAllText(Path.Combine(RootPath, "application.play"), "\n// external edit\n");
    }

    void Because() => _error = Catch.Exception(() => new McpDisk(Root).Apply(_proposal));

    [Fact] void should_refuse_disk_drift_before_writing() => _error!.Message.Contains("DiskDrift", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_keep_the_external_edit() => File.ReadAllText(Path.Combine(RootPath, "application.play")).EndsWith("// external edit\n", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_create_any_folder_file() => Directory.EnumerateFiles(RootPath, "*.play", SearchOption.AllDirectories).Count().ShouldEqual(1);
}
