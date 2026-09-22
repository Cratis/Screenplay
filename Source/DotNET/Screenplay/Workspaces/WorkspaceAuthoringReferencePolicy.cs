// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Controls admission of new unresolved typed references independently from executable readiness.
/// Neither policy permits an existing resolved occurrence to silently change its target.
/// </summary>
public enum WorkspaceAuthoringReferencePolicy
{
    /// <summary>
    /// Reject new unresolved or ambiguous typed references; retain only proven unchanged reference debt.
    /// </summary>
    Safe,

    /// <summary>
    /// Admit new unresolved reference debt with explicit diagnostics, but never silently retarget existing bindings.
    /// </summary>
    Draft
}
