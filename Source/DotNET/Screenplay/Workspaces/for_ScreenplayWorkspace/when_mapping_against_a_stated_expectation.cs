// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_mapping_against_a_stated_expectation : given.a_workspace_with_a_produced_mapping
{
    WorkspaceTransactionResult _result = null!;

    void Establish() => CreateWorkspace(Source.Replace("          displayName = \"Same\"", "          displayName = \"New\"", StringComparison.Ordinal));

    void Because() => _result = _workspace.Propose(Request(_operation));

    [Fact] void should_compile_the_original_expectation() => _workspace.Compilation.Success.ShouldBeTrue();
    [Fact] void should_not_rewrite_the_expectation_to_make_it_pass() => _result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.CompilationFailed);
    [Fact] void should_report_the_contradicted_outcome() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnreachableSpecificationOutcome).ShouldBeTrue();
    [Fact] void should_offer_no_candidate() => _result.Workspace.ShouldBeNull();
}
#endif
