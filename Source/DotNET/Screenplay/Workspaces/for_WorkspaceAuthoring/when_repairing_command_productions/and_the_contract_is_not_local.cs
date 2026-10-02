// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_command_productions;

public class and_the_contract_is_not_local : given.a_command_production
{
    [Fact]
    void should_not_duplicate_an_imported_event()
    {
        Create("import External.ProjectRegistered\n" + Source);
        HasRepair(DiagnosticCodes.UnknownEvent).ShouldBeFalse();
    }

    [Fact]
    void should_not_duplicate_a_cross_file_declaration()
    {
        Create(Source, "module Projects\n  feature Other\n    slice StateChange Other\n      event ProjectRegistered\n        name ProjectName\n        registeredAt DateTime\n");
        HasRepair(DiagnosticCodes.UnknownEvent).ShouldBeFalse();
    }

    [Fact]
    void should_not_infer_for_a_producer_in_another_file()
    {
        Create(Source, "module Projects\n  feature Other\n    slice StateChange Other\n      command Other\n        name String\n        produces ProjectRegistered\n          name = name\n");
        HasRepair(DiagnosticCodes.UnknownEvent).ShouldBeFalse();
    }

    [Fact]
    void should_not_infer_for_a_reaction()
    {
        Create("module Projects\n  feature Other\n    slice Automation Other\n      reaction React\n        on Unknown\n        produces ProjectRegistered\n          name = name\n");
        HasRepair(DiagnosticCodes.UnknownEvent).ShouldBeFalse();
    }
}
