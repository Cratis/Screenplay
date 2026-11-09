// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_a_placed_element : given.a_refactoring_workspace
{
    const string Source = """
        module M
          feature F
            slice StateChange Place
              event Placed
                value String

            slice StateView List
              readmodel Row
                id String key
              projection BuildRow => Row
                from Placed
                  id = $eventSourceId

            slice StateView Review
              shows event Placed
              shows readmodel Row
        """;

    WorkspaceAuthoringResult _event = null!;
    WorkspaceAuthoringResult _readModel = null!;

    void Establish() => Workspace = ScreenplayWorkspace.Create(
        "Orders",
        [Document("model", "model.play", Source)],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Orders")));

    void Because()
    {
        _event = Workspace.ProposeRename(Rename<EventSyntax>("Placed", "Submitted"));
        _readModel = Workspace.ProposeRename(Rename<ReadModelSyntax>("Row", "Line"));
    }

    [Fact] void should_accept_the_event_rename() => string.Join(" | ", _event.Conflicts.Select(conflict => conflict.Message)).ShouldEqual(string.Empty);
    [Fact] void should_rename_the_event_placement() => Text(_event).ShouldContain("      shows event Submitted\n");
    [Fact] void should_accept_the_read_model_rename() => string.Join(" | ", _readModel.Conflicts.Select(conflict => conflict.Message)).ShouldEqual(string.Empty);
    [Fact] void should_rename_the_read_model_placement() => Text(_readModel).ShouldContain("      shows readmodel Line");
    [Fact] void should_leave_the_other_placement_alone() => Text(_readModel).ShouldContain("      shows event Placed\n");

    static string Text(WorkspaceAuthoringResult result) => result.Workspace.Documents.Single().Text;
}
