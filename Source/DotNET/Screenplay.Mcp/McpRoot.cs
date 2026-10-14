// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

/// <summary>
/// The MCP server's model root: the shared <see cref="ModelRootDirectory"/> rules, refusing with <see cref="McpFailure"/>.
/// </summary>
sealed class McpRoot
{
    internal const int MaximumFiles = ModelRootDirectory.MaximumFiles;
    internal const int MaximumBytes = ModelRootDirectory.MaximumBytes;
    internal const int MaximumFileBytes = ModelRootDirectory.MaximumFileBytes;

    // missingLevels bounds how many trailing folders may not exist yet. A path a host or user names may only miss its
    // last folder, so a mistyped path never creates a tree; the server's own Documents/Screenplay fallback may miss two.
    internal McpRoot(string path, int missingLevels = 1) => Directory = McpRootFailures.Run(() => new ModelRootDirectory(path, missingLevels));

    internal ModelRootDirectory Directory { get; }

    internal string ApplicationName => Directory.ApplicationName;

    // Rebinding must not dereference a previous root that has been removed or made unreadable.
    internal bool Exists => Directory.Exists;

    internal string DirectoryPath => McpRootFailures.Run(() => Directory.DirectoryPath);

    internal static void CheckDocuments(ImmutableArray<WorkspaceDocument> documents, bool allowEmpty = false) =>
        McpRootFailures.Run(() => ModelRootDirectory.CheckDocuments(documents, allowEmpty));

    internal static void CheckAncestors(string path) => McpRootFailures.Run(() => ModelRootDirectory.CheckAncestors(path));

    internal bool SamePath(McpRoot other) => Directory.SamePath(other.Directory);

    internal void Create() => McpRootFailures.Run(Directory.Create);

    internal ImmutableArray<WorkspaceDocument> Read(bool allowEmpty = false) => McpRootFailures.Run(() => Directory.Read(allowEmpty));

    internal string PathFor(PortablePlayPath path, bool createParents = false) => McpRootFailures.Run(() => Directory.PathFor(path, createParents));

    internal void Verify(ScreenplayWorkspace workspace) => McpRootFailures.Run(() => Directory.Verify(workspace));
}
