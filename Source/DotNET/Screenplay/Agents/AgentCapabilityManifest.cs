// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Agents;

/// <summary>
/// The versioned, explicit agent allowlist of a successful complete compilation.
/// </summary>
/// <param name="Contract">The manifest contract version, independent of the executable model version.</param>
/// <param name="TokenKind">The admitted token kind.</param>
/// <param name="Operations">The explicitly exposed operations, in canonical order.</param>
public sealed record AgentCapabilityManifest(string Contract, string TokenKind, IReadOnlyList<AgentCapabilityManifestOperation> Operations)
{
    /// <summary>
    /// The supported manifest contract.
    /// </summary>
    public const string Version = "screenplay-capability-manifest/1";

    /// <summary>
    /// The manifest-only description limit, measured in Unicode code points.
    /// </summary>
    public const int MaximumDescriptionLength = 1024;

    // The approve directive is refused until #1166.
    const bool ApprovalRequired = false;

    /// <summary>
    /// Emits only the explicit allowlist of a successful complete compilation.
    /// </summary>
    /// <param name="compilation">The complete source and semantic compilation result.</param>
    /// <returns>The manifest, or a refusal with no partial allowlist.</returns>
    public static AgentCapabilityManifestEmission From(CompilationResult<SemanticCompilation> compilation)
    {
        if (!compilation.Success) return new(null, AgentCapabilityManifestRefusal.CompilationErrors);
        if (AgentCapabilities.From(compilation) is not { } capabilities) return new(null, AgentCapabilityManifestRefusal.NoAgentsBlock);
        var oversized = capabilities.Operations.FirstOrDefault(operation => operation.Description.EnumerateRunes().Count() > MaximumDescriptionLength);
        if (oversized is not null) return new(null, AgentCapabilityManifestRefusal.DescriptionTooLong, oversized.QualifiedName);
        var operations = capabilities.Operations.Select(Project)
            .OrderBy(operation => operation.Operation, StringComparer.Ordinal)
            .ThenBy(operation => operation.Kind, StringComparer.Ordinal).ToArray();
        var tokenKind = capabilities.TokenKind switch
        {
            AgentTokenKind.Personal => "personal",
            _ => throw new ArgumentOutOfRangeException(nameof(compilation), "Unknown agent token kind.")
        };
        return new(new(Version, tokenKind, operations), AgentCapabilityManifestRefusal.None);
    }

    static AgentCapabilityManifestOperation Project(AgentCapability capability) => new(
        capability.QualifiedName,
        capability.Path[0],
        [.. capability.Path.Skip(1).SkipLast(1)],
        capability.Path[^1],
        capability.Name,
        capability.Kind switch
        {
            AgentOperationKind.Query => "query",
            AgentOperationKind.Command => "command",
            _ => throw new ArgumentOutOfRangeException(nameof(capability), "Unknown agent operation kind.")
        },
        capability.Scope switch
        {
            AgentScope.Read => "read",
            AgentScope.Write => "write",
            _ => throw new ArgumentOutOfRangeException(nameof(capability), "Unknown agent scope.")
        },
        capability.Description,
        ApprovalRequired);
}
