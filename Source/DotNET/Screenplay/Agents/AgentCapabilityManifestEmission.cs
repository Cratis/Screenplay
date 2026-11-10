// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Agents;

/// <summary>
/// Reasons a capability manifest cannot be emitted.
/// </summary>
public enum AgentCapabilityManifestRefusal
{
    /// <summary>No refusal.</summary>
    None,

    /// <summary>The complete compilation contains errors.</summary>
    CompilationErrors,

    /// <summary>The application never declared agent exposure.</summary>
    NoAgentsBlock,

    /// <summary>An operation description exceeds the manifest-only limit.</summary>
    DescriptionTooLong
}

/// <summary>
/// The outcome of attempting to emit a capability manifest.
/// </summary>
/// <param name="Manifest">The manifest, or null on refusal.</param>
/// <param name="Refusal">Why emission was refused.</param>
/// <param name="Operation">The canonical operation whose description exceeds the limit, when applicable.</param>
public sealed record AgentCapabilityManifestEmission(AgentCapabilityManifest? Manifest, AgentCapabilityManifestRefusal Refusal, string? Operation = null);
