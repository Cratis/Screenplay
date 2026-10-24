// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_command_productions;

public class and_the_inferred_event_is_inconsistent : given.a_command_production
{
    void Establish()
    {
        Create(Source + "\n      specification Registers\n        when Register\n          name = \"project\"\n        then ProjectRegistered\n          extra = \"value\"\n");
        WorkspaceSyntaxIndex.Create(Workspace).Diagnostics.ShouldBeEmpty();
        Workspace.Compilation.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownEvent).ShouldBeTrue();
    }

    [Fact]
    void should_not_discover_a_repair_that_fails_event_field_consistency()
    {
        var workspace = CreateInconsistentWorkspace();
        HasRepair(workspace, DiagnosticCodes.UnknownEvent).ShouldBeFalse();
        WorkspaceProductionRepairs.TransactionCount(workspace).ShouldEqual(1);
        HasRepair(workspace, DiagnosticCodes.UnknownEvent).ShouldBeFalse();
        WorkspaceProductionRepairs.TransactionCount(workspace).ShouldEqual(1);
    }

    [Fact]
    void should_return_fresh_transaction_diagnostics_after_failed_discovery()
    {
        var workspace = CreateInconsistentWorkspace();
        HasRepair(workspace, DiagnosticCodes.UnknownEvent).ShouldBeFalse();
        var subject = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(entry => entry.Node is ProducesSyntax);
        var result = WorkspaceDiagnosticRepairs.ProposeRepair(workspace, DiagnosticCodes.UnknownEvent, subject.Handle, Request(workspace));
        result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.CompilationFailed);
        result.AuthoringDiagnostics.Any(diagnostic => diagnostic.Code == "PLAY0287").ShouldBeTrue();
        result.ExecutableDiagnostics.ShouldBeEmpty();
        result.ExecutableReady.ShouldBeFalse();
        result.Accepted.ShouldBeFalse();
        result.Workspace.ShouldBeNull();
        result.WritePlan.ShouldBeNull();
        WorkspaceProductionRepairs.TransactionCount(workspace).ShouldEqual(2);
        result = WorkspaceDiagnosticRepairs.ProposeRepair(workspace, DiagnosticCodes.UnknownEvent, subject.Handle, Request(workspace));
        result.AuthoringDiagnostics.Any(diagnostic => diagnostic.Code == "PLAY0287").ShouldBeTrue();
        result.ExecutableDiagnostics.ShouldBeEmpty();
        result.Accepted.ShouldBeFalse();
        WorkspaceProductionRepairs.TransactionCount(workspace).ShouldEqual(3);
        HasRepair(workspace, DiagnosticCodes.UnknownEvent).ShouldBeFalse();
        WorkspaceProductionRepairs.TransactionCount(workspace).ShouldEqual(3);
    }

    [Fact]
    void should_verify_only_once_when_proposing_without_discovery()
    {
        var workspace = CreateInconsistentWorkspace();
        var subject = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(entry => entry.Node is ProducesSyntax);
        var result = WorkspaceDiagnosticRepairs.ProposeRepair(workspace, DiagnosticCodes.UnknownEvent, subject.Handle, Request(workspace));
        result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.CompilationFailed);
        result.AuthoringDiagnostics.Any(diagnostic => diagnostic.Code == "PLAY0287").ShouldBeTrue();
        result.ExecutableDiagnostics.ShouldBeEmpty();
        WorkspaceProductionRepairs.TransactionCount(workspace).ShouldEqual(1);
        HasRepair(workspace, DiagnosticCodes.UnknownEvent).ShouldBeFalse();
        WorkspaceProductionRepairs.TransactionCount(workspace).ShouldEqual(1);
    }

    static ScreenplayWorkspace CreateWorkspace(string source) => ScreenplayWorkspace.Create(
        "Projects",
        [WorkspaceDocument.Create("document-0", PortablePlayPath.Parse("document-0.play"), Encoding.UTF8.GetBytes(source))],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));

    static ScreenplayWorkspace CreateInconsistentWorkspace() => CreateWorkspace(
        Source + "\n      specification Registers\n        when Register\n          name = \"project\"\n        then ProjectRegistered\n          extra = \"value\"\n");

    static WorkspaceAuthoringRequest Request(ScreenplayWorkspace workspace) => new()
    {
        ExpectedRevision = workspace.Revision,
        ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
        Validation = WorkspaceAuthoringValidation.Authoring,
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments
    };

    static bool HasRepair(ScreenplayWorkspace workspace, string code) => WorkspaceSyntaxIndex.Create(workspace).RepairableDiagnostics
        .Where(diagnostic => diagnostic.Code == code)
        .SelectMany(diagnostic => WorkspaceDiagnosticRepairs.Find(workspace, workspace.Revision, diagnostic)).Any();
}
