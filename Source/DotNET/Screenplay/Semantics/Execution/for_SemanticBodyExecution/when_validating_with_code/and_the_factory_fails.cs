// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_validating_with_code;

public class and_the_factory_fails : given.a_code_validation
{
    void Because() => Result = new SemanticEvaluator(new unavailable_factory(), Bodies).Execute(Plan, SemanticWorld.Empty, Request);

    [Fact] void should_fail_closed() => Result.ShouldBeOfExactType<SemanticUnsupported>();
    [Fact] void should_preserve_the_factory_diagnostic() => ((SemanticUnsupported)Result).Details.ShouldContain("Provider factory threw InvalidOperationException.");
    [Fact] void should_name_the_requirement() => ((SemanticUnsupported)Result).Details.ShouldContain(Bodies.Single().Requirement.RequirementId);

    sealed class unavailable_factory : ISemanticBodyExecutionProviderFactory
    {
        public ValueTask<ISemanticBodyExecutionProvider> CreateAsync(SemanticApplication application, IReadOnlyDictionary<string, SemanticImplementationBody> bodies, CancellationToken cancellationToken) => throw new InvalidOperationException();
    }
}
