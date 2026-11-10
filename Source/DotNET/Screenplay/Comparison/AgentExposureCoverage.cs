// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison;

/// <summary>States whether the entire authored exposure allowlist was compared.</summary>
public enum AgentExposureCoverage
{
    /// <summary>Every exposure row has a known structural classification.</summary>
    Complete,

    /// <summary>Parsing or validation errors prohibit a safe no-widening conclusion, even with no rows.</summary>
    Incomplete
}
