// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_command_productions;

public class and_the_destination_is_omitted : given.a_command_production
{
    Diagnostic _diagnostic;

    void Establish()
    {
        Create(DestinationSource);
        _diagnostic = WorkspaceSyntaxIndex.Create(Workspace).RepairableDiagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.OmittedProductionDestination);
        Repair = Find(_diagnostic.Code);
    }

    void Because() => Result = WorkspaceDiagnosticRepairs.ProposeRepair(Workspace, Repair, Request());

    [Fact] void should_be_information_only() => _diagnostic.Severity.ShouldEqual(DiagnosticSeverity.Information);
    [Fact] void should_disclose_the_routing_change() => Repair.Title.ShouldEqual("Change routing to command identifier 'projectId'");
    [Fact] void should_require_individual_review_not_fix_all() => Repair.CanFixAll.ShouldBeFalse();
    [Fact] void should_replace_the_production() => Repair.Operations.Single().ShouldBeOfExactType<ReplaceWorkspaceNode>();
    [Fact] void should_use_the_identifier() => ((PathExpressionSyntax)((ProducesSyntax)((ReplaceWorkspaceNode)Repair.Operations.Single()).Node).For!).Path.ShouldEqual("projectId");
    [Fact] void should_accept_the_proposal() => Result.Accepted.ShouldBeTrue();
    [Fact] void should_print_the_destination() => Result.Workspace!.Documents[0].Text.ShouldContain("for projectId");
    [Fact] void should_resolve_the_information() => Result.Workspace!.Compilation.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.OmittedProductionDestination).ShouldBeFalse();
    [Fact] void should_preserve_comments() => Result.Workspace!.Documents[0].Text.ShouldContain("// Keep the production's explanation.");
}
