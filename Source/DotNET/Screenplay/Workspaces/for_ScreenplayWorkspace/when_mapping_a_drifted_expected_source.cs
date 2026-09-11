// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_mapping_a_drifted_expected_source : given.a_workspace_with_a_produced_mapping
{
    WorkspaceTransactionResult _result = null!;

    void Because() => _result = _workspace.Propose(Request(_operation with { ExpectedSourceCommandProperty = _operation.NewSourceCommandProperty }));

    [Fact] void should_reject_expected_value_drift() => _result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.SemanticFieldValueDrift);
    [Fact] void should_offer_no_candidate() => _result.Workspace.ShouldBeNull();
    [Fact] void should_offer_no_write_plan() => _result.WritePlan.ShouldBeNull();
}
#endif
