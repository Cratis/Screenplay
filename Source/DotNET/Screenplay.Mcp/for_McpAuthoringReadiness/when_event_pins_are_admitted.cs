// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpAuthoringReadiness;

public class when_event_pins_are_admitted : Specification
{
    [Fact]
    void should_have_no_pending_admission_for_a_pinned_rename()
    {
        var syntax = new ScreenplayCompiler().Compile("module M\n  feature F\n    slice StateChange S\n      event Renamed\n        id \"Original\"\n").Value!;
        var readiness = new McpAuthoringReadiness(syntax);
        readiness.ModelSyntaxOnly.ShouldBeFalse();
        readiness.ModelExecutionReadiness.ShouldBeNull();
    }
}
