// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_event_capture_fixtures;

public class with_one_source_event : given.an_event_capture_workspace
{
    WorkspaceAuthoringResult _result;
    WorkspaceDiagnosticRepair _repair;

    void Establish() => Open("""
        when capture Feed
          id = "one"
        then Local
          id = "one"
        """);

    void Because()
    {
        _repair = WorkspaceDiagnosticRepairs.Find(_index, _workspace.Revision, _diagnostic).Single();
        _result = WorkspaceDiagnosticRepairs.ProposeRepair(_workspace, _diagnostic.Code, _repair.Subject, Request());
    }

    [Fact] void should_offer_a_reviewed_repair_not_fix_all() => _repair.CanFixAll.ShouldBeFalse();
    [Fact] void should_accept_the_typed_replacement() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_present_the_source_event() => WorkspaceSyntaxIndex.Create(_result.Workspace!).Entries.Select(entry => entry.Node).OfType<SpecificationSyntax>().Single().WhenAppended!.EventType.ShouldEqual("Arrived");
    [Fact] void should_preserve_the_record_fields() => WorkspaceSyntaxIndex.Create(_result.Workspace!).Entries.Select(entry => entry.Node).OfType<SpecificationSyntax>().Single().WhenAppended!.Values.Single().Property.ShouldEqual("id");
}
