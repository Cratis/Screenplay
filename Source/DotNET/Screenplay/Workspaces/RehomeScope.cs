// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Defines how much of a workspace's model a re-homed document set must contain.
/// </summary>
public enum RehomeScope
{
    /// <summary>
    /// The documents contain the whole model: every semantic and event address must still be present.
    /// </summary>
    Whole = 0,

    /// <summary>
    /// The documents contain a self-contained part of the model, such as the features a host has generated so far.
    /// Assignments for declarations the documents do not contain are dropped.
    /// </summary>
    Subset = 1
}
