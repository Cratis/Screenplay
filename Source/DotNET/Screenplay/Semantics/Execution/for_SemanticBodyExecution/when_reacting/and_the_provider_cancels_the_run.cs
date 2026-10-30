// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_reacting;

public class and_the_provider_cancels_the_run : given.a_reaction_effect
{
    CancellationTokenSource _source;
    OperationCanceledException _original;
    Exception _failure;

    void Establish()
    {
        _source = new();
        _original = new(_source.Token);
        Provider = new((_, _) =>
        {
            _source.Cancel();
            throw _original;
        });
    }
    void Because() => _failure = Catch.Exception(() => Fire(new SemanticCancellationEvaluator(new SemanticEvaluator(Provider, Bodies), _source.Token)));
    void Destroy() => _source.Dispose();
    [Fact] void should_propagate_the_original_cancellation() => ReferenceEquals(_failure, _original).ShouldBeTrue();
}
