// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_ModelNotesAnchors;

public class when_resolving_an_unbound_slice_target : for_ScreenplayWorkspace.given.a_valid_workspace
{
    WorkspaceSyntaxIndex _index = null!;
    WorkspaceSyntaxEntry _slice = null!;
    ModelNotesAnchor _anchor = null!;

    void Establish()
    {
        Workspace = ScreenplayWorkspace.Create("Projects", [Concepts, Document("registration", "Projects/Registration.play", RegistrationSource.Replace("produces ProjectRegistered", "reads Missing by projectId\n        produces ProjectRegistered", StringComparison.Ordinal))], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
        _index = WorkspaceSyntaxIndex.Create(Workspace);
        _slice = _index.Entries.Single(entry => entry.Address?.Kind == SemanticKind.Slice);
    }

    void Because() => _anchor = ModelNotesAnchors.ForHandle(_index, _slice.Handle);

    [Fact] void should_have_a_deterministic_slice_identity() => _slice.SemanticId.ShouldEqual(SemanticId.Create(_slice.Address!));
    [Fact] void should_anchor_to_the_slice_identity() => _anchor.SemanticId.ShouldEqual(_slice.SemanticId!.Value.ToString());
    [Fact] void should_resolve_to_the_slice_address() => ModelNotesAnchors.Resolve(_anchor, Workspace.IdentityCatalog).ShouldEqual(_slice.Address);
}
