// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_renaming_a_document_key : given.a_valid_workspace
{
    const string NewKey = "registration-slice";
    WorkspaceTransactionResult _result = null!;

    void Because() => _result = Workspace.Propose(Request(new RenameWorkspaceDocument
    {
        Document = Registration.Id,
        StableKey = NewKey
    }));

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_preserve_the_document_identity() => _result.Workspace!.Documents.Single(document => document.StableKey == NewKey).Id.ShouldEqual(Registration.Id);
    [Fact] void should_preserve_the_document_path() => _result.Workspace!.Documents.Single(document => document.Id == Registration.Id).Path.ShouldEqual(Registration.Path);
    [Fact] void should_migrate_the_catalog_assignment() => _result.Workspace!.IdentityCatalog.Documents.Single(assignment => assignment.Key == NewKey).Id.ShouldEqual(Registration.Id);
    [Fact] void should_remove_the_previous_catalog_key() => _result.Workspace!.IdentityCatalog.Documents.Any(assignment => assignment.Key == Registration.StableKey).ShouldBeFalse();
    [Fact] void should_plan_one_rename() => _result.WritePlan!.Entries.Select(entry => entry.Kind).ShouldContainOnly(WorkspaceWriteKind.Renamed);
}
