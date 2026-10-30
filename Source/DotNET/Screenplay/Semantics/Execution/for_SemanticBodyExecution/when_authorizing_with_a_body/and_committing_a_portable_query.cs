// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_authorizing_with_a_body;

public class and_committing_a_portable_query : given.a_policy
{
    scope_factory _factory = null!;

    void Establish()
    {
        Configure(Source.Replace("by name String\n        authorize Permitted", "by name String\n        authorize Member", StringComparison.Ordinal));
        Request = Request with { Queries = QueryRequest().Queries };
        _factory = new(() => new policy_provider((_, _) => SemanticPolicyBodyResult.Allow()));
    }
    void Because() => Result = new SemanticEvaluator(_factory, Bodies).Decide(Plan, SemanticWorld.Empty, Request).Commit(SemanticWorld.Empty);

    [Fact] void should_accept() => Result.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_not_open_a_commit_scope() => _factory.Providers.Count.ShouldEqual(1);
    [Fact] void should_dispose_the_decision_scope_once() => _factory.Providers.Single().Disposals.ShouldEqual(1);

    sealed class scope_factory(Func<policy_provider> create) : ISemanticBodyExecutionProviderFactory
    {
        internal readonly List<policy_provider> Providers = [];
        public ValueTask<ISemanticBodyExecutionProvider> CreateAsync(SemanticApplication application, IReadOnlyDictionary<string, SemanticImplementationBody> bodies, CancellationToken cancellationToken)
        {
            var provider = create();
            Providers.Add(provider);
            return new(provider);
        }
    }
}
