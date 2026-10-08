// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpSemanticDiff;

public class when_the_document_moves : given.a_semantic_comparison
{
    void Because()
    {
        Proposal = new McpProposal(Workspace, Workspace.Propose(new()
        {
            ExpectedRevision = Workspace.Revision,
            ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
            Operations = [new MoveWorkspaceDocument { Document = Workspace.Documents[0].Id, Path = PortablePlayPath.Parse("models/projects.play") }]
        }));
        Diff = Read();
    }

    [Fact] void should_report_moves_only() => Items("declarations").All(item => item.GetProperty("changeKind").GetString() == "moved").ShouldBeTrue();
    [Fact] void should_not_report_a_semantic_change() => Diff.GetProperty("hasSemanticChange").GetBoolean().ShouldBeFalse();
    [Fact] void should_not_report_changed_members() => Items("members").ShouldBeEmpty();
    [Fact] void should_not_report_changed_event_contracts() => Items("events").ShouldBeEmpty();
}
