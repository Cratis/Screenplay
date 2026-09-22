// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_rejecting_a_partial_logical_header_rename : given.an_authoring_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Establish()
    {
        var fragment = Document("fragment", "Projects/Other.play", "module Projects\n  feature Other");
        Workspace = ScreenplayWorkspace.Create(Workspace.ApplicationName, [Concepts, Registration, fragment], Workspace.IdentityCatalog);
        Index = WorkspaceSyntaxIndex.Create(Workspace);
    }

    void Because()
    {
        var module = Index.Entries.Single(entry => entry.Handle.Document == Registration.Id && entry.Node is ModuleSyntax);
        _result = Workspace.ProposeAuthoring(Authoring(new ReplaceWorkspaceNode(module.Handle, module.Node, (ModuleSyntax)module.Node with { Name = "Renamed" })));
    }

    [Fact] void should_reject_a_single_fragment_logical_rename() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_explain_the_multiple_source_owners() => _result.Conflicts.Single().Message.Contains("multiple source fragments", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_expose_an_incomplete_candidate() => _result.Workspace.ShouldBeNull();
}
