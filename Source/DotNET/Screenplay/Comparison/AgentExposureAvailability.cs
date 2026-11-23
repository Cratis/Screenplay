// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison;

/// <summary>Whether the complete compilation provides an authoritative explicit allowlist.</summary>
public enum AgentExposureAvailability
{
    /// <summary>The complete compilation succeeded; absence of agents is known empty.</summary>
    Available,

    /// <summary>The complete compilation failed; no subset may be treated as an allowlist.</summary>
    CompilationFailed
}
