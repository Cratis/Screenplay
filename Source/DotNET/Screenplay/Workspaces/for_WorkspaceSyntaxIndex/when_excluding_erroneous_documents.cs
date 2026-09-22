// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceSyntaxIndex;

public class when_excluding_erroneous_documents : Specification
{
    ScreenplayWorkspace _workspace = null!;
    WorkspaceSyntaxIndex _index = null!;

    void Establish()
    {
        var document = WorkspaceDocument.Create("broken", PortablePlayPath.Parse("broken.play"), Encoding.UTF8.GetBytes("unexpected declaration\nmodule Billing\n"));
        _workspace = ScreenplayWorkspace.CreateValidated("Billing", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing")), ScreenplayWorkspace.EmptyCompilation());
    }

    void Because() => _index = WorkspaceSyntaxIndex.Create(_workspace);

    [Fact] void should_preserve_the_syntax_diagnostics() => _index.Diagnostics.ShouldNotBeEmpty();
    [Fact] void should_not_offer_recovered_nodes_as_editable_occurrences() => _index.Entries.ShouldBeEmpty();
}
