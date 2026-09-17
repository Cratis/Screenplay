// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_combining_mapping_operations : given.a_workspace_with_a_produced_mapping
{
    WorkspaceTransactionResult[] _results = [];

    void Because()
    {
        var replacement = new ReplaceWorkspaceDocument { Document = _concepts.Id, Bytes = _concepts.Bytes };
        var description = new UpdateSliceDescription { SemanticId = _slice.Id, ExpectedCurrentDescription = "Registers a project", NewDescription = "Changed" };
        _results = [.. new[]
        {
            Request(_operation, replacement), Request(replacement, _operation),
            Request(_operation, description), Request(description, _operation), Request(_operation, _operation)
        }.Select(_workspace.Propose)];
    }

    [Fact] void should_require_a_standalone_mapping_operation() => _results.All(result => result.Conflicts.Single().Kind == WorkspaceConflictKind.InvalidOperation).ShouldBeTrue();
    [Fact] void should_offer_no_candidate() => _results.All(result => result.Workspace is null).ShouldBeTrue();
    [Fact] void should_offer_no_write_plan() => _results.All(result => result.WritePlan is null).ShouldBeTrue();
}
#endif
