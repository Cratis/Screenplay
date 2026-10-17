// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_adding_an_unbound_slice_and_advancing_an_event_revision : Specification
{
    WorkspaceAuthoringResult _result = null!;
    ScreenplayWorkspace _workspace = null!;
    WorkspaceAuthoringRequest _request = null!;
    SemanticAddress _addedSlice = null!;
    EventContractIdentityAssignment _event = null!;

    void Establish()
    {
        const string initial = "module Projects\n  feature Registration\n    slice StateChange RegisterProject\n      event Registered\n";
        const string unbound = initial + "      command Register\n        value Missing\n";
        const string evolved = unbound + "      event Registered generation 2\n    slice StateView Pending\n";
        var document = WorkspaceDocument.Create("events", PortablePlayPath.Parse("events.play"), Encoding.UTF8.GetBytes(initial));
        var bound = ScreenplayWorkspace.Create("Projects", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
        _event = bound.IdentityCatalog.EventContracts.Single();
        var invalid = WorkspaceDocument.Create(document.Id, document.StableKey, document.Path, Encoding.UTF8.GetBytes(unbound));
        _workspace = ScreenplayWorkspace.Create("Projects", [invalid], bound.IdentityCatalog);
        _addedSlice = SemanticAddress.ForSlice(_workspace.IdentityCatalog.Application, "Projects", "Registration", "Pending");
        _request = new WorkspaceAuthoringRequest
        {
            ExpectedRevision = _workspace.Revision,
            ExpectedCatalogRevision = _workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Documents = [new ReplaceWorkspaceSyntaxDocument(document.Id, new ScreenplayCompiler().Parse(evolved).Value!)],
            EventRevisionAdvancements = [new(_event.Address, new(2))]
        };
    }

    void Because() => _result = _workspace.ProposeAuthoring(_request);

    [Fact] void should_start_with_an_unbound_workspace() => _workspace.Compilation.Success.ShouldBeFalse();
    [Fact] void should_accept_the_batch() => Assert.True(_result.Accepted, string.Join("; ", _result.Conflicts.Select(conflict => conflict.Message)));
    [Fact] void should_remain_unbound() => _result.ExecutableReady.ShouldBeFalse();
    [Fact] void should_assign_the_new_container_deterministically() => _result.Workspace!.IdentityCatalog.Semantics.Single(assignment => assignment.Address.Equals(_addedSlice)).ShouldEqual(new SemanticIdentityAssignment(_addedSlice, SemanticId.Create(_addedSlice), SemanticIdentityOrigin.LegacyBootstrap));
    [Fact] void should_preserve_the_existing_event_identity() => _result.Workspace!.IdentityCatalog.EventContracts.Single().Id.ShouldEqual(_event.Id);
    [Fact] void should_advance_the_existing_event_revision() => _result.Workspace!.IdentityCatalog.EventContracts.Single().Revision.Value.ShouldEqual(2u);
}
