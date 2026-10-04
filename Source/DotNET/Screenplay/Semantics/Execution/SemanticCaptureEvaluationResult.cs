// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Represents evaluated capture facts or a reached value the reference evaluator cannot execute.
/// </summary>
/// <param name="Facts">The facts, empty when unsupported.</param>
/// <param name="Unsupported">The unavailable value capability, or null on success.</param>
internal sealed record SemanticCaptureEvaluationResult(ImmutableArray<SemanticFact> Facts, string? Unsupported);
