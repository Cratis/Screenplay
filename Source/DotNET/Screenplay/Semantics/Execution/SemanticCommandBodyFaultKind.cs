// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Distinguishes execution faults after a command handler body has been admitted.
/// </summary>
public enum SemanticCommandBodyFaultKind
{
    /// <summary>The admitted handler failed during execution.</summary>
    Faulted,

    /// <summary>The host's execution time limit was exceeded.</summary>
    TimedOut,

    /// <summary>The host's execution memory limit was exceeded.</summary>
    MemoryExceeded,

    /// <summary>The handler was cancelled independently of the supplied run token.</summary>
    Cancelled
}
