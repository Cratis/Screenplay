// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Identifies a host-provided reducer transition's outcome.
/// </summary>
public enum SemanticReducerBodyOutcome
{
    /// <summary>
    /// The transition could not be executed.
    /// </summary>
    Unsupported = 0,

    /// <summary>
    /// The transition supplies the next state.
    /// </summary>
    State,

    /// <summary>
    /// The transition deletes the instance.
    /// </summary>
    Delete
}
