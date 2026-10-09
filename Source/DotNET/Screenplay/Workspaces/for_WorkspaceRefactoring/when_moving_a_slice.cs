// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_moving_a_slice : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _result = null!;
    WorkspaceMoveRequest _request = null!;

    void Establish()
    {
        Registration = Document("registration", "Projects/Registration.play", RegistrationSource + """

              readmodel RegisteredProject
                projectId ProjectId
                name ProjectName
              query GetProject => RegisteredProject?
                by projectId ProjectId
              specification Registers
                when RegisterProject
                  projectId = "b0f2e9b0-1b9a-4e54-9b0a-222222222222"
                  name = "A project"
                then ProjectRegistered
                  projectId = "b0f2e9b0-1b9a-4e54-9b0a-222222222222"
                  name = "A project"
          feature Archive
        """);
        Workspace = ScreenplayWorkspace.Create("Projects", [Concepts, Registration], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
        var application = Workspace.IdentityCatalog.Application;
        _request = new()
        {
            ExpectedRevision = Workspace.Revision,
            ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
            Target = SemanticAddress.ForSlice(application, "Projects", "Registration", "RegisterProject"),
            NewParent = SemanticAddress.ForFeature(application, "Projects", "Archive"),
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Validation = WorkspaceAuthoringValidation.Executable
        };
    }

    void Because() => _result = Workspace.ProposeMove(_request);

    [Fact] void should_accept() => Assert.True(_result.Accepted, string.Join(" | ", _result.Conflicts.Select(conflict => conflict.Message)));
    [Fact] void should_have_an_executable_baseline() => Assert.True(Workspace.Compilation.Success, string.Join(" | ", Workspace.Compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
    [Fact] void should_keep_every_semantic_identity() => _result.Workspace.IdentityCatalog.Semantics.Select(assignment => assignment.Id).ShouldContainOnly(Workspace.IdentityCatalog.Semantics.Select(assignment => assignment.Id));
    [Fact] void should_keep_every_event_contract_identity() => _result.Workspace.IdentityCatalog.EventContracts.Select(assignment => assignment.Id).ShouldContainOnly(Workspace.IdentityCatalog.EventContracts.Select(assignment => assignment.Id));
    [Fact] void should_move_every_assigned_descendant() => _result.MoveReport.IdentityMigrations.All(migration => migration.CurrentAddress.Parts[2].Key == "Archive").ShouldBeTrue();
    [Fact] void should_report_event_and_semantic_migrations() => _result.MoveReport.IdentityMigrations.Select(migration => migration.Domain).Distinct().ShouldContainOnly("semantic", "event");
    [Fact] void should_never_retire_an_identity() => _result.MoveReport.Retired.ShouldBeEmpty();
    [Fact] void should_preserve_event_name_and_pin() => WorkspaceSyntaxIndex.Create(_result.Workspace).Entries.Select(entry => entry.Node).OfType<EventSyntax>().Single().Name.ShouldEqual("ProjectRegistered");
    [Fact] void should_leave_the_original_parent() => WorkspaceSyntaxIndex.Create(_result.Workspace).Entries.Any(entry => entry.Address?.Equals(SemanticAddress.ForFeature(Workspace.IdentityCatalog.Application, "Projects", "Registration")) == true).ShouldBeTrue();
    [Fact] void should_keep_document_paths() => _result.Workspace.Documents.Select(document => document.Path).ShouldContainOnly(Workspace.Documents.Select(document => document.Path));
    [Fact] void should_reject_a_no_op() => Workspace.ProposeMove(_request with { NewParent = SemanticAddress.ForFeature(Workspace.IdentityCatalog.Application, "Projects", "Registration") }).Accepted.ShouldBeFalse();
    [Fact] void should_reject_a_stale_revision() => Workspace.ProposeMove(_request with { ExpectedRevision = default }).Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.StaleWorkspaceRevision);
    [Fact] void should_reject_a_stale_catalog() => Workspace.ProposeMove(_request with { ExpectedCatalogRevision = default }).Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.StaleCatalogRevision);
}
