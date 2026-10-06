// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_requesting_executable_responses_in_a_workspace : Workspaces.for_WorkspaceAuthoring.given.an_authoring_workspace
{
    WorkspaceAuthoringResult _authoring = null!;
    WorkspaceAuthoringResult _executable = null!;

    void Because()
    {
        var entry = Index.Entries.Single(entry => entry.Node is CommandSyntax);
        var command = (CommandSyntax)entry.Node;
        var response = new ScalarCommandResponseSyntax(new("name", command.Location), command.Location);
        var request = Authoring(new ReplaceWorkspaceNode(entry.Handle, command, command with { Response = response }));
        _authoring = Workspace.ProposeAuthoring(request);
        _executable = Workspace.ProposeAuthoring(request with { Validation = WorkspaceAuthoringValidation.Executable });
    }

    [Fact] void should_accept_the_syntax_contract_for_authoring() => _authoring.Accepted.ShouldBeTrue();
    [Fact] void should_refuse_executable_admission() => _executable.Accepted.ShouldBeFalse();
    [Fact] void should_return_no_executable_candidate() => _executable.Workspace.ShouldBeNull();
    [Fact] void should_return_no_executable_write_plan() => _executable.WritePlan.ShouldBeNull();
    [Fact] void should_explain_the_esm_boundary() => _executable.ExecutableDiagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax && diagnostic.Message.Contains("not admitted by any supported executable model (ESM) version yet (#300/#303)", StringComparison.Ordinal)).ShouldBeTrue();
}
