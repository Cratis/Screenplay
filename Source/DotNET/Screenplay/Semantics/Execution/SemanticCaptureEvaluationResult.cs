// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Represents the first terminal capture disposition, with accepted prefix facts already submitted.
/// </summary>
/// <param name="Unsupported">The unavailable capture value capability, or null.</param>
/// <param name="Failure">The rejected or unsupported append or reaction result, or null.</param>
internal sealed record SemanticCaptureEvaluationResult(string? Unsupported, SemanticExecutionResult? Failure);
