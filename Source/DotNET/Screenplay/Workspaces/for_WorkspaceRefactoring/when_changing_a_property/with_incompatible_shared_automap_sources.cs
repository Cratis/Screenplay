// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class with_incompatible_shared_automap_sources : given.a_property_chain_workspace
{
    void Establish() => Workspace = Create(Source.Replace("from Registered", "all", StringComparison.Ordinal));

    void Because() => Result = Workspace.ProposeProperty(Request("Registered") with { EventNeverPersisted = true });

    [Fact] void should_refuse_a_shared_mapping_that_would_retarget_other_sources() => Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.AmbiguousPropertyReference);
    [Fact] void should_keep_every_unselected_source_unchanged() => Result.WritePlan.ShouldBeNull();
}
