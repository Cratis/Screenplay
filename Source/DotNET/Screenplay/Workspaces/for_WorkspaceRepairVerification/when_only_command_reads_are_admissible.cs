// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRepairVerification;

public class when_only_command_reads_are_admissible : Specification
{
    const string Performer = "\n      query External => View[]\n        performer\n          file External.cs\n";
    const string FanOut = "\n      event Othered\n      command Other\n        id String identifier\n        destination String\n        produces Othered\n          for destination\n";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_offer_and_preview_the_repair_despite_unrelated_executable_blockers(bool suppressesHint)
    {
        var workspace = Create(suppressesHint);
        var index = WorkspaceSyntaxIndex.Create(workspace);
        workspace.Compilation.Success.ShouldBeFalse();
        index.RepairableDiagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyCommandReadsRepairAvailable).ShouldEqual(!suppressesHint);
        var repair = index.RepairableDiagnostics.SelectMany(diagnostic => WorkspaceDiagnosticRepairs.Find(index, workspace.Revision, diagnostic)).Single();
        repair.DiagnosticCode.ShouldEqual(DiagnosticCodes.LegacyCommandReadsRepairAvailable);
        var result = WorkspaceDiagnosticRepairs.ProposeRepair(workspace, repair, given.legacy_command_reads.Request(workspace));
        Assert.True(result.Accepted, string.Join("; ", result.Conflicts.Select(conflict => conflict.Message)));
        result.ExecutableReady.ShouldBeFalse();
        result.Workspace!.Documents.Single().Text.ShouldContain("command reads protected");
        result.ExecutableDiagnostics.Where(WorkspaceCommandReadsRepairs.IsReadRefusal).ShouldBeEmpty();
        workspace.Documents.Single().Text.ShouldNotContain("command reads protected");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_distinguish_an_explicit_executable_request_blocked_elsewhere(bool suppressesHint)
    {
        var workspace = Create(suppressesHint);
        var subject = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(entry => entry.Node is ReadsSyntax).Handle;
        var result = WorkspaceDiagnosticRepairs.ProposeRepair(workspace, DiagnosticCodes.LegacyCommandReadsRepairAvailable, subject, given.legacy_command_reads.Request(workspace) with { Validation = WorkspaceAuthoringValidation.Executable });
        result.Accepted.ShouldBeFalse();
        result.Conflicts.Single().Message.ShouldEqual("The model is not executable-ready for reasons unrelated to command reads");
        result.Workspace.ShouldBeNull();
        result.WritePlan.ShouldBeNull();
    }

    [Fact]
    void should_report_only_read_related_reasons_for_a_refused_read()
    {
        var source = (given.legacy_command_reads.Source + Performer).Replace("        remove with Removed", string.Empty, StringComparison.Ordinal);
        var workspace = given.legacy_command_reads.Create(("source.play", source));
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var subject = index.Entries.Single(entry => entry.Node is ReadsSyntax).Handle;
        var result = WorkspaceDiagnosticRepairs.ProposeRepair(workspace, DiagnosticCodes.LegacyCommandReadsRepairAvailable, subject, given.legacy_command_reads.Request(workspace));
        result.Accepted.ShouldBeFalse();
        result.Conflicts.Single().Message.ShouldContain("PLAY0781");
        result.Conflicts.Single().Message.ShouldContain("NoRemoval");
        result.Conflicts.Single().Message.ShouldNotContain("performer");
        index.RepairableDiagnostics.SelectMany(diagnostic => WorkspaceDiagnosticRepairs.Find(index, workspace.Revision, diagnostic)).ShouldBeEmpty();
    }

    [Fact]
    void should_return_no_unavailable_reason_without_failures()
    {
        var workspace = given.legacy_command_reads.Create(("source.play", given.legacy_command_reads.Source));
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var subject = index.Entries.Single(entry => entry.Node is ReadsSyntax).Handle;
        WorkspaceCommandReadsRepairs.UnavailableReason(index, subject).ShouldBeNull();
    }

    static ScreenplayWorkspace Create(bool suppressesHint) => given.legacy_command_reads.Create(("source.play", given.legacy_command_reads.Source + (suppressesHint ? FanOut : Performer)));
}
