// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_proposing_no_changes_to_an_empty_workspace : Specification
{
    ScreenplayWorkspace _workspace;
    WorkspaceTransactionResult _result;

    void Establish() => _workspace = ScreenplayWorkspace.CreateEmpty(ApplicationIdentity.Create("Example"), "Example");

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
