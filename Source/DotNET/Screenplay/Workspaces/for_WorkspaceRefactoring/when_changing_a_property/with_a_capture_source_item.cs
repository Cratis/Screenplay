// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class with_a_capture_source_item : given.a_property_chain_workspace
{
    void Establish() => Workspace = Create("""
        module Projects
          feature Registration
            slice Translate Import
              direction inbound
              public event Arrived from "External"
                id Uuid
              event Received
                id Uuid
              capture Arrival
                source events
                  from Arrived
                key id
                append Received
                  id = $.id
        """);

    void Because() => Result = Workspace.ProposeProperty(Request("Arrived", name: "id") with { EventNeverPersisted = true });

    [Fact] void should_accept_the_resolved_capture_reader() => Assert.True(Result.Accepted, string.Join(" | ", Result.AuthoringDiagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
    [Fact] void should_preserve_the_source_item_prefix() => Candidate.Contains("id = $.label", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_rewrite_the_capture_key() => Candidate.Contains("key label", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_leave_the_appended_event_shape_unchanged() => Candidate.Contains("event Received\n        id Uuid", StringComparison.Ordinal).ShouldBeTrue();
}
