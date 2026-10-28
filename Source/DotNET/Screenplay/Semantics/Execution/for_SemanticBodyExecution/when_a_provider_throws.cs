// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution;

public class when_a_provider_throws : given.a_rule
{
    void Establish() => Provider.Evaluate(Arg.Any<SemanticImplementationBody>(), Arg.Any<SemanticRuleContext>()).Returns(_ => throw new InvalidOperationException("not returned"));
    void Because() => Result = new SemanticEvaluator(Provider, Bodies).Execute(Plan, SemanticWorld.Empty, Request);

    [Fact] void should_be_unsupported_not_a_crash() => Result.ShouldBeOfExactType<SemanticUnsupported>();
    [Fact] void should_identify_the_failed_requirement() => ((SemanticUnsupported)Result).Details.ShouldContain(Requirement.RequirementId);
    [Fact] void should_describe_the_failure_without_exception_payloads() => ((SemanticUnsupported)Result).Details.ShouldContain("Provider threw InvalidOperationException.");
}
