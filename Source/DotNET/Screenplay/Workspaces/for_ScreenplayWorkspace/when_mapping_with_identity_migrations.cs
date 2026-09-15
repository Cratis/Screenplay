// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_mapping_with_identity_migrations : given.a_workspace_with_a_produced_mapping
{
    WorkspaceTransactionResult[] _results = [];

    void Because()
    {
        var address = _workspace.IdentityCatalog.Semantics.Single(value => value.Id == _operation.Command).Address;
        var eventAddress = _workspace.IdentityCatalog.Semantics.Single(value => value.Id == _operation.ProducedEvent).Address;
        _results = [.. new[]
        {
            Request(_operation) with { SemanticRenames = [new(address, address)] },
            Request(_operation) with { EventRenames = [new(eventAddress, eventAddress)] },
            Request(_operation) with { RetiredSemanticAddresses = [address] },
            Request(_operation) with { RetiredEventAddresses = [eventAddress] }
        }.Select(_workspace.Propose)];
    }

    [Fact] void should_reject_all_identity_migrations() => _results.All(result => result.Conflicts.Single().Kind == WorkspaceConflictKind.InvalidOperation).ShouldBeTrue();
    [Fact] void should_offer_no_candidate() => _results.All(result => result.Workspace is null).ShouldBeTrue();
    [Fact] void should_offer_no_write_plan() => _results.All(result => result.WritePlan is null).ShouldBeTrue();
}
#endif
