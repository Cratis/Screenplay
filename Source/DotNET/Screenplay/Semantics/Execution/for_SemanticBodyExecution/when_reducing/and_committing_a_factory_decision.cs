// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_reducing;

public class and_committing_a_factory_decision : given.a_reducer
{
    readonly List<reducer_provider> _providers = [];
    lifetime_factory _factory = null!;

    void Establish() => _factory = new(() =>
    {
        var provider = new reducer_provider(context =>
        {
            Contexts.Add(context);
            return SemanticReducerBodyResult.Next(State);
        });
        _providers.Add(provider);
        return provider;
    });

    [Fact]
    void should_open_a_fresh_scope_for_each_commit_and_dispose_before_returning()
    {
        Prepare();
        using var cancellation = new CancellationTokenSource();
        var decision = new SemanticEvaluator(_factory, Bodies).DecideWithCancellation(Plan, SemanticWorld.Empty, Request(), cancellation.Token);
        _providers.Count.ShouldEqual(1);
        _providers.Single().Disposals.ShouldEqual(1);
        Contexts.ShouldBeEmpty();
        Accept(decision.Commit(SemanticWorld.Empty));
        _providers.Count.ShouldEqual(2);
        _providers[1].Disposals.ShouldEqual(1);
        var other = Fact() with { Destination = SemanticValue.Text("other"), Context = new(new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), SemanticValue.Text("other"))) };
        var concurrent = SemanticWorld.Create([other], []);
        var committed = Accept(decision.Commit(concurrent));
        committed.World.Facts.Length.ShouldEqual(2);
        committed.World.ReadModels.Single().Key.ShouldEqual(Key);
        Contexts.Select(context => context.SequenceNumber).ShouldEqual([0UL, 1UL]);
        _providers.Count.ShouldEqual(3);
        _providers[2].Disposals.ShouldEqual(1);
        _factory.Tokens.TrueForAll(token => token == cancellation.Token).ShouldBeTrue();
    }

    [Fact]
    void should_not_open_a_reducer_scope_for_an_abandoned_decision()
    {
        Prepare();
        new SemanticEvaluator(_factory, Bodies).Decide(Plan, SemanticWorld.Empty, Request());
        _providers.Count.ShouldEqual(1);
        Contexts.ShouldBeEmpty();
        _providers.Single().Disposals.ShouldEqual(1);
    }

    [Fact]
    void should_refuse_a_commit_factory_failure_without_reusing_the_decide_provider()
    {
        Prepare();
        var decision = new SemanticEvaluator(_factory, Bodies).Decide(Plan, SemanticWorld.Empty, Request());
        _factory.Failure = new InvalidOperationException("secret-factory");
        var result = (SemanticUnsupported)decision.Commit(SemanticWorld.Empty);
        result.Details.ShouldContain("Provider factory threw InvalidOperationException");
        result.Details.ShouldNotContain("secret-factory");
        Contexts.ShouldBeEmpty();
    }

    [Fact]
    void should_not_open_a_commit_scope_without_reducer_work()
    {
        Prepare();
        Configure(Source[..Source.IndexOf("  reducer BalanceReducer", StringComparison.Ordinal)]);
        var decision = new SemanticEvaluator(_factory, Bodies).Decide(Plan, SemanticWorld.Empty, Request());
        Accept(decision.Commit(SemanticWorld.Empty));
        _providers.Count.ShouldEqual(1);
    }

    void Prepare()
    {
        Reset();
        _providers.Clear();
        Establish();
    }

    sealed class lifetime_factory(Func<reducer_provider> create) : ISemanticBodyExecutionProviderFactory
    {
        internal readonly List<CancellationToken> Tokens = [];
        internal Exception? Failure;
        public ValueTask<ISemanticBodyExecutionProvider> CreateAsync(SemanticApplication application, IReadOnlyDictionary<string, SemanticImplementationBody> bodies, CancellationToken cancellationToken)
        {
            Tokens.Add(cancellationToken);
            if (Failure is { } failure) throw failure;
            return new(create());
        }
    }
}
