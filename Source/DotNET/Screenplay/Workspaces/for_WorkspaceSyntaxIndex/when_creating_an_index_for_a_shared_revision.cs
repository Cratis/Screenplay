// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceSyntaxIndex;

public class when_creating_an_index_for_a_shared_revision : Specification
{
    ScreenplayWorkspace _workspace = null!;
    WorkspaceSyntaxIndex _shared = null!;
    WorkspaceSyntaxIndex _first = null!;
    WorkspaceSyntaxIndex _again = null!;

    void Establish() => _workspace = ScreenplayWorkspace.Create(
        "Acme",
        [WorkspaceDocument.Create("orders", PortablePlayPath.Parse("orders.play"), Encoding.UTF8.GetBytes("module Orders\n  feature Fulfilment\n"))],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Acme")));

    void Because()
    {
        _shared = WorkspaceSyntaxIndex.Shared(_workspace);
        _first = WorkspaceSyntaxIndex.Create(_workspace);
        _again = WorkspaceSyntaxIndex.Create(_workspace);
    }

    [Fact] void should_create_a_new_index_each_time() => ReferenceEquals(_again, _first).ShouldBeFalse();
    [Fact] void should_not_hand_out_the_shared_index() => ReferenceEquals(_first, _shared).ShouldBeFalse();
    [Fact] void should_not_hand_out_the_shared_nodes() => _first.Entries.Any(entry => _shared.Entries.Any(shared => ReferenceEquals(shared.Node, entry.Node))).ShouldBeFalse();
}
