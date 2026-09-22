// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_an_event_referenced_in_another_file : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Establish()
    {
        var view = Document("view", "view.play", "module Projects\n  feature Listing\n    slice StateView Browse\n      projection ProjectList\n        from ProjectRegistered\n          key projectId");
        Workspace = ScreenplayWorkspace.Create("Projects", [Concepts, Registration, view], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
    }

    void Because() => _result = Workspace.ProposeRename(Rename<EventSyntax>("ProjectRegistered", "ProjectCreated"));

    [Fact] void should_accept() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_repair_the_other_document() => WorkspaceSyntaxIndex.Create(_result.Workspace).Entries.Select(entry => entry.Node).OfType<EventSpecSyntax>().Single().Event.ShouldEqual("ProjectCreated");
    [Fact] void should_touch_only_the_declaration_and_reference_documents() => _result.WritePlan.Entries.Length.ShouldEqual(2);
}
