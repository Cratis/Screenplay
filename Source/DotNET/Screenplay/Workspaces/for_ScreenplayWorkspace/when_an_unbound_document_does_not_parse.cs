// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_an_unbound_document_does_not_parse : given.an_unbound_container_workspace
{
    ScreenplayWorkspace _incomplete = null!;
    WorkspaceSyntaxIndex _index = null!;

    void Because()
    {
        _incomplete = ScreenplayWorkspace.Create("Acme", [Document(Source), Document("not valid Screenplay", "broken")], SemanticIdentityCatalog.Empty(Application));
        _index = WorkspaceSyntaxIndex.Create(_incomplete);
    }

    [Fact] void should_assign_nothing_even_in_the_valid_document() => _incomplete.IdentityCatalog.Semantics.ShouldBeEmpty();
    [Fact] void should_report_the_unparsed_document() => _index.UnparsedDocuments.Single().StableKey.ShouldEqual("broken");
    [Fact] void should_not_claim_a_complete_address_set() => new WorkspaceContainerIdentities(_index).Complete.ShouldBeFalse();
    [Fact] void should_explain_why_valid_containers_are_unidentified() => new WorkspaceContainerIdentities(_index).Issues.All(issue => issue.Reason == "incomplete").ShouldBeTrue();
    [Fact] void should_refuse_instead_of_substituting_to_application() => Catch.Exception(() => ModelNotesAnchors.ForHandle(_index, _index.Entries.Single(entry => entry.Address?.Kind == SemanticKind.Slice).Handle)).Message.ShouldContain("incomplete");
    [Fact] void should_keep_earlier_assignments() => ScreenplayWorkspace.Create("Acme", _incomplete.Documents, Workspace.IdentityCatalog).IdentityCatalog.Revision.ShouldEqual(Workspace.IdentityCatalog.Revision);
}
