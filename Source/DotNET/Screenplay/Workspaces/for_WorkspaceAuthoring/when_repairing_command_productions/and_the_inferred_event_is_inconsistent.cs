// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
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
        HasRepair(DiagnosticCodes.UnknownEvent).ShouldBeFalse();
        WorkspaceProductionRepairs.TransactionCount(Workspace).ShouldEqual(1);
        HasRepair(DiagnosticCodes.UnknownEvent).ShouldBeFalse();
        WorkspaceProductionRepairs.TransactionCount(Workspace).ShouldEqual(1);
    }

    [Fact]
    void should_reuse_the_failed_verification_and_preserve_its_typed_diagnostics()
    {
        HasRepair(DiagnosticCodes.UnknownEvent).ShouldBeFalse();
        var subject = WorkspaceSyntaxIndex.Create(Workspace).Entries.Single(entry => entry.Node is ProducesSyntax);
        Result = WorkspaceDiagnosticRepairs.ProposeRepair(Workspace, DiagnosticCodes.UnknownEvent, subject.Handle, Request());
        Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.CompilationFailed);
        Result.AuthoringDiagnostics.Any(diagnostic => diagnostic.Code == "PLAY0287").ShouldBeTrue();
        Result.Accepted.ShouldBeFalse();
        Result.Workspace.ShouldBeNull();
        Result.WritePlan.ShouldBeNull();
        WorkspaceProductionRepairs.TransactionCount(Workspace).ShouldEqual(1);
    }

    [Fact]
    void should_verify_only_once_when_proposing_without_discovery()
    {
        var subject = WorkspaceSyntaxIndex.Create(Workspace).Entries.Single(entry => entry.Node is ProducesSyntax);
        Result = WorkspaceDiagnosticRepairs.ProposeRepair(Workspace, DiagnosticCodes.UnknownEvent, subject.Handle, Request());
        Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.CompilationFailed);
        WorkspaceProductionRepairs.TransactionCount(Workspace).ShouldEqual(1);
        HasRepair(DiagnosticCodes.UnknownEvent).ShouldBeFalse();
        WorkspaceProductionRepairs.TransactionCount(Workspace).ShouldEqual(1);
    }
}
