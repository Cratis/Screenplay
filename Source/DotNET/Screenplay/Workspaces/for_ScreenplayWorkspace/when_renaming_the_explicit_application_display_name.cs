// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_renaming_the_explicit_application_display_name : given.a_workspace_with_external_identities
{
    ScreenplayWorkspace _renamed = null!;

    void Because() => _renamed = ScreenplayWorkspace.Create(
        StableApplicationIdentity,
        "Project Portfolio",
        Workspace.Documents,
        Workspace.IdentityCatalog);

    [Fact] void should_keep_the_new_friendly_name() => _renamed.Compilation.Value.Model.Application.Name.ShouldEqual("Project Portfolio");
    [Fact] void should_preserve_the_application_identity() => _renamed.IdentityCatalog.Application.ShouldEqual(StableApplicationIdentity);
    [Fact] void should_preserve_the_application_semantic_identity() => _renamed.Compilation.Value.Model.Application.Id.ShouldEqual(ApplicationSemanticIdentity);
    [Fact] void should_preserve_every_semantic_assignment() => _renamed.IdentityCatalog.Semantics.SequenceEqual(Workspace.IdentityCatalog.Semantics).ShouldBeTrue();
    [Fact] void should_preserve_every_event_contract_assignment() => _renamed.IdentityCatalog.EventContracts.SequenceEqual(Workspace.IdentityCatalog.EventContracts).ShouldBeTrue();
    [Fact] void should_preserve_the_catalog_revision() => _renamed.IdentityCatalog.Revision.ShouldEqual(Workspace.IdentityCatalog.Revision);
    [Fact] void should_advance_the_workspace_revision() => _renamed.Revision.ShouldNotEqual(Workspace.Revision);
    [Fact] void should_advance_the_semantic_revision() => _renamed.Compilation.Value.Model.Revision.ShouldNotEqual(Workspace.Compilation.Value.Model.Revision);
}
