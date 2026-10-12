// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_ModelNotesAnchors;

public class when_resolving_syntax_only_targets : for_ScreenplayWorkspace.given.a_valid_workspace
{
    WorkspaceSyntaxIndex _index = null!;
    WorkspaceSyntaxEntry _target = null!;
    ModelNotesAnchor _anchor = null!;

    void Establish()
    {
        _index = WorkspaceSyntaxIndex.Create(Workspace);
        _target = _index.Entries.First(entry => entry.Kind == "ProducesSyntax");
    }

    void Because() => _anchor = ModelNotesAnchors.ForHandle(_index, _target.Handle);

    [Fact] void should_anchor_to_the_slice() => ModelNotesAnchors.Resolve(_anchor, Workspace.IdentityCatalog)!.Kind.ShouldEqual(SemanticKind.Slice);
    [Fact] void should_not_invent_an_identity_for_the_syntax() => _target.SemanticId.ShouldBeNull();
    [Fact] void should_refuse_document_anchors() => Catch.Exception(() => ModelNotesAnchors.ForHandle(_index, _index.Entries.First(entry => entry.Parent is null).Handle)).ShouldBeOfExactType<InvalidModelNotes>();
    [Fact] void should_compute_unknown_orphans() => ModelNotesAnchors.Items(new(Workspace.IdentityCatalog.Application.ToString(), [new(ModelNotesStore.NewId(), "question", "Why?", new(SemanticId.Create(SemanticAddress.ForConcept(Workspace.IdentityCatalog.Application, "Missing")).ToString()), null, "author", DateTimeOffset.UtcNow, [], null)]), Workspace.IdentityCatalog).Single().State.ShouldEqual("orphaned");
}
