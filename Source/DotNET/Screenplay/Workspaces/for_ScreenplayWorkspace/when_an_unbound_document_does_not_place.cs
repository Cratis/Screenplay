// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_an_unbound_document_does_not_place : given.an_unbound_container_workspace
{
    ScreenplayWorkspace _incomplete = null!;
    WorkspaceSyntaxIndex _index = null!;

    void Because()
    {
        _incomplete = ScreenplayWorkspace.Create(
            "Acme",
            [
                Document("module One\n  feature First\n    import \"slice.play\"\n  feature Second\n    import \"slice.play\"", "root"),
                Document("slice StateChange S\n  event Fact", "slice"),
                Document(Source, "valid")
            ],
            SemanticIdentityCatalog.Empty(Application));
        _index = WorkspaceSyntaxIndex.Create(_incomplete);
    }

    [Fact] void should_assign_nothing_anywhere() => _incomplete.IdentityCatalog.Semantics.ShouldBeEmpty();
    [Fact] void should_report_the_unplaced_document() => _index.UnresolvedPlacementDocuments.Single().StableKey.ShouldEqual("slice");
    [Fact] void should_not_claim_completeness() => new WorkspaceContainerIdentities(_index).Complete.ShouldBeFalse();
    [Fact] void should_keep_earlier_assignments() => ScreenplayWorkspace.Create("Acme", _incomplete.Documents, Workspace.IdentityCatalog).IdentityCatalog.Revision.ShouldEqual(Workspace.IdentityCatalog.Revision);
}
