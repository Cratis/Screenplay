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
    [Fact] void should_report_compilation_failure() => _result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.CompilationFailed);
    [Fact] void should_surface_the_duplicate_owner_diagnostic() => _result.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error && diagnostic.Message.Contains("slice", StringComparison.OrdinalIgnoreCase)).ShouldBeTrue();
    [Fact] void should_keep_the_original_document_count() => Workspace.Documents.Length.ShouldEqual(2);
}
