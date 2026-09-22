// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceSyntaxIndex;

public class when_finding_occurrences_in_large_workspaces : Specification
{
    WorkspaceSyntaxIndex[] _indexes = [];
    bool _foundOriginals;

    void Establish() => _indexes = [CreateIndex(100), CreateIndex(400)];

    void Because() => _foundOriginals = _indexes.All(index => Enumerable.Range(0, 10).All(_ => index.Entries.All(entry => ReferenceEquals(index.Find(entry.Handle), entry))));

    [Fact] void should_index_all_one_hundred_file_occurrences() => _indexes[0].Entries.Length.ShouldEqual(700);
    [Fact] void should_index_all_four_hundred_file_occurrences() => _indexes[1].Entries.Length.ShouldEqual(2800);
    [Fact] void should_find_the_same_occurrence_on_every_lookup() => _foundOriginals.ShouldBeTrue();
    [Fact] void should_reject_an_unknown_path() => _indexes.All(index => index.Find(index.Entries[0].Handle with { Path = "/missing" }) is null).ShouldBeTrue();
    [Fact] void should_reject_a_stale_revision() => _indexes.All(index => index.Find(index.Entries[0].Handle with { Revision = default }) is null).ShouldBeTrue();
    [Fact] void should_reject_an_unknown_document_identity() => _indexes.All(index => index.Find(index.Entries[0].Handle with { Document = default }) is null).ShouldBeTrue();

    static WorkspaceSyntaxIndex CreateIndex(int count)
    {
        var documents = Enumerable.Range(0, count).Select(index => WorkspaceDocument.Create($"slice{index}", PortablePlayPath.Parse($"slice{index}.play"), Encoding.UTF8.GetBytes($$"""
            module Billing
              feature Accounts
                slice StateChange Register{{index}}
                  command Register
                    produces Opened
                  event Opened
            """)));
        var workspace = ScreenplayWorkspace.CreateValidated("Billing", [.. documents], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing")), ScreenplayWorkspace.EmptyCompilation());
        return WorkspaceSyntaxIndex.Create(workspace);
    }
}
