// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

/// <summary>
/// The MCP view of <see cref="PersistedIdentityState"/>; format and open rules live in the shared workspace library.
/// </summary>
/// <param name="ApplicationName">The friendly application name.</param>
/// <param name="Catalog">The persisted identity catalog.</param>
/// <param name="Mappings">The document mappings as source-less workspace documents.</param>
sealed record McpState(string ApplicationName, SemanticIdentityCatalog Catalog, ImmutableArray<WorkspaceDocument> Mappings)
{
    internal const string FileName = PersistedIdentityStateSerializer.FileName;

    PersistedIdentityState Persisted => new(
        ApplicationName,
        Catalog,
        [.. Mappings.Select(document => new PersistedDocumentMapping(document.Id, document.StableKey, document.Path))]);

    internal static byte[] Serialize(ScreenplayWorkspace workspace) => Serialize(PersistedIdentityStateSerializer.From(workspace));

    internal static McpState Deserialize(byte[] bytes)
    {
        var state = McpRootFailures.Run(() => ScreenplayModelRoot.ReadIdentities(bytes));
        return new(
            state.ApplicationName,
            state.IdentityCatalog,
            [.. state.Documents.Select(mapping => WorkspaceDocument.Create(mapping.Id, mapping.StableKey, mapping.Path, []))]);
    }

    internal ScreenplayWorkspace Open(McpRoot root) => McpRootFailures.Run(() => PersistedWorkspaceOpener.Open(root.Directory, Persisted));

    // Read-only verification must retain catalog identities even when source removed their declarations.
    // This workspace must never be installed as an authoring state or serialized back to identities.json.
    internal ScreenplayWorkspace OpenForReleaseBaselineCheck(McpRoot root) =>
        McpRootFailures.Run(() => PersistedWorkspaceOpener.OpenForReleaseBaselineCheck(root.Directory, Persisted));

    internal byte[] Serialize() => Serialize(Persisted);

    static byte[] Serialize(PersistedIdentityState state)
    {
        try
        {
            return PersistedIdentityStateSerializer.Serialize(state);
        }
        catch (InvalidPersistedIdentityState exception)
        {
            throw new McpFailure(exception.Message);
        }
    }
}
