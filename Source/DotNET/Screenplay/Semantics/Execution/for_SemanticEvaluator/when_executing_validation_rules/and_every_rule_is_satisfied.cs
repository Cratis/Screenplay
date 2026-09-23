// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_executing_validation_rules;

public class and_every_rule_is_satisfied : given.a_validated_command_plan
{
    SemanticExecutionResult _result;

    void Because() => _result = Execute();

    [Fact] void should_compile_a_plan() => _compilation.Success.ShouldBeTrue();
    [Fact] void should_accept_the_command() => _result.ShouldBeOfExactType<SemanticAccepted>();
}
