// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_event_capture_fixtures;

public class with_several_source_events : given.an_event_capture_workspace
{
    ImmutableArray<WorkspaceDiagnosticRepair> _repairs;

    void Establish() => Open("""
        when capture Feed
          id = "one"
        then Local
          id = "one"
        """, severalSources: true);

    void Because() => _repairs = WorkspaceDiagnosticRepairs.Find(_index, _workspace.Revision, _diagnostic);

    [Fact] void should_not_guess_which_event_arrived() => _repairs.ShouldBeEmpty();
}
