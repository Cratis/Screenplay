// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Creates an isolated provider for one evaluation, specification run, or reducer-bearing commit.
/// </summary>
public interface ISemanticBodyExecutionProviderFactory
{
    /// <summary>
    /// Creates a provider from the resolved application and its implementation bodies.
    /// </summary>
    /// <param name="application">The application including concepts and typed context declarations.</param>
    /// <param name="bodies">Resolved bodies keyed by requirement identity; bodies are not stored on the application.</param>
    /// <param name="cancellationToken">Cancellation for the run.</param>
    /// <returns>A run-owned provider, disposed even after failure or cancellation.</returns>
    /// <remarks>
    /// Creation failures, timeouts and cancellation make opaque bodies Unsupported. A factory-backed decision disposes
    /// its validation provider before returning. Each commit with reducer work requests and disposes a fresh provider
    /// using the decision's captured cancellation token; repeated commits may therefore call this factory again.
    /// No commit provider is requested without reducer work.
    /// </remarks>
    ValueTask<ISemanticBodyExecutionProvider> CreateAsync(SemanticApplication application, IReadOnlyDictionary<string, SemanticImplementationBody> bodies, CancellationToken cancellationToken);
}
