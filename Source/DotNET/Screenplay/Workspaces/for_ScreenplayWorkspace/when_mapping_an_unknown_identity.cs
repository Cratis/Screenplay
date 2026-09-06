// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_mapping_an_unknown_identity : given.a_workspace_with_a_produced_mapping
{
    WorkspaceTransactionResult[] _results = [];

    void Because() => _results = [.. new[]
    {
        _operation with { Command = default },
        _operation with { ProducedEvent = default },
        _operation with { TargetProperty = default },
        _operation with { ExpectedSourceCommandProperty = default },
        _operation with { NewSourceCommandProperty = default }
    }.Select(operation => _workspace.Propose(Request(operation)))];

    [Fact] void should_reject_every_unresolved_address() => _results.All(result => result.Conflicts.Single().Kind == WorkspaceConflictKind.SemanticIdNotFound).ShouldBeTrue();
    [Fact] void should_offer_no_candidate() => _results.All(result => result.Workspace is null).ShouldBeTrue();
    [Fact] void should_offer_no_write_plan() => _results.All(result => result.WritePlan is null).ShouldBeTrue();
}
#endif
