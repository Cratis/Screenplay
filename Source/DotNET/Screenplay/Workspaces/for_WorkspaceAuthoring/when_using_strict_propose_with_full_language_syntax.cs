// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_using_strict_propose_with_full_language_syntax : given.an_authoring_workspace
{
    WorkspaceTransactionResult _result = null!;

    void Because() => _result = Workspace.Propose(Request(new ReplaceWorkspaceDocument
    {
        Document = Registration.Id,
        Bytes = Bytes($"import External.Unused\n{RegistrationSource}")
    }));

    [Fact] void should_keep_the_existing_strict_contract() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_a_compilation_conflict() => _result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.CompilationFailed);
}
