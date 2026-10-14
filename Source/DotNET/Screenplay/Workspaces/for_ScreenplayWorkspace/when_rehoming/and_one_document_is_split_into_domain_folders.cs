// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace.when_rehoming;

public class and_one_document_is_split_into_domain_folders : given.a_workspace_with_assigned_events
{
    ScreenplayWorkspace _rehomed = null!;

    void Because() => _rehomed = Original.Rehome(Split);

    [Fact] void should_compile() => _rehomed.Compilation.Success.ShouldBeTrue();
    [Fact] void should_preserve_every_semantic_assignment() => _rehomed.IdentityCatalog.Semantics.ShouldContainOnly(Original.IdentityCatalog.Semantics);
    [Fact] void should_preserve_every_event_assignment() => _rehomed.IdentityCatalog.EventContracts.ShouldContainOnly(Original.IdentityCatalog.EventContracts);
    [Fact] void should_bootstrap_new_document_ids() => _rehomed.Documents.All(document => document.Id == DocumentId.Create(document.StableKey)).ShouldBeTrue();
    [Fact] void should_mark_new_documents_as_bootstrap_assignments() => _rehomed.IdentityCatalog.Documents.All(assignment => assignment.Origin == SemanticIdentityOrigin.LegacyBootstrap).ShouldBeTrue();
    [Fact] void should_keep_the_semantic_model_revision() => _rehomed.Compilation.Value!.Model.Revision.ShouldEqual(Original.Compilation.Value!.Model.Revision);
    [Fact] void should_drop_old_document_assignments() => _rehomed.IdentityCatalog.Documents.Any(assignment => assignment.Key == "memory").ShouldBeFalse();
    [Fact] void should_change_the_catalog_revision() => _rehomed.IdentityCatalog.Revision.ShouldNotEqual(Original.IdentityCatalog.Revision);
    [Fact] void should_change_the_workspace_revision() => _rehomed.Revision.ShouldNotEqual(Original.Revision);
    [Fact] void should_leave_the_original_unchanged() => SemanticIdentityCatalogSerializer.Serialize(Original.IdentityCatalog).ShouldEqual(OriginalCatalog);
    [Fact] void should_preserve_the_application_identity() => _rehomed.IdentityCatalog.Application.ShouldEqual(Original.IdentityCatalog.Application);
}
