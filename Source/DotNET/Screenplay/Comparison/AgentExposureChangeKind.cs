// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison;

/// <summary>Classifies an explicitly exposed operation without claiming runtime equivalence.</summary>
public enum AgentExposureChangeKind
{
    /// <summary>An operation becomes exposed.</summary>
    Added,

    /// <summary>An operation is no longer exposed.</summary>
    Removed,

    /// <summary>The scope is higher, tokens are loosened, or the operation contract changes.</summary>
    Widened,

    /// <summary>Only the scope is lower.</summary>
    Narrowed,

    /// <summary>The target and exposure contract are unchanged; description changes are metadata.</summary>
    Unchanged,

    /// <summary>Parsing or validation errors prevent a safe conclusion; executable-binding refusal alone does not.</summary>
    Unknown
}
