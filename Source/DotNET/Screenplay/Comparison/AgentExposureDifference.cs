// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison;

/// <summary>Compares explicit allowlist membership and metadata independently of structural coverage.</summary>
/// <param name="Matching">The identity or canonical address matching mode.</param>
/// <param name="Complete">Whether both complete semantic compilations are available.</param>
/// <param name="HasExposureChange">True for a reported change, false for known unchanged exposure, null when unavailable.</param>
/// <param name="BeforeAvailability">The baseline's availability and bounded reason.</param>
/// <param name="AfterAvailability">The candidate's availability and bounded reason.</param>
/// <param name="Changes">Deterministically ordered individual findings; empty when either side is unavailable.</param>
/// <param name="NotCompared">What this comparison does not establish.</param>
public sealed record AgentExposureDifference(
    DeclarationMatching Matching,
    bool Complete,
    bool? HasExposureChange,
    AgentExposureAvailability BeforeAvailability,
    AgentExposureAvailability AfterAvailability,
    IReadOnlyList<AgentExposureChange> Changes,
    IReadOnlyList<string> NotCompared)
{
    /// <summary>Gets an unknown result for consumers constructing a structural result without exposure analysis.</summary>
    public static AgentExposureDifference Unavailable { get; } = new(DeclarationMatching.Address, false, null, AgentExposureAvailability.CompilationFailed, AgentExposureAvailability.CompilationFailed, [], []);
}
