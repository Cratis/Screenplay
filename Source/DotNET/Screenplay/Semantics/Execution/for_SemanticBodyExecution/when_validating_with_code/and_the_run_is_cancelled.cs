// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_validating_with_code;

public class and_the_run_is_cancelled : given.a_code_validation
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_refuse_cancellation_before_or_during_execution(bool alreadyCancelled)
    {
        using var cancellation = new CancellationTokenSource();
        Provider.BeforeValidation = _ => cancellation.Cancel();
        if (alreadyCancelled) cancellation.Cancel();
        var result = (SemanticUnsupported)Execute(cancellation.Token);
        result.Details.ShouldContain("OperationCanceledException");
        result.Details.ShouldContain(Bodies.Single().Requirement.RequirementId);
    }
}
