// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_mapping_to_the_current_source : given.a_workspace_with_a_produced_mapping
{
    WorkspaceTransactionResult _result = null!;

    void Because() => _result = _workspace.Propose(Request(_operation with { NewSourceCommandProperty = _operation.ExpectedSourceCommandProperty }));

    [Fact] void should_admit_a_no_op() => _result.Success.ShouldBeTrue();
    [Fact] void should_preserve_the_workspace_revision() => _result.Workspace!.Revision.ShouldEqual(_workspace.Revision);
    [Fact] void should_preserve_the_semantic_revision() => _result.Workspace!.Compilation.Value!.Model.Revision.ShouldEqual(_workspace.Compilation.Value!.Model.Revision);
    [Fact] void should_offer_no_writes() => _result.WritePlan!.Entries.ShouldBeEmpty();
}
#endif
