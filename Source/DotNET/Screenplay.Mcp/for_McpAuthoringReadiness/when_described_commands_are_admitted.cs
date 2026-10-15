// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpAuthoringReadiness;

public class when_described_commands_are_admitted : Specification
{
    [Fact]
    void should_not_label_an_admitted_described_command_or_its_specs_as_syntax_only()
    {
        var application = new ScreenplayCompiler().Parse(for_McpConnection.given.described_commands.Source).Value!;
        var readiness = new McpAuthoringReadiness(application);
        var slice = application.Modules.Single().Features.Single().Slices.Single();
        readiness.ModelSyntaxOnly.ShouldBeFalse();
        readiness.ModelExecutionReadiness.ShouldBeNull();
        readiness.SyntaxOnly(slice.Commands.Single()).ShouldBeFalse();
        slice.Specifications.All(specification => !readiness.SyntaxOnly(specification)).ShouldBeTrue();
    }
}
