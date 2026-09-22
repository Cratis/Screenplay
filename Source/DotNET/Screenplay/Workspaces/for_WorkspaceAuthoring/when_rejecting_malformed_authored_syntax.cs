// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_rejecting_malformed_authored_syntax : given.an_authoring_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Because() => _result = Workspace.ProposeAuthoring(Authoring(new AddWorkspaceNode(
        ConceptsRoot.Handle, ConceptsRoot.Node, "imports", new ImportSyntax("External.Name\nmodule Injected", SourceLocation.Start))));

    [Fact] void should_not_bless_injected_or_unrepresentable_syntax() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_return_no_partial_workspace() => _result.Workspace.ShouldBeNull();
}
