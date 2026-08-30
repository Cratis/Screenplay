// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_proposing_no_operations : given.a_valid_workspace
{
    WorkspaceTransactionResult _result = null!;

    void Because() => _result = Workspace.Propose(Request());

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_return_the_same_workspace_instance() => ReferenceEquals(_result.Workspace, Workspace).ShouldBeTrue();
    [Fact] void should_keep_the_exact_revision() => _result.Workspace!.Revision.ShouldEqual(Workspace.Revision);
    [Fact] void should_keep_the_exact_catalog_revision() => _result.Workspace!.IdentityCatalog.Revision.ShouldEqual(Workspace.IdentityCatalog.Revision);
    [Fact] void should_plan_no_document_writes() => _result.WritePlan!.Entries.ShouldBeEmpty();
}
