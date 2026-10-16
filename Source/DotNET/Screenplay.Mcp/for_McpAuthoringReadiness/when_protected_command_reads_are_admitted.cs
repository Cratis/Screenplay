// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpAuthoringReadiness;

public class when_protected_command_reads_are_admitted : Specification
{
    [Fact]
    void should_not_label_admitted_reads_and_read_paths_as_syntax_only()
    {
        var application = new ScreenplayCompiler().Parse("command reads protected\n" + for_McpAuthoringWorkflow.when_protecting_command_reads.Source).Value!;
        var readiness = new McpAuthoringReadiness(application);
        readiness.ModelSyntaxOnly.ShouldBeFalse();
        readiness.ModelExecutionReadiness.ShouldBeNull();
        readiness.SyntaxOnly(application.Modules.Single().Features.Single().Slices.First().Commands.Single()).ShouldBeFalse();
    }
}
