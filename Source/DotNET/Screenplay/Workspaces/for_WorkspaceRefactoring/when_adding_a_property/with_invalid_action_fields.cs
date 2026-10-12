// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_adding_a_property;

public class with_invalid_action_fields : given.a_property_add_workspace
{
    WorkspaceAuthoringResult[] _results;

    void Because() => _results = [.. new[]
    {
        AddRequest() with { Target = Property("Register").Handle },
        AddRequest() with { ExpectedName = "name" },
        AddRequest() with { NewName = "label" },
        AddRequest() with { AlsoRename = [Select("Register")] },
        AddRequest() with { AlsoRemove = [Select("Register")] },
        AddRequest() with { NewGeneration = true, EventNeverPersisted = true },
        Request() with { Owner = Owner("Register").Handle },
        Request(action: "remove") with { Type = "String" }
    }.Select(Workspace.ProposeProperty)];

    [Fact] void should_refuse_every_cross_action_shape() => _results.All(result => result.Conflicts.Single().Kind == WorkspaceConflictKind.InvalidOperation).ShouldBeTrue();
    [Fact] void should_not_offer_any_partial_writes() => _results.All(result => result.WritePlan is null && result.Workspace is null).ShouldBeTrue();
}
