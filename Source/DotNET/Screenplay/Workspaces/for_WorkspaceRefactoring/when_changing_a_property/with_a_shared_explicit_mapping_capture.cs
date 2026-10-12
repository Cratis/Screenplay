// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class with_a_shared_explicit_mapping_capture : given.a_multi_source_property_workspace
{
    void Establish() => UseBindableSource(SharedMappingSource);

    void Because() => Result = Workspace.ProposeProperty(Request("A") with { EventNeverPersisted = true });

    [Fact] void should_refuse_retargeting_the_unselected_event() => Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.AmbiguousPropertyReference);
    [Fact] void should_return_no_partial_candidate() => Result.Workspace.ShouldBeNull();
    [Fact] void should_disclose_the_shared_mapping_location() => Result.PropertyReport.NeedsDecision.Single().Candidates.ShouldNotBeEmpty();
}
