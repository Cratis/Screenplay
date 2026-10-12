// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_adding_a_property;

public class with_an_inline_event_owner : given.a_property_add_workspace
{
    void Establish() => Workspace = Create("""
        module Projects
          feature Registration
            slice StateChange Register
              command Register
                id String identifier
                name String
                produces event Registered
                  name String = name
        """);

    void Because() => Result = Workspace.ProposeProperty(AddRequest("Registered") with { EventNeverPersisted = true, Optional = true });

    [Fact] void should_require_explicit_extraction() => Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.InlineEventRequiresExtraction);
    [Fact] void should_not_offer_partial_writes() => Result.WritePlan.ShouldBeNull();
}
