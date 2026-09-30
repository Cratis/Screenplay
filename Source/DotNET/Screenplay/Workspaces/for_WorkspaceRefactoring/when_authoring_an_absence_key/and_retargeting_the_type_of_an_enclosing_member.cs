// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_authoring_an_absence_key;

public class and_retargeting_the_type_of_an_enclosing_member : given.a_workspace_with_an_absent_read_model_key
{
    WorkspaceAuthoringResult _safe = null!;
    WorkspaceAuthoringResult _draft = null!;

    void Establish() => CreateWith($"type OtherPart\n  part String\n{Source}");

    void Because()
    {
        var reference = Entry<TypeRefSyntax>(type => type.Name == "InvoicePart");
        var request = Authoring(
            WorkspaceAuthoringReferencePolicy.Safe,
            new ReplaceWorkspaceNode(reference.Handle, reference.Node, ((TypeRefSyntax)reference.Node) with { Name = "OtherPart" }));
        _safe = Workspace.ProposeAuthoring(request);
        _draft = Workspace.ProposeAuthoring(request with { ReferencePolicy = WorkspaceAuthoringReferencePolicy.Draft });
    }

    [Fact] void should_refuse_safe_authoring() => _safe.Accepted.ShouldBeFalse();
    [Fact] void should_not_return_a_safe_write_plan() => _safe.WritePlan.ShouldBeNull();
    [Fact] void should_name_the_silent_rebind() => _safe.Conflicts.Single().Message.ShouldContain("Absence key 'part'");
    [Fact] void should_explain_the_rebind() => _safe.Conflicts.Single().Message.ShouldContain("would silently rebind");
    [Fact] void should_refuse_draft_authoring() => _draft.Accepted.ShouldBeFalse();
    [Fact] void should_not_return_a_draft_write_plan() => _draft.WritePlan.ShouldBeNull();
}
