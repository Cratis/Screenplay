// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison;

/// <summary>The explicit allowlist change, not an effective authorization verdict.</summary>
public enum AgentExposureChangeKind
{
    /// <summary>An operation newly enters the allowlist.</summary>
    Added,

    /// <summary>An operation leaves the allowlist.</summary>
    Removed,

    /// <summary>A retained operation's required token scope changes.</summary>
    ScopeChanged,

    /// <summary>A retained operation's human description changes.</summary>
    DescriptionChanged
}
