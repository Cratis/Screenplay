// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRepairVerification;

public class when_a_command_read_cannot_be_protected : Specification
{
    [Theory]
    [InlineData("                remove with Removed", "", "NoRemoval")]
    [InlineData("id String identifier", "id Id generated", "GeneratedKey")]
    void should_refuse_with_the_admission_reason(string before, string after, string reason)
    {
        var source = given.legacy_command_reads.Source.Replace(before.TrimStart(), after.TrimStart(), StringComparison.Ordinal);
        if (reason == "GeneratedKey") source = "concept Id : Uuid\n" + source.Replace("id String", "id Id", StringComparison.Ordinal);
        var workspace = given.legacy_command_reads.Create(("source.play", source));
        var index = WorkspaceSyntaxIndex.Create(workspace);
        index.RepairableDiagnostics.ShouldNotContain(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyCommandReadsRepairAvailable);
        var subject = index.Entries.Single(entry => entry.Node is ReadsSyntax).Handle;
        var result = WorkspaceDiagnosticRepairs.ProposeRepair(workspace, DiagnosticCodes.LegacyCommandReadsRepairAvailable, subject, given.legacy_command_reads.Request(workspace));
        result.Accepted.ShouldBeFalse();
        result.Conflicts.ShouldContain(conflict => conflict.Message.Contains("PLAY0781", StringComparison.Ordinal) && conflict.Message.Contains(reason, StringComparison.Ordinal));
        result.WritePlan.ShouldBeNull();
    }

    [Fact]
    void should_not_choose_between_multiple_application_documents()
    {
        var workspace = given.legacy_command_reads.Create(("source.play", given.legacy_command_reads.Source), ("other.play", "concept Label : String\n"));
        var diagnostic = workspace.Compilation.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyCommandReadsRepairAvailable);
        WorkspaceDiagnosticRepairs.Find(workspace, workspace.Revision, diagnostic).ShouldBeEmpty();
    }
}
