// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_examining_unused_routed_examples
{
    [Fact]
    void should_report_the_examples_own_route_as_unadmitted()
    {
        var application = new ScreenplayCompiler().Compile("eventsource A\n  identifier String\n  stream S\nmodule M\n  feature F\n    slice StateChange S\n      event E\n      example Unused : E\n        stream A.S").Value!;
        var example = application.Modules.Single().Features.Single().Slices.Single().Examples.Single();
        var readiness = new McpAuthoringReadiness(application);
        readiness.SyntaxOnly(example).ShouldBeTrue();
        readiness.ExecutionReadiness(example)!.ShouldContain("PLAY0268");
        readiness.ExecutionReadiness(example)!.ShouldContain("specification event routes (#457)");
    }
}
