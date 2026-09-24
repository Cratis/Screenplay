// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_adding_a_conflicting_owner : given.a_valid_workspace
{
    WorkspaceTransactionResult _result = null!;

    void Because() => _result = Workspace.Propose(Request(new AddWorkspaceDocument
    {
        StableKey = "duplicate-projects-module",
        Path = PortablePlayPath.Parse("DuplicateProjects.play"),
        Bytes = Bytes(
            """
            module Projects
              feature Registration
                slice StateChange RegisterProject
            """)
    }));

    [Fact] void should_reject_the_complete_transaction() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_typed_owner_conflict() => _result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.ConflictingOwner);
    [Fact] void should_name_the_new_owner() => _result.Conflicts.Single().Path!.Value.ShouldEqual("DuplicateProjects.play");
    [Fact] void should_name_the_previous_owner() => _result.Conflicts.Single().OtherPath!.Value.ShouldEqual(Registration.Path.Value);
    [Fact] void should_not_offer_a_write_plan() => _result.WritePlan.ShouldBeNull();
    [Fact] void should_surface_the_duplicate_owner_diagnostic() => _result.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error && diagnostic.Message.Contains("slice", StringComparison.OrdinalIgnoreCase)).ShouldBeTrue();
    [Fact] void should_keep_the_original_document_count() => Workspace.Documents.Length.ShouldEqual(2);
}
