// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_authoring_an_absence_key;

public class and_removing_the_declaration_a_key_member_binds : given.a_workspace_with_an_absent_read_model_key
{
    WorkspaceAuthoringResult _safe = null!;
    WorkspaceAuthoringResult _draft = null!;

    void Establish() => CreateWith(Source.Replace("  part String", "  part String\n  code String", StringComparison.Ordinal));

    void Because()
    {
        var part = Entry<PropertySyntax>(property => property.Name == "part");
        var request = Authoring(WorkspaceAuthoringReferencePolicy.Safe, new RemoveWorkspaceNode(part.Handle, part.Node));
        _safe = Workspace.ProposeAuthoring(request);
        _draft = Workspace.ProposeAuthoring(request with { ReferencePolicy = WorkspaceAuthoringReferencePolicy.Draft });
    }

    [Fact] void should_refuse_safe_authoring() => _safe.Accepted.ShouldBeFalse();
    [Fact] void should_not_return_a_safe_write_plan() => _safe.WritePlan.ShouldBeNull();
    [Fact] void should_name_the_member_that_loses_its_target() => _safe.Conflicts.Single().Message.ShouldContain("Absence key 'part'");
    [Fact] void should_explain_the_lost_target() => _safe.Conflicts.Single().Message.ShouldContain("would lose its resolved target without an explicit edit");
    [Fact] void should_refuse_draft_authoring() => _draft.Accepted.ShouldBeFalse();
    [Fact] void should_explain_the_lost_target_under_draft() => _draft.Conflicts.Single().Message.ShouldContain("would lose its resolved target without an explicit edit");
}
