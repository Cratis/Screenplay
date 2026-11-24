// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalCorpus;

/// <summary>
/// Disposes one frozen protected-read expectation, never its admission or serialization contract.
/// Consumers must exercise the original, match every field and failure, and reject unused dispositions.
/// </summary>
/// <param name="VectorId">The original API-derived vector identity.</param>
/// <param name="SemanticRevision">The frozen model revision.</param>
/// <param name="Sha256">The lowercase SHA-256 of the exact original resource bytes.</param>
/// <param name="ReplacementVectorId">The named runnable replacement.</param>
/// <param name="CaseName">The original specification or race case name.</param>
/// <param name="OriginalMessage">The frozen keyed refusal.</param>
/// <param name="ReplacementMessage">The exact keyless refusal.</param>
/// <param name="CommitIndex">The race commit index, or null for a source specification.</param>
public sealed record ProtectedReadVectorDisposition(
    string VectorId,
    SemanticRevision SemanticRevision,
    string Sha256,
    string ReplacementVectorId,
    string CaseName,
    string OriginalMessage,
    string ReplacementMessage,
    int? CommitIndex = null)
{
    /// <summary>
    /// Gets the complete ordered mismatch that the original must produce under current execution.
    /// </summary>
    public ImmutableArray<string> ExpectedFailures => CommitIndex is null
        ? [$"Expected rejection message '{OriginalMessage}', got '{ReplacementMessage}' (string key: False)."]
        : [$"Expected refusal message '{OriginalMessage}', got '{ReplacementMessage}'."];

    /// <summary>
    /// Gets the correction's rationale and owning decision and issue.
    /// </summary>
    public string Reason => "Decision 0170 / Screenplay#1384 removes runtime keys from protected-read refusals; frozen originals remain unchanged.";
}
