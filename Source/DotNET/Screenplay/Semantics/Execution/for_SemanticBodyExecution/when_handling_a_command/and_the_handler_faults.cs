// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_handling_a_command;

public class and_the_handler_faults : given.a_command_handler
{
    [Theory]
    [InlineData(SemanticCommandBodyFaultKind.Faulted, false)]
    [InlineData(SemanticCommandBodyFaultKind.Faulted, true)]
    [InlineData(SemanticCommandBodyFaultKind.TimedOut, false)]
    [InlineData(SemanticCommandBodyFaultKind.TimedOut, true)]
    [InlineData(SemanticCommandBodyFaultKind.MemoryExceeded, false)]
    [InlineData(SemanticCommandBodyFaultKind.MemoryExceeded, true)]
    [InlineData(SemanticCommandBodyFaultKind.Cancelled, false)]
    [InlineData(SemanticCommandBodyFaultKind.Cancelled, true)]
    void should_preserve_typed_faults_and_dispose_without_committing(SemanticCommandBodyFaultKind kind, bool asynchronous)
    {
        var original = new InvalidOperationException("host failure");
        var failure = new SemanticCommandBodyFaultException(Bodies.Single().Requirement.RequirementId, kind, original);
        var provider = new fault_provider(_ => Fail(failure, asynchronous));
        var evaluator = new SemanticEvaluator(new fault_factory(provider), Bodies);
        var fault = Catch.Exception(() => evaluator.Execute(Plan, SemanticWorld.Empty, Request));
        ReferenceEquals(fault, failure).ShouldBeTrue();
        ReferenceEquals(failure.InnerException, original).ShouldBeTrue();
        failure.RequirementId.ShouldEqual(Bodies.Single().Requirement.RequirementId);
        failure.Kind.ShouldEqual(kind);
        provider.Disposals.ShouldEqual(1);
        SemanticWorld.Empty.Facts.ShouldBeEmpty();
        SemanticWorld.Empty.ReadModels.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    void should_wrap_untyped_faults_not_refuse_them(bool cancelled, bool asynchronous)
    {
        Exception original = cancelled ? new OperationCanceledException("host cancelled") : new InvalidOperationException("host failed");
        var provider = new fault_provider(_ => Fail(original, asynchronous));
        var evaluator = new SemanticEvaluator(new fault_factory(provider), Bodies);
        var exception = Catch.Exception(() => evaluator.Execute(Plan, SemanticWorld.Empty, Request));
        exception.ShouldBeOfExactType<SemanticCommandBodyFaultException>();
        var fault = (SemanticCommandBodyFaultException)exception!;
        fault.Kind.ShouldEqual(cancelled ? SemanticCommandBodyFaultKind.Cancelled : SemanticCommandBodyFaultKind.Faulted);
        fault.RequirementId.ShouldEqual(Bodies.Single().Requirement.RequirementId);
        ReferenceEquals(fault.InnerException, original).ShouldBeTrue();
        provider.Disposals.ShouldEqual(1);
        SemanticWorld.Empty.Facts.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_preserve_requested_cancellation_and_its_original_token(bool asynchronous)
    {
        using var source = new CancellationTokenSource();
        using var originalSource = new CancellationTokenSource();
        var original = new OperationCanceledException("host cancelled", originalSource.Token);
        var provider = new fault_provider(token =>
        {
            token.ShouldEqual(source.Token);
            source.Cancel();
            return Fail(original, asynchronous);
        });
        var evaluator = new SemanticEvaluator(new fault_factory(provider), Bodies);
        var fault = Catch.Exception(() => evaluator.ExecuteWithCancellation(Plan, SemanticWorld.Empty, Request, source.Token));
        ReferenceEquals(fault, original).ShouldBeTrue();
        ((OperationCanceledException)fault!).CancellationToken.ShouldEqual(originalSource.Token);
        provider.Disposals.ShouldEqual(1);
        SemanticWorld.Empty.Facts.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_check_the_run_token_outside_the_fault_wrapper(bool before)
    {
        using var source = new CancellationTokenSource();
        var calls = 0;
        var provider = new fault_provider(token =>
        {
            calls++;
            token.ShouldEqual(source.Token);
            source.Cancel();
            return new(Answer);
        });
        if (before) source.Cancel();
        var evaluator = new SemanticEvaluator(new fault_factory(provider), Bodies);
        var exception = Catch.Exception(() => evaluator.ExecuteWithCancellation(Plan, SemanticWorld.Empty, Request, source.Token));
        exception.ShouldBeOfExactType<OperationCanceledException>();
        var fault = (OperationCanceledException)exception!;
        fault.CancellationToken.ShouldEqual(source.Token);
        calls.ShouldEqual(before ? 0 : 1);
        provider.Disposals.ShouldEqual(1);
        SemanticWorld.Empty.Facts.ShouldBeEmpty();
    }

    static ValueTask<SemanticCommandBodyResult> Fail(Exception failure, bool asynchronous)
    {
        if (!asynchronous) throw failure;
        return FailAsync(failure);
    }

    static async ValueTask<SemanticCommandBodyResult> FailAsync(Exception failure)
    {
        await Task.Yield();
        throw failure;
    }

    sealed class fault_provider(Func<CancellationToken, ValueTask<SemanticCommandBodyResult>> handle) : ISemanticBodyExecutionProvider
    {
        internal int Disposals { get; private set; }
        public ValueTask<SemanticBodyExecutionResult> EvaluateAsync(SemanticImplementationBody body, SemanticRuleContext context, CancellationToken cancellationToken) => new(new SemanticBodyExecutionResult(true));
        public ValueTask<SemanticCommandBodyResult> HandleAsync(SemanticImplementationBody body, SemanticCommandContext context, CancellationToken cancellationToken) => handle(cancellationToken);
        public ValueTask DisposeAsync()
        {
            Disposals++;
            return ValueTask.CompletedTask;
        }
    }

    sealed class fault_factory(fault_provider provider) : ISemanticBodyExecutionProviderFactory
    {
        public ValueTask<ISemanticBodyExecutionProvider> CreateAsync(SemanticApplication application, IReadOnlyDictionary<string, SemanticImplementationBody> bodies, CancellationToken cancellationToken) => new(provider);
    }
}
