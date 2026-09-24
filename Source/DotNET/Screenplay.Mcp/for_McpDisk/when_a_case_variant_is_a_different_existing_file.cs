// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpDisk;

public class when_a_case_variant_is_a_different_existing_file : for_McpConnection.given.a_connection
{
    bool _caseSensitive;
    Exception? _error;

    void Because()
    {
        _caseSensitive = !File.Exists(Path.Combine(RootPath, "Application.play"));
        if (!_caseSensitive)
        {
            return;
        }

        var workspace = Workspace();
        var transaction = workspace.Propose(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Operations = [new MoveWorkspaceDocument { Document = workspace.Documents[0].Id, Path = PortablePlayPath.Parse("Application.play") }]
        });
        transaction.Success.ShouldBeTrue();
        File.WriteAllText(Path.Combine(RootPath, "Application.play"), "external content");
        _error = Catch.Exception(() => new McpDisk(Root).Apply(new McpProposal(workspace, transaction)));
    }

    [Fact] void should_refuse_the_distinct_occupied_destination() => (!_caseSensitive || _error is McpFailure).ShouldBeTrue();
    [Fact] void should_not_treat_the_files_as_the_same_entry() => (!_caseSensitive || !McpDiskPaths.SameEntry(Root, Path.Combine(RootPath, "application.play"), Path.Combine(RootPath, "Application.play"))).ShouldBeTrue();
    [Fact] void should_leave_the_existing_destination_untouched() => (!_caseSensitive || File.ReadAllText(Path.Combine(RootPath, "Application.play")) == "external content").ShouldBeTrue();
    [Fact] void should_leave_the_original_untouched() => File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(Source);
}
