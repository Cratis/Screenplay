// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_validating_with_code;

public class and_the_provider_reports_a_diagnostic : given.a_code_validation
{
    void Establish() => Answer = SemanticValidationBodyResult.Unsupported("\n\0" + new string('x', 600));
    void Because() => Result = Execute();

    [Fact] void should_fail_closed() => Result.ShouldBeOfExactType<SemanticUnsupported>();
    [Fact] void should_strip_control_characters() => ((SemanticUnsupported)Result).Details.Any(char.IsControl).ShouldBeFalse();
    [Fact] void should_limit_the_diagnostic() => ((SemanticUnsupported)Result).Details.ShouldEqual($"Rule body (requirement {Bodies.Single().Requirement.RequirementId}): {new string('x', 500)}");
}
