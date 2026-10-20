// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceSyntaxIndex;

public class when_indexing_a_revision_again : Specification
{
    ScreenplayWorkspace _workspace = null!;
    ScreenplayWorkspace _changed = null!;
    WorkspaceSyntaxIndex _first = null!;
    WorkspaceSyntaxIndex _again = null!;
    WorkspaceSyntaxIndex _changedIndex = null!;

    void Establish()
    {
        _workspace = Create("module Orders\n  feature Fulfilment\n");
        _changed = Create("module Sales\n  feature Fulfilment\n");
    }

    void Because()
    {
        _first = WorkspaceSyntaxIndex.Shared(_workspace);
        _again = WorkspaceSyntaxIndex.Shared(_workspace);
        _changedIndex = WorkspaceSyntaxIndex.Shared(_changed);
    }

    [Fact] void should_build_the_index_once_for_the_revision() => _again.ShouldBeSame(_first);
    [Fact] void should_not_serve_that_index_for_a_changed_revision() => ReferenceEquals(_changedIndex, _first).ShouldBeFalse();
    [Fact] void should_index_the_changed_revision() => _changedIndex.Entries.All(entry => entry.Handle.Revision == _changed.Revision).ShouldBeTrue();
    [Fact] void should_index_the_changed_source() => _changedIndex.Entries.Any(entry => entry.Node is ModuleSyntax module && string.Equals(module.Name, "Sales", StringComparison.Ordinal)).ShouldBeTrue();

    static ScreenplayWorkspace Create(string source) => ScreenplayWorkspace.Create(
        "Acme",
        [WorkspaceDocument.Create("orders", PortablePlayPath.Parse("orders.play"), Encoding.UTF8.GetBytes(source))],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Acme")));
}
