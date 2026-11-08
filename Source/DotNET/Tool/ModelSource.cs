// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Mcp;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Tool;

// Opens a model the way the MCP server does, so every command sees the same semantic identities.
// A folder is a model root: its .screenplay/identities.json is used when present, and a source edit that would
// change a persisted identity is refused rather than silently re-derived. A single file has no model root, so its
// identities are derived from addresses.
static class ModelSource
{
    // tolerateStaleIdentities: a specification run never depends on identity state, so when the persisted identities no
    // longer match the sources it runs with address-derived identities instead of refusing.
    internal static ModelSourceLoad Load(string target, bool tolerateStaleIdentities = false)
    {
        var path = Path.GetFullPath(target);
        if (Directory.Exists(path))
        {
            path = RealPath(path);
            try
            {
                var (workspace, persisted) = new McpWorkspaces(new McpRoot(path)).ReadComparisonPath(path);
                return new(workspace, persisted);
            }
            catch (McpFailure) when (tolerateStaleIdentities)
            {
                return FromCompiler(path, path);
            }
        }

        return FromCompiler(path, Path.GetDirectoryName(path)!);
    }

    // The user names the folder, so a link above it (macOS /tmp, for example) is followed before the model root
    // opens; links inside the root are still refused by the root itself.
    internal static string RealPath(string path)
    {
        var full = Path.GetFullPath(path);
        var root = Path.GetPathRoot(full)!;
        var current = root;
        foreach (var part in full[root.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, part);
            if (new DirectoryInfo(current).ResolveLinkTarget(true) is { } target) current = RealPath(target.FullName);
        }

        return current;
    }

    internal static string? Refuse(string target)
    {
        var isFile = File.Exists(target);
        return (!isFile && !Directory.Exists(target)) || (isFile && !target.EndsWith(".play", StringComparison.OrdinalIgnoreCase))
            ? $"'{target}' must be an existing .play file or directory."
            : null;
    }

    internal static bool IsReadFailure(Exception exception) => exception is McpFailure or IOException or UnauthorizedAccessException or
        InvalidWorkspaceDocument or InvalidScreenplayWorkspace or InvalidSemanticContract or InvalidPortablePlayPath;

    static ModelSourceLoad FromCompiler(string path, string directory)
    {
        var snapshot = McpSnapshot.Compile(path, File.Exists(path));

        // The compiler owns file/import discovery. Only its selected source set enters the workspace.
        var documents = McpTestDocuments.From(snapshot, directory, out var rootDirectory);
        var name = new DirectoryInfo(rootDirectory).Name;
        var identity = ApplicationIdentity.Create(name);
        var created = documents.Length == 0
            ? ScreenplayWorkspace.CreateEmpty(identity, name)
            : ScreenplayWorkspace.Create(identity, name, documents, SemanticIdentityCatalog.Empty(identity));
        return new(created, false);
    }
}

sealed record ModelSourceLoad(ScreenplayWorkspace Workspace, bool HasPersistedIdentities);
