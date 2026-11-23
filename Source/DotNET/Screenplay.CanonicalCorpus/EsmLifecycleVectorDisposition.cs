// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalCorpus;

/// <summary>
/// Supersedes one frozen vector's lifecycle execution expectation, never its serialization or admission contract.
/// Consumers must match every field and every failure exactly, and reject unused dispositions.
/// </summary>
/// <param name="VectorId">The API-derived identity, for example <c>EsmGoldenVectors.V36</c>.</param>
/// <param name="SemanticVersion">The original ESM version.</param>
/// <param name="SemanticRevision">The frozen original semantic revision.</param>
/// <param name="Sha256">The lowercase SHA-256 of the exact original bytes.</param>
/// <param name="ReplacementVectorId">The API-derived identity of the runnable replacement.</param>
/// <param name="OwnerKind">The lifecycle owner kind: <c>state machine</c> or <c>workflow</c>.</param>
/// <param name="Owner">The state machine or workflow name returned by the lifecycle runner.</param>
/// <param name="Specification">The exact specification name returned by the lifecycle runner.</param>
/// <param name="ExpectedFailures">The complete ordered failure messages that the original must still produce.</param>
/// <param name="Reason">The correction's rationale and owning issue.</param>
public sealed record EsmLifecycleVectorDisposition(
    string VectorId,
    SemanticVersion SemanticVersion,
    SemanticRevision SemanticRevision,
    string Sha256,
    string ReplacementVectorId,
    string OwnerKind,
    string Owner,
    string Specification,
    ImmutableArray<string> ExpectedFailures,
    string Reason);
