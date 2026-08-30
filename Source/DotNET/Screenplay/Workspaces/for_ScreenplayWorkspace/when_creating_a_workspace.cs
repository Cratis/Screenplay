// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_creating_a_workspace : given.a_valid_workspace
{
    ScreenplayWorkspace _reversed = null!;

    void Because() => _reversed = ScreenplayWorkspace.Create(
        "Projects",
        [Concepts, Registration],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));

    [Fact] void should_compile_the_workspace() => Workspace.Compilation.Success.ShouldBeTrue();
    [Fact] void should_materialize_both_document_identities() => Workspace.IdentityCatalog.Documents.Length.ShouldEqual(2);
    [Fact] void should_materialize_semantic_identities() => Workspace.IdentityCatalog.Semantics.ShouldNotBeEmpty();
    [Fact] void should_materialize_the_event_contract_identity() => Workspace.IdentityCatalog.EventContracts.Length.ShouldEqual(1);
    [Fact] void should_order_documents_by_identity() => Workspace.Documents.Select(document => document.Id.ToString()).SequenceEqual(Workspace.Documents.Select(document => document.Id.ToString()).Order(StringComparer.Ordinal)).ShouldBeTrue();
    [Fact] void should_make_revision_independent_of_input_order() => _reversed.Revision.ShouldEqual(Workspace.Revision);
    [Fact] void should_keep_exact_document_instances() => Workspace.Documents.All(document => ReferenceEquals(document, Concepts) || ReferenceEquals(document, Registration)).ShouldBeTrue();
}
