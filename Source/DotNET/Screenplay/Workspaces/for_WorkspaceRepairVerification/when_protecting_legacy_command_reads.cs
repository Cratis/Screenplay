// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRepairVerification;

public class when_protecting_legacy_command_reads : Specification
{
    ScreenplayWorkspace _workspace;
    WorkspaceDiagnosticRepair _repair;
    WorkspaceAuthoringResult _result;

    void Establish()
    {
        _workspace = given.legacy_command_reads.Create(("source.play", given.legacy_command_reads.Source));
        var diagnostic = _workspace.Compilation.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyCommandReadsRepairAvailable);
        _repair = WorkspaceDiagnosticRepairs.Find(_workspace, _workspace.Revision, diagnostic).Single();
    }

    void Because() => _result = WorkspaceDiagnosticRepairs.ProposeRepair(_workspace, _repair, given.legacy_command_reads.Request(_workspace));

    [Fact] void should_preview_without_changing_the_original() => _workspace.Documents.Single().Text.ShouldEqual(given.legacy_command_reads.Source);
    [Fact] void should_accept_the_candidate() => Assert.True(_result.Accepted, string.Join("; ", _result.Conflicts.Select(conflict => conflict.Message)));
    [Fact] void should_admit_all_reads() => _result.ExecutableReady.ShouldBeTrue();
    [Fact] void should_insert_the_preamble() => _result.Workspace!.Documents.Single().Text.ShouldContain("command reads protected");
    [Fact] void should_keep_the_read_subject() => WorkspaceSyntaxIndex.Create(_workspace).Find(_repair.Subject)!.Node.ShouldBeOfExactType<ReadsSyntax>();
    [Fact] void should_require_individual_review() => _repair.CanFixAll.ShouldBeFalse();
    [Fact] void should_disclose_the_behavior_change() => _repair.Title!.ShouldContain("absence and conflict");
    [Fact] void should_remove_the_legacy_diagnostic() => _result.Workspace!.Compilation.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyCommandReadsRepairAvailable);

    [Fact]
    void should_refuse_a_stale_revision()
    {
        var changed = given.legacy_command_reads.Create(("source.play", given.legacy_command_reads.Source + "\n// changed\n"));
        var result = WorkspaceDiagnosticRepairs.ProposeRepair(changed, _repair, given.legacy_command_reads.Request(_workspace));
        result.Accepted.ShouldBeFalse();
        result.Conflicts.ShouldContain(conflict => conflict.Kind == WorkspaceConflictKind.StaleWorkspaceRevision);
    }

    [Fact]
    void should_require_canonical_formatting_consent()
    {
        var result = WorkspaceDiagnosticRepairs.ProposeRepair(_workspace, _repair, given.legacy_command_reads.Request(_workspace) with { Formatting = WorkspaceAuthoringFormatting.PreserveTrivia });
        result.Conflicts.ShouldContain(conflict => conflict.Kind == WorkspaceConflictKind.FormattingConsentRequired);
    }
}
