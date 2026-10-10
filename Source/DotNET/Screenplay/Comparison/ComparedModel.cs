// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Comparison;

/// <summary>
/// Declares whether a workspace's identity catalog is authoritative for comparison matching.
/// </summary>
public sealed class ComparedModel
{
    ComparedModel(ScreenplayWorkspace workspace, bool hasPersistedIdentities)
    {
        Workspace = workspace;
        HasPersistedIdentities = hasPersistedIdentities;
    }

    /// <summary>
    /// Gets the immutable authored workspace.
    /// </summary>
    public ScreenplayWorkspace Workspace { get; }

    /// <summary>
    /// Gets whether the caller declares the catalog authoritative for identity continuity.
    /// </summary>
    public bool HasPersistedIdentities { get; }

    /// <summary>
    /// Uses the workspace catalog as authoritative persisted identities.
    /// </summary>
    /// <param name="workspace">The authored workspace with its authoritative catalog.</param>
    /// <returns>The identity-bearing comparison input.</returns>
    public static ComparedModel WithIdentities(ScreenplayWorkspace workspace) => new(workspace, true);

    /// <summary>
    /// Ignores catalog identities for matching, without changing the workspace.
    /// </summary>
    /// <param name="workspace">The workspace to compare by exact addresses.</param>
    /// <returns>The address-matched comparison input.</returns>
    public static ComparedModel WithoutIdentities(ScreenplayWorkspace workspace) => new(workspace, false);

    /// <summary>
    /// Creates a comparison input from in-memory portable .play sources without persisted identities.
    /// </summary>
    /// <param name="applicationName">The friendly application name supplied to compilation.</param>
    /// <param name="sources">Portable document paths mapped to complete source text.</param>
    /// <returns>The workspace, including diagnostics for invalid-but-editable source, marked without persisted identities.</returns>
    /// <exception cref="InvalidScreenplayWorkspace">The workspace name or document set is invalid.</exception>
    /// <exception cref="InvalidPortablePlayPath">A source path is not portable.</exception>
    /// <exception cref="InvalidWorkspaceDocument">A source document is invalid.</exception>
    /// <exception cref="InvalidSemanticContract">The application identity is invalid.</exception>
    public static ComparedModel FromSources(string applicationName, IReadOnlyDictionary<string, string> sources)
    {
        var application = ApplicationIdentity.Create(applicationName);
        var workspace = sources.Count == 0 ? ScreenplayWorkspace.CreateEmpty(application, applicationName)
            : ScreenplayWorkspace.Create(
                application,
                applicationName,
                [.. sources.OrderBy(source => source.Key, StringComparer.Ordinal).Select(source => WorkspaceDocument.Create(source.Key, PortablePlayPath.Parse(source.Key), Encoding.UTF8.GetBytes(source.Value)))],
                SemanticIdentityCatalog.Empty(application));

        return WithoutIdentities(workspace);
    }

    /// <summary>
    /// Reads a canonical workspace export whose identity catalog is authoritative.
    /// </summary>
    /// <param name="json">The complete canonical UTF-8 workspace snapshot; callers must bound its size.</param>
    /// <returns>The identity-bearing restored workspace.</returns>
    /// <exception cref="InvalidScreenplayWorkspace">The export is malformed, noncanonical or inconsistent.</exception>
    /// <exception cref="InvalidSemanticContract">The exported catalog is invalid.</exception>
    public static ComparedModel FromWorkspaceExport(ReadOnlySpan<byte> json) => WithIdentities(ScreenplayWorkspaceSerializer.Deserialize(json));
}
