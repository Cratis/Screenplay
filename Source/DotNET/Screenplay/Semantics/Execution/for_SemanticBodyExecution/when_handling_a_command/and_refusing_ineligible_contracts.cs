// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_handling_a_command;

public class and_refusing_ineligible_contracts : given.a_command_handler
{
    [Theory]
    [InlineData("pending")]
    [InlineData("empty")]
    [InlineData("conditional")]
    [InlineData("result-v2")]
    void should_refuse_before_invoking_the_body(string shape)
    {
        Contexts.Clear();
        Configure(shape == "pending"
            ? Source.Replace("          ```csharp\n          return payloads;\n          ```", "          implementation\n            hint \"Return the declared payloads\"", StringComparison.Ordinal)
            : Source);
        if (shape == "empty") ChangeCommand(command => command with { Produces = [] });
        if (shape == "conditional") ChangeCommand(command => command with { Produces = [command.Produces[0] with { Condition = SemanticExpression.FromValue(SemanticValue.Boolean(true)) }] });
        if (shape == "result-v2") ChangeCommand(command => command with { Handler = command.Handler! with { ResultVersion = 2 } });
        Execute().ShouldBeOfExactType<SemanticUnsupported>();
        Contexts.ShouldBeEmpty();
    }
}
