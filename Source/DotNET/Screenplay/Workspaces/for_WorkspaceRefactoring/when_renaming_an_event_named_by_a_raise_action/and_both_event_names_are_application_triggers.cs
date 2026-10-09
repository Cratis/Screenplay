// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_renaming_an_event_named_by_a_raise_action;

public class and_both_event_names_are_application_triggers : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Establish()
    {
        const string Source = """
            trigger Created
            trigger Renamed
            module App
              feature Items
                slice StateView Details
                  event Created
                  screen Details
                    on click
                      raise Created
            """;
        Workspace = ScreenplayWorkspace.Create("App", [Document("model", "model.play", Source)], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("App")));
    }

    void Because() => _result = Workspace.ProposeRename(Rename<EventSyntax>("Created", "Renamed"));

    [Fact] void should_accept_the_event_rename() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_rename_the_event() => WorkspaceSyntaxIndex.Create(_result.Workspace!).Entries.Select(entry => entry.Node).OfType<EventSyntax>().Single().Name.ShouldEqual("Renamed");
    [Fact] void should_keep_raising_the_original_application_trigger() => WorkspaceSyntaxIndex.Create(_result.Workspace!).Entries.Select(entry => entry.Node).OfType<RaiseTriggerActionSyntax>().Single().Trigger.ShouldEqual("Created");

    [Fact]
    void should_bind_the_raise_action_only_to_the_application_trigger()
    {
        var binding = new WorkspaceReferenceBindings(WorkspaceSyntaxIndex.Create(Workspace)).Bindings.Single(binding => binding.Reference.Entry.Node is RaiseTriggerActionSyntax);
        binding.Outcome.ShouldEqual("resolved");
        binding.Target!.Entry!.Node.ShouldBeOfExactType<TriggerSyntax>();
    }
}
