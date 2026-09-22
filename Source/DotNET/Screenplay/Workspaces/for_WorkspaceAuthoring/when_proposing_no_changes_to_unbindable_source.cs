// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_proposing_no_changes_to_unbindable_source : Specification
{
    ScreenplayWorkspace _workspace;
    WorkspaceTransactionResult _result;

    void Establish()
    {
        var document = WorkspaceDocument.Create("source", PortablePlayPath.Parse("application.play"), Encoding.UTF8.GetBytes("import External.Contract"));
        _workspace = ScreenplayWorkspace.Create("Example", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Example")));
    }

    void Because() => _result = _workspace.Propose(new()
    {
        ExpectedRevision = _workspace.Revision,
        ExpectedCatalogRevision = _workspace.IdentityCatalog.Revision
    });

    [Fact] void should_not_report_executable_success() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_a_compilation_conflict() => _result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.CompilationFailed);
    [Fact] void should_expose_no_candidate() => _result.Workspace.ShouldBeNull();
    [Fact] void should_expose_no_write_plan() => _result.WritePlan.ShouldBeNull();
    [Fact] void should_retain_the_readiness_diagnostics() => _result.Diagnostics.ShouldNotBeEmpty();
}
