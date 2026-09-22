// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_an_event_shadowed_by_a_trigger : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Establish()
    {
        var document = Document("model", "model.play", "trigger Created\nmodule App\n  feature F\n    slice Automation S\n      event Created\n      reaction React\n        when Created");
        Workspace = ScreenplayWorkspace.Create("App", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("App")));
    }

    void Because() => _result = Workspace.ProposeRename(Rename<EventSyntax>("Created", "Recorded"));

    [Fact] void should_accept_the_event_rename() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_preserve_the_declared_trigger_reference() => WorkspaceSyntaxIndex.Create(_result.Workspace!).Entries.Select(entry => entry.Node).OfType<NamedTriggerSourceSyntax>().Single().Name.ShouldEqual("Created");
    [Fact] void should_rename_the_event() => WorkspaceSyntaxIndex.Create(_result.Workspace!).Entries.Select(entry => entry.Node).OfType<EventSyntax>().Single().Name.ShouldEqual("Recorded");
}
