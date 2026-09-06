// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_mapping_a_wrong_owner : given.a_workspace_with_a_produced_mapping
{
    WorkspaceTransactionResult[] _results = [];

    void Because()
    {
        var otherSource = _slice.Commands.Single(value => value.Name == "OtherCommand").Properties.Single().Id;
        var otherTarget = _slice.Events.Single(value => value.Name == "OtherEvent").Properties.Single().Id;
        _results = [.. new[]
        {
            _operation with { ExpectedSourceCommandProperty = otherSource },
            _operation with { NewSourceCommandProperty = otherSource },
            _operation with { TargetProperty = otherTarget },
            _operation with { Command = _event.Id },
            _operation with { ProducedEvent = _command.Id }
        }.Select(operation => _workspace.Propose(Request(operation)))];
    }

    [Fact] void should_reject_wrong_owners_and_kinds() => _results.All(result => result.Conflicts.Single().Kind == WorkspaceConflictKind.UnsupportedSemanticField).ShouldBeTrue();
    [Fact] void should_offer_no_candidate() => _results.All(result => result.Workspace is null).ShouldBeTrue();
    [Fact] void should_offer_no_write_plan() => _results.All(result => result.WritePlan is null).ShouldBeTrue();
}
#endif
