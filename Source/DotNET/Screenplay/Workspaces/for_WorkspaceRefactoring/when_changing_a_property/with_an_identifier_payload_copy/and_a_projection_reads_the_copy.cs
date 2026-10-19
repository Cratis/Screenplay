// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property.with_an_identifier_payload_copy;

public class and_a_projection_reads_the_copy : given.an_identifier_payload_copy_workspace
{
    void Establish() => Create(InlineSource + "    slice StateView Lookup\n      readmodel Name\n        projectId Uuid\n      projection Names => Name\n        from Renamed\n          projectId = projectId\n      query Names => Name[]\n");

    void Because() => Result = Workspace.ProposeProperty(Request());

    [Fact] void should_refuse_for_the_reader() => Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.PropertyReadersExist);
    [Fact] void should_extract_nothing() => Result.PropertyReport!.ExtractedInlineEvents.ShouldBeEmpty();
    [Fact] void should_not_plan_a_write() => Result.WritePlan.ShouldBeNull();
}
