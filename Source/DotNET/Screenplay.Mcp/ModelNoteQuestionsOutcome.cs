// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

/// <summary>
/// Open questions read from a model root, without changing source or sidecars.
/// </summary>
/// <param name="Findings">The selected open questions, including unscoped orphans.</param>
/// <param name="IdentityStatus">persisted, absent, or unavailable when identity loading failed.</param>
/// <param name="NotesStatus">ok, absent, unreadable, or null when reading could not reach the notes.</param>
/// <param name="EffectiveScope">The requested scope, or null when identities are absent.</param>
/// <param name="OrphanReason">identity-state-absent for unscoped findings without identities, otherwise null.</param>
/// <param name="FailureKind">UnreadableNotes or ReadFailure, or null on a readable result.</param>
/// <param name="Reason">The unreadable or read-failure details, or null.</param>
public sealed record ModelNoteQuestionsOutcome(ImmutableArray<ModelNotesFinding> Findings, string IdentityStatus, string? NotesStatus, string? EffectiveScope, string? OrphanReason = null, string? FailureKind = null, string? Reason = null)
{
    /// <summary>
    /// Computes the report or fail-mode exit code independently of compiler warnings.
    /// </summary>
    /// <param name="failOnOpenQuestions">True to gate on the reported questions; false to report only.</param>
    /// <returns>0 for a readable report, 1 for questions in fail mode, or 2 when unreadable.</returns>
    public int GetExitCode(bool failOnOpenQuestions = false)
    {
        if (FailureKind is not null) return 2;

        return failOnOpenQuestions && !Findings.IsEmpty ? 1 : 0;
    }
}
