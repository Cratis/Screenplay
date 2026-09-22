// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_rejecting_drifted_expectations : given.an_authoring_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Because()
    {
        var entry = Index.Entries.First(entry => entry.Node is ConceptSyntax);
        _result = Workspace.ProposeAuthoring(Authoring(new ReplaceWorkspaceNode(entry.Handle, (ConceptSyntax)entry.Node with { Name = "Drifted" }, entry.Node)));
    }

    [Fact] void should_reject_the_non_original_expectation() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_leave_no_write_plan() => _result.WritePlan.ShouldBeNull();
}
