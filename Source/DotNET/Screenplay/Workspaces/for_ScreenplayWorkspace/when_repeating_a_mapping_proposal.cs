// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_repeating_a_mapping_proposal : given.a_workspace_with_a_produced_mapping
{
    WorkspaceTransactionResult _first = null!;
    WorkspaceTransactionResult _second = null!;

    void Because()
    {
        _first = _workspace.Propose(Request(_operation));
        _second = _workspace.Propose(Request(_operation));
    }

    [Fact] void should_produce_the_same_revision() => _first.Workspace!.Revision.ShouldEqual(_second.Workspace!.Revision);
    [Fact] void should_produce_the_same_semantics() => _first.Workspace!.Compilation.Value!.Model.Revision.ShouldEqual(_second.Workspace!.Compilation.Value!.Model.Revision);
    [Fact] void should_produce_identical_write_bytes() => _first.WritePlan!.Entries.Single().After!.Bytes.AsSpan().SequenceEqual(_second.WritePlan!.Entries.Single().After!.Bytes.AsSpan()).ShouldBeTrue();
}
#endif
