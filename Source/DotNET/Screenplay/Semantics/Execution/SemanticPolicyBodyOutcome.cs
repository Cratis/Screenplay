// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Identifies a policy provider's answer; the default is never permission or denial.
/// </summary>
public enum SemanticPolicyBodyOutcome
{
    /// <summary>
    /// No evaluable answer was supplied.
    /// </summary>
    Unsupported = 0,

    /// <summary>
    /// The policy allows the artifact.
    /// </summary>
    Allow,

    /// <summary>
    /// The policy denies the artifact.
    /// </summary>
    Deny
}
