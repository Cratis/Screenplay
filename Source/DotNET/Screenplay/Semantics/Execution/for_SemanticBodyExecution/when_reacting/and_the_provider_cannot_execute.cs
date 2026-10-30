// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_reacting;

public class and_the_provider_cannot_execute : given.a_reaction_effect
{
    [Theory]
    [InlineData("fault")]
    [InlineData("timeout")]
    [InlineData("cancellation")]
    void should_sanitize_provider_faults_without_settling(string kind)
    {
        Provider.Failure = kind switch
        {
            "timeout" => new TimeoutException("secret\nsource"),
            "cancellation" => new OperationCanceledException("secret\nsource"),
            _ => new InvalidOperationException("secret\nsource")
        };
        Fire();
        Failure.ShouldBeOfExactType<SemanticUnsupported>();
        ((SemanticUnsupported)Failure!).Details.ShouldContain(Provider.Failure.GetType().Name);
        ((SemanticUnsupported)Failure!).Details.Contains("secret", StringComparison.Ordinal).ShouldBeFalse();
        Loop.Facts.ShouldBeEmpty();
    }

    [Fact]
    void should_propagate_run_cancellation()
    {
        using var source = new CancellationTokenSource();
        var original = new OperationCanceledException(source.Token);
        Provider = new((_, _) =>
        {
            source.Cancel();
            throw original;
        });
        var failure = Catch.Exception(() => Fire(new SemanticCancellationEvaluator(new SemanticEvaluator(Provider, Bodies), source.Token)));
        ReferenceEquals(failure, original).ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_check_run_cancellation_before_and_after_execution(bool before)
    {
        using var source = new CancellationTokenSource();
        Provider = new((_, _) =>
        {
            source.Cancel();
            return Answer;
        });
        if (before) source.Cancel();
        var failure = Catch.Exception(() => Fire(new SemanticCancellationEvaluator(new SemanticEvaluator(Provider, Bodies), source.Token)));
        failure.ShouldBeOfExactType<OperationCanceledException>();
        ((OperationCanceledException)failure!).CancellationToken.ShouldEqual(source.Token);
        Provider.Calls.ShouldEqual(before ? 0 : 1);
    }

    [Fact]
    async Task should_default_to_explicit_unsupported()
    {
        ISemanticBodyExecutionProvider provider = new old_provider();
        var result = await provider.ReactAsync(Bodies.Single(), new(Cause.EventContract, new(Cause.Values), Cause.Destination, null, null, null, null, null), TestContext.Current.CancellationToken);
        result.Diagnostic!.ShouldContain("Reaction effect body");
        result.Diagnostic.ShouldContain("is not supported by this provider");
        result.Events.IsDefault.ShouldBeTrue();
        result.Commands.IsDefault.ShouldBeTrue();
    }

    sealed class old_provider : ISemanticBodyExecutionProvider
    {
        public ValueTask<SemanticBodyExecutionResult> EvaluateAsync(SemanticImplementationBody body, SemanticRuleContext context, CancellationToken cancellationToken) => new(new SemanticBodyExecutionResult(true));
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
