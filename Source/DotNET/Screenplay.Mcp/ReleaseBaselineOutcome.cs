// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

/// <summary>
/// The root-aware release baseline check or journaled admission outcome.
/// </summary>
/// <param name="ExitCode">0 for clean or admitted, 1 for violations, 2 for unreadable or refused.</param>
/// <param name="Violations">The changed or missing admitted revisions.</param>
/// <param name="FailureKind">The refusal discriminator, or null.</param>
/// <param name="Reason">The complete display-ready failure message, or null.</param>
/// <param name="AdmittedEntryCount">The total entries in the successfully written baseline, or null.</param>
/// <param name="BaselineRevision">The new baseline revision after successful admission, or null.</param>
public sealed record ReleaseBaselineOutcome(int ExitCode, ImmutableArray<ReleaseBaselineViolation> Violations, string? FailureKind = null, string? Reason = null, int? AdmittedEntryCount = null, string? BaselineRevision = null);
