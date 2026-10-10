// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpDurableState;

public class when_applying_only_state_to_a_missing_root : for_McpConnection.given.a_connection
{
    McpAuthoringProposal _proposal = null!;
    McpDiskResult _result = null!;

    void Establish()
    {
        Root = new(Path.Combine(RootPath, "Screenplay"));
        var workspace = ScreenplayWorkspace.CreateEmpty(ApplicationIdentity.Create("Projects"), "Projects");
        var transaction = workspace.ProposeAuthoring(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Validation = WorkspaceAuthoringValidation.Authoring
        });
        _proposal = new(workspace, transaction, WorkspaceAuthoringValidation.Authoring);
    }

    void Because() => _result = new McpDisk(Root).Apply(_proposal);

    [Fact] void should_apply_without_document_changes() => _result.Success.ShouldBeTrue();
    [Fact] void should_create_the_root_for_state() => Root.Exists.ShouldBeTrue();
    [Fact] void should_persist_the_identity_state() => new McpManagedFiles(Root).Read(McpState.FileName).ShouldNotBeNull();
    [Fact] void should_leave_no_pending_journal() => McpRecoveryJournal.Load(Root).ShouldBeNull();
    [Fact] void should_leave_the_source_empty() => Root.Read(allowEmpty: true).ShouldBeEmpty();
}
