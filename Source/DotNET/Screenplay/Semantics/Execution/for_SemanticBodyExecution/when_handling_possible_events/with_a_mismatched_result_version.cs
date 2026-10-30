// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_handling_possible_events;

public class with_a_mismatched_result_version : given.a_possible_events_handler
{
    [Theory]
    [InlineData(1u)]
    [InlineData(3u)]
    void should_not_call_a_provider_with_a_mismatched_body(uint version)
    {
        Bodies = [.. Bodies.Select(body => body with { Requirement = body.Requirement with { ResultVersion = version } })];
        var result = Execute();
        ((SemanticUnsupported)result).Capability.ShouldEqual(SemanticExecutionCapability.ImplementationBody);
        Provider.Calls.ShouldEqual(0);
    }

    [Fact]
    void should_not_call_a_provider_for_an_unknown_handler_version()
    {
        ChangeCommand(command => command with { Handler = command.Handler with { ResultVersion = 3 } });
        ((SemanticUnsupported)Execute()).Capability.ShouldEqual(SemanticExecutionCapability.ImplementationBody);
        Provider.Calls.ShouldEqual(0);
    }
}
