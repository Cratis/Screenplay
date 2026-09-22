// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.Mcp.for_McpDisk;

public class when_a_multifile_installation_partially_fails : for_McpConnection.given.a_connection
{
    McpDiskResult _result = null!;

    void Because()
    {
        var workspace = Workspace();
        var transaction = workspace.Propose(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Operations = McpLayout.Expand(workspace)
        });
        transaction.Success.ShouldBeTrue();
        var moves = 0;
        _result = new McpDisk(Root, (source, destination) =>
        {
            if (++moves == 3)
            {
                throw new McpFailure("Injected failure after installing one expanded document.");
            }

            File.Move(source, destination);
        }).Apply(new(workspace, transaction));
    }

    [Fact] void should_not_report_partial_success() => _result.Success.ShouldBeFalse();
    [Fact] void should_restore_the_original_document_set() => Directory.EnumerateFiles(RootPath, "*.play", SearchOption.AllDirectories).Count().ShouldEqual(1);
    [Fact] void should_restore_the_exact_original_source() => File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(Source);
    [Fact] void should_report_rollback() => _result.Status.ShouldEqual("RolledBack");
}
