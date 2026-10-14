// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Validation;

/// <summary>
/// Identifies why an authored gate was refused.
/// </summary>
public enum ValidationGateFailureKind
{
    /// <summary>
    /// The path cannot be safely read as a regular file.
    /// </summary>
    Unreadable,

    /// <summary>
    /// The file does not satisfy the strict v1 contract.
    /// </summary>
    Invalid
}

/// <summary>
/// A refusal, never a missing-file default.
/// </summary>
/// <param name="Kind">The refusal kind.</param>
/// <param name="Path">The gate-file path.</param>
/// <param name="Line">One-based line, or 1 for a filesystem refusal.</param>
/// <param name="Column">One-based column, or 1 for a filesystem refusal.</param>
/// <param name="Reason">The actionable reason.</param>
public sealed record ValidationGateFailure(ValidationGateFailureKind Kind, string Path, long Line, long Column, string Reason);
