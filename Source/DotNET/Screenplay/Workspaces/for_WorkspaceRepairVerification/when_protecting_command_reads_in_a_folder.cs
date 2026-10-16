// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRepairVerification;

public class when_protecting_command_reads_in_a_folder : Specification
{
    ScreenplayWorkspace _workspace;
    WorkspaceAuthoringResult _result;

    void Establish() => _workspace = given.legacy_command_reads.Create(
        ("application.play", "domain Example\nimport \"model.play\"\n"),
        ("model.play", given.legacy_command_reads.Source));

    void Because()
    {
        var diagnostic = _workspace.Compilation.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyCommandReadsRepairAvailable);
        var repair = WorkspaceDiagnosticRepairs.Find(_workspace, _workspace.Revision, diagnostic).Single();
        _result = WorkspaceDiagnosticRepairs.ProposeRepair(_workspace, repair, given.legacy_command_reads.Request(_workspace));
    }

    [Fact] void should_accept_the_composed_model() => Assert.True(_result.Accepted, string.Join("; ", _result.Conflicts.Select(conflict => conflict.Message)));
    [Fact] void should_admit_the_reads() => _result.ExecutableReady.ShouldBeTrue();
    [Fact] void should_insert_only_in_the_application_root() => _result.Workspace!.Documents.Single(document => document.Path.Value == "application.play").Text.ShouldContain("command reads protected");
    [Fact] void should_not_reprint_the_read_document() => _result.Workspace!.Documents.Single(document => document.Path.Value == "model.play").Text.ShouldEqual(given.legacy_command_reads.Source);
}
