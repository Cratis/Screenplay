// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_command_productions;

public class and_destinations_must_be_explicit : given.a_command_production
{
    [Theory]
    [InlineData("        produces Elsewhere\n          for otherId\n")]
    [InlineData("        produces Elsewhere\n")]
    void should_not_guess_routing_without_an_executable_baseline(string sibling)
    {
        Create("module Projects\n  feature Naming\n    slice StateChange Rename\n      command Rename\n        projectId Uuid identifier\n        otherId Uuid\n        produces event Renamed\n          name String = \"something\"\n" + sibling + "      event Elsewhere\n");
        var index = WorkspaceSyntaxIndex.Create(Workspace);
        index.Diagnostics.Any(value => value.Severity == DiagnosticSeverity.Error).ShouldBeFalse();
        index.RepairableDiagnostics.Any(value => value.Code == DiagnosticCodes.ExplicitProducesTargetsRequired).ShouldBeTrue();
        Workspace.Compilation.Value.ShouldBeNull();
        HasRepair(DiagnosticCodes.ExplicitProducesTargetsRequired).ShouldBeFalse();
    }
}
