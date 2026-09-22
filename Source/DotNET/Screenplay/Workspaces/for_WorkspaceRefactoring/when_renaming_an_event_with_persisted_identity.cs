// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_an_event_with_persisted_identity : for_ScreenplayWorkspace.given.a_workspace_with_external_identities
{
    WorkspaceAuthoringResult _result = null!;
    SemanticAddress _renamed = null!;

    void Establish()
    {
        _renamed = SemanticAddress.ForEventContract(SemanticAddress.ForSlice(StableApplicationIdentity, "Projects", "Registration", "RegisterProject"), "ProjectCreated");
    }

    void Because() => _result = Workspace.ProposeRename(new()
    {
        ExpectedRevision = Workspace.Revision,
        ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
        Target = WorkspaceSyntaxIndex.Create(Workspace).Entries.Single(entry => entry.Node is EventSyntax).Handle,
        ExpectedName = "ProjectRegistered",
        NewName = "ProjectCreated"
    });

    [Fact] void should_accept() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_preserve_external_semantic_identity() => _result.Workspace.IdentityCatalog.ResolveSemantic(_renamed).ShouldEqual(EventSemanticIdentity);
    [Fact] void should_preserve_external_contract_identity() => _result.Workspace.IdentityCatalog.EventContracts.Single(assignment => assignment.Address.Equals(_renamed)).Id.ShouldEqual(EventContractIdentity);
    [Fact] void should_preserve_contract_revision() => _result.Workspace.IdentityCatalog.EventContracts.Single(assignment => assignment.Address.Equals(_renamed)).Revision.ShouldEqual(Workspace.IdentityCatalog.EventContracts.Single().Revision);
    [Fact] void should_rewrite_produces() => WorkspaceSyntaxIndex.Create(_result.Workspace).Entries.Select(entry => entry.Node).OfType<ProducesSyntax>().Single().Event.ShouldEqual("ProjectCreated");
    [Fact] void should_preserve_all_assigned_ids() => _result.Workspace.IdentityCatalog.Semantics.Select(assignment => assignment.Id).OrderBy(identity => identity.ToString()).SequenceEqual(Workspace.IdentityCatalog.Semantics.Select(assignment => assignment.Id).OrderBy(identity => identity.ToString())).ShouldBeTrue();
}
