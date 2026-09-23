// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpDisk;

public class when_replacing_a_restricted_file : given.a_restricted_source
{
    McpDiskResult _result = null!;

    void Because()
    {
        var workspace = Workspace();
        var transaction = workspace.Propose(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Operations = [new ReplaceWorkspaceDocument
            {
                Document = workspace.Documents[0].Id,
                Bytes = [.. Encoding.UTF8.GetBytes(Source.Replace("Registers a new project", "Registers the project", StringComparison.Ordinal))]
            }]
        });
        _result = new McpDisk(Root).Apply(new McpProposal(workspace, transaction));
    }

    [Fact] void should_apply_the_replacement() => _result.Success.ShouldBeTrue();
    [Fact] void should_preserve_the_restricted_access() => Access(Path.Combine(RootPath, "application.play")).ShouldEqual(OriginalAccess);
    [Fact] void should_write_the_proposed_content() => File.ReadAllText(Path.Combine(RootPath, "application.play")).Contains("Registers the project", StringComparison.Ordinal).ShouldBeTrue();
}
