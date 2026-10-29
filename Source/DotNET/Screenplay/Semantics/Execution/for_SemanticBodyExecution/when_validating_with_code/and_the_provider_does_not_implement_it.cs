// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_validating_with_code;

public class and_the_provider_does_not_implement_it : given.a_rule
{
    void Establish()
    {
        Configure(given.a_code_validation.Source);
        Bodies = [new(Requirement, given.a_code_validation.Body)];
    }
    void Because() => Result = new SemanticEvaluator(Provider, Bodies).Execute(Plan, SemanticWorld.Empty, Request);

    [Fact] void should_remain_unsupported() => Result.ShouldBeOfExactType<SemanticUnsupported>();
    [Fact] void should_name_the_kind() => ((SemanticUnsupported)Result).Details.ShouldContain("Code validation");
    [Fact] void should_name_the_member() => ((SemanticUnsupported)Result).Details.ShouldContain(Requirement.Member!);
    [Fact] void should_name_the_requirement() => ((SemanticUnsupported)Result).Details.ShouldContain(Requirement.RequirementId);
}
