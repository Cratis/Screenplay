// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_mapping_unsupported_production_grammar : given.a_workspace_with_a_produced_mapping
{
    WorkspaceTransactionResult _result = null!;

    void Establish()
    {
        var source = Source.Replace("        produces ProjectRegistered\n          for name\n          name = name", "        produces when count == 42\n          ProjectRegistered\n            for name\n            name = name", StringComparison.Ordinal);
        var document = WorkspaceDocument.Create(_document.Id, _document.StableKey, _document.Path, Encoding.UTF8.GetBytes(source));
        _workspace = ScreenplayWorkspace.Create("Projects", [document, _concepts], _workspace.IdentityCatalog);
    }

    void Because() => _result = _workspace.Propose(Request(_operation));

    [Fact] void should_keep_existing_binder_admission() => _result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.CompilationFailed);
    [Fact] void should_offer_no_candidate() => _result.Workspace.ShouldBeNull();
    [Fact] void should_offer_no_write_plan() => _result.WritePlan.ShouldBeNull();
}
#endif
