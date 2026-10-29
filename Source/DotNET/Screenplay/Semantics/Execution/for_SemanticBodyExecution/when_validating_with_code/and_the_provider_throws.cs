// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_validating_with_code;

public class and_the_provider_throws : given.a_code_validation
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_refuse_exceptions_including_cancellation(bool cancelled)
    {
        Provider.Failure = cancelled ? new OperationCanceledException("private") : new InvalidOperationException("private");
        var result = (SemanticUnsupported)Execute(TestContext.Current.CancellationToken);
        result.Details.ShouldContain($"Provider threw {Provider.Failure.GetType().Name}.");
        result.Details.ShouldContain(Bodies.Single().Requirement.RequirementId);
        result.Details.Contains("private", StringComparison.Ordinal).ShouldBeFalse();
    }
}
