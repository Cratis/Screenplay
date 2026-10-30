// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_handling_a_command;

public class and_committing_a_factory_decision : given.a_command_handler
{
    handler_factory _factory = null!;
    SemanticExecutionResult _first = null!;
    SemanticExecutionResult _second = null!;

    void Establish()
    {
        Configure(Source.Replace("  projection Orders => Order\n        from Placed\n          key name\n          name = name", "  reducer Orders => Order\n        on Placed\n          ```csharp\n          return context.State;\n          ```", StringComparison.Ordinal));
        _factory = new(() => new handler_provider((_, _) => Answer)
        {
            Reduce = context => SemanticReducerBodyResult.Next(SemanticValue.Composite([
                new(Plan.Model.Application.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.ReadModels).Single().Properties.Single().Id, context.Key)]))
        });
    }
    void Because()
    {
        var decision = new SemanticEvaluator(_factory, Bodies).Decide(Plan, SemanticWorld.Empty, Request);
        _first = decision.Commit(SemanticWorld.Empty);
        _second = decision.Commit(SemanticWorld.Empty);
    }

    [Fact] void should_accept_both_commits() => new[] { _first, _second }.All(result => result is SemanticAccepted).ShouldBeTrue();
    [Fact] void should_create_fresh_commit_scopes() => _factory.Providers.Count.ShouldEqual(3);
    [Fact] void should_run_the_handler_once_at_decision_not_again_at_commit() => _factory.Providers.Select(provider => provider.HandleCalls).ShouldEqual(new[] { 1, 0, 0 });
    [Fact] void should_dispose_every_scope_once() => _factory.Providers.All(provider => provider.Disposals == 1).ShouldBeTrue();
    [Fact] void should_keep_facts_equivalent() => _first.World.Facts.Select(fact => fact.EventContract).ShouldEqual(_second.World.Facts.Select(fact => fact.EventContract));

    [Fact]
    void should_dispose_an_abandoned_decision()
    {
        var factory = new handler_factory(() => new handler_provider((_, _) => Answer));
        _ = new SemanticEvaluator(factory, Bodies).Decide(Plan, SemanticWorld.Empty, Request);
        factory.Providers.Single().Disposals.ShouldEqual(1);
    }

    [Fact]
    void should_not_commit_any_facts_or_state_when_a_reducer_refuses()
    {
        var factory = new handler_factory(() => new handler_provider((_, _) => Answer)
        {
            Reduce = _ => SemanticReducerBodyResult.Unsupported("unavailable")
        });
        var decision = new SemanticEvaluator(factory, Bodies).Decide(Plan, SemanticWorld.Empty, Request);
        var result = (SemanticUnsupported)decision.Commit(SemanticWorld.Empty);
        ReferenceEquals(result.World, SemanticWorld.Empty).ShouldBeTrue();
        result.World.Facts.ShouldBeEmpty();
        result.World.ReadModels.ShouldBeEmpty();
        factory.Providers.All(provider => provider.Disposals == 1).ShouldBeTrue();
    }

    [Fact]
    void should_capture_cancellation_for_fresh_commit_scopes()
    {
        using var source = new CancellationTokenSource();
        var factory = new handler_factory(() => new handler_provider((_, _) => Answer));
        var decision = new SemanticEvaluator(factory, Bodies).DecideWithCancellation(Plan, SemanticWorld.Empty, Request, source.Token);
        source.Cancel();
        decision.Commit(SemanticWorld.Empty).ShouldBeOfExactType<SemanticUnsupported>();
        factory.Tokens.ShouldEqual([source.Token, source.Token]);
        factory.Providers.All(provider => provider.Disposals == 1).ShouldBeTrue();
    }

    sealed class handler_factory(Func<handler_provider> create) : ISemanticBodyExecutionProviderFactory
    {
        internal readonly List<handler_provider> Providers = [];
        internal readonly List<CancellationToken> Tokens = [];
        public ValueTask<ISemanticBodyExecutionProvider> CreateAsync(SemanticApplication application, IReadOnlyDictionary<string, SemanticImplementationBody> bodies, CancellationToken cancellationToken)
        {
            Tokens.Add(cancellationToken);
            var provider = create();
            Providers.Add(provider);
            return new(provider);
        }
    }
}
