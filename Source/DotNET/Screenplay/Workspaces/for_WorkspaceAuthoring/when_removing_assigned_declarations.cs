// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_removing_assigned_declarations : given.an_authoring_workspace
{
    WorkspaceAuthoringResult _retired = null!;
    WorkspaceAuthoringResult _unexplained = null!;

    void Because()
    {
        var owned = Index.Entries.Where(entry => entry.Handle.Document == Registration.Id && entry.Address is not null && entry.Address.Kind != SemanticKind.Application)
            .Select(entry => entry.Address).Distinct().ToArray();
        var request = Authoring() with
        {
            Documents = [new RemoveWorkspaceDocument { Document = Registration.Id }],
            RetiredSemanticAddresses = [.. owned],
            RetiredEventAddresses = [.. owned.Where(address => address.Kind == SemanticKind.EventContract)]
        };
        _retired = Workspace.ProposeAuthoring(request);
        _unexplained = Workspace.ProposeAuthoring(request with { RetiredSemanticAddresses = [], RetiredEventAddresses = [] });
    }

    [Fact] void should_accept_explicit_retirement() => _retired.Accepted.ShouldBeTrue();
    [Fact] void should_remove_only_the_requested_document() => _retired.Workspace.Documents.Single().Id.ShouldEqual(Concepts.Id);
    [Fact] void should_reject_unexplained_identity_loss() => _unexplained.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.InvalidIdentityMigration);
}
