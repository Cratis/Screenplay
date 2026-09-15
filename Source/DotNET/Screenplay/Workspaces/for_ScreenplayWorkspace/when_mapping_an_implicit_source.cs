// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_mapping_an_implicit_source : given.a_workspace_with_a_produced_mapping
{
    WorkspaceTransactionResult _result = null!;

    void Establish()
    {
        var source = Source.Replace("          name = name  // name = name: café 🍰\n", string.Empty, StringComparison.Ordinal);
        var document = WorkspaceDocument.Create(_document.Id, _document.StableKey, _document.Path, Encoding.UTF8.GetBytes(source));
        _workspace = ScreenplayWorkspace.Create("Projects", [document, _concepts], _workspace.IdentityCatalog);
    }
    void Because() => _result = _workspace.Propose(Request(_operation));

    [Fact] void should_require_an_existing_explicit_mapping() => _result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.CompilationFailed);
    [Fact] void should_offer_no_candidate() => _result.Workspace.ShouldBeNull();
    [Fact] void should_offer_no_write_plan() => _result.WritePlan.ShouldBeNull();
}
#endif
