// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_requesting_executable_validation : given.an_authoring_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Because() => _result = Workspace.ProposeAuthoring(Authoring(
        new AddWorkspaceNode(RegistrationRoot.Handle, RegistrationRoot.Node, "imports", new ImportSyntax("External.Unused", SourceLocation.Start))) with
    {
        Validation = WorkspaceAuthoringValidation.Executable
    });

    [Fact] void should_reject_the_unsupported_backend_construct() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_return_no_writable_candidate() => _result.Workspace.ShouldBeNull();
    [Fact] void should_return_no_write_plan() => _result.WritePlan.ShouldBeNull();
    [Fact] void should_report_backend_errors() => _result.ExecutableDiagnostics.ShouldNotBeEmpty();
}
