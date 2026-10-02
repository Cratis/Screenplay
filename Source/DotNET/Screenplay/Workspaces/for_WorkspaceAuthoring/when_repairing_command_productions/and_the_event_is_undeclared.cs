// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_command_productions;

public class and_the_event_is_undeclared : given.a_command_production
{
    string _original;

    void Establish()
    {
        Create(Source);
        _original = Workspace.Documents[0].Text;
        Repair = Find(DiagnosticCodes.UnknownEvent);
    }

    void Because() => Result = WorkspaceDiagnosticRepairs.ProposeRepair(Workspace, Repair, Request());

    EventSyntax Declaration => (EventSyntax)((AddWorkspaceNode)Repair.Operations.Single()).Node;

    [Fact] void should_propose_a_typed_addition() => Repair.Operations.Single().ShouldBeOfExactType<AddWorkspaceNode>();
    [Fact] void should_target_the_producing_slice() => WorkspaceSyntaxIndex.Create(Workspace).Find(((AddWorkspaceNode)Repair.Operations.Single()).Parent)!.Node.ShouldBeOfExactType<SliceSyntax>();
    [Fact] void should_preserve_mapping_order() => Declaration.Properties.Select(property => property.Name).ShouldContainOnly("name", "registeredAt");
    [Fact] void should_preserve_the_nested_property_concept() => Declaration.Properties.First().Type.Name.ShouldEqual("ProjectName");
    [Fact] void should_infer_occurrence_time() => Declaration.Properties.Last().Type.Name.ShouldEqual("DateTime");
    [Fact] void should_accept_the_proposal() => Result.Accepted.ShouldBeTrue();
    [Fact] void should_compile_the_candidate() => Result.ExecutableReady.ShouldBeTrue();
    [Fact] void should_resolve_the_unknown_event() => Result.Workspace!.Compilation.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownEvent || diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding).ShouldBeFalse();
    [Fact] void should_preserve_comments() => Result.Workspace!.Documents[0].Text.ShouldContain("// Keep the production's explanation.");
    [Fact] void should_preserve_other_comments() => Result.Workspace!.Documents[0].Text.ShouldContain("// Keep the command's explanation.");
    [Fact] void should_not_change_the_original() => Workspace.Documents[0].Text.ShouldEqual(_original);
    [Fact] void should_offer_a_write_plan_for_explicit_acceptance() => Result.WritePlan.ShouldNotBeNull();
}
