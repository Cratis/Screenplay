// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp;

// A configured root that holds no model, such as a project's code folder chosen at startup, may give way to a new
// model folder beside or inside it when the user asks for one by name.
internal sealed partial class McpWorkspaces
{
    static bool AdmitsModelFolder(McpRoot configured, McpRoot requested)
    {
        var configuredPath = configured.DirectoryPath;
        var requestedPath = requested.DirectoryPath;
        var parent = Path.GetDirectoryName(configuredPath);
        var container = Path.GetDirectoryName(requestedPath);
        if (parent is null || container is null || SameLexicalPath(requestedPath, parent) ||
            !(SameLexicalPath(container, parent) || SameLexicalPath(container, configuredPath)))
        {
            return false;
        }

        if (configured.Exists && (LooksLikeScreenplayRoot(configuredPath) || StateAt(configuredPath).HasState))
        {
            return false;
        }

        var folder = new DirectoryInfo(requestedPath);
        if (folder.LinkTarget is not null || File.Exists(requestedPath))
        {
            return false;
        }

        return !folder.Exists || !folder.EnumerateFileSystemInfos().Any() ||
            LooksLikeScreenplayRoot(requestedPath) || StateAt(requestedPath).HasState;
    }

    static bool SameLexicalPath(string first, string second) =>
        string.Equals(Path.TrimEndingDirectorySeparator(first), Path.TrimEndingDirectorySeparator(second), StringComparison.Ordinal);
}
