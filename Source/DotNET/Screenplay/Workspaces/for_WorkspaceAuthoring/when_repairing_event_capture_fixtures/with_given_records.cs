// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_event_capture_fixtures;

public class with_given_records : given.an_event_capture_workspace
{
    WorkspaceAuthoringResult _result;

    void Establish() => Open("""
        given capture Feed
          id = "one"
        when append Arrived
          id = "two"
        then Local
          id = "two"
        """);

    void Because()
    {
        var repair = WorkspaceDiagnosticRepairs.Find(_index, _workspace.Revision, _diagnostic).Single();
        _result = WorkspaceDiagnosticRepairs.ProposeRepair(_workspace, _diagnostic.Code, repair.Subject, Request());
    }

    [Fact] void should_accept_the_history_repair() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_present_given_events() => WorkspaceSyntaxIndex.Create(_result.Workspace!).Entries.Select(entry => entry.Node).OfType<SpecificationSyntax>().Single().Given.Single().EventType.ShouldEqual("Arrived");
    [Fact] void should_remove_the_given_capture_record() => WorkspaceSyntaxIndex.Create(_result.Workspace!).Entries.Select(entry => entry.Node).OfType<SpecificationSyntax>().Single().GivenCaptures.ShouldBeEmpty();
}
