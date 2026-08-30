// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_an_edit_breaks_compilation : given.a_valid_workspace
{
    const string InvalidSource =
        """
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                name MissingConcept
        """;
    WorkspaceTransactionResult _result = null!;

    void Because() => _result = Workspace.Propose(Request(new ReplaceWorkspaceDocument
    {
        Document = Registration.Id,
        Bytes = Bytes(InvalidSource)
    }));

    [Fact] void should_reject_the_complete_transaction() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_compilation_failure() => _result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.CompilationFailed);
    [Fact] void should_surface_blocking_compiler_diagnostics() => _result.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeTrue();
    [Fact] void should_return_no_candidate_workspace() => _result.Workspace.ShouldBeNull();
    [Fact] void should_keep_the_original_document_bytes() => Workspace.Documents.Single(document => document.Id == Registration.Id).Text.ShouldEqual(RegistrationSource);
}
