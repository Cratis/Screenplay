// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution;

public class when_running_with_a_provider_factory : given.a_rule
{
    rule_factory _factory = null!;

    void Establish() => _factory = new([Provider]);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    void should_dispose_after_acceptance_and_rejection(bool satisfied)
    {
        Answer = new(satisfied);
        var result = Execute(TestContext.Current.CancellationToken);
        result.Kind.ShouldEqual(satisfied ? SemanticExecutionOutcomeKind.Accepted : SemanticExecutionOutcomeKind.Rejected);
        Provider.Disposals.ShouldEqual(1);
        _factory.Creations.ShouldEqual(1);
        _factory.Application.ShouldEqual(Plan.Model.Application);
        _factory.Bodies![Requirement.RequirementId].ShouldEqual(Bodies.Single());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_dispose_after_provider_failure(bool throwing)
    {
        Answer = new(null, "Unsupported language");
        if (throwing) Provider.Failure = new InvalidOperationException();
        Execute(TestContext.Current.CancellationToken).ShouldBeOfExactType<SemanticUnsupported>();
        Provider.Disposals.ShouldEqual(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_map_cancellation_to_unsupported_and_dispose(bool alreadyCancelled)
    {
        using var cancellation = new CancellationTokenSource();
        Provider.BeforeEvaluation = token =>
        {
            cancellation.Cancel();
            token.ThrowIfCancellationRequested();
        };
        if (alreadyCancelled) cancellation.Cancel();
        Execute(cancellation.Token).ShouldBeOfExactType<SemanticUnsupported>();
        _factory.Cancellation.ShouldEqual(cancellation.Token);
        Provider.Disposals.ShouldEqual(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_map_factory_timeout_or_cancellation_to_unsupported(bool timeout)
    {
        _factory.Failure = timeout ? new TimeoutException() : new OperationCanceledException();
        Execute(TestContext.Current.CancellationToken).ShouldBeOfExactType<SemanticUnsupported>();
        Context.ShouldBeNull();
    }

    [Fact]
    void should_create_a_fresh_provider_for_each_run()
    {
        var next = new rule_provider((_, _) => new(true));
        var factory = new rule_factory([Provider, next]);
        var evaluator = new SemanticEvaluator(factory, Bodies);
        evaluator.ExecuteWithCancellation(Plan, SemanticWorld.Empty, Request, TestContext.Current.CancellationToken);
        evaluator.ExecuteWithCancellation(Plan, SemanticWorld.Empty, Request, TestContext.Current.CancellationToken);
        Provider.Disposals.ShouldEqual(1);
        next.Disposals.ShouldEqual(1);
        factory.Creations.ShouldEqual(2);
    }

    [Fact]
    void should_propagate_specification_runner_cancellation()
    {
        Configure("""
            module Orders
              feature Ordering
                slice StateChange Place
                  command Place
                    name String
                    validate
                      name rule valid
                        ```csharp
                        return context.Value != null;
                        ```
                    returns name
                  specification Valid
                    when Place
                      name = "First"
                    then returns "First"
            """);
        using var cancellation = new CancellationTokenSource();
        Provider.BeforeEvaluation = token =>
        {
            cancellation.Cancel();
            token.ThrowIfCancellationRequested();
        };
        var runner = new SemanticSpecificationRunner(new rule_factory([Provider]), Bodies);
        var run = runner.RunWithCancellation(Plan, Plan.Specifications.Keys.Single(), cancellation.Token);
        run.Execution.ShouldBeOfExactType<SemanticUnsupported>();
        run.Passed.ShouldBeFalse();
        Provider.Cancellation.ShouldEqual(cancellation.Token);
        Provider.Disposals.ShouldEqual(1);
    }

    SemanticExecutionResult Execute(CancellationToken cancellationToken) => new SemanticEvaluator(_factory, Bodies).ExecuteWithCancellation(Plan, SemanticWorld.Empty, Request, cancellationToken);

    sealed class rule_factory(IEnumerable<rule_provider> providers) : ISemanticBodyExecutionProviderFactory
    {
        readonly Queue<rule_provider> _providers = new(providers);

        internal Exception? Failure { get; set; }
        internal int Creations { get; private set; }
        internal SemanticApplication? Application { get; private set; }
        internal IReadOnlyDictionary<string, SemanticImplementationBody>? Bodies { get; private set; }
        internal CancellationToken Cancellation { get; private set; }

        public ValueTask<ISemanticBodyExecutionProvider> CreateAsync(SemanticApplication application, IReadOnlyDictionary<string, SemanticImplementationBody> bodies, CancellationToken cancellationToken)
        {
            Application = application;
            Bodies = bodies;
            Cancellation = cancellationToken;
            Creations++;
            if (Failure is { } failure) throw failure;
            return new(_providers.Dequeue());
        }
    }
}
