// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_removing_a_document_with_declarations : given.a_valid_workspace
{
    WorkspaceTransactionResult _withoutRetirements = null!;
    WorkspaceTransactionResult _withRetirements = null!;

    void Because()
    {
        var remove = new RemoveWorkspaceDocument { Document = Registration.Id };
        _withoutRetirements = Workspace.Propose(Request(remove));
        _withRetirements = Workspace.Propose(Request(remove) with
        {
            RetiredSemanticAddresses =
            [
                .. Workspace.IdentityCatalog.Semantics
                    .Where(assignment => assignment.Address.Kind is not (SemanticKind.Application or SemanticKind.Concept))
                    .Select(assignment => assignment.Address)
            ],
            RetiredEventAddresses = [.. Workspace.IdentityCatalog.EventContracts.Select(assignment => assignment.Address)]
        });
    }

    [Fact] void should_reject_implicit_declaration_retirement() => _withoutRetirements.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.InvalidIdentityMigration);
    [Fact] void should_return_no_implicit_candidate_workspace() => _withoutRetirements.Workspace.ShouldBeNull();
    [Fact] void should_accept_explicit_declaration_retirement() => _withRetirements.Success.ShouldBeTrue();
    [Fact] void should_keep_only_the_concepts_document() => _withRetirements.Workspace!.Documents.Select(document => document.Id).ShouldContainOnly(Concepts.Id);
    [Fact] void should_remove_every_retired_event_contract() => _withRetirements.Workspace!.IdentityCatalog.EventContracts.ShouldBeEmpty();
    [Fact] void should_keep_the_remaining_compilation_valid() => _withRetirements.Workspace!.Compilation.Success.ShouldBeTrue();
}
