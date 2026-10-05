// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_command_productions;

public class and_the_proposal_is_not_authorized : given.a_command_production
{
    void Prepare(string code)
    {
        if (code == DiagnosticCodes.OmittedProductionDestination)
        {
            Create(DestinationSource);
        }
    }

    void Establish()
    {
        Create(Source.Replace("          for projectId\n", string.Empty, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(DiagnosticCodes.UnknownEvent)]
    [InlineData(DiagnosticCodes.OmittedProductionDestination)]
    void should_reject_a_stale_workspace_revision(string code)
    {
        Prepare(code);
        Repair = Find(code);
        Result = WorkspaceDiagnosticRepairs.ProposeRepair(Workspace, Repair, Request() with { ExpectedRevision = default });
        Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.StaleWorkspaceRevision);
        Result.Workspace.ShouldBeNull();
        Result.WritePlan.ShouldBeNull();
    }

    [Theory]
    [InlineData(DiagnosticCodes.UnknownEvent)]
    [InlineData(DiagnosticCodes.OmittedProductionDestination)]
    void should_reject_a_stale_catalog_revision(string code)
    {
        Prepare(code);
        Repair = Find(code);
        Result = WorkspaceDiagnosticRepairs.ProposeRepair(Workspace, Repair, Request() with { ExpectedCatalogRevision = default });
        Result.Accepted.ShouldBeFalse();
        Result.Workspace.ShouldBeNull();
    }

    [Theory]
    [InlineData(DiagnosticCodes.UnknownEvent)]
    [InlineData(DiagnosticCodes.OmittedProductionDestination)]
    void should_require_formatting_consent(string code)
    {
        Prepare(code);
        Repair = Find(code);
        Result = WorkspaceDiagnosticRepairs.ProposeRepair(Workspace, Repair, Request() with { Formatting = WorkspaceAuthoringFormatting.PreserveTrivia });
        Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.FormattingConsentRequired);
    }

    [Fact]
    void should_reject_a_tampered_addition()
    {
        Repair = Find(DiagnosticCodes.UnknownEvent);
        var add = (AddWorkspaceNode)Repair.Operations.Single();
        Repair = Repair with { Operations = [add with { Node = ((EventSyntax)add.Node) with { Name = "Tampered" } }] };
        Result = WorkspaceDiagnosticRepairs.ProposeRepair(Workspace, Repair, Request());
        Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.UnknownRepair);
    }

    [Fact]
    void should_reject_a_tampered_destination()
    {
        Prepare(DiagnosticCodes.OmittedProductionDestination);
        Repair = Find(DiagnosticCodes.OmittedProductionDestination);
        var replace = (ReplaceWorkspaceNode)Repair.Operations.Single();
        Repair = Repair with { Operations = [replace with { Node = ((ProducesSyntax)replace.Node) with { For = new LiteralExpressionSyntax("tampered", replace.Node.Location) } }] };
        Result = WorkspaceDiagnosticRepairs.ProposeRepair(Workspace, Repair, Request());
        Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.UnknownRepair);
    }

    [Theory]
    [InlineData(DiagnosticCodes.UnknownEvent)]
    [InlineData(DiagnosticCodes.OmittedProductionDestination)]
    void should_accept_structurally_equivalent_wire_operations(string code)
    {
        Prepare(code);
        Repair = Find(code);
        Repair = Repair with { Operations = [Repair.Operations.Single() switch
        {
            AddWorkspaceNode add => add with { ExpectedParent = SyntaxJson.Deserialize(SyntaxJson.Serialize(add.ExpectedParent)), Node = SyntaxJson.Deserialize(SyntaxJson.Serialize(add.Node)) },
            ReplaceWorkspaceNode replace => replace with { Expected = SyntaxJson.Deserialize(SyntaxJson.Serialize(replace.Expected)), Node = SyntaxJson.Deserialize(SyntaxJson.Serialize(replace.Node)) },
            _ => Repair.Operations.Single()
        }] };
        Result = WorkspaceDiagnosticRepairs.ProposeRepair(Workspace, Repair, Request());
        string.Join('\n', Result.Conflicts.Select(conflict => $"{conflict.Kind}: {conflict.Message}")).ShouldEqual(string.Empty);
        Result.Accepted.ShouldBeTrue();
    }
}
