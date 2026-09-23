// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_mapping_an_incompatible_source : given.a_workspace_with_a_produced_mapping
{
    WorkspaceTransactionResult _result = null!;

    void Because() => _result = _workspace.Propose(Request(_operation with { NewSourceCommandProperty = _command.Properties.Single(value => value.Name == "count").Id }));

    [Fact] void should_reject_the_type_mismatch() => _result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.UnsupportedSemanticField);
    [Fact] void should_offer_no_candidate() => _result.Workspace.ShouldBeNull();
    [Fact] void should_offer_no_write_plan() => _result.WritePlan.ShouldBeNull();
}
#endif
