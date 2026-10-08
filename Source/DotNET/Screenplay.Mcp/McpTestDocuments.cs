// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

[SuppressMessage("Usage", "MA0182", Justification = "Used by the friend CLI assembly to adapt its discovered sources.")]
static class McpTestDocuments
{
    internal static ImmutableArray<WorkspaceDocument> From(McpSnapshot snapshot, string directory, out string rootDirectory)
    {
        var basePath = Path.GetFullPath(directory);
        var paths = snapshot.Sources.Keys.ToDictionary(path => path, path => Path.GetFullPath(Path.Combine(basePath, path)), StringComparer.Ordinal);
        var root = basePath;
        while (paths.Values.Any(path => Path.GetRelativePath(root, path).Replace('\\', '/').StartsWith("../", StringComparison.Ordinal)))
        {
            root = Path.GetDirectoryName(root)!;
        }

        rootDirectory = root;
        var prefix = Path.GetRelativePath(root, basePath).Replace('\\', '/');
        return [.. snapshot.Sources.OrderBy(source => source.Key, StringComparer.Ordinal).Select(source =>
        {
            var relative = NormalizeExtension(Path.GetRelativePath(root, paths[source.Key]).Replace('\\', '/'));
            var lines = source.Value.Split('\n');
            foreach (var discovered in ScreenplayCompiler.DiscoverImports(source.Value, source.Key, snapshot.Languages))
            {
                var pattern = discovered.Import.Pattern;
                var adapted = NormalizeExtension(pattern);
                if (pattern.StartsWith('/') && prefix != ".")
                {
                    adapted = $"/{prefix}/{adapted.TrimStart('/')}";
                }

                var index = discovered.Import.Location.Line - 1;
                lines[index] = lines[index].Replace($"\"{pattern}\"", $"\"{adapted}\"", StringComparison.Ordinal);
            }

            // Use the same path-derived document keys as a disk-backed MCP workspace.
            return WorkspaceDocument.Create(McpDocumentKeys.For(relative), PortablePlayPath.Parse(relative), Encoding.UTF8.GetBytes(string.Join('\n', lines)));
        })];
    }

    static string NormalizeExtension(string path) => path.EndsWith(".play", StringComparison.OrdinalIgnoreCase) ? $"{path[..^5]}.play" : path;
}
