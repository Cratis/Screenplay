// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_patching_an_unbindable_workspace : given.a_workspace_with_a_described_slice
{
    WorkspaceTransactionResult _result = null!;

    void Because()
    {
        var invalidRegistration = WorkspaceDocument.Create(
            Registration.Id,
            Registration.StableKey,
            Registration.Path,
            Bytes(RegistrationSource.Replace("ProjectName", "MissingConcept", StringComparison.Ordinal)).AsSpan());
        var invalidWorkspace = ScreenplayWorkspace.Create(
            Workspace.ApplicationName,
            [invalidRegistration, Concepts],
            Workspace.IdentityCatalog);
        _result = invalidWorkspace.Propose(new WorkspaceTransactionRequest
        {
            ExpectedRevision = invalidWorkspace.Revision,
            ExpectedCatalogRevision = invalidWorkspace.IdentityCatalog.Revision,
            Operations =
            [
                new UpdateSliceDescription
                {
                    SemanticId = SliceId,
                    ExpectedCurrentDescription = OriginalDescription,
                    NewDescription = "Changed"
                }
            ]
        });
    }

    [Fact] void should_reject_the_patch() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_compilation_failure() => _result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.CompilationFailed);
    [Fact] void should_return_no_candidate_workspace() => _result.Workspace.ShouldBeNull();
}
