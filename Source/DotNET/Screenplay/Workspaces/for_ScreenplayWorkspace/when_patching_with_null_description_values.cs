// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_patching_with_null_description_values : given.a_workspace_with_a_described_slice
{
    WorkspaceTransactionResult _nullExpected = null!;
    WorkspaceTransactionResult _nullReplacement = null!;

    void Because()
    {
        _nullExpected = Workspace.Propose(Request(new UpdateSliceDescription
        {
            SemanticId = SliceId,
            ExpectedCurrentDescription = null!,
            NewDescription = "Changed"
        }));
        _nullReplacement = Workspace.Propose(Request(new UpdateSliceDescription
        {
            SemanticId = SliceId,
            ExpectedCurrentDescription = OriginalDescription,
            NewDescription = null!
        }));
    }

    [Fact] void should_reject_a_null_expected_value() => _nullExpected.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.InvalidOperation);
    [Fact] void should_reject_a_null_replacement_value() => _nullReplacement.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.InvalidOperation);
    [Fact] void should_return_no_candidate_workspace() => new[] { _nullExpected, _nullReplacement }.All(result => result.Workspace is null).ShouldBeTrue();
}
