// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison;

/// <summary>The direction of a required token scope change.</summary>
public enum AgentExposureScopeChange
{
    /// <summary>No scope change.</summary>
    None,

    /// <summary>The required scope changes from write to read.</summary>
    Widened,

    /// <summary>The required scope changes from read to write.</summary>
    Narrowed
}
