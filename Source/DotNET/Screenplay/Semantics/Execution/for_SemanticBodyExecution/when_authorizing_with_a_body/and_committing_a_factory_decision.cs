// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_authorizing_with_a_body;

public class and_committing_a_factory_decision : given.a_policy
{
    policy_factory _factory = null!;
    SemanticDecision _decision = null!;
    SemanticExecutionResult _first = null!;
    SemanticExecutionResult _second = null!;

    void Establish()
    {
        Request = Request with { Queries = QueryRequest().Queries };
        _factory = new();
    }
    void Because()
    {
        _decision = new SemanticEvaluator(_factory, Bodies).Decide(Plan, SemanticWorld.Empty, Request);
        _first = _decision.Commit(SemanticWorld.Empty);
        _second = _decision.Commit(SemanticWorld.Empty);
    }

    [Fact] void should_allow_both_commits() => new[] { _first, _second }.All(result => result is SemanticAccepted).ShouldBeTrue();
    [Fact] void should_not_call_the_disposed_decision_provider() => _factory.Providers.Select(provider => provider.Calls).ShouldEqual(new[] { 1, 1, 1 });
    [Fact] void should_dispose_each_scope_once() => _factory.Providers.Select(provider => provider.Disposals).ShouldEqual(new[] { 1, 1, 1 });

    [Fact]
    void should_dispose_an_abandoned_decision_scope()
    {
        var factory = new policy_factory();
        _ = new SemanticEvaluator(factory, Bodies).Decide(Plan, SemanticWorld.Empty, Request);
        factory.Providers.Single().Disposals.ShouldEqual(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_refuse_factory_failure_at_commit_without_committing_facts(bool returnNull)
    {
        var factory = new policy_factory { FailCommits = true, ReturnNull = returnNull };
        var decision = new SemanticEvaluator(factory, Bodies).Decide(Plan, SemanticWorld.Empty, Request);
        var result = (SemanticUnsupported)decision.Commit(SemanticWorld.Empty);
        result.Capability.ShouldEqual(SemanticExecutionCapability.Authorization);
        result.Details.ShouldNotContain("secret");
        ReferenceEquals(result.World, SemanticWorld.Empty).ShouldBeTrue();
        factory.Providers.Single().Disposals.ShouldEqual(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_preserve_the_world_when_the_commit_query_denies_or_is_unsupported(bool unsupported)
    {
        var factory = new policy_factory { CommitAnswer = unsupported ? SemanticPolicyBodyResult.Unsupported("not evaluable") : SemanticPolicyBodyResult.Deny() };
        var decision = new SemanticEvaluator(factory, Bodies).Decide(Plan, SemanticWorld.Empty, Request);
        var result = decision.Commit(SemanticWorld.Empty);
        if (unsupported) ((SemanticUnsupported)result).Capability.ShouldEqual(SemanticExecutionCapability.Authorization);
        else ((SemanticRejected)result).Category.ShouldEqual(SemanticRejectionCategory.Unauthorized);
        ReferenceEquals(result.World, SemanticWorld.Empty).ShouldBeTrue();
        factory.Providers.All(provider => provider.Disposals == 1).ShouldBeTrue();
    }

    [Fact]
    void should_capture_cancellation_for_each_commit_scope()
    {
        using var source = new CancellationTokenSource();
        var factory = new policy_factory();
        var decision = new SemanticEvaluator(factory, Bodies).DecideWithCancellation(Plan, SemanticWorld.Empty, Request, source.Token);
        source.Cancel();
        decision.Commit(SemanticWorld.Empty).ShouldBeOfExactType<SemanticUnsupported>();
        factory.Tokens.ShouldEqual(new[] { source.Token, source.Token });
        factory.Providers.All(provider => provider.Disposals == 1).ShouldBeTrue();
    }

    sealed class policy_factory : ISemanticBodyExecutionProviderFactory
    {
        internal readonly List<scoped_policy> Providers = [];
        internal readonly List<CancellationToken> Tokens = [];
        internal bool FailCommits;
        internal bool ReturnNull;
        internal SemanticPolicyBodyResult CommitAnswer = SemanticPolicyBodyResult.Allow();
        public ValueTask<ISemanticBodyExecutionProvider> CreateAsync(SemanticApplication application, IReadOnlyDictionary<string, SemanticImplementationBody> bodies, CancellationToken cancellationToken)
        {
            bodies.Count.ShouldEqual(1);
            Tokens.Add(cancellationToken);
            if (FailCommits && Providers.Count != 0)
            {
                if (ReturnNull) return new((ISemanticBodyExecutionProvider)null!);
                throw new InvalidOperationException("secret");
            }
            var provider = new scoped_policy(Providers.Count == 0 ? SemanticPolicyBodyResult.Allow() : CommitAnswer);
            Providers.Add(provider);
            return new(provider);
        }
    }

    sealed class scoped_policy(SemanticPolicyBodyResult answer) : ISemanticBodyExecutionProvider
    {
        internal int Calls;
        internal int Disposals;
        public ValueTask<SemanticBodyExecutionResult> EvaluateAsync(SemanticImplementationBody body, SemanticRuleContext context, CancellationToken cancellationToken) => throw new InvalidOperationException();
        public ValueTask<SemanticPolicyBodyResult> AuthorizeAsync(SemanticImplementationBody body, SemanticPolicyContext context, CancellationToken cancellationToken)
        {
            if (Disposals != 0) throw new InvalidOperationException("Disposed provider.");
            Calls++;
            return new(answer);
        }
        public ValueTask DisposeAsync()
        {
            Disposals++;
            return ValueTask.CompletedTask;
        }
    }
}
