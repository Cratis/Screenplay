// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

sealed class SemanticCancellationEvaluator(ISemanticEvaluator evaluator, CancellationToken cancellationToken) : ISemanticEvaluator
{
    internal ISemanticEvaluator Evaluator => evaluator;
    internal CancellationToken CancellationToken => cancellationToken;

    public SemanticExecutionResult Execute(SemanticExecutionPlan plan, SemanticWorld world, SemanticExecutionRequest request) =>
        evaluator.ExecuteWithCancellation(plan, world, request, cancellationToken);
}
