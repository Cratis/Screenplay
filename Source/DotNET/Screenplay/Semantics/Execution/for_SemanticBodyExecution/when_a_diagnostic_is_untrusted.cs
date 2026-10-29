// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution;

public class when_a_diagnostic_is_untrusted : given.a_rule
{
    void Establish() => Answer = new(null, "\0\r\n\t\u001b" + new string('x', 600));
    void Because() => Result = new SemanticEvaluator(Provider, Bodies).Execute(Plan, SemanticWorld.Empty, Request);

    [Fact] void should_remain_unsupported() => Result.ShouldBeOfExactType<SemanticUnsupported>();
    [Fact] void should_strip_controls_before_truncating() => ((SemanticUnsupported)Result).Details.ShouldEqual($"Rule body (requirement {Requirement.RequirementId}): {new string('x', 500)}");
}
