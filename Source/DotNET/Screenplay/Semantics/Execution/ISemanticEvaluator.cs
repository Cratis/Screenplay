// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Executes capability-admitted semantic plans deterministically in memory.
/// </summary>
public interface ISemanticEvaluator
{
    /// <summary>
    /// Executes one command and its requested post-success queries atomically.
    /// </summary>
    /// <param name="plan">The capability-admitted execution plan.</param>
    /// <param name="world">The immutable world before execution.</param>
    /// <param name="request">The command values, deterministic identity inputs, and requested queries.</param>
    /// <returns>The normalized accepted, rejected, conflict, or unsupported result.</returns>
    SemanticExecutionResult Execute(SemanticExecutionPlan plan, SemanticWorld world, SemanticExecutionRequest request);

    /// <summary>Executes a request with host implementation cancellation.</summary>
    /// <param name="plan">The compiled plan.</param>
    /// <param name="world">The starting world.</param>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">Cancellation for host execution. Command handlers propagate requested cancellation and typed execution faults; other provider roles retain their Unsupported mapping.</param>
    /// <returns>The normalized result.</returns>
    SemanticExecutionResult ExecuteWithCancellation(SemanticExecutionPlan plan, SemanticWorld world, SemanticExecutionRequest request, CancellationToken cancellationToken) => Execute(plan, world, request);
}
