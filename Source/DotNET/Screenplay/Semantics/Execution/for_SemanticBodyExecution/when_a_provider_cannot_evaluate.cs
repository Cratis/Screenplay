// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution;

public class when_a_provider_cannot_evaluate : given.a_rule
{
    void Establish() => Answer = new(null, "Compilation refused: unsupported language construct");
    void Because() => Result = new SemanticEvaluator(Provider, Bodies).Execute(Plan, SemanticWorld.Empty, Request);

    [Fact] void should_be_unsupported() => Result.ShouldBeOfExactType<SemanticUnsupported>();
    [Fact] void should_identify_the_requirement() => ((SemanticUnsupported)Result).Details.ShouldContain(Requirement.RequirementId);
    [Fact] void should_surface_the_diagnostic() => ((SemanticUnsupported)Result).Details.ShouldContain(Answer.Diagnostic!);
}
